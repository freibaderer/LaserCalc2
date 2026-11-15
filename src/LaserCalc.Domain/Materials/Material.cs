namespace LaserCalc.Domain.Materials;

public enum MaterialKind { Steel, StainlessSteel, Aluminum }

/// <summary>All prices are net EUR; density is kg per cubic metre.</summary>
public sealed record Material(
    Guid Id,
    string Name,
    MaterialKind Kind,
    decimal PricePerKilogram,
    decimal DensityKgPerM3,
    bool IsActive = true);
