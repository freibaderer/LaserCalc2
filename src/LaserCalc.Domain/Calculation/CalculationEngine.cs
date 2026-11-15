namespace LaserCalc.Domain.Calculation;

/// <summary>
/// Deterministic shop-floor costing model. It keeps stock blank weight separate from
/// finished-part weight and exposes the time basis and commercial margin assumptions.
/// </summary>
public sealed class CalculationEngine
{
    public CalculationResult Calculate(CalculationInputs input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Validate(input);
        var warnings = new List<string>();

        // "10 mm border" is interpreted as 10 mm on each of the four sides.
        var blankWidth = input.BoundingWidthMm + 2m * input.MaterialBorderMm;
        var blankHeight = input.BoundingHeightMm + 2m * input.MaterialBorderMm;
        var blankAreaMm2 = blankWidth * blankHeight;
        var blankAreaM2 = blankAreaMm2 / 1_000_000m;
        var blankVolumeM3 = blankAreaM2 * (input.ThicknessMm / 1000m);
        decimal? utilizationPercent = input.NetPartAreaMm2.HasValue
            ? input.NetPartAreaMm2.Value / blankAreaMm2 * 100m
            : null;
        decimal? scrapArea = input.NetPartAreaMm2.HasValue
            ? Math.Max(0m, blankAreaMm2 - input.NetPartAreaMm2.Value)
            : null;
        var blankWeight = blankVolumeM3 * input.DensityKgPerM3;
        var materialCost = blankWeight * input.MaterialPricePerKg;

        decimal? finishedWeight = null;
        if (input.NetPartAreaMm2 is > 0m)
        {
            var finishedVolumeM3 = input.NetPartAreaMm2.Value * input.ThicknessMm / 1_000_000_000m;
            finishedWeight = finishedVolumeM3 * input.DensityKgPerM3;
        }
        else
        {
            warnings.Add("Nettoteilfläche fehlt: Fertigteilgewicht kann nicht bestimmt werden. Materialkosten basieren weiterhin auf dem Rohteil-Rechteck.");
        }

        var timeBasis = "Geometrie-Schätzung";
        decimal cuttingMinutes;
        if (input.ActualOrConfirmedMachineMinutes is > 0m)
        {
            cuttingMinutes = input.ActualOrConfirmedMachineMinutes.Value;
            timeBasis = "Tatsächliche / bestätigte Maschinenzeit";
        }
        else
        {
            var pathMinutes = input.ContourLengthMm / 1000m / input.FeedRateMetersPerMinute;
            var pierceMinutes = input.EstimatedPierceCount * input.PierceSeconds / 60m;
            cuttingMinutes = pathMinutes + pierceMinutes;
            if (input.EstimatedPierceCount == 0)
                warnings.Add("Keine Einstiche erkannt. Die Einstichzeit ist daher nicht enthalten.");
        }

        var machineCost = cuttingMinutes * input.MachineRatePerHour / 60m;
        var setupCost = input.SetupMinutes / input.Quantity * input.MachineRatePerHour / 60m;
        var costPerPart = materialCost + machineCost + setupCost;

        // Margins are percentages of the selling price, not markups on cost.
        var minimumPrice = PriceForMargin(costPerPart, input.MinimumMarginPercent);
        var recommendedPrice = PriceForMargin(costPerPart, input.TargetMarginPercent);

        // MachineRatePerHour is explicitly the user's internal full-cost rate
        // (including the machine-related operating cost base), not a customer sales rate.
        // The 10 mm per-side stock allowance is a confirmed business rule, not a warning.

        var roundedCostPerPart = Round(costPerPart);
        var roundedMinimumPrice = Round(minimumPrice);
        var roundedRecommendedPrice = Round(recommendedPrice);

        return new CalculationResult(
            Round(blankWidth), Round(blankHeight), Round(blankWeight),
            finishedWeight.HasValue ? Round(finishedWeight.Value) : null,
            Round(materialCost), Round(cuttingMinutes), Round(setupCost), Round(machineCost),
            roundedCostPerPart, roundedMinimumPrice, roundedRecommendedPrice,
            Round(roundedCostPerPart * input.Quantity),
            Round(roundedMinimumPrice * input.Quantity),
            Round(roundedRecommendedPrice * input.Quantity),
            utilizationPercent.HasValue ? Round(utilizationPercent.Value) : null,
            scrapArea.HasValue ? Round(scrapArea.Value) : null,
            timeBasis, warnings);
    }

    private static decimal PriceForMargin(decimal cost, decimal marginPercent)
    {
        var margin = marginPercent / 100m;
        return cost / (1m - margin);
    }

    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private static void Validate(CalculationInputs x)
    {
        if (x.NetPartAreaMm2.HasValue && x.BoundingWidthMm > 0 && x.BoundingHeightMm > 0 &&
            x.NetPartAreaMm2.Value > x.BoundingWidthMm * x.BoundingHeightMm * 1.001m)
            throw new ArgumentOutOfRangeException(nameof(x), "Die Nettoteilfläche ist größer als das umschließende Rechteck. Geometrie bitte prüfen.");

        if (x.MaterialPricePerKg < 0 || x.DensityKgPerM3 <= 0 || x.ThicknessMm <= 0 ||
            x.BoundingWidthMm <= 0 || x.BoundingHeightMm <= 0 || x.NetPartAreaMm2 is < 0m ||
            x.ContourLengthMm < 0 || x.EstimatedPierceCount < 0 || x.FeedRateMetersPerMinute <= 0 ||
            x.MachineRatePerHour < 0 || x.SetupMinutes < 0 || x.Quantity <= 0 ||
            x.MinimumMarginPercent < 0 || x.MinimumMarginPercent >= 100 ||
            x.TargetMarginPercent < 0 || x.TargetMarginPercent >= 100 ||
            x.TargetMarginPercent < x.MinimumMarginPercent || x.MaterialBorderMm < 0 ||
            x.PierceSeconds < 0 || x.ActualOrConfirmedMachineMinutes is <= 0m)
            throw new ArgumentOutOfRangeException(nameof(x), "Mindestens ein Eingabewert ist außerhalb des zulässigen Bereichs. Eine eingetragene bestätigte Maschinenzeit muss größer als null sein. Prüfe Mengen, Materialdaten und Margen.");
    }
}
