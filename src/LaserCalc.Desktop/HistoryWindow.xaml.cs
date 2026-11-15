using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using LaserCalc.Infrastructure.Export;
using Microsoft.Win32;
using LaserCalc.Infrastructure.Persistence;

namespace LaserCalc.Desktop;

public partial class HistoryWindow : Window
{
    private readonly QuoteHistoryStore _historyStore;
    private readonly QuoteHistoryCsvExporter _csvExporter = new();
    private readonly ObservableCollection<QuoteHistoryEntry> _entries = new();
    private ICollectionView? _view;

    public HistoryWindow(QuoteHistoryStore historyStore)
    {
        InitializeComponent();
        _historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
        HistoryGrid.ItemsSource = _entries;
        _view = CollectionViewSource.GetDefaultView(_entries);
        _view.Filter = FilterEntry;
        HistoryGrid.SelectionChanged += HistoryGrid_SelectionChanged;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var history = await _historyStore.LoadRecentAsync(5000);
            _entries.Clear();
            foreach (var entry in history) _entries.Add(entry);
            _view?.Refresh();
            UpdateResultCount();
        }
        catch (Exception ex)
        {
            CountText.Text = "Historie konnte nicht geladen werden.";
            MessageBox.Show(this, ex.Message, "Kalkulationshistorie", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool FilterEntry(object item)
    {
        if (item is not QuoteHistoryEntry entry) return false;
        var term = SearchBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(term)) return true;
        return Contains(entry.SourceFile, term) || Contains(entry.Material, term) ||
               Contains(entry.Format, term) || Contains(entry.TimeBasis, term) ||
               entry.CreatedAt.ToString("dd.MM.yyyy HH:mm").Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string? source, string term) =>
        source?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _view?.Refresh();
        UpdateResultCount();
    }

    private void UpdateResultCount()
    {
        var visible = HistoryGrid.Items.Count;
        CountText.Text = $"{visible:N0} Treffer · {_entries.Count:N0} geladen";
        ExportCsvButton.IsEnabled = visible > 0;
    }

    private void HistoryGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var selected = HistoryGrid.SelectedItems.OfType<QuoteHistoryEntry>().Take(3).ToList();
        if (selected.Count == 0)
        {
            DetailText.Text = "Eine Zeile auswählen: Details · Zwei Zeilen mit Strg auswählen: Kalkulationen vergleichen.";
            return;
        }
        if (selected.Count == 1)
        {
            var entry = selected[0];
            DetailText.Text = $"Zeitbasis: {entry.TimeBasis} · Schnittlänge: {entry.CutLengthMm:N0} mm · Einstiche: {entry.PierceCount} · Vorschub: {entry.FeedRateMetersPerMinute:N2} m/min · Maschinen-Vollkosten: {entry.MachineRatePerHour:N2} €/h · Material {entry.MaterialCostPerPart:N2} € · Maschine {entry.MachineCostPerPart:N2} € · Rüstanteil {entry.SetupCostPerPart:N2} € je Stück";
            return;
        }
        if (selected.Count > 2)
        {
            DetailText.Text = "Für einen Vergleich bitte genau zwei Kalkulationen auswählen (Strg + Klick).";
            return;
        }

        var ordered = selected.OrderBy(x => x.CreatedAt).ToArray();
        var comparison = QuoteHistoryComparison.Compare(ordered[0], ordered[1]);
        var verdict = comparison.LikelySamePart ? "Werkstück wahrscheinlich identisch" : string.Join(" ", comparison.Warnings);
        DetailText.Text = $"VERGLEICH (neu minus alt) · {verdict} · Δ Selbstkosten {comparison.SelfCostDeltaPerPart:+0.00;-0.00;0.00} € · Δ Mindestpreis {comparison.MinimumPriceDeltaPerPart:+0.00;-0.00;0.00} € · Δ Empfehlung {comparison.RecommendedPriceDeltaPerPart:+0.00;-0.00;0.00} € · Δ Material {comparison.MaterialCostDeltaPerPart:+0.00;-0.00;0.00} € · Δ Maschine {comparison.MachineCostDeltaPerPart:+0.00;-0.00;0.00} € · Δ Zeit {comparison.MachineMinutesDeltaPerPart:+0.000;-0.000;0.000} min";
    }

    private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        var entries = HistoryGrid.Items.OfType<QuoteHistoryEntry>().ToList();
        if (entries.Count == 0)
        {
            MessageBox.Show(this, "Es sind keine Kalkulationen zum Exportieren vorhanden.",
                "CSV-Export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Kalkulationshistorie exportieren",
            Filter = "CSV-Datei für Excel (*.csv)|*.csv",
            AddExtension = true,
            DefaultExt = ".csv",
            FileName = $"LaserCalc-Historie-{DateTime.Now:yyyyMMdd}.csv"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            _csvExporter.Export(dialog.FileName, entries);
            MessageBox.Show(this, $"{entries.Count:N0} Kalkulationen wurden exportiert.",
                "CSV-Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "CSV-Export fehlgeschlagen",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
