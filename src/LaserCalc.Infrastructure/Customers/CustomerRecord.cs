namespace LaserCalc.Infrastructure.Customers;

public sealed record CustomerRecord(
    string Name,
    string Contact,
    string Street,
    string PostalCode,
    string City,
    string Email,
    DateTime LastUsedAt)
{
    public string DisplayName => $"{Name} · {PostalCode} {City}";
}
