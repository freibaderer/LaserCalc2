namespace LaserCalc.Domain.Offers;

/// <summary>Commercial calculation for one quoted line. All amounts are net EUR.</summary>
public sealed record OfferPricingInput(
    decimal UnitNetPrice,
    int Quantity,
    decimal DiscountPercent,
    decimal VatPercent);

public sealed record OfferPricingResult(
    decimal LineNetBeforeDiscount,
    decimal DiscountAmount,
    decimal NetTotal,
    decimal VatAmount,
    decimal GrossTotal,
    decimal EffectiveUnitNetPrice);

public static class OfferPricingCalculator
{
    public static OfferPricingResult Calculate(OfferPricingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.UnitNetPrice < 0m)
            throw new ArgumentOutOfRangeException(nameof(input), "Der Netto-Stückpreis darf nicht negativ sein.");
        if (input.Quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(input), "Die Angebotsmenge muss mindestens 1 sein.");
        if (input.DiscountPercent is < 0m or >= 100m)
            throw new ArgumentOutOfRangeException(nameof(input), "Der Rabatt muss zwischen 0 und kleiner 100 Prozent liegen.");
        if (input.VatPercent is < 0m or > 100m)
            throw new ArgumentOutOfRangeException(nameof(input), "Der Umsatzsteuersatz muss zwischen 0 und 100 Prozent liegen.");

        var line = Round(input.UnitNetPrice * input.Quantity);
        var discount = Round(line * input.DiscountPercent / 100m);
        var net = Round(line - discount);
        var vat = Round(net * input.VatPercent / 100m);
        var gross = Round(net + vat);
        return new OfferPricingResult(line, discount, net, vat, gross,
            Round(net / input.Quantity));
    }

    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
