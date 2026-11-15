using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using LaserCalc.Domain.Calculation;
using LaserCalc.Domain.Geometry;
using LaserCalc.Infrastructure.Import;
using LaserCalc.Infrastructure.Export;
using LaserCalc.Infrastructure.Persistence;
using LaserCalc.Infrastructure.Offers;
using LaserCalc.Infrastructure.Customers;
using LaserCalc.Desktop.Offers;
using LaserCalc.Desktop.Settings;
using Microsoft.Win32;

namespace LaserCalc.Desktop;

public partial class MainWindow : Window
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private readonly TruTopsClassicGeoImporter _geoImporter = new();
    private readonly AsciiDxfImporter _dxfImporter = new();
    private readonly CalculationEngine _calculationEngine = new();
    private readonly CalculationPdfExporter _pdfExporter = new();
    private readonly AppSettingsStore _settingsStore = new();
    private readonly AppSettings _settings;
    private readonly QuoteHistoryStore _historyStore;
    private readonly OfferSettingsStore _offerSettingsStore;
    private readonly CustomerBookStore _customerStore;
    private readonly AppDataBackupService _backupService;
    private PartGeometry? _geometry;
    private PartGeometry? _customerDxfGeometry;
    private PartGeometry? _truTopsGeoGeometry;
    private CalculationInputs? _lastInputs;
    private CalculationResult? _lastResult;
    private string? _lastMaterialName;
    private bool _cuttingPresetApplied;
    private bool _isInitializing = true;

    public MainWindow()
    {
        InitializeComponent();
        _settings = _settingsStore.Load();
        _historyStore = new QuoteHistoryStore(_settingsStore.DataDirectory);
        _offerSettingsStore = new OfferSettingsStore(_settingsStore.DataDirectory);
        _customerStore = new CustomerBookStore(_settingsStore.DataDirectory);
        _backupService = new AppDataBackupService(_settingsStore.DataDirectory);

        MaterialCombo.ItemsSource = _settings.Materials;
        MaterialCombo.SelectedIndex = 0;
        BorderBox.Text = _settings.MaterialBorderMm.ToString(German);
        FeedRateBox.Text = _settings.DefaultFeedRateMetersPerMinute.ToString(German);
        PierceSecondsBox.Text = _settings.PierceSeconds.ToString(German);
        SetupMinutesBox.Text = _settings.DefaultSetupMinutes.ToString(German);
        MinimumMarginBox.Text = _settings.MinimumMarginPercent.ToString(German);
        TargetMarginBox.Text = _settings.TargetMarginPercent.ToString(German);
        _isInitializing = false;
    }

    private void HistoryButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new HistoryWindow(_historyStore) { Owner = this };
        window.ShowDialog();
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Laserteil-Datei auswählen",
            Filter = "LaserCalc-Dateien (*.geo;*.dxf)|*.geo;*.dxf|TruTops GEO (*.geo)|*.geo|Kunden-DXF (*.dxf)|*.dxf|Alle Dateien (*.*)|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) == true) ImportFiles(dialog.FileNames);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            ImportFiles(files);
    }

    private void ImportFiles(IEnumerable<string> paths)
    {
        try
        {
            _customerDxfGeometry = null;
            _truTopsGeoGeometry = null;
            _lastInputs = null;
            _lastResult = null;
            _lastMaterialName = null;
            ResultsPanel.Visibility = Visibility.Collapsed;
            ExportPdfButton.IsEnabled = false;
            CreateOfferButton.IsEnabled = false;
            var importedFiles = 0;
            foreach (var path in paths.Where(File.Exists))
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension == ".geo")
                {
                    if (_truTopsGeoGeometry is not null)
                        throw new InvalidOperationException("Bitte nur eine GEO-Datei pro Kalkulation auswählen.");
                    _truTopsGeoGeometry = _geoImporter.Import(path);
                }
                else if (extension == ".dxf")
                {
                    if (_customerDxfGeometry is not null)
                        throw new InvalidOperationException("Bitte nur eine DXF-Datei pro Kalkulation auswählen.");
                    _customerDxfGeometry = _dxfImporter.Import(path);
                }
                else continue;
                importedFiles++;
            }
            if (importedFiles == 0) throw new NotSupportedException("Bitte mindestens eine .GEO- oder .DXF-Datei auswählen.");

            // When both sources are supplied, TruTops GEO is the active costing source;
            // the customer's original DXF is retained as an independent geometry check.
            _geometry = _truTopsGeoGeometry ?? _customerDxfGeometry;
            if (_geometry is null) throw new InvalidOperationException("Keine Geometrie konnte importiert werden.");

            ImportStatusText.Text = importedFiles > 1 ? "QUELLEN IMPORTIERT" : "IMPORT ERFOLGREICH";
            ImportStatusText.Foreground = Brushes.LightGreen;
            SourceFileText.Text = _customerDxfGeometry is not null && _truTopsGeoGeometry is not null
                ? $"DXF: {_customerDxfGeometry.SourceFile}\nGEO: {_truTopsGeoGeometry.SourceFile} (aktive Kalkulationsquelle)"
                : $"{_geometry.SourceFile}  ·  {_geometry.Format}";
            SourceCompareText.Text = CompareSources();
            PartNameText.Text = Path.GetFileNameWithoutExtension(_geometry.SourceFile);
            DimensionsText.Text = $"{_geometry.WidthMm:N2} × {_geometry.HeightMm:N2} mm";
            CutLengthText.Text = $"{_geometry.CutLengthMm / 1000.0:N3} m";
            AreaText.Text = _geometry.NetAreaMm2.HasValue ? $"{_geometry.NetAreaMm2.Value:N2} mm²" : "Nicht ermittelt";
            ContoursText.Text = $"{_geometry.ContourCount} / {_geometry.EstimatedPierceCount}";
            PreviewCaption.Text = $"{_geometry.Primitives.Count:N0} Geometrieelemente · {_geometry.PointCount:N0} Polylinienpunkte / GEO-Punkte";
            RenderPreview(_geometry);

            var sourceComparison = _customerDxfGeometry is not null && _truTopsGeoGeometry is not null
                ? GeometrySourceComparer.Compare(_customerDxfGeometry, _truTopsGeoGeometry)
                : null;
            IReadOnlyList<string> sourcePairWarnings = sourceComparison?.Warnings ?? Array.Empty<string>();
            var sourcePairIsConsistent = sourceComparison?.IsConsistent ?? true;
            var hasSourcePair = _customerDxfGeometry is not null && _truTopsGeoGeometry is not null;
            var pairVerified = hasSourcePair && sourcePairIsConsistent;
            var requiresManualGeometryCheck = (_geometry.RequiresAreaVerification || _customerDxfGeometry?.RequiresUnitVerification == true) && !pairVerified && !hasSourcePair;
            ManualGeometryCheck.IsChecked = false;
            ManualGeometryCheck.Visibility = requiresManualGeometryCheck ? Visibility.Visible : Visibility.Collapsed;
            CalculateButton.IsEnabled = _geometry.IsSafeForPreliminaryCalculation && sourcePairIsConsistent && !requiresManualGeometryCheck;
            ResultsPanel.Visibility = Visibility.Collapsed;
            ExportPdfButton.IsEnabled = false;
            var allWarnings = GetImportWarnings(pairVerified);
            if (hasSourcePair)
                allWarnings.AddRange(sourcePairWarnings);
            if (requiresManualGeometryCheck)
                allWarnings.Add("Vor der Kalkulation bitte die Geometriefläche bzw. DXF-Einheit anhand der Kundenzeichnung prüfen und den Prüfhinweis bestätigen.");
            StatusText.Text = allWarnings.Count == 0
                ? "Import abgeschlossen. Geometrie prüfen und anschließend kalkulieren."
                : string.Join(Environment.NewLine, allWarnings.Distinct());
            if (!_geometry.IsSafeForPreliminaryCalculation)
                StatusText.Text += Environment.NewLine + "HINWEIS: Offene Konturen, fehlende Fläche oder nicht unterstützte Geometrie. Automatische Kalkulation gesperrt.";
            else if (!sourcePairIsConsistent)
                StatusText.Text += Environment.NewLine + "HINWEIS: DXF und GEO unterscheiden sich. Bitte Quellen einzeln prüfen, bevor kalkuliert wird.";
        }
        catch (Exception ex)
        {
            _geometry = null;
            _lastInputs = null;
            _lastResult = null;
            _lastMaterialName = null;
            ResultsPanel.Visibility = Visibility.Collapsed;
            ExportPdfButton.IsEnabled = false;
            CreateOfferButton.IsEnabled = false;
            CalculateButton.IsEnabled = false;
            ManualGeometryCheck.IsChecked = false;
            ManualGeometryCheck.Visibility = Visibility.Collapsed;
            ImportStatusText.Text = "IMPORT FEHLGESCHLAGEN";
            ImportStatusText.Foreground = Brushes.OrangeRed;
            StatusText.Text = ex.Message;
            MessageBox.Show(this, ex.Message, "LaserCalc – Importfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private List<string> GetImportWarnings(bool pairVerified)
    {
        var warnings = new List<string>();
        if (_geometry is not null) warnings.AddRange(FormatGeometryWarnings(_geometry, pairVerified));
        if (_customerDxfGeometry is not null && _customerDxfGeometry != _geometry)
            warnings.AddRange(FormatGeometryWarnings(_customerDxfGeometry, pairVerified));
        if (_truTopsGeoGeometry is not null && _truTopsGeoGeometry != _geometry)
            warnings.AddRange(FormatGeometryWarnings(_truTopsGeoGeometry, pairVerified));
        return warnings.Distinct().ToList();
    }

    private static List<string> FormatGeometryWarnings(PartGeometry geometry, bool pairVerified)
    {
        var warnings = geometry.Warnings.ToList();
        if (pairVerified && geometry.RequiresAreaVerification)
        {
            for (var i = 0; i < warnings.Count; i++)
                if (warnings[i].Contains("Die Fläche muss anhand der Kundendatei oder Zeichnung geprüft werden.", StringComparison.Ordinal))
                    warnings[i] = warnings[i].Replace("Die Fläche muss anhand der Kundendatei oder Zeichnung geprüft werden.", "Die aus der Geometrie rekonstruierte Fläche wurde mit der zugehörigen Kunden-DXF abgeglichen; der gespeicherte GEO-Metadatenwert weicht weiterhin ab.", StringComparison.Ordinal);
        }
        if (pairVerified && geometry.RequiresUnitVerification)
        {
            for (var i = 0; i < warnings.Count; i++)
                if (warnings[i].Contains("Maße werden vorläufig als Millimeter interpretiert", StringComparison.Ordinal))
                    warnings[i] = warnings[i].Replace("DXF enthält keine eindeutige Einheitenangabe. Maße werden vorläufig als Millimeter interpretiert; bitte vor dem Angebot prüfen.", "DXF-Einheit ist nicht eingetragen; die Maßeinheit wurde durch den Abgleich mit der zugehörigen GEO-Datei bestätigt.", StringComparison.Ordinal);
        }
        return warnings;
    }

    private string CompareSources()
    {
        if (_customerDxfGeometry is null || _truTopsGeoGeometry is null)
            return "DXF und GEO gemeinsam auswählen, um beide Quellen direkt abzugleichen.";
        var comparison = GeometrySourceComparer.Compare(_customerDxfGeometry, _truTopsGeoGeometry);
        return comparison.Summary + (comparison.IsConsistent ? " · innerhalb der Prüftoleranzen" : " · Abweichung prüfen");
    }

    private void ManualGeometryCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_geometry is null) return;
        InvalidateCalculation();
        var pairVerified = _customerDxfGeometry is not null && _truTopsGeoGeometry is not null &&
            GeometrySourceComparer.Compare(_customerDxfGeometry, _truTopsGeoGeometry).IsConsistent;
        var requiresManualCheck = _geometry.RequiresAreaVerification || _customerDxfGeometry?.RequiresUnitVerification == true;
        var areaVerified = !requiresManualCheck || pairVerified || ManualGeometryCheck.IsChecked == true;
        var pairOkay = _customerDxfGeometry is null || _truTopsGeoGeometry is null || pairVerified;
        CalculateButton.IsEnabled = _geometry.IsSafeForPreliminaryCalculation && areaVerified && pairOkay;
    }

    private void MaterialCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (MachineRateBox is null || MaterialCombo.SelectedItem is not MaterialPreset material) return;
        MachineRateBox.Text = material.MachineRatePerHour.ToString(German);
        PricePerKgBox.Text = material.PricePerKg.ToString(German);
        DensityBox.Text = material.DensityKgPerM3.ToString("N0", German);
        ApplyCuttingParameterPreset();
        InvalidateCalculation();
    }

    private void ThicknessBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ApplyCuttingParameterPreset();
        InvalidateCalculation();
    }

    private void CalculationInputs_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) => InvalidateCalculation();

    private void InvalidateCalculation()
    {
        if (_isInitializing || _lastResult is null) return;
        _lastInputs = null;
        _lastResult = null;
        _lastMaterialName = null;
        ExportPdfButton.IsEnabled = false;
        CreateOfferButton.IsEnabled = false;
        ResultsPanel.Visibility = Visibility.Collapsed;
        StatusText.Text = "Kalkulationsdaten geändert – bitte erneut kalkulieren. Das vorherige Ergebnis wurde gesperrt.";
    }

    private void ApplyCuttingParameterPreset()
    {
        if (FeedRateBox is null || PierceSecondsBox is null || ThicknessBox is null ||
            MaterialCombo?.SelectedItem is not MaterialPreset material ||
            !decimal.TryParse(ThicknessBox.Text, NumberStyles.Number, German, out var thickness))
            return;

        var preset = CuttingParameterResolver.Find(_settings.CuttingParameters ?? new List<CuttingParameterPreset>(), material.Name, thickness);
        if (preset is null)
        {
            if (_cuttingPresetApplied)
            {
                FeedRateBox.Text = _settings.DefaultFeedRateMetersPerMinute.ToString(German);
                PierceSecondsBox.Text = _settings.PierceSeconds.ToString(German);
                _cuttingPresetApplied = false;
            }
            if (CuttingPresetStatus is not null)
                CuttingPresetStatus.Text = "Kein Dicken-Kennwert hinterlegt – allgemeiner Startwert wird verwendet.";
            return;
        }

        FeedRateBox.Text = preset.FeedRateMetersPerMinute.ToString(German);
        PierceSecondsBox.Text = preset.PierceSeconds.ToString(German);
        _cuttingPresetApplied = true;
        if (CuttingPresetStatus is not null)
            CuttingPresetStatus.Text = $"Kennwert geladen: {preset.MaterialName} · {preset.ThicknessMm:N2} mm";
    }

    private void CuttingParametersButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new CuttingParameterWindow(_settings.CuttingParameters ?? new List<CuttingParameterPreset>())
        {
            Owner = this
        };
        if (window.ShowDialog() != true) return;
        _settings.CuttingParameters = window.Parameters.Select(x => new CuttingParameterPreset
        {
            MaterialName = x.MaterialName,
            ThicknessMm = x.ThicknessMm,
            FeedRateMetersPerMinute = x.FeedRateMetersPerMinute,
            PierceSeconds = x.PierceSeconds
        }).ToList();
        _settingsStore.Save(_settings);
        ApplyCuttingParameterPreset();
        StatusText.Text = $"Schnittparameter gespeichert: {_settings.CuttingParameters.Count:N0} Werkstoff-/Dickenkombinationen.";
    }

    private void MaterialsButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedName = (MaterialCombo.SelectedItem as MaterialPreset)?.Name;
        var window = new MaterialSettingsWindow(_settings.Materials) { Owner = this };
        if (window.ShowDialog() != true) return;

        _settings.Materials = window.Materials.Select(x => new MaterialPreset
        {
            Name = x.Name,
            PricePerKg = x.PricePerKg,
            DensityKgPerM3 = x.DensityKgPerM3,
            MachineRatePerHour = x.MachineRatePerHour
        }).ToList();
        _settingsStore.Save(_settings);

        MaterialCombo.ItemsSource = null;
        MaterialCombo.ItemsSource = _settings.Materials;
        MaterialCombo.SelectedItem = _settings.Materials.FirstOrDefault(x =>
            string.Equals(x.Name, selectedName, StringComparison.OrdinalIgnoreCase)) ?? _settings.Materials.First();
        StatusText.Text = $"Materialstamm gespeichert: {_settings.Materials.Count:N0} Werkstoffe. Preise, Dichten und interne Vollkosten sind aktualisiert.";
    }

    private void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "LaserCalc-Datensicherung speichern",
            Filter = "LaserCalc-Datensicherung (*.zip)|*.zip",
            AddExtension = true,
            DefaultExt = ".zip",
            FileName = $"LaserCalc2-Datensicherung-{DateTime.Now:yyyyMMdd-HHmm}.zip"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            _backupService.CreateBackup(dialog.FileName);
            StatusText.Text = $"Datensicherung erstellt: {dialog.FileName}";
            MessageBox.Show(this, "Materialstamm, Einstellungen, Kunden und Kalkulationshistorie wurden gesichert.",
                "Datensicherung erstellt", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Datensicherung fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "LaserCalc-Datensicherung auswählen",
            Filter = "LaserCalc-Datensicherung (*.zip)|*.zip",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true) return;

        var confirmation = MessageBox.Show(this,
            "Die ausgewählten LaserCalc-Daten ersetzen die aktuellen Einstellungen, Materialstammdaten, Kunden und Historie. Vorhandene Daten werden zunächst automatisch gesichert.\n\nFortfahren?",
            "Datensicherung wiederherstellen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes) return;

        try
        {
            var result = _backupService.RestoreBackup(dialog.FileName);
            var safety = string.IsNullOrWhiteSpace(result.SafetyBackupPath)
                ? "Es war kein vorheriger Datenstand vorhanden."
                : $"Vorheriger Datenstand: {result.SafetyBackupPath}";
            MessageBox.Show(this,
                $"Wiederhergestellt: {string.Join(", ", result.RestoredFiles)}\n\n{ safety }\n\nLaserCalc wird neu gestartet, damit alle Einstellungen sauber neu geladen werden.",
                "Wiederherstellung erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);

            var executable = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executable) &&
                string.Equals(Path.GetFileName(executable), "LaserCalc.exe", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
                Application.Current.Shutdown();
            }
            else
            {
                MessageBox.Show(this, "Bitte LaserCalc schließen und erneut starten, damit die wiederhergestellten Daten geladen werden.",
                    "Neustart erforderlich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Wiederherstellung fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CalculateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_geometry is null)
        {
            MessageBox.Show(this, "Bitte zuerst eine DXF- oder GEO-Datei importieren.", "Keine Geometrie", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!_geometry.IsSafeForPreliminaryCalculation)
        {
            MessageBox.Show(this, "Die Geometrieprüfung ist nicht bestanden. Bitte offene Konturen, fehlende Flächen oder nicht unterstützte Elemente beheben, bevor ein Preis berechnet wird.", "Kalkulation gesperrt", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var hasSourcePair = _customerDxfGeometry is not null && _truTopsGeoGeometry is not null;
        var pairAlreadyVerifiesSource = hasSourcePair &&
            GeometrySourceComparer.Compare(_customerDxfGeometry!, _truTopsGeoGeometry!).IsConsistent;
        if (hasSourcePair && !pairAlreadyVerifiesSource)
        {
            MessageBox.Show(this, "DXF und GEO unterscheiden sich außerhalb der Prüftoleranzen. Bitte beide Dateien einzeln prüfen oder die Ursache beheben.", "Quellenabweichung", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var requiresManualCheck = _geometry.RequiresAreaVerification || _customerDxfGeometry?.RequiresUnitVerification == true;
        if (requiresManualCheck && !pairAlreadyVerifiesSource && ManualGeometryCheck.IsChecked != true)
        {
            MessageBox.Show(this, "Die Flächen- oder Einheitenprüfung ist noch offen. Bitte die passende DXF/GEO-Datei gemeinsam importieren oder die Werte anhand der Zeichnung prüfen und den Prüfhinweis bestätigen.", "Geometrieprüfung erforderlich", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var material = (MaterialPreset?)MaterialCombo.SelectedItem ?? throw new InvalidOperationException("Bitte ein Material auswählen.");
            var inputs = new CalculationInputs(
                ReadDecimal(PricePerKgBox, "Materialpreis"),
                ReadDecimal(DensityBox, "Werkstoffdichte"),
                ReadDecimal(ThicknessBox, "Blechdicke"),
                ToDecimal(_geometry.WidthMm),
                ToDecimal(_geometry.HeightMm),
                _geometry.NetAreaMm2.HasValue ? ToDecimal(_geometry.NetAreaMm2.Value) : null,
                ToDecimal(_geometry.CutLengthMm),
                _geometry.EstimatedPierceCount,
                ReadDecimal(FeedRateBox, "Vorschub"),
                ReadDecimal(MachineRateBox, "interne Maschinen-Vollkosten pro Stunde"),
                ReadDecimal(SetupMinutesBox, "Rüstzeit"),
                ReadInt(QuantityBox, "Stückzahl"),
                ReadDecimal(MinimumMarginBox, "Mindestmarge"),
                ReadDecimal(TargetMarginBox, "Zielmarge"),
                ReadDecimal(BorderBox, "Materialrand"),
                ReadDecimal(PierceSecondsBox, "Einstichzeit"),
                string.IsNullOrWhiteSpace(ActualMachineMinutesBox.Text) ? null : ReadDecimal(ActualMachineMinutesBox, "Bestätigte Maschinenzeit"));

            var result = _calculationEngine.Calculate(inputs);
            _lastInputs = inputs;
            _lastResult = result;
            _lastMaterialName = material.Name;
            ExportPdfButton.IsEnabled = true;
            CreateOfferButton.IsEnabled = true;
            CostResultText.Text = Money(result.CostPerPart);
            MinimumResultText.Text = Money(result.MinimumSellingPricePerPart);
            RecommendedResultText.Text = Money(result.RecommendedSellingPricePerPart);
            BlankWeightText.Text = $"{result.BlankWeightKg:N3} kg";
            FinishedWeightText.Text = result.FinishedPartWeightKg.HasValue ? $"{result.FinishedPartWeightKg.Value:N3} kg" : "–";
            MaterialUtilizationText.Text = result.MaterialUtilizationPercent.HasValue
                ? $"Materialausnutzung: {result.MaterialUtilizationPercent.Value:N1} % · Verschnittfläche im Rohteilrechteck: {result.ScrapAreaMm2:N0} mm²"
                : "Materialausnutzung nicht ermittelt (Nettoteilfläche fehlt).";
            TimeBasisText.Text = $"Zeitbasis: {result.TimeBasis} · Schnitt-/Maschinenzeit: {result.CuttingMinutesPerPart:N2} min";
            CostBreakdownText.Text = $"Material {Money(result.MaterialCostPerPart)}  +  Maschine {Money(result.MachineCostPerPart)}  +  Rüstanteil {Money(result.SetupCostPerPart)} je Stück";
            OrderTotalsText.Text = $"Auftrag ({inputs.Quantity} Stück): Selbstkosten {Money(result.OrderCostTotal)}  ·  Mindestpreis {Money(result.MinimumSellingPriceTotal)}  ·  Empfehlung {Money(result.RecommendedSellingPriceTotal)}";
            var displayWarnings = GetImportWarnings(pairAlreadyVerifiesSource).Concat(result.Warnings).Distinct().ToList();
            if ((_geometry.RequiresAreaVerification || _customerDxfGeometry?.RequiresUnitVerification == true) && ManualGeometryCheck.IsChecked == true)
                displayWarnings.Add("Geometrie- / Einheitenabweichung wurde manuell anhand der Zeichnung geprüft und bestätigt.");
            ResultWarningsText.Text = string.Join("  •  ", displayWarnings);
            ResultsPanel.Visibility = Visibility.Visible;

            material.PricePerKg = inputs.MaterialPricePerKg;
            material.DensityKgPerM3 = inputs.DensityKgPerM3;
            material.MachineRatePerHour = inputs.MachineRatePerHour;
            _settings.MaterialBorderMm = inputs.MaterialBorderMm;
            if (!_cuttingPresetApplied)
            {
                _settings.DefaultFeedRateMetersPerMinute = inputs.FeedRateMetersPerMinute;
                _settings.PierceSeconds = inputs.PierceSeconds;
            }
            _settings.DefaultSetupMinutes = inputs.SetupMinutes;
            _settings.MinimumMarginPercent = inputs.MinimumMarginPercent;
            _settings.TargetMarginPercent = inputs.TargetMarginPercent;
            _settingsStore.Save(_settings);

            await _historyStore.AppendAsync(new QuoteHistoryEntry(
                DateTime.Now, _geometry.SourceFile, _geometry.Format, material.Name,
                inputs.MaterialPricePerKg, inputs.DensityKgPerM3, inputs.ThicknessMm, inputs.Quantity,
                inputs.BoundingWidthMm, inputs.BoundingHeightMm, inputs.NetPartAreaMm2,
                _geometry.ReportedNetAreaMm2.HasValue ? ToDecimal(_geometry.ReportedNetAreaMm2.Value) : null,
                inputs.ContourLengthMm, inputs.EstimatedPierceCount, inputs.FeedRateMetersPerMinute,
                inputs.MachineRatePerHour, inputs.SetupMinutes, inputs.MaterialBorderMm, inputs.PierceSeconds,
                inputs.ActualOrConfirmedMachineMinutes, result.MaterialCostPerPart, result.MachineCostPerPart,
                result.SetupCostPerPart, result.CuttingMinutesPerPart, result.CostPerPart,
                result.MinimumSellingPricePerPart, result.RecommendedSellingPricePerPart, result.TimeBasis,
                pairAlreadyVerifiesSource, ManualGeometryCheck.IsChecked == true));

            StatusText.Text = $"Kalkulation gespeichert. Maschinenstundensatz als interne Vollkosten angesetzt. Historie: {_settingsStore.DataDirectory}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Kalkulationsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }


    private void CreateOfferButton_Click(object sender, RoutedEventArgs e)
    {
        if (_geometry is null || _lastInputs is null || _lastResult is null || string.IsNullOrWhiteSpace(_lastMaterialName))
        {
            MessageBox.Show(this, "Bitte zuerst eine Geometrie importieren und eine Kalkulation durchführen.",
                "Kein Kalkulationsergebnis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new OfferWindow(_geometry, _lastInputs, _lastResult, _lastMaterialName, _offerSettingsStore, _customerStore)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
    {
        if (_geometry is null || _lastInputs is null || _lastResult is null || string.IsNullOrWhiteSpace(_lastMaterialName))
        {
            MessageBox.Show(this, "Bitte zuerst eine Kalkulation durchführen.", "Kein Kalkulationsergebnis", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var safeName = string.Concat(Path.GetFileNameWithoutExtension(_geometry.SourceFile)
            .Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        var dialog = new SaveFileDialog
        {
            Title = "Kalkulationsblatt als PDF speichern",
            Filter = "PDF-Datei (*.pdf)|*.pdf",
            AddExtension = true,
            DefaultExt = ".pdf",
            FileName = $"LaserCalc_{safeName}_{DateTime.Now:yyyyMMdd}.pdf"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            _pdfExporter.Export(dialog.FileName, _geometry, _lastInputs, _lastResult, _lastMaterialName);
            StatusText.Text = $"PDF-Kalkulationsblatt gespeichert: {dialog.FileName}";
            MessageBox.Show(this, "Das Kalkulationsblatt wurde als PDF gespeichert.", "PDF-Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "PDF-Export fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RenderPreview(PartGeometry geometry)
    {
        PreviewCanvas.Children.Clear();
        var samples = geometry.Primitives.SelectMany(p => p.Sample(3.0)).ToList();
        if (samples.Count == 0) return;
        var minX = samples.Min(p => p.X); var maxX = samples.Max(p => p.X);
        var minY = samples.Min(p => p.Y); var maxY = samples.Max(p => p.Y);
        var width = Math.Max(0.001, maxX - minX); var height = Math.Max(0.001, maxY - minY);
        var canvasWidth = PreviewCanvas.Width; var canvasHeight = PreviewCanvas.Height;
        var scale = Math.Min((canvasWidth - 36) / width, (canvasHeight - 36) / height);
        var offsetX = (canvasWidth - width * scale) / 2.0;
        var offsetY = (canvasHeight - height * scale) / 2.0;

        foreach (var primitive in geometry.Primitives)
        {
            var pts = primitive.Sample(3.0);
            if (pts.Count < 2) continue;
            var polyline = new Polyline
            {
                Stroke = LayerBrush(primitive.Layer),
                StrokeThickness = 1.15,
                SnapsToDevicePixels = true,
                StrokeLineJoin = PenLineJoin.Round
            };
            foreach (var p in pts)
                polyline.Points.Add(new Point(offsetX + (p.X - minX) * scale, canvasHeight - offsetY - (p.Y - minY) * scale));
            PreviewCanvas.Children.Add(polyline);
        }
    }

    private static Brush LayerBrush(string layer)
    {
        var name = layer.ToUpperInvariant();
        if (name.Contains("INTERIOR") || name.Contains("INNER") || name.Contains("HOLE") || name.Contains("INNEN"))
            return new SolidColorBrush(Color.FromRgb(77, 171, 230));
        if (name.Contains("OUTER") || name.Contains("EXTERIOR") || name.Contains("AUSSEN"))
            return new SolidColorBrush(Color.FromRgb(244, 247, 250));
        return new SolidColorBrush(Color.FromRgb(123, 190, 235));
    }

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

    private static decimal ToDecimal(double value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    private static string Money(decimal value) => value.ToString("C2", German);
}
