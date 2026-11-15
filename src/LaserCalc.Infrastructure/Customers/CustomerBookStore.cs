using System.Text.Json;

namespace LaserCalc.Infrastructure.Customers;

/// <summary>Local customer address book. Data never leaves the PC.</summary>
public sealed class CustomerBookStore(string dataDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path = Path.Combine(dataDirectory, "customers.json");
    private readonly object _gate = new();

    public IReadOnlyList<CustomerRecord> Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_path)) return Array.Empty<CustomerRecord>();
            try
            {
                var items = JsonSerializer.Deserialize<List<CustomerRecord>>(File.ReadAllText(_path), JsonOptions)
                    ?? new List<CustomerRecord>();
                return items
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                    .OrderByDescending(x => x.LastUsedAt)
                    .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
                    .AsReadOnly();
            }
            catch (Exception ex) when (ex is IOException or JsonException)
            {
                var backup = _path + ".invalid-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                try { File.Copy(_path, backup, overwrite: true); }
                catch (IOException) { /* Preserve the original error path; never destroy the source file. */ }
                return Array.Empty<CustomerRecord>();
            }
        }
    }

    public void Upsert(CustomerRecord customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        if (string.IsNullOrWhiteSpace(customer.Name)) throw new ArgumentException("Kundenname fehlt.", nameof(customer));
        if (string.IsNullOrWhiteSpace(customer.Street) || string.IsNullOrWhiteSpace(customer.PostalCode) ||
            string.IsNullOrWhiteSpace(customer.City))
            throw new ArgumentException("Für den Kundenstamm wird eine vollständige Anschrift benötigt.", nameof(customer));

        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var customers = File.Exists(_path)
                ? (JsonSerializer.Deserialize<List<CustomerRecord>>(File.ReadAllText(_path), JsonOptions) ?? new List<CustomerRecord>())
                : new List<CustomerRecord>();
            var key = Normalize(customer.Name, customer.PostalCode, customer.City);
            customers.RemoveAll(x => Normalize(x.Name, x.PostalCode, x.City) == key);
            customers.Add(customer with { Name = customer.Name.Trim(), Street = customer.Street.Trim(),
                PostalCode = customer.PostalCode.Trim(), City = customer.City.Trim(), LastUsedAt = DateTime.Now });
            customers = customers.OrderByDescending(x => x.LastUsedAt).Take(1000).ToList();

            var temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(customers, JsonOptions));
            File.Move(temp, _path, overwrite: true);
        }
    }

    private static string Normalize(string name, string postalCode, string city) =>
        string.Join("|", name.Trim(), postalCode.Trim(), city.Trim()).ToUpperInvariant();
}
