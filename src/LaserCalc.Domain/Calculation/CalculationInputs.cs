namespace LaserCalc.Domain.Calculation;

/// <summary>All monetary values are EUR net. Geometry dimensions are millimetres.</summary>
public sealed record CalculationInputs(
    decimal MaterialPricePerKg,
    decimal DensityKgPerM3,
    decimal ThicknessMm,
    decimal BoundingWidthMm,
    decimal BoundingHeightMm,
    decimal? NetPartAreaMm2,
    decimal ContourLengthMm,
    int EstimatedPierceCount,
    decimal FeedRateMetersPerMinute,
    decimal MachineRatePerHour,
    decimal SetupMinutes,
    int Quantity,
    decimal MinimumMarginPercent,
    decimal TargetMarginPercent,
    decimal MaterialBorderMm = 10m,
    decimal PierceSeconds = 1m,
    decimal? ActualOrConfirmedMachineMinutes = null);

public sealed record CalculationResult(
    decimal BlankWidthMm,
    decimal BlankHeightMm,
    decimal BlankWeightKg,
    decimal? FinishedPartWeightKg,
    decimal MaterialCostPerPart,
    decimal CuttingMinutesPerPart,
    decimal SetupCostPerPart,
    decimal MachineCostPerPart,
    decimal CostPerPart,
    decimal MinimumSellingPricePerPart,
    decimal RecommendedSellingPricePerPart,
    decimal OrderCostTotal,
    decimal MinimumSellingPriceTotal,
    decimal RecommendedSellingPriceTotal,
    decimal? MaterialUtilizationPercent,
    decimal? ScrapAreaMm2,
    string TimeBasis,
    IReadOnlyList<string> Warnings);
