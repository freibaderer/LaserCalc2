using System.Text.Json;

namespace LaserCalc.Infrastructure.Offers;

/// <summary>Local offer/company settings and sequential annual offer numbers.</summary>
public sealed class OfferSettingsStore(string dataDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path = Path.Combine(dataDirectory, "offer-settings.json");
    private readonly object _gate = new();

    public OfferSettings Load()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        if (!File.Exists(_path))
        {
            var defaults = new OfferSettings();
            Save(defaults);
            return defaults;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<OfferSettings>(File.ReadAllText(_path), JsonOptions) ?? new OfferSettings();
            loaded.Company ??= new OfferCompanyProfile();
            loaded.NextSequenceNumber = Math.Max(1, loaded.NextSequenceNumber);
            loaded.DefaultValidityDays = Math.Clamp(loaded.DefaultValidityDays, 1, 365);
            if (loaded.DefaultVatPercent is < 0m or > 100m) loaded.DefaultVatPercent = 19m;
            return loaded;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            var backup = _path + ".invalid-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(_path, backup, overwrite: true);
            return new OfferSettings();
        }
    }

    public void Save(OfferSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (_gate) SaveCore(settings);
    }

    public string PreviewNextNumber()
    {
        var settings = Load();
        var year = DateTime.Now.Year;
        var sequence = settings.SequenceYear == year ? settings.NextSequenceNumber : 1;
        return $"ANG-{year}-{sequence:0000}";
    }

    public string ReserveNextNumber(OfferSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (_gate)
        {
            var year = DateTime.Now.Year;
            if (settings.SequenceYear != year)
            {
                settings.SequenceYear = year;
                settings.NextSequenceNumber = 1;
            }
            var number = $"ANG-{year}-{settings.NextSequenceNumber:0000}";
            settings.NextSequenceNumber++;
            SaveCore(settings);
            return number;
        }
    }

    private void SaveCore(OfferSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temp, _path, overwrite: true);
    }
}
