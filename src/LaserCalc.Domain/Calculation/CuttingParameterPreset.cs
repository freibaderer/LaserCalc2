namespace LaserCalc.Domain.Calculation;

/// <summary>Operator-maintained process preset for one material/thickness combination.</summary>
public sealed class CuttingParameterPreset
{
    public string MaterialName { get; set; } = string.Empty;
    public decimal ThicknessMm { get; set; }
    public decimal FeedRateMetersPerMinute { get; set; }
    public decimal PierceSeconds { get; set; }
}

public static class CuttingParameterResolver
{
    public static CuttingParameterPreset? Find(
        IEnumerable<CuttingParameterPreset> presets,
        string materialName,
        decimal thicknessMm,
        decimal thicknessToleranceMm = 0.01m)
    {
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentException.ThrowIfNullOrWhiteSpace(materialName);
        if (thicknessMm <= 0m) throw new ArgumentOutOfRangeException(nameof(thicknessMm));
        if (thicknessToleranceMm < 0m) throw new ArgumentOutOfRangeException(nameof(thicknessToleranceMm));

        return presets.FirstOrDefault(x =>
            string.Equals(x.MaterialName, materialName, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(x.ThicknessMm - thicknessMm) <= thicknessToleranceMm);
    }
}
