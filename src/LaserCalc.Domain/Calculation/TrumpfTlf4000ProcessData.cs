namespace LaserCalc.Domain.Calculation;

/// <summary>
/// Reference values transcribed from the user's TRUMPF TLF 4000 parameter collection:
/// table 28 (mild steel/O2), table 31 (stainless/N2), table 34 (aluminium/N2).
/// Only large-contour cutting and normal piercing with PCS are included.
/// Values are machine reference data, not a guarantee of current machine condition.
/// </summary>
public static class TrumpfTlf4000ProcessData
{
    public const string SourceDescription = "TRUMPF TLF 4000 · große Kontur · normales Einstechen mit PCS";

    public static List<CuttingParameterPreset> CreatePresets()
    {
        var presets = new List<CuttingParameterPreset>();
        Add(presets, "Stahl", new decimal[] { 1.5m, 2m, 2.5m, 3m, 4m, 5m, 6m, 8m, 10m, 12m, 15m, 20m },
            new decimal[] { 6.4m, 5m, 4.1m, 3.9m, 3.5m, 3.4m, 3m, 2.4m, 2m, 1.6m, 1.3m, 0.9m },
            new decimal[] { 0.05m, 0.05m, 0.05m, 0.06m, 0.07m, 0.08m, 0.1m, 0.2m, 0.3m, 0.6m, 3m, 5m });
        Add(presets, "Edelstahl / VA", new decimal[] { 1m, 1.5m, 2m, 2.5m, 3m, 4m, 5m, 6m, 8m, 10m, 12m, 15m },
            new decimal[] { 10.5m, 8.2m, 6.7m, 5.4m, 4.1m, 2.9m, 2.5m, 2.2m, 1.4m, 0.84m, 0.43m, 0.22m },
            new decimal[] { 0.04m, 0.05m, 0.06m, 0.06m, 0.06m, 0.06m, 0.06m, 0.08m, 0.15m, 0.25m, 0.7m, 12m });
        Add(presets, "Aluminium", new decimal[] { 1m, 1.5m, 2m, 2.5m, 3m, 4m, 5m, 6m, 8m, 10m },
            new decimal[] { 11.4m, 8.9m, 7.4m, 6m, 4.9m, 3.4m, 2.5m, 1.8m, 0.79m, 0.7m },
            new decimal[] { 0.04m, 0.04m, 0.05m, 0.05m, 0.06m, 0.1m, 0.2m, 0.2m, 0.7m, 2m });
        return presets;
    }

    private static void Add(List<CuttingParameterPreset> target, string material, decimal[] thicknesses, decimal[] feeds, decimal[] pierces)
    {
        if (thicknesses.Length != feeds.Length || feeds.Length != pierces.Length)
            throw new InvalidOperationException("TRUMPF-Parameterreihen haben unterschiedliche Längen.");
        for (var i = 0; i < thicknesses.Length; i++)
        {
            target.Add(new CuttingParameterPreset
            {
                MaterialName = material,
                ThicknessMm = thicknesses[i],
                FeedRateMetersPerMinute = feeds[i],
                PierceSeconds = pierces[i]
            });
        }
    }
}
