using LaserCalc.Domain.Offers;

namespace LaserCalc.Infrastructure.Offers;

public sealed class OfferCompanyProfile
{
    public string CompanyName { get; set; } = "Mages mechanische Fertigung GmbH";
    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string VatId { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string Bic { get; set; } = string.Empty;
}

public sealed class OfferSettings
{
    public OfferCompanyProfile Company { get; set; } = new();
    public int SequenceYear { get; set; } = DateTime.Now.Year;
    public int NextSequenceNumber { get; set; } = 1;
    public int DefaultValidityDays { get; set; } = 14;
    public decimal DefaultVatPercent { get; set; } = 19m;
    public string DefaultPaymentTerms { get; set; } = "14 Tage netto";
    public string DefaultDeliveryTerms { get; set; } = "Liefertermin nach Vereinbarung";
}

public sealed record OfferDocument(
    string OfferNumber,
    DateTime OfferDate,
    DateTime ValidUntil,
    OfferCompanyProfile Company,
    string CustomerName,
    string CustomerContact,
    string CustomerStreet,
    string CustomerPostalCode,
    string CustomerCity,
    string CustomerEmail,
    string Subject,
    string PositionDescription,
    string Material,
    decimal ThicknessMm,
    int Quantity,
    decimal UnitNetPrice,
    decimal DiscountPercent,
    decimal VatPercent,
    string DeliveryTerms,
    string PaymentTerms,
    string ClosingText,
    decimal SelfCostPerPart,
    decimal MinimumPricePerPart,
    decimal RecommendedPricePerPart)
{
    public OfferPricingResult Pricing => OfferPricingCalculator.Calculate(
        new OfferPricingInput(UnitNetPrice, Quantity, DiscountPercent, VatPercent));
}
