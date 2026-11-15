using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using LaserCalc.Domain.Calculation;

namespace LaserCalc.Desktop.Settings;

public partial class CuttingParameterWindow : Window
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private readonly ObservableCollection<CuttingParameterPreset> _parameters;
    public IReadOnlyList<CuttingParameterPreset> Parameters => _parameters.ToList().AsReadOnly();

    public CuttingParameterWindow(IEnumerable<CuttingParameterPreset> parameters)
    {
        InitializeComponent();
        _parameters = new ObservableCollection<CuttingParameterPreset>(parameters.Select(Clone));
        ParameterGrid.ItemsSource = _parameters;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        _parameters.Add(new CuttingParameterPreset
        {
            MaterialName = "Stahl",
            ThicknessMm = 1m,
            FeedRateMetersPerMinute = 0m,
            PierceSeconds = 0m
        });
        ParameterGrid.SelectedIndex = _parameters.Count - 1;
        ParameterGrid.ScrollIntoView(ParameterGrid.SelectedItem);
        ParameterGrid.BeginEdit();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (ParameterGrid.SelectedItem is not CuttingParameterPreset selected) return;
        _parameters.Remove(selected);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ParameterGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        ParameterGrid.CommitEdit(DataGridEditingUnit.Row, true);
        var errors = new List<string>();
        for (var i = 0; i < _parameters.Count; i++)
        {
            var item = _parameters[i];
            if (string.IsNullOrWhiteSpace(item.MaterialName)) errors.Add($"Zeile {i + 1}: Werkstoff fehlt.");
            if (item.ThicknessMm <= 0m) errors.Add($"Zeile {i + 1}: Blechdicke muss größer als null sein.");
            if (item.FeedRateMetersPerMinute <= 0m) errors.Add($"Zeile {i + 1}: Vorschub muss größer als null sein.");
            if (item.PierceSeconds < 0m) errors.Add($"Zeile {i + 1}: Einstichzeit darf nicht negativ sein.");
        }

        var duplicates = _parameters
            .GroupBy(x => (x.MaterialName.Trim().ToUpperInvariant(), Math.Round(x.ThicknessMm, 2)))
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicates.Count > 0) errors.Add("Doppelte Werkstoff-/Dickenkombinationen gefunden. Jede Kombination darf nur einmal vorkommen.");

        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Schnittparameter prüfen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }

    private static CuttingParameterPreset Clone(CuttingParameterPreset source) => new()
    {
        MaterialName = source.MaterialName,
        ThicknessMm = source.ThicknessMm,
        FeedRateMetersPerMinute = source.FeedRateMetersPerMinute,
        PierceSeconds = source.PierceSeconds
    };

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
