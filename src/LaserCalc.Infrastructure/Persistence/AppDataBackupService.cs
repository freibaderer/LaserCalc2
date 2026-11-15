using System.IO.Compression;
using System.Text.Json;
using LaserCalc.Infrastructure.Customers;
using LaserCalc.Infrastructure.Offers;

namespace LaserCalc.Infrastructure.Persistence;

/// <summary>Creates and restores a validated backup of LaserCalc's local JSON data.</summary>
public sealed class AppDataBackupService(string dataDirectory)
{
    private const long MaxEntryBytes = 64 * 1024 * 1024;
    private static readonly string[] SupportedFiles =
    {
        "settings.json", "offer-settings.json", "customers.json", "quote-history.json"
    };
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _dataDirectory = Path.GetFullPath(dataDirectory);

    public sealed record BackupManifest(string Product, string FormatVersion, DateTimeOffset CreatedAt, string[] Files);
    public sealed record RestoreResult(string SafetyBackupPath, IReadOnlyList<string> RestoredFiles);

    public void CreateBackup(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        Directory.CreateDirectory(_dataDirectory);
        var fullPath = Path.GetFullPath(outputPath);
        var outputName = Path.GetFileName(fullPath);
        if (SupportedFiles.Contains(outputName, StringComparer.OrdinalIgnoreCase) ||
            string.Equals(outputName, "manifest.json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Die Datensicherung darf keine interne LaserCalc-Datendatei überschreiben.");
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var available = SupportedFiles.Where(name => File.Exists(Path.Combine(_dataDirectory, name))).ToArray();
        if (!available.Contains("settings.json", StringComparer.Ordinal))
            throw new InvalidOperationException("Die LaserCalc-Einstellungen fehlen. Es kann keine vollständige Datensicherung erstellt werden.");

        var tempPath = fullPath + ".tmp";
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false))
            {
                var manifest = new BackupManifest("LaserCalc 2.0", "1", DateTimeOffset.UtcNow, available);
                WriteEntry(archive, "manifest.json", JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions));
                foreach (var name in available)
                {
                    var bytes = File.ReadAllBytes(Path.Combine(_dataDirectory, name));
                    if (bytes.LongLength > MaxEntryBytes)
                        throw new InvalidDataException($"Die Datei {name} ist zu groß für eine Datensicherung.");
                    WriteEntry(archive, name, bytes);
                }
            }
            File.Move(tempPath, fullPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }
    }

    public RestoreResult RestoreBackup(string backupPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);
        if (!File.Exists(backupPath)) throw new FileNotFoundException("Datensicherung wurde nicht gefunden.", backupPath);

        var payloads = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        using (var stream = File.OpenRead(backupPath))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false))
        {
            var names = archive.Entries.Select(x => x.FullName).ToList();
            if (names.Count != names.Distinct(StringComparer.Ordinal).Count())
                throw new InvalidDataException("Die Datensicherung enthält doppelte Dateieinträge.");
            if (names.Any(name => name.Contains('/') || name.Contains('\\') || name is "." or ".."))
                throw new InvalidDataException("Die Datensicherung enthält ungültige Pfadangaben.");
            if (names.Any(name => name != "manifest.json" && !SupportedFiles.Contains(name, StringComparer.Ordinal)))
                throw new InvalidDataException("Die Datensicherung enthält unbekannte Dateien.");

            var manifestEntry = archive.GetEntry("manifest.json") ?? throw new InvalidDataException("Manifest der Datensicherung fehlt.");
            var manifestBytes = ReadEntry(manifestEntry);
            var manifest = JsonSerializer.Deserialize<BackupManifest>(manifestBytes, JsonOptions)
                ?? throw new InvalidDataException("Manifest der Datensicherung ist ungültig.");
            if (manifest.Product != "LaserCalc 2.0" || manifest.FormatVersion != "1")
                throw new InvalidDataException("Die Datensicherung stammt nicht aus einem unterstützten LaserCalc-Format.");
            if (manifest.Files is null || !manifest.Files.Contains("settings.json", StringComparer.Ordinal))
                throw new InvalidDataException("Die Datensicherung enthält keine Anwendungseinstellungen.");

            var declared = manifest.Files.Distinct(StringComparer.Ordinal).ToArray();
            if (declared.Length != manifest.Files.Length || declared.Any(x => !SupportedFiles.Contains(x, StringComparer.Ordinal)))
                throw new InvalidDataException("Die Dateiliste im Backup ist ungültig.");
            var archiveFiles = names.Where(x => x != "manifest.json").ToHashSet(StringComparer.Ordinal);
            if (!archiveFiles.SetEquals(declared))
                throw new InvalidDataException("Manifest und tatsächliche Dateien der Datensicherung stimmen nicht überein.");

            foreach (var name in declared)
            {
                var entry = archive.GetEntry(name) ?? throw new InvalidDataException($"Datei {name} fehlt in der Datensicherung.");
                var bytes = ReadEntry(entry);
                ValidateJsonFile(name, bytes);
                payloads.Add(name, bytes);
            }
        }

        Directory.CreateDirectory(_dataDirectory);
        var safetyPath = Path.Combine(_dataDirectory, $"LaserCalc2-vor-Wiederherstellung-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
        if (File.Exists(Path.Combine(_dataDirectory, "settings.json"))) CreateBackup(safetyPath);

        var stagingDirectory = Path.Combine(_dataDirectory, ".restore-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingDirectory);
        try
        {
            foreach (var item in payloads)
                File.WriteAllBytes(Path.Combine(stagingDirectory, item.Key), item.Value);
            foreach (var item in payloads)
            {
                var staged = Path.Combine(stagingDirectory, item.Key);
                var target = Path.Combine(_dataDirectory, item.Key);
                File.Move(staged, target, overwrite: true);
            }
            // A backup is a snapshot: files absent from it must not leak forward
            // from a newer data directory after restoration.
            foreach (var name in SupportedFiles.Where(name => !payloads.ContainsKey(name)))
            {
                var target = Path.Combine(_dataDirectory, name);
                if (File.Exists(target)) File.Delete(target);
            }
        }
        finally
        {
            if (Directory.Exists(stagingDirectory)) Directory.Delete(stagingDirectory, recursive: true);
        }

        return new RestoreResult(File.Exists(safetyPath) ? safetyPath : string.Empty, payloads.Keys.OrderBy(x => x).ToList().AsReadOnly());
    }

    private static void ValidateJsonFile(string fileName, byte[] bytes)
    {
        try
        {
            switch (fileName)
            {
                case "settings.json":
                    var appSettings = JsonSerializer.Deserialize<AppSettings>(bytes, JsonOptions);
                    if (appSettings?.Materials is not { Count: > 0 })
                        throw new InvalidDataException("Materialstamm in settings.json fehlt oder ist leer.");
                    break;
                case "offer-settings.json":
                    var offerSettings = JsonSerializer.Deserialize<OfferSettings>(bytes, JsonOptions);
                    if (offerSettings?.Company is null)
                        throw new InvalidDataException("Absenderprofil in offer-settings.json fehlt.");
                    break;
                case "customers.json":
                    _ = JsonSerializer.Deserialize<List<CustomerRecord>>(bytes, JsonOptions)
                        ?? throw new InvalidDataException("Kundenliste ist leer oder ungültig.");
                    break;
                case "quote-history.json":
                    _ = JsonSerializer.Deserialize<List<QuoteHistoryEntry>>(bytes, JsonOptions)
                        ?? throw new InvalidDataException("Kalkulationshistorie ist leer oder ungültig.");
                    break;
                default:
                    throw new InvalidDataException("Unbekannte Datendatei.");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Die Datei {fileName} enthält ungültige oder inkompatible Daten.", ex);
        }
    }

    private static byte[] ReadEntry(ZipArchiveEntry entry)
    {
        if (entry.Length > MaxEntryBytes) throw new InvalidDataException($"Backup-Datei {entry.Name} überschreitet die zulässige Größe.");
        using var input = entry.Open();
        using var output = new MemoryStream();
        input.CopyTo(output);
        if (output.Length > MaxEntryBytes) throw new InvalidDataException($"Backup-Datei {entry.Name} ist nach Dekomprimierung zu groß.");
        return output.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string name, byte[] bytes)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var output = entry.Open();
        output.Write(bytes, 0, bytes.Length);
    }
}
