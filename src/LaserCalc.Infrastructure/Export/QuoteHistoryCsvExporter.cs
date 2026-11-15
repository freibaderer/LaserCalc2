using System.Globalization;
using System.Text;
using LaserCalc.Infrastructure.Persistence;

namespace LaserCalc.Infrastructure.Export;

/// <summary>Exports saved costing snapshots as a German-Excel-friendly semicolon CSV.</summary>
public sealed class QuoteHistoryCsvExporter
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public void Export(string outputPath, IEnumerable<QuoteHistoryEntry> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(entries);

        var rows = entries.OrderByDescending(x => x.CreatedAt).ToList();
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(";", new[]
        {
            "Datum", "Datei / Teil", "Quellformat", "Material", "Dicke (mm)", "Menge",
            "Breite (mm)", "Hoehe (mm)", "Nettoteilflaeche (mm2)", "Schnittlaenge (mm)",
            "Einstiche", "Vorschub (m/min)", "Zeitbasis", "Maschinenzeit je Teil (min)",
            "Materialpreis (EUR/kg)", "Maschinen-Vollkosten (EUR/h)", "Materialkosten je Teil (EUR)",
            "Maschinenkosten je Teil (EUR)", "Ruestkosten je Teil (EUR)", "Selbstkosten je Teil (EUR)",
            "Mindestpreis je Teil (EUR)", "Empfehlung je Teil (EUR)", "Materialausnutzung (%)",
            "DXF-GEO abgeglichen", "Manuelle Geometriepruefung"
        }.Select(Escape)));

        foreach (var entry in rows)
        {
            var fields = new[]
            {
                entry.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss", German), entry.SourceFile, entry.Format,
                entry.Material, F(entry.ThicknessMm), entry.Quantity.ToString(German), F(entry.WidthMm),
                F(entry.HeightMm), F(entry.NetAreaMm2), F(entry.CutLengthMm),
                entry.PierceCount.ToString(German), F(entry.FeedRateMetersPerMinute), entry.TimeBasis,
                F(entry.ActualMachineMinutesPerPart ?? entry.EstimatedMachineMinutesPerPart),
                F(entry.MaterialPricePerKg), F(entry.MachineRatePerHour), F(entry.MaterialCostPerPart),
                F(entry.MachineCostPerPart), F(entry.SetupCostPerPart), F(entry.CostPerPart),
                F(entry.MinimumPricePerPart), F(entry.RecommendedPricePerPart),
                entry.NetAreaMm2.HasValue && entry.WidthMm > 0 && entry.HeightMm > 0
                    ? F(entry.NetAreaMm2.Value / (entry.WidthMm + 2 * entry.MaterialBorderMmPerSide) /
                        (entry.HeightMm + 2 * entry.MaterialBorderMmPerSide) * 100m)
                    : string.Empty,
                entry.SourcePairVerified ? "Ja" : "Nein", entry.ManualGeometryCheckAccepted ? "Ja" : "Nein"
            };
            builder.AppendLine(string.Join(";", fields.Select(Escape)));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var temp = outputPath + ".tmp";
        File.WriteAllText(temp, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        File.Move(temp, outputPath, overwrite: true);
    }

    private static string F(decimal? value) => value?.ToString("0.00", German) ?? string.Empty;

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        return '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';
    }
}
