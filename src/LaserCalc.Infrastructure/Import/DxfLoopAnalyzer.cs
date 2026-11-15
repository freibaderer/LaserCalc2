using LaserCalc.Domain.Geometry;

namespace LaserCalc.Infrastructure.Import;

/// <summary>Stitches unconnected DXF line/arc primitives into closed cutting loops.</summary>
internal static class DxfLoopAnalyzer
{
    private readonly record struct NodeKey(long X, long Y);
    private sealed record Loop(string Layer, double SignedArea, IReadOnlyList<Point2> Polygon, Point2 ProbePoint);

    public sealed record Result(double? NetAreaMm2, int ClosedLoopCount, int OpenChainCount, int AmbiguousBranchCount, IReadOnlyList<string> Warnings);

    public static Result Analyze(IReadOnlyList<GeometryPrimitive> primitives, double toleranceMm = 0.05)
    {
        var warnings = new List<string>();
        var loops = new List<Loop>();
        var ambiguousBranchCount = 0;
        var circles = primitives.Where(p => p.Type == GeometryPrimitiveType.Circle).ToList();
        foreach (var circle in circles)
        {
            var points = circle.Sample(3.0);
            loops.Add(new Loop(circle.Layer, Math.PI * circle.Radius * circle.Radius, points,
                circle.Center ?? circle.Start));
        }

        foreach (var layerGroup in primitives.Where(p => p.Type is GeometryPrimitiveType.Line or GeometryPrimitiveType.Arc)
                     .GroupBy(p => p.Layer, StringComparer.OrdinalIgnoreCase))
        {
            var edges = layerGroup.ToList();
            var adjacency = new Dictionary<NodeKey, List<int>>();
            for (var i = 0; i < edges.Count; i++)
            {
                Add(Node(edges[i].Start, toleranceMm), i);
                Add(Node(edges[i].End, toleranceMm), i);
            }

            var used = new bool[edges.Count];
            for (var seed = 0; seed < edges.Count; seed++)
            {
                if (used[seed]) continue;
                // Seed the walk with the selected edge. If we start by asking for all
                // unused edges at the seed's start point, a perfectly normal closed
                // contour has two candidates (the seed and its neighbour) and was
                // incorrectly flagged as a branch. Marking the seed first makes the
                // remaining degree at each visited node meaningful: one continuation
                // is normal, two or more continuations are a real ambiguous junction.
                var firstEdge = edges[seed];
                var firstKey = Node(firstEdge.Start, toleranceMm);
                var currentKey = Node(firstEdge.End, toleranceMm);
                var polygon = new List<Point2> { firstEdge.Start };
                var signedArea = firstEdge.SignedAreaContribution();
                var seedSamples = firstEdge.Sample(3.0);
                for (var s = 1; s < seedSamples.Count; s++) polygon.Add(seedSamples[s]);
                used[seed] = true;
                var closed = currentKey == firstKey;
                var steps = 1;

                while (!closed && steps <= edges.Count)
                {
                    var candidates = adjacency.TryGetValue(currentKey, out var incident)
                        ? incident.Where(index => !used[index]).ToList()
                        : new List<int>();
                    if (candidates.Count == 0) break;
                    if (candidates.Count > 1)
                    {
                        ambiguousBranchCount++;
                        warnings.Add($"DXF-Layer '{layerGroup.Key}': Verzweigter Konturpunkt. Konturzuordnung ist mehrdeutig.");
                    }

                    var edgeIndex = candidates[0];
                    var edge = edges[edgeIndex];
                    GeometryPrimitive directed;
                    NodeKey nextKey;
                    if (Node(edge.Start, toleranceMm) == currentKey)
                    {
                        directed = edge;
                        nextKey = Node(edge.End, toleranceMm);
                    }
                    else
                    {
                        directed = edge.Reversed();
                        nextKey = Node(edge.Start, toleranceMm);
                    }

                    used[edgeIndex] = true;
                    signedArea += directed.SignedAreaContribution();
                    var samples = directed.Sample(3.0);
                    for (var s = 1; s < samples.Count; s++) polygon.Add(samples[s]);
                    currentKey = nextKey;
                    steps++;
                    closed = currentKey == firstKey;
                }

                // A branch can also exist exactly at the seed node. The walk stops
                // as soon as it returns there, so inspect unconsumed incident edges
                // once more before accepting the contour as unambiguous.
                if (closed && adjacency.TryGetValue(firstKey, out var startIncident) &&
                    startIncident.Any(index => !used[index]))
                {
                    ambiguousBranchCount++;
                    warnings.Add($"DXF-Layer '{layerGroup.Key}': Verzweigter Konturpunkt am Konturstart. Konturzuordnung ist mehrdeutig.");
                }

                if (closed && polygon.Count >= 4 && Math.Abs(signedArea) > 1e-6)
                    loops.Add(new Loop(layerGroup.Key, signedArea, polygon, polygon[0]));
                else
                {
                    warnings.Add($"DXF-Layer '{layerGroup.Key}': Offene oder nicht eindeutig verbundene Schnittkette erkannt.");
                }
            }

            void Add(NodeKey key, int edgeIndex)
            {
                if (!adjacency.TryGetValue(key, out var list)) adjacency[key] = list = new List<int>();
                list.Add(edgeIndex);
            }
        }

        if (loops.Count == 0)
        {
            warnings.Add("Keine geschlossenen Schnittkonturen gefunden; Nettoteilfläche bleibt offen.");
            return new Result(null, 0, warnings.Count(x => x.Contains("Offene", StringComparison.Ordinal)), ambiguousBranchCount, warnings);
        }

        var area = 0.0;
        for (var i = 0; i < loops.Count; i++)
        {
            var loop = loops[i];
            var layer = loop.Layer.ToUpperInvariant();
            int sign;
            if (layer.Contains("INTERIOR") || layer.Contains("INNER") || layer.Contains("HOLE") || layer.Contains("INNEN"))
                sign = -1;
            else if (layer.Contains("OUTER") || layer.Contains("EXTERIOR") || layer.Contains("AUSSEN"))
                sign = 1;
            else
            {
                var depth = 0;
                for (var j = 0; j < loops.Count; j++)
                {
                    if (i == j || Math.Abs(loops[j].SignedArea) <= Math.Abs(loop.SignedArea)) continue;
                    if (PointInPolygon(loop.ProbePoint, loops[j].Polygon)) depth++;
                }
                sign = depth % 2 == 0 ? 1 : -1;
            }
            area += Math.Abs(loop.SignedArea) * sign;
        }

        if (area <= 0)
        {
            warnings.Add("Ermittelte Nettoteilfläche ist null oder negativ. Bitte DXF-Konturen prüfen.");
            return new Result(null, loops.Count, warnings.Count(x => x.Contains("Offene", StringComparison.Ordinal)), ambiguousBranchCount, warnings);
        }
        return new Result(area, loops.Count, warnings.Count(x => x.Contains("Offene", StringComparison.Ordinal)), ambiguousBranchCount, warnings);
    }

    private static NodeKey Node(Point2 point, double tolerance) =>
        new((long)Math.Round(point.X / tolerance), (long)Math.Round(point.Y / tolerance));

    private static bool PointInPolygon(Point2 point, IReadOnlyList<Point2> polygon)
    {
        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var a = polygon[i];
            var b = polygon[j];
            var intersects = ((a.Y > point.Y) != (b.Y > point.Y)) &&
                (point.X < (b.X - a.X) * (point.Y - a.Y) / ((b.Y - a.Y) == 0 ? double.Epsilon : (b.Y - a.Y)) + a.X);
            if (intersects) inside = !inside;
        }
        return inside;
    }
}
