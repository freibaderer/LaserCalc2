using LaserCalc.Domain.Calculation;

using System.Text.Json;

namespace LaserCalc.Infrastructure.Persistence;

public sealed class MaterialPreset
{
    public string Name { get; set; } = "Stahl";
    public decimal PricePerKg { get; set; }
    public decimal DensityKgPerM3 { get; set; }
    public decimal MachineRatePerHour { get; set; }
}

public sealed class AppSettings
{
    public decimal MaterialBorderMm { get; set; } = 10m;
    public decimal DefaultFeedRateMetersPerMinute { get; set; } = 12m;
    public decimal PierceSeconds { get; set; } = 1m;
    public decimal DefaultSetupMinutes { get; set; } = 0m;
    // Initial configurable defaults only; the business must confirm its pricing policy.
    public decimal MinimumMarginPercent { get; set; } = 5m;
    public decimal TargetMarginPercent { get; set; } = 20m;
    public List<CuttingParameterPreset> CuttingParameters { get; set; } = TrumpfTlf4000ProcessData.CreatePresets();
    public List<MaterialPreset> Materials { get; set; } = new()
    {
        new() { Name = "Stahl", PricePerKg = 0.90m, DensityKgPerM3 = 7850m, MachineRatePerHour = 80m },
        new() { Name = "Edelstahl / VA", PricePerKg = 3.20m, DensityKgPerM3 = 8000m, MachineRatePerHour = 100m },
        new() { Name = "Aluminium", PricePerKg = 4.60m, DensityKgPerM3 = 2700m, MachineRatePerHour = 100m }
    };
}

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public string DataDirectory { get; }
    private string SettingsPath => Path.Combine(DataDirectory, "settings.json");

    public AppSettingsStore(string? dataDirectory = null)
    {
        DataDirectory = string.IsNullOrWhiteSpace(dataDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mages", "LaserCalc2")
            : Path.GetFullPath(dataDirectory);
    }

    public AppSettings Load()
    {
        Directory.CreateDirectory(DataDirectory);
        if (!File.Exists(SettingsPath))
        {
            var defaults = new AppSettings();
            Save(defaults);
            return defaults;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions);
            if (loaded is not { Materials.Count: > 0 }) return new AppSettings();

            // Upgrade older installations that have no cutting presets yet.
            // Existing operator-edited presets are never overwritten.
            if (loaded.CuttingParameters is null || loaded.CuttingParameters.Count == 0)
            {
                loaded.CuttingParameters = TrumpfTlf4000ProcessData.CreatePresets();
                Save(loaded);
            }
            return loaded;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            var backup = SettingsPath + ".invalid-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(SettingsPath, backup, overwrite: true);
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DataDirectory);
        var temp = SettingsPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temp, SettingsPath, overwrite: true);
    }
}
