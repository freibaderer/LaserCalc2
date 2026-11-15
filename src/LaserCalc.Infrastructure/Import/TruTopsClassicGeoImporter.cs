using System.Globalization;
using System.Text;
using LaserCalc.Domain.Geometry;

namespace LaserCalc.Infrastructure.Import;

/// <summary>
/// Reader for the supplied TRUMPF TruTops Classic GEO text files.
/// It is based on verified sample structures and preserves warnings for any gap.
/// </summary>
public sealed class TruTopsClassicGeoImporter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public PartGeometry Import(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("GEO-Datei wurde nicht gefunden.", path);
        return ImportText(File.ReadAllText(path, Encoding.Latin1), Path.GetFileName(path));
    }

    public PartGeometry ImportText(string text, string sourceFile = "unbekannt.GEO")
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        if (lines.Length < 12 || lines[0].Trim() != "#~1")
            throw new InvalidDataException("Kein unterstützter TruTops-Classic-GEO-Kopf (#~1).");

        var warnings = new List<string>();
        var points = ReadPoints(lines, warnings);
        var primitives = new List<GeometryPrimitive>();
        var contourAreas = new List<(int Role, double ReportedArea, double GeometryArea)>();
        var contourCount = 0;
        var openContourCount = 0;
        var unknownCount = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() != "#~33") continue;
            var end = FindMarker(lines, i + 1, "#~KONT_END");
            if (end < 0)
            {
                warnings.Add($"Konturblock ab Zeile {i + 1} ist nicht mit #~KONT_END abgeschlossen.");
                break;
            }

            var headerIndex = i + 1;
            while (headerIndex < end && string.IsNullOrWhiteSpace(lines[headerIndex])) headerIndex++;
            var header = ParseIntegers(lines.ElementAtOrDefault(headerIndex) ?? "");
            var contourId = header.Count > 0 ? header[0] : contourCount + 1;
            var role = header.Count > 2 ? header[2] : 0;
            var areaLineIndex = headerIndex + 6;
            var contourArea = areaLineIndex < end ? ParseDouble(lines[areaLineIndex]) : null;
            if (!contourArea.HasValue) warnings.Add($"Flächenwert für Kontur {contourId} fehlt oder ist ungültig.");

            var primitiveStart = FindMarker(lines, headerIndex + 1, "#~331");
            var contourPrimitives = new List<GeometryPrimitive>();
            if (primitiveStart >= 0 && primitiveStart < end)
            {
                var p = primitiveStart + 1;
                while (p < end)
                {
                    var token = lines[p].Trim();
                    if (token is "LIN" or "ARC" or "CIR")
                    {
                        var parsed = ParsePrimitive(token, lines, p, end, points, contourId, role, warnings);
                        if (parsed.Primitive is not null)
                        {
                            primitives.Add(parsed.Primitive);
                            contourPrimitives.Add(parsed.Primitive);
                        }
                        else if (!parsed.IsIgnored) unknownCount++;
                        p = Math.Max(p + 1, parsed.NextIndex);
                    }
                    else
                    {
                        if (LooksLikeUnknownPrimitive(token))
                        {
                            unknownCount++;
                            warnings.Add($"Unbekanntes GEO-Geometrieelement '{token}' in Kontur {contourId}.");
                        }
                        p++;
                    }
                }
            }
            else
            {
                warnings.Add($"Geometriebereich #~331 fehlt in Kontur {contourId}.");
            }

            if (contourArea.HasValue)
                contourAreas.Add((role, contourArea.Value, CalculateContourArea(contourPrimitives)));
            if (!IsClosedAndContinuous(contourPrimitives))
            {
                openContourCount++;
                warnings.Add($"Kontur {contourId} ist offen oder die Segmentreihenfolge ist nicht durchgehend verbunden.");
            }
            contourCount++;
            i = end;
        }

        var widthHeight = ParseNumbers(lines.ElementAtOrDefault(5) ?? string.Empty);
        // ElementAtOrDefault returns 0 for missing entries; preserve absence explicitly
        // so downstream HasValue checks distinguish a missing header from a real zero.
        double? widthFromHeader = widthHeight.Count > 0 ? widthHeight[0] : null;
        double? heightFromHeader = widthHeight.Count > 1 ? widthHeight[1] : null;
        var fileArea = ParseDouble(lines.ElementAtOrDefault(6) ?? string.Empty);
        var signedReportedArea = contourAreas.Sum(x => x.ReportedArea * (x.Role == 1 ? -1.0 : 1.0));
        var signedGeometryArea = contourAreas.Sum(x => x.GeometryArea * (x.Role == 1 ? -1.0 : 1.0));
        // Geometry is the physical outline. TruTops' stored area is retained separately
        // because the supplied samples show small and sometimes material differences.
        double? netArea = contourAreas.Count > 0 && signedGeometryArea > 0
            ? signedGeometryArea
            : fileArea ?? (contourAreas.Count > 0 ? signedReportedArea : null);

        if (fileArea.HasValue && contourAreas.Count > 0 && Math.Abs(fileArea.Value - signedReportedArea) > 0.1)
            warnings.Add($"GEO-Flächenfelder stimmen nicht überein: Dateikopf {fileArea.Value:F4} mm², Kontursumme {signedReportedArea:F4} mm².");
        var requiresAreaVerification = fileArea.HasValue && netArea.HasValue &&
            Math.Abs(fileArea.Value - netArea.Value) > Math.Max(1.0, fileArea.Value * 0.0005);
        if (requiresAreaVerification)
            warnings.Add($"TruTops-Flächenwert ({fileArea!.Value:F2} mm²) weicht von der aus LIN/ARC/CIR rekonstruierten geometrischen Fläche ({netArea!.Value:F2} mm²) ab. Die Fläche muss anhand der Kundendatei oder Zeichnung geprüft werden.");

        if (points.Count == 0) warnings.Add("Keine Punktdatensätze im Abschnitt #~31 gefunden.");
        if (contourCount == 0) warnings.Add("Keine Konturen (#~33) gefunden.");
        if (unknownCount > 0) warnings.Add("Mindestens ein GEO-Element ist unbekannt. Automatische Kalkulation ist gesperrt.");

        var bounds = GeometryBounds.Calculate(primitives);
        var width = widthFromHeader ?? bounds.Width;
        var height = heightFromHeader ?? bounds.Height;
        if (widthFromHeader.HasValue && bounds.Width > 0 && Math.Abs(widthFromHeader.Value - bounds.Width) > 0.5)
            warnings.Add($"Abmessungsprüfung X: GEO-Kopf {widthFromHeader.Value:F3} mm, Geometrie {bounds.Width:F3} mm.");
        if (heightFromHeader.HasValue && bounds.Height > 0 && Math.Abs(heightFromHeader.Value - bounds.Height) > 0.5)
            warnings.Add($"Abmessungsprüfung Y: GEO-Kopf {heightFromHeader.Value:F3} mm, Geometrie {bounds.Height:F3} mm.");
        var dimensionMismatch = (widthFromHeader.HasValue && bounds.Width > 0 && Math.Abs(widthFromHeader.Value - bounds.Width) > 0.5) ||
                                (heightFromHeader.HasValue && bounds.Height > 0 && Math.Abs(heightFromHeader.Value - bounds.Height) > 0.5);

        return new PartGeometry
        {
            SourceFile = sourceFile,
            Format = "TRUMPF TruTops Classic GEO",
            WidthMm = width,
            HeightMm = height,
            NetAreaMm2 = netArea,
            ReportedNetAreaMm2 = fileArea,
            CutLengthMm = primitives.Sum(x => x.LengthMm),
            PointCount = points.Count,
            ContourCount = contourCount,
            OpenContourCount = openContourCount,
            EstimatedPierceCount = contourCount,
            UnknownEntityCount = unknownCount,
            RequiresAreaVerification = requiresAreaVerification,
            IsSafeForPreliminaryCalculation = unknownCount == 0 && openContourCount == 0 && !dimensionMismatch && primitives.Count > 0 && width > 0 && height > 0 && netArea is > 0,
            Primitives = primitives.AsReadOnly(),
            Warnings = warnings.AsReadOnly()
        };
    }

    private static Dictionary<int, Point2> ReadPoints(string[] lines, List<string> warnings)
    {
        var points = new Dictionary<int, Point2>();
        var section = Array.FindIndex(lines, x => x.Trim() == "#~31");
        if (section < 0) return points;
        for (var i = section + 1; i < lines.Length && !lines[i].Trim().StartsWith("#~", StringComparison.Ordinal);)
        {
            if (lines[i].Trim() == "P")
            {
                var id = int.TryParse(lines.ElementAtOrDefault(i + 1)?.Trim(), NumberStyles.Integer, Inv, out var parsedId) ? parsedId : -1;
                var xyz = ParseNumbers(lines.ElementAtOrDefault(i + 2) ?? string.Empty);
                if (id >= 0 && xyz.Count >= 2) points[id] = new Point2(xyz[0], xyz[1]);
                else warnings.Add($"Ungültiger Punktdatensatz nahe GEO-Zeile {i + 1}.");
                i += 4;
            }
            else i++;
        }
        return points;
    }

    private static (GeometryPrimitive? Primitive, int NextIndex, bool IsIgnored) ParsePrimitive(
        string kind, string[] lines, int index, int sectionEnd,
        IReadOnlyDictionary<int, Point2> points, int contourId, int contourRole, List<string> warnings)
    {
        var refsLine = index + 2;
        if (refsLine >= sectionEnd) return (null, index + 1, false);
        var ids = ParseIntegers(lines[refsLine]);
        var layer = contourRole == 1 ? "INNEN" : "AUSSEN";

        if (kind == "LIN")
        {
            if (ids.Count < 2 || !points.TryGetValue(ids[^2], out var a) || !points.TryGetValue(ids[^1], out var b))
            {
                warnings.Add($"LIN in Kontur {contourId}: Punktreferenz fehlt.");
                return (null, index + 4, false);
            }
            return (new GeometryPrimitive(GeometryPrimitiveType.Line, a, b, null, 0, 0, layer, contourId), index + 4, false);
        }

        if (kind == "ARC")
        {
            if (ids.Count < 3 || !points.TryGetValue(ids[^3], out var center) ||
                !points.TryGetValue(ids[^2], out var start) || !points.TryGetValue(ids[^1], out var finish))
            {
                warnings.Add($"ARC in Kontur {contourId}: Mittelpunkt-/Endpunktreferenz fehlt.");
                return (null, index + 5, false);
            }
            var direction = int.TryParse(lines.ElementAtOrDefault(index + 3)?.Trim(), NumberStyles.Integer, Inv, out var d) ? d : 1;
            var r1 = center.DistanceTo(start);
            var r2 = center.DistanceTo(finish);
            if (Math.Abs(r1 - r2) > 0.05)
            {
                warnings.Add($"ARC in Kontur {contourId}: Radiusabweichung {Math.Abs(r1 - r2):F4} mm; die Geometrie ist ungültig.");
                return (null, index + 5, false);
            }
            var startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
            var endAngle = Math.Atan2(finish.Y - center.Y, finish.X - center.X);
            var sweep = direction >= 0 ? PositiveAngle(endAngle - startAngle) : -PositiveAngle(startAngle - endAngle);
            if (Math.Abs(sweep) < 1e-10) sweep = direction >= 0 ? 2 * Math.PI : -2 * Math.PI;
            var radius = (r1 + r2) / 2.0;
            return (new GeometryPrimitive(GeometryPrimitiveType.Arc, start, finish, center, radius, sweep, layer, contourId), index + 5, false);
        }

        if (kind == "CIR")
        {
            var centerId = ids.LastOrDefault(-1);
            var radius = ParseDouble(lines.ElementAtOrDefault(index + 3) ?? string.Empty);
            if (centerId < 0 || !points.TryGetValue(centerId, out var center) || radius is null or <= 0)
            {
                warnings.Add($"CIR in Kontur {contourId}: Mittelpunkt oder Radius ungültig.");
                return (null, index + 5, false);
            }
            var start = new Point2(center.X + radius.Value, center.Y);
            return (new GeometryPrimitive(GeometryPrimitiveType.Circle, start, start, center, radius.Value, Math.PI * 2, layer, contourId), index + 5, false);
        }

        return (null, index + 1, false);
    }


    private static bool IsClosedAndContinuous(IReadOnlyList<GeometryPrimitive> contourPrimitives, double toleranceMm = 0.05)
    {
        if (contourPrimitives.Count == 0) return false;
        if (contourPrimitives.Count == 1 && contourPrimitives[0].Type == GeometryPrimitiveType.Circle) return true;
        for (var i = 0; i < contourPrimitives.Count; i++)
        {
            var current = contourPrimitives[i];
            var next = contourPrimitives[(i + 1) % contourPrimitives.Count];
            if (current.Type == GeometryPrimitiveType.Circle || next.Type == GeometryPrimitiveType.Circle)
                return false;
            if (current.End.DistanceTo(next.Start) > toleranceMm) return false;
        }
        return true;
    }

    private static double CalculateContourArea(IReadOnlyList<GeometryPrimitive> contourPrimitives)
    {
        if (contourPrimitives.Count == 0) return 0;
        var area = 0.0;
        foreach (var primitive in contourPrimitives)
        {
            if (primitive.Type == GeometryPrimitiveType.Circle)
                area += Math.PI * primitive.Radius * primitive.Radius;
            else
                area += primitive.SignedAreaContribution();
        }
        return Math.Abs(area);
    }

    private static int FindMarker(string[] lines, int start, string marker)
    {
        for (var i = start; i < lines.Length; i++)
            if (lines[i].Trim() == marker) return i;
        return -1;
    }

    private static bool LooksLikeUnknownPrimitive(string value) =>
        value.Length is > 1 and < 12 && value.All(char.IsLetter) && value is not "P" and not "##~~";

    private static double PositiveAngle(double value)
    {
        var twoPi = 2 * Math.PI;
        value %= twoPi;
        return value < 0 ? value + twoPi : value;
    }

    private static List<int> ParseIntegers(string value) => value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => int.TryParse(x, NumberStyles.Integer, Inv, out var v) ? (int?)v : null)
        .Where(x => x.HasValue).Select(x => x!.Value).ToList();

    private static List<double> ParseNumbers(string value) => value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => ParseDouble(x)).Where(x => x.HasValue).Select(x => x!.Value).ToList();

    private static double? ParseDouble(string value) => double.TryParse(value.Trim(), NumberStyles.Float, Inv, out var v) ? v : null;
}
