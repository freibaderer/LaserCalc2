using System.Globalization;
using System.Text;
using LaserCalc.Domain.Geometry;

namespace LaserCalc.Infrastructure.Import;

/// <summary>
/// ASCII DXF reader for the 2D laser-cut entities most commonly delivered by customers.
/// Supports LINE, ARC, CIRCLE, LWPOLYLINE and classic POLYLINE/VERTEX, including bulges.
/// R12 files are supported by the group-code reader. Binary DXF and complex entities
/// (e.g. INSERT/SPLINE/REGION) are reported instead of being silently ignored.
/// </summary>
public sealed class AsciiDxfImporter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private sealed record Pair(int Code, string Value);
    private sealed record Vertex(double X, double Y, double Bulge);
    private sealed record EntityRecord(string Type, List<Pair> Data, List<List<Pair>> Vertices);

    public PartGeometry Import(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("DXF-Datei wurde nicht gefunden.", path);
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 16 && Encoding.ASCII.GetString(bytes, 0, Math.Min(18, bytes.Length)).StartsWith("AutoCAD Binary DXF", StringComparison.Ordinal))
            throw new NotSupportedException("Diese DXF-Datei ist binär. Bitte in TruTops/CAD als ASCII-DXF (bevorzugt R12 oder R2013) exportieren.");
        var text = Encoding.Latin1.GetString(bytes);
        return ImportText(text, Path.GetFileName(path));
    }

    public PartGeometry ImportText(string text, string sourceFile = "unbekannt.dxf")
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var pairs = new List<Pair>();
        for (var i = 0; i + 1 < lines.Length; i += 2)
            if (int.TryParse(lines[i].Trim(), NumberStyles.Integer, Inv, out var code))
                pairs.Add(new Pair(code, lines[i + 1].Trim()));

        var warnings = new List<string>();
        var units = ReadInsUnits(pairs);
        var scale = UnitScaleToMillimeters(units);
        var unitsAssumed = units is null or 0;
        if (unitsAssumed) warnings.Add("DXF enthält keine eindeutige Einheitenangabe. Maße werden vorläufig als Millimeter interpretiert; bitte vor dem Angebot prüfen.");
        else if (scale is null) warnings.Add($"DXF-Einheit {units} wird nicht unterstützt. Maße werden nicht automatisch umgerechnet.");
        if (units.HasValue && units.Value != 0 && scale is null)
            throw new NotSupportedException($"DXF-Einheit {units} wird nicht unterstützt. Bitte die Kundendatei in Millimeter (mm) exportieren.");
        scale ??= 1.0;

        var entities = ReadEntityRecords(pairs, warnings);
        var primitives = new List<GeometryPrimitive>();
        var unknown = 0;
        var pointCount = 0;
        foreach (var entity in entities)
        {
            var layer = GetString(entity.Data, 8) ?? "0";
            if (!IsPlanarEntity(entity))
            {
                unknown++;
                warnings.Add($"DXF-Element {entity.Type} auf Layer '{layer}' ist nicht eindeutig 2D-planar und wird nicht kalkuliert.");
                continue;
            }
            switch (entity.Type)
            {
                case "LINE":
                {
                    var a = GetPoint(entity.Data, 10, 20, scale.Value);
                    var b = GetPoint(entity.Data, 11, 21, scale.Value);
                    if (a is null || b is null) { unknown++; warnings.Add("DXF-LINE ohne vollständige Start-/Endkoordinaten."); break; }
                    primitives.Add(new GeometryPrimitive(GeometryPrimitiveType.Line, a.Value, b.Value, null, 0, 0, layer));
                    break;
                }
                case "ARC":
                {
                    var center = GetPoint(entity.Data, 10, 20, scale.Value);
                    var radius = GetDouble(entity.Data, 40);
                    var startDeg = GetDouble(entity.Data, 50);
                    var endDeg = GetDouble(entity.Data, 51);
                    if (center is null || radius is null || radius <= 0 || startDeg is null || endDeg is null)
                    { unknown++; warnings.Add("DXF-ARC ohne vollständige Mittelpunkt-/Radius-/Winkelwerte."); break; }
                    var a = startDeg.Value * Math.PI / 180.0;
                    var delta = PositiveAngle((endDeg.Value - startDeg.Value) * Math.PI / 180.0);
                    if (delta < 1e-10) delta = 2 * Math.PI;
                    var b = a + delta;
                    var start = new Point2(center.Value.X + radius.Value * scale.Value * Math.Cos(a), center.Value.Y + radius.Value * scale.Value * Math.Sin(a));
                    var end = new Point2(center.Value.X + radius.Value * scale.Value * Math.Cos(b), center.Value.Y + radius.Value * scale.Value * Math.Sin(b));
                    primitives.Add(new GeometryPrimitive(GeometryPrimitiveType.Arc, start, end, center, radius.Value * scale.Value, delta, layer));
                    break;
                }
                case "CIRCLE":
                {
                    var center = GetPoint(entity.Data, 10, 20, scale.Value);
                    var radius = GetDouble(entity.Data, 40);
                    if (center is null || radius is null || radius <= 0) { unknown++; warnings.Add("DXF-CIRCLE ohne gültigen Mittelpunkt/Radius."); break; }
                    var r = radius.Value * scale.Value;
                    var start = new Point2(center.Value.X + r, center.Value.Y);
                    primitives.Add(new GeometryPrimitive(GeometryPrimitiveType.Circle, start, start, center, r, 2 * Math.PI, layer));
                    break;
                }
                case "LWPOLYLINE":
                case "POLYLINE":
                {
                    var vertices = entity.Type == "LWPOLYLINE"
                        ? ReadLightweightVertices(entity.Data, scale.Value)
                        : entity.Vertices.Select(x => ReadVertex(x, scale.Value)).Where(x => x is not null).Select(x => x!).ToList();
                    pointCount += vertices.Count;
                    var flags = GetInt(entity.Data, 70) ?? 0;
                    if (entity.Type == "POLYLINE" && (flags & (8 | 16 | 64)) != 0)
                    {
                        unknown++;
                        warnings.Add($"3D- oder Mesh-POLYLINE auf Layer '{layer}' wird nicht als 2D-Laserschnitt importiert.");
                        break;
                    }
                    var closed = (flags & 1) == 1;
                    if (vertices.Count < 2) { unknown++; warnings.Add($"DXF-{entity.Type} enthält weniger als zwei gültige Eckpunkte."); break; }
                    var segmentCount = closed ? vertices.Count : vertices.Count - 1;
                    for (var v = 0; v < segmentCount; v++)
                    {
                        var next = (v + 1) % vertices.Count;
                        var primitive = FromBulge(vertices[v], vertices[next], layer);
                        if (primitive is not null) primitives.Add(primitive);
                    }
                    if (!closed) warnings.Add($"Offene DXF-{entity.Type}-Polylinie auf Layer '{layer}' erkannt.");
                    break;
                }
                case "SPLINE":
                case "INSERT":
                case "ELLIPSE":
                case "REGION":
                case "3DSOLID":
                case "HATCH":
                    unknown++;
                    warnings.Add($"DXF-Element {entity.Type} auf Layer '{layer}' wird in diesem Entwicklungsstand noch nicht als Schnittgeometrie ausgewertet.");
                    break;
                default:
                    // Text, dimensions, points, tables etc. are intentionally not cutting geometry.
                    break;
            }
        }

        if (primitives.Count == 0) warnings.Add("Keine unterstützten Schnittgeometrien in der DXF-Datei gefunden.");
        var bounds = GeometryBounds.Calculate(primitives);
        var areaResult = DxfLoopAnalyzer.Analyze(primitives);
        warnings.AddRange(areaResult.Warnings);
        if (areaResult.OpenChainCount > 0) warnings.Add("Mindestens eine Kontur ist offen; Flächen- und Einstichwerte können unvollständig sein.");
        if (areaResult.AmbiguousBranchCount > 0) warnings.Add("Mindestens ein verzweigter Konturpunkt ist mehrdeutig. Bitte DXF-Konturen bereinigen.");
        if (unknown > 0) warnings.Add("Unbekannte oder nicht unterstützte Geometrie vorhanden. Keine automatische Preisfreigabe.");

        return new PartGeometry
        {
            SourceFile = sourceFile,
            Format = "Kunden-DXF (ASCII 2D)",
            WidthMm = bounds.Width,
            HeightMm = bounds.Height,
            NetAreaMm2 = areaResult.NetAreaMm2,
            CutLengthMm = primitives.Sum(x => x.LengthMm),
            PointCount = pointCount,
            ContourCount = areaResult.ClosedLoopCount,
            OpenContourCount = areaResult.OpenChainCount + areaResult.AmbiguousBranchCount,
            EstimatedPierceCount = areaResult.ClosedLoopCount,
            UnknownEntityCount = unknown,
            UnitsAssumedMillimeters = unitsAssumed,
            RequiresUnitVerification = unitsAssumed,
            IsSafeForPreliminaryCalculation = unknown == 0 && areaResult.OpenChainCount == 0 && areaResult.AmbiguousBranchCount == 0 && primitives.Count > 0 && bounds.Width > 0 && bounds.Height > 0 && areaResult.NetAreaMm2 is > 0,
            Primitives = primitives.AsReadOnly(),
            Warnings = warnings.Distinct().ToList().AsReadOnly()
        };
    }

    private static bool IsPlanarEntity(EntityRecord entity)
    {
        // LINE endpoints may carry Z coordinates; other 2D entities can carry an extrusion vector.
        if (entity.Type == "LINE")
        {
            var z1 = GetDouble(entity.Data, 30) ?? 0.0;
            var z2 = GetDouble(entity.Data, 31) ?? 0.0;
            return Math.Abs(z1) < 1e-6 && Math.Abs(z2) < 1e-6;
        }
        var nx = GetDouble(entity.Data, 210) ?? 0.0;
        var ny = GetDouble(entity.Data, 220) ?? 0.0;
        var nz = GetDouble(entity.Data, 230) ?? 1.0;
        if (Math.Abs(nx) > 1e-6 || Math.Abs(ny) > 1e-6 || Math.Abs(nz - 1.0) > 1e-6)
            return false;
        if (entity.Type == "LWPOLYLINE" && Math.Abs(GetDouble(entity.Data, 38) ?? 0.0) > 1e-6)
            return false;
        if (entity.Type == "POLYLINE")
        {
            if (Math.Abs(GetDouble(entity.Data, 30) ?? 0.0) > 1e-6)
                return false;
            if (entity.Vertices.Any(vertex => Math.Abs(GetDouble(vertex, 30) ?? 0.0) > 1e-6))
                return false;
        }
        return true;
    }

    private static List<EntityRecord> ReadEntityRecords(List<Pair> pairs, List<string> warnings)
    {
        var start = -1;
        var end = -1;
        for (var i = 0; i + 1 < pairs.Count; i++)
        {
            if (pairs[i].Code == 0 && pairs[i].Value.Equals("SECTION", StringComparison.OrdinalIgnoreCase) &&
                pairs[i + 1].Code == 2 && pairs[i + 1].Value.Equals("ENTITIES", StringComparison.OrdinalIgnoreCase))
            { start = i + 2; break; }
        }
        if (start < 0) { warnings.Add("DXF-ENTITIES-Sektion nicht gefunden."); return new List<EntityRecord>(); }

        var records = new List<EntityRecord>();
        var iPair = start;
        while (iPair < pairs.Count)
        {
            if (pairs[iPair].Code == 0 && pairs[iPair].Value.Equals("ENDSEC", StringComparison.OrdinalIgnoreCase)) break;
            if (pairs[iPair].Code != 0) { iPair++; continue; }
            var type = pairs[iPair].Value.ToUpperInvariant();
            iPair++;
            var data = new List<Pair>();
            while (iPair < pairs.Count && pairs[iPair].Code != 0) data.Add(pairs[iPair++]);

            if (type == "POLYLINE")
            {
                var vertices = new List<List<Pair>>();
                while (iPair < pairs.Count && pairs[iPair].Code == 0 && pairs[iPair].Value.Equals("VERTEX", StringComparison.OrdinalIgnoreCase))
                {
                    iPair++;
                    var vertexData = new List<Pair>();
                    while (iPair < pairs.Count && pairs[iPair].Code != 0) vertexData.Add(pairs[iPair++]);
                    vertices.Add(vertexData);
                }
                if (iPair < pairs.Count && pairs[iPair].Code == 0 && pairs[iPair].Value.Equals("SEQEND", StringComparison.OrdinalIgnoreCase))
                {
                    iPair++;
                    while (iPair < pairs.Count && pairs[iPair].Code != 0) iPair++;
                }
                records.Add(new EntityRecord(type, data, vertices));
            }
            else records.Add(new EntityRecord(type, data, new List<List<Pair>>()));
        }
        return records;
    }

    private static List<Vertex> ReadLightweightVertices(List<Pair> data, double scale)
    {
        var result = new List<Vertex>();
        double? x = null;
        double? y = null;
        double bulge = 0;
        foreach (var pair in data)
        {
            if (pair.Code == 10)
            {
                if (x.HasValue && y.HasValue) result.Add(new Vertex(x.Value * scale, y.Value * scale, bulge));
                x = ParseDouble(pair.Value); y = null; bulge = 0;
            }
            else if (pair.Code == 20) y = ParseDouble(pair.Value);
            else if (pair.Code == 42) bulge = ParseDouble(pair.Value) ?? 0;
        }
        if (x.HasValue && y.HasValue) result.Add(new Vertex(x.Value * scale, y.Value * scale, bulge));
        return result;
    }

    private static Vertex? ReadVertex(List<Pair> data, double scale)
    {
        var x = GetDouble(data, 10); var y = GetDouble(data, 20);
        if (x is null || y is null) return null;
        return new Vertex(x.Value * scale, y.Value * scale, GetDouble(data, 42) ?? 0);
    }

    private static GeometryPrimitive? FromBulge(Vertex start, Vertex end, string layer)
    {
        var a = new Point2(start.X, start.Y);
        var b = new Point2(end.X, end.Y);
        var bulge = start.Bulge;
        if (Math.Abs(bulge) < 1e-10)
            return new GeometryPrimitive(GeometryPrimitiveType.Line, a, b, null, 0, 0, layer);

        var chord = a.DistanceTo(b);
        if (chord < 1e-10) return null;
        var sweep = 4.0 * Math.Atan(bulge);
        var radius = chord * (1.0 + bulge * bulge) / (4.0 * Math.Abs(bulge));
        var offset = chord * (1.0 - bulge * bulge) / (4.0 * bulge);
        var mid = new Point2((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0);
        var leftX = -(b.Y - a.Y) / chord;
        var leftY = (b.X - a.X) / chord;
        var center = new Point2(mid.X + leftX * offset, mid.Y + leftY * offset);
        return new GeometryPrimitive(GeometryPrimitiveType.Arc, a, b, center, radius, sweep, layer);
    }

    private static Point2? GetPoint(List<Pair> data, int xCode, int yCode, double scale)
    {
        var x = GetDouble(data, xCode); var y = GetDouble(data, yCode);
        return x is null || y is null ? null : new Point2(x.Value * scale, y.Value * scale);
    }

    private static double? GetDouble(List<Pair> data, int code)
    {
        var pair = data.FirstOrDefault(x => x.Code == code);
        return pair is null ? null : ParseDouble(pair.Value);
    }

    private static int? GetInt(List<Pair> data, int code)
    {
        var pair = data.FirstOrDefault(x => x.Code == code);
        return pair is not null && int.TryParse(pair.Value, NumberStyles.Integer, Inv, out var value) ? value : null;
    }

    private static string? GetString(List<Pair> data, int code) => data.FirstOrDefault(x => x.Code == code)?.Value;
    private static double? ParseDouble(string value) => double.TryParse(value, NumberStyles.Float, Inv, out var v) ? v : null;

    private static int? ReadInsUnits(List<Pair> pairs)
    {
        for (var i = 0; i + 2 < pairs.Count; i++)
            if (pairs[i].Code == 9 && pairs[i].Value.Equals("$INSUNITS", StringComparison.OrdinalIgnoreCase))
                for (var j = i + 1; j < Math.Min(i + 5, pairs.Count); j++)
                    if (pairs[j].Code == 70 && int.TryParse(pairs[j].Value, out var code)) return code;
        return null;
    }

    private static double? UnitScaleToMillimeters(int? code) => code switch
    {
        1 => 25.4,                 // inches
        2 => 304.8,                // feet
        3 => 1_609_344.0,          // miles
        4 => 1.0,                  // millimeters
        5 => 10.0,                 // centimeters
        6 => 1_000.0,              // meters
        7 => 1_000_000.0,          // kilometers
        8 => 0.0000254,            // microinches
        9 => 0.0254,               // mils
        10 => 914.4,               // yards
        11 => 0.0000001,           // angstroms
        12 => 0.000001,            // nanometers
        13 => 0.001,               // microns
        14 => 100.0,               // decimeters
        15 => 10_000.0,            // dekameters
        16 => 100_000.0,           // hectometers
        17 => 1_000_000.0,         // gigameters
        18 => 149_597_870_700_000.0, // astronomical units
        19 => 9.460730472e18,      // light years
        20 => 3.085677581491367e19, // parsecs
        21 => 304.8006096012192,   // US survey feet
        22 => 25.4000508001016,    // US survey inches
        23 => 914.401828804,       // US survey yards
        24 => 1_609_347.218694,    // US survey miles
        0 or null => null,
        _ => null
    };

    private static double PositiveAngle(double value)
    {
        var twoPi = 2 * Math.PI;
        value %= twoPi;
        return value < 0 ? value + twoPi : value;
    }
}
