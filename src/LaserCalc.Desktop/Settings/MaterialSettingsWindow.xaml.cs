using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using LaserCalc.Infrastructure.Persistence;

namespace LaserCalc.Desktop.Settings;

public partial class MaterialSettingsWindow : Window
{
    private readonly ObservableCollection<MaterialPreset> _materials;
    public IReadOnlyList<MaterialPreset> Materials => _materials.Select(Clone).ToList().AsReadOnly();

    public MaterialSettingsWindow(IEnumerable<MaterialPreset> materials)
    {
        InitializeComponent();
        _materials = new ObservableCollection<MaterialPreset>(materials.Select(Clone));
        MaterialGrid.ItemsSource = _materials;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        MaterialGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        MaterialGrid.CommitEdit(DataGridEditingUnit.Row, true);

        var errors = new List<string>();
        for (var i = 0; i < _materials.Count; i++)
        {
            var material = _materials[i];
            if (string.IsNullOrWhiteSpace(material.Name)) errors.Add($"Zeile {i + 1}: Werkstoffname fehlt.");
            if (material.PricePerKg < 0m) errors.Add($"Zeile {i + 1}: Materialpreis darf nicht negativ sein.");
            if (material.DensityKgPerM3 <= 0m) errors.Add($"Zeile {i + 1}: Dichte muss größer als null sein.");
            if (material.MachineRatePerHour < 0m) errors.Add($"Zeile {i + 1}: Maschinen-Vollkosten dürfen nicht negativ sein.");
        }

        var duplicateNames = _materials
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicateNames.Count > 0)
            errors.Add("Werkstoffnamen müssen eindeutig sein: " + string.Join(", ", duplicateNames));

        if (_materials.Count == 0) errors.Add("Mindestens ein Werkstoff muss vorhanden sein.");

        if (errors.Count > 0)
        {
            StatusText.Text = string.Join(" ", errors);
            StatusText.Foreground = System.Windows.Media.Brushes.Orange;
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Materialstamm prüfen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        foreach (var material in _materials) material.Name = material.Name.Trim();
        DialogResult = true;
        Close();
    }

    private static MaterialPreset Clone(MaterialPreset source) => new()
    {
        Name = source.Name,
        PricePerKg = source.PricePerKg,
        DensityKgPerM3 = source.DensityKgPerM3,
        MachineRatePerHour = source.MachineRatePerHour
    };

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
