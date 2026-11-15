using LaserCalc.Domain.Geometry;

namespace LaserCalc.Infrastructure.Import;

/// <summary>
/// Compares the customer's original DXF with the processed TruTops GEO geometry.
/// The comparison is intentionally conservative: missing values, unsafe input,
/// missing contours or incompatible dimensions never count as a successful match.
/// A 90-degree rotation is accepted because it does not change the part itself.
/// </summary>
public static class GeometrySourceComparer
{
    public const double LengthToleranceMm = 0.5;
    public const double AreaToleranceMm2 = 1.0;
    public const double DimensionToleranceMm = 0.2;

    public sealed record Result(
        bool IsConsistent,
        double CutLengthDeltaMm,
        double? NetAreaDeltaMm2,
        IReadOnlyList<string> Warnings)
    {
        public string Summary => NetAreaDeltaMm2.HasValue
            ? $"DXF ↔ GEO | Δ Schnittlänge {CutLengthDeltaMm:+0.000;-0.000;0.000} mm | Δ Fläche {NetAreaDeltaMm2.Value:+0.00;-0.00;0.00} mm²"
            : $"DXF ↔ GEO | Δ Schnittlänge {CutLengthDeltaMm:+0.000;-0.000;0.000} mm | Fläche nicht vergleichbar";
    }

    public static Result Compare(PartGeometry customerDxf, PartGeometry truTopsGeo)
    {
        ArgumentNullException.ThrowIfNull(customerDxf);
        ArgumentNullException.ThrowIfNull(truTopsGeo);

        var warnings = new List<string>();
        var lengthDelta = truTopsGeo.CutLengthMm - customerDxf.CutLengthMm;
        double? areaDelta = customerDxf.NetAreaMm2.HasValue && truTopsGeo.NetAreaMm2.HasValue
            ? truTopsGeo.NetAreaMm2.Value - customerDxf.NetAreaMm2.Value
            : null;

        if (!customerDxf.IsSafeForPreliminaryCalculation)
            warnings.Add("DXF/GEO-Abgleich: Die Kunden-DXF enthält offene, mehrdeutige oder nicht unterstützte Geometrie und kann nicht als sichere Referenz dienen.");
        if (!truTopsGeo.IsSafeForPreliminaryCalculation)
            warnings.Add("DXF/GEO-Abgleich: Die TruTops-GEO-Datei hat die interne Geometrieprüfung nicht bestanden.");

        if (Math.Abs(lengthDelta) > LengthToleranceMm)
            warnings.Add($"DXF/GEO-Abgleich: Schnittlänge weicht um {Math.Abs(lengthDelta):F3} mm ab.");

        if (!areaDelta.HasValue)
            warnings.Add("DXF/GEO-Abgleich: Die Nettoteilfläche fehlt in mindestens einer Quelle; eine Flächenbestätigung ist nicht möglich.");
        else if (Math.Abs(areaDelta.Value) > AreaToleranceMm2)
            warnings.Add($"DXF/GEO-Abgleich: Nettoteilfläche weicht um {Math.Abs(areaDelta.Value):F2} mm² ab.");

        if (customerDxf.ContourCount != truTopsGeo.ContourCount)
            warnings.Add($"DXF/GEO-Abgleich: Anzahl geschlossener Konturen weicht ab (DXF {customerDxf.ContourCount}, GEO {truTopsGeo.ContourCount}).");

        var sameOrientation = Math.Abs(truTopsGeo.WidthMm - customerDxf.WidthMm) <= DimensionToleranceMm &&
                              Math.Abs(truTopsGeo.HeightMm - customerDxf.HeightMm) <= DimensionToleranceMm;
        var rotated = Math.Abs(truTopsGeo.WidthMm - customerDxf.HeightMm) <= DimensionToleranceMm &&
                      Math.Abs(truTopsGeo.HeightMm - customerDxf.WidthMm) <= DimensionToleranceMm;
        if (!sameOrientation && !rotated)
            warnings.Add("DXF/GEO-Abgleich: Außenmaße weichen ab. Möglicherweise andere Einheit, Skalierung oder abweichende Kontur.");

        var consistent = warnings.Count == 0;
        if (consistent)
            warnings.Add("DXF/GEO-Abgleich: Geometriewerte liegen innerhalb der vorläufigen Toleranzen.");

        return new Result(consistent, lengthDelta, areaDelta, warnings.AsReadOnly());
    }
}
