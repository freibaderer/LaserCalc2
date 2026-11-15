using System.IO;
using System.Globalization;
using System.Windows;
using LaserCalc.Domain.Calculation;
using LaserCalc.Domain.Geometry;
using LaserCalc.Domain.Offers;
using LaserCalc.Infrastructure.Export;
using LaserCalc.Infrastructure.Customers;
using LaserCalc.Infrastructure.Offers;
using Microsoft.Win32;

namespace LaserCalc.Desktop.Offers;

public partial class OfferWindow : Window
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private readonly PartGeometry _geometry;
    private readonly CalculationInputs _inputs;
    private readonly CalculationResult _result;
    private readonly string _materialName;
    private readonly OfferSettingsStore _store;
    private readonly CustomerBookStore _customerStore;
    private readonly CustomerOfferPdfExporter _exporter = new();
    private readonly CalculationEngine _calculationEngine = new();
    private readonly OfferSettings _settings;
    private bool _isInitializing = true;
    private bool _isSettingSuggestedPrice;
    private bool _userOverrodeUnitPrice;

    public OfferWindow(PartGeometry geometry, CalculationInputs inputs, CalculationResult result,
        string materialName, OfferSettingsStore store, CustomerBookStore customerStore)
    {
        InitializeComponent();
        _geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        _inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
        _result = result ?? throw new ArgumentNullException(nameof(result));
        _materialName = materialName ?? throw new ArgumentNullException(nameof(materialName));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _customerStore = customerStore ?? throw new ArgumentNullException(nameof(customerStore));
        _settings = _store.Load();
        CustomerPicker.ItemsSource = _customerStore.Load();
        PopulateDefaults();
        _isInitializing = false;
        UpdatePricingPreview();
    }

    private void PopulateDefaults()
    {
        var company = _settings.Company;
        CompanyNameBox.Text = company.CompanyName;
        CompanyStreetBox.Text = company.Street;
        CompanyPostalBox.Text = company.PostalCode;
        CompanyCityBox.Text = company.City;
        CompanyPhoneBox.Text = company.Phone;
        CompanyEmailBox.Text = company.Email;
        CompanyWebsiteBox.Text = company.Website;
        VatIdBox.Text = company.VatId;
        TaxNumberBox.Text = company.TaxNumber;
        BankNameBox.Text = company.BankName;
        IbanBox.Text = company.Iban;
        BicBox.Text = company.Bic;

        var partName = Path.GetFileNameWithoutExtension(_geometry.SourceFile);
        CustomerNameBox.Text = string.Empty;
        SubjectBox.Text = $"Angebot für Laserzuschnitt – {partName}";
        DescriptionBox.Text = $"Laserzuschnitt nach beigestellter Kundendatei.\nTeil: {partName}\nAbmessungen: {_geometry.WidthMm:N2} × {_geometry.HeightMm:N2} mm";
        PartDetailsText.Text = $"{_materialName} · {_inputs.ThicknessMm:N2} mm · {_inputs.Quantity:N0} Stück · Schnittlänge {_geometry.CutLengthMm / 1000.0:N3} m";
        QuantityBox.Text = _inputs.Quantity.ToString(German);
        UnitPriceBox.Text = _result.RecommendedSellingPricePerPart.ToString("N2", German);
        DiscountBox.Text = "0,00";
        VatBox.Text = _settings.DefaultVatPercent.ToString("N2", German);
        OfferDatePicker.SelectedDate = DateTime.Today;
        ValidityDaysBox.Text = _settings.DefaultValidityDays.ToString(German);
        DeliveryBox.Text = _settings.DefaultDeliveryTerms;
        PaymentBox.Text = _settings.DefaultPaymentTerms;
        ClosingTextBox.Text = "Wir freuen uns auf Ihre Auftragserteilung. Für Rückfragen stehen wir Ihnen gerne zur Verfügung.";
        OfferNumberText.Text = _store.PreviewNextNumber();
    }

    private void CustomerPicker_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CustomerPicker.SelectedItem is not CustomerRecord customer) return;
        CustomerNameBox.Text = customer.Name;
        CustomerContactBox.Text = customer.Contact;
        CustomerStreetBox.Text = customer.Street;
        CustomerPostalBox.Text = customer.PostalCode;
        CustomerCityBox.Text = customer.City;
        CustomerEmailBox.Text = customer.Email;
    }

    private void PricingInput_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!_isInitializing) UpdatePricingPreview();
    }

    private void UnitPriceBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (!_isSettingSuggestedPrice) _userOverrodeUnitPrice = true;
        UpdatePricingPreview();
    }

    private void QuantityBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (!_userOverrodeUnitPrice && int.TryParse(QuantityBox.Text, NumberStyles.Integer, German, out var quantity) && quantity > 0)
        {
            var currentCalculation = _calculationEngine.Calculate(_inputs with { Quantity = quantity });
            _isSettingSuggestedPrice = true;
            UnitPriceBox.Text = currentCalculation.RecommendedSellingPricePerPart.ToString("N2", German);
            _isSettingSuggestedPrice = false;
        }
        UpdatePricingPreview();
    }

    private CalculationResult RecalculateForOfferQuantity(int quantity) =>
        _calculationEngine.Calculate(_inputs with { Quantity = quantity });

    private void UpdatePricingPreview()
    {
        if (NetTotalText is null) return;
        try
        {
            var quantity = ReadInt(QuantityBox, "Menge");
            var currentCalculation = RecalculateForOfferQuantity(quantity);
            var result = OfferPricingCalculator.Calculate(new OfferPricingInput(
                ReadDecimal(UnitPriceBox, "Netto-Stückpreis"), quantity,
                ReadDecimal(DiscountBox, "Rabatt"), ReadDecimal(VatBox, "Umsatzsteuer")));
            NetTotalText.Text = "Netto: " + Money(result.NetTotal);
            VatTotalText.Text = "Umsatzsteuer: " + Money(result.VatAmount);
            GrossTotalText.Text = "Brutto: " + Money(result.GrossTotal);
            var priceWarning = result.EffectiveUnitNetPrice < currentCalculation.CostPerPart
                ? " ACHTUNG: Der rabattierte Angebotspreis liegt unter den kalkulierten Selbstkosten."
                : result.EffectiveUnitNetPrice < currentCalculation.MinimumSellingPricePerPart
                    ? " Hinweis: Der rabattierte Angebotspreis liegt unter deinem Mindestpreis."
                    : string.Empty;
            PriceHintText.Text = $"Kalkulation je Stück bei {quantity:N0} Stk.: Selbstkosten {Money(currentCalculation.CostPerPart)} · Mindestpreis {Money(currentCalculation.MinimumSellingPricePerPart)} · Empfehlung {Money(currentCalculation.RecommendedSellingPricePerPart)}. Gewählt nach Rabatt: {Money(result.EffectiveUnitNetPrice)} netto.{priceWarning}";
        }
        catch
        {
            NetTotalText.Text = "Netto: Eingaben prüfen";
            VatTotalText.Text = "Umsatzsteuer: –";
            GrossTotalText.Text = "Brutto: –";
        }
    }

    private void CreateOfferButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var profile = ReadCompanyProfile();
            var offerDate = OfferDatePicker.SelectedDate ?? DateTime.Today;
            var validityDays = ReadInt(ValidityDaysBox, "Gültigkeit");
            if (validityDays < 1 || validityDays > 365) throw new ArgumentOutOfRangeException(nameof(validityDays), "Die Angebotsgültigkeit muss zwischen 1 und 365 Tagen liegen.");

            var customer = Required(CustomerNameBox, "Kunde / Firma");
            var customerStreet = Required(CustomerStreetBox, "Kundenstraße");
            var customerPostal = Required(CustomerPostalBox, "Kunden-PLZ");
            var customerCity = Required(CustomerCityBox, "Kundenort");
            var quantity = ReadInt(QuantityBox, "Menge");
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Die Angebotsmenge muss mindestens 1 sein.");

            var currentCalculation = RecalculateForOfferQuantity(quantity);
            var pricing = OfferPricingCalculator.Calculate(new OfferPricingInput(
                ReadDecimal(UnitPriceBox, "Netto-Stückpreis"), quantity,
                ReadDecimal(DiscountBox, "Rabatt"), ReadDecimal(VatBox, "Umsatzsteuer")));
            if (pricing.EffectiveUnitNetPrice < currentCalculation.CostPerPart)
            {
                var answer = MessageBox.Show(this,
                    $"Der rabattierte Netto-Stückpreis ({Money(pricing.EffectiveUnitNetPrice)}) liegt unter den Selbstkosten ({Money(currentCalculation.CostPerPart)}).\n\nMöchtest du dieses Angebot trotzdem erstellen?",
                    "Angebot unter Selbstkosten", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (answer != MessageBoxResult.Yes) return;
            }
            else if (pricing.EffectiveUnitNetPrice < currentCalculation.MinimumSellingPricePerPart)
            {
                var answer = MessageBox.Show(this,
                    $"Der rabattierte Netto-Stückpreis ({Money(pricing.EffectiveUnitNetPrice)}) liegt unter deinem Mindestpreis ({Money(currentCalculation.MinimumSellingPricePerPart)}).\n\nMöchtest du dieses Angebot trotzdem erstellen?",
                    "Mindestpreis unterschritten", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (answer != MessageBoxResult.Yes) return;
            }

            var safeCustomer = string.Concat(customer.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
            var dialog = new SaveFileDialog
            {
                Title = "Kundenangebot als PDF speichern",
                Filter = "PDF-Datei (*.pdf)|*.pdf",
                AddExtension = true,
                DefaultExt = ".pdf",
                FileName = $"{_store.PreviewNextNumber()}_{safeCustomer}.pdf"
            };
            if (dialog.ShowDialog(this) != true) return;

            var number = _store.ReserveNextNumber(_settings);
            _settings.Company = profile;
            _settings.DefaultValidityDays = validityDays;
            _settings.DefaultVatPercent = ReadDecimal(VatBox, "Umsatzsteuer");
            _settings.DefaultPaymentTerms = PaymentBox.Text.Trim();
            _settings.DefaultDeliveryTerms = DeliveryBox.Text.Trim();
            _store.Save(_settings);

            var offer = new OfferDocument(
                number, offerDate, offerDate.AddDays(validityDays), profile,
                customer, CustomerContactBox.Text.Trim(), customerStreet, customerPostal, customerCity,
                CustomerEmailBox.Text.Trim(), Required(SubjectBox, "Betreff"), Required(DescriptionBox, "Positionsbeschreibung"),
                _materialName, _inputs.ThicknessMm, quantity, ReadDecimal(UnitPriceBox, "Netto-Stückpreis"),
                ReadDecimal(DiscountBox, "Rabatt"), ReadDecimal(VatBox, "Umsatzsteuer"),
                DeliveryBox.Text.Trim(), PaymentBox.Text.Trim(), ClosingTextBox.Text.Trim(),
                currentCalculation.CostPerPart, currentCalculation.MinimumSellingPricePerPart, currentCalculation.RecommendedSellingPricePerPart);
            _exporter.Export(dialog.FileName, offer);
            string? customerBookNotice = null;
            try
            {
                _customerStore.Upsert(new CustomerRecord(
                    customer, CustomerContactBox.Text.Trim(), customerStreet, customerPostal, customerCity,
                    CustomerEmailBox.Text.Trim(), DateTime.Now));
                CustomerPicker.ItemsSource = _customerStore.Load();
            }
            catch (Exception customerStoreError) when (customerStoreError is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
            {
                customerBookNotice = "\n\nHinweis: Das PDF wurde erstellt, aber der Kunde konnte nicht im lokalen Kundenstamm gespeichert werden: " + customerStoreError.Message;
            }

            OfferNumberText.Text = number;
            MessageBox.Show(this, $"Das Kundenangebot {number} wurde erstellt.\n\nNetto: {Money(pricing.NetTotal)}\nBrutto: {Money(pricing.GrossTotal)}\n\n{dialog.FileName}{customerBookNotice}",
                "Angebot erstellt", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Angebot konnte nicht erstellt werden", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private OfferCompanyProfile ReadCompanyProfile() => new()
    {
        CompanyName = Required(CompanyNameBox, "Firmenname"),
        Street = Required(CompanyStreetBox, "Firmenstraße"),
        PostalCode = Required(CompanyPostalBox, "Firmen-PLZ"),
        City = Required(CompanyCityBox, "Firmenort"),
        Phone = CompanyPhoneBox.Text.Trim(),
        Email = CompanyEmailBox.Text.Trim(),
        Website = CompanyWebsiteBox.Text.Trim(),
        VatId = VatIdBox.Text.Trim(),
        TaxNumber = TaxNumberBox.Text.Trim(),
        BankName = BankNameBox.Text.Trim(),
        Iban = IbanBox.Text.Trim(),
        Bic = BicBox.Text.Trim()
    };

    private static string Required(System.Windows.Controls.TextBox box, string label) =>
        string.IsNullOrWhiteSpace(box.Text) ? throw new FormatException($"Bitte '{label}' ausfüllen.") : box.Text.Trim();

    private static decimal ReadDecimal(System.Windows.Controls.TextBox box, string label)
    {
        if (decimal.TryParse(box.Text, NumberStyles.Number, German, out var value)) return value;
        if (decimal.TryParse(box.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)) return value;
        throw new FormatException($"Bitte einen gültigen Zahlenwert für '{label}' eingeben.");
    }

    private static int ReadInt(System.Windows.Controls.TextBox box, string label)
    {
        if (int.TryParse(box.Text, NumberStyles.Integer, German, out var value)) return value;
        throw new FormatException($"Bitte eine ganze Zahl für '{label}' eingeben.");
    }

    private static string Money(decimal value) => value.ToString("C2", German);
    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
