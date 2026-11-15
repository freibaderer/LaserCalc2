using System.Text.Json;

namespace LaserCalc.Infrastructure.Persistence;

/// <summary>Complete input/output snapshot for later quote comparisons and auditability.</summary>
public sealed record QuoteHistoryEntry(
    DateTime CreatedAt,
    string SourceFile,
    string Format,
    string Material,
    decimal MaterialPricePerKg,
    decimal DensityKgPerM3,
    decimal ThicknessMm,
    int Quantity,
    decimal WidthMm,
    decimal HeightMm,
    decimal? NetAreaMm2,
    decimal? ReportedSourceAreaMm2,
    decimal CutLengthMm,
    int PierceCount,
    decimal FeedRateMetersPerMinute,
    decimal MachineRatePerHour,
    decimal SetupMinutesPerOrder,
    decimal MaterialBorderMmPerSide,
    decimal PierceSeconds,
    decimal? ActualMachineMinutesPerPart,
    decimal MaterialCostPerPart,
    decimal MachineCostPerPart,
    decimal SetupCostPerPart,
    decimal EstimatedMachineMinutesPerPart,
    decimal CostPerPart,
    decimal MinimumPricePerPart,
    decimal RecommendedPricePerPart,
    string TimeBasis,
    bool SourcePairVerified,
    bool ManualGeometryCheckAccepted);

public sealed class QuoteHistoryStore(string dataDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private string HistoryPath => Path.Combine(dataDirectory, "quote-history.json");


    public async Task<IReadOnlyList<QuoteHistoryEntry>> LoadRecentAsync(int limit = 500, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 5000)
            throw new ArgumentOutOfRangeException(nameof(limit), "Die Anzahl muss zwischen 1 und 5000 liegen.");
        if (!File.Exists(HistoryPath)) return Array.Empty<QuoteHistoryEntry>();

        try
        {
            var json = await File.ReadAllTextAsync(HistoryPath, cancellationToken);
            var entries = JsonSerializer.Deserialize<List<QuoteHistoryEntry>>(json, JsonOptions) ?? new List<QuoteHistoryEntry>();
            return entries.OrderByDescending(x => x.CreatedAt).Take(limit).ToList().AsReadOnly();
        }
        catch (JsonException)
        {
            var backup = HistoryPath + ".invalid-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(HistoryPath, backup, overwrite: true);
            return Array.Empty<QuoteHistoryEntry>();
        }
    }

    public async Task AppendAsync(QuoteHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(dataDirectory);
        List<QuoteHistoryEntry> history;
        if (File.Exists(HistoryPath))
        {
            try
            {
                history = JsonSerializer.Deserialize<List<QuoteHistoryEntry>>(await File.ReadAllTextAsync(HistoryPath, cancellationToken), JsonOptions) ?? new();
            }
            catch (JsonException)
            {
                var backup = HistoryPath + ".invalid-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                File.Copy(HistoryPath, backup, overwrite: true);
                history = new();
            }
        }
        else history = new();

        history.Add(entry);
        var temp = HistoryPath + ".tmp";
        await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(history, JsonOptions), cancellationToken);
        File.Move(temp, HistoryPath, overwrite: true);
    }
}
