using System.IO;

namespace LaserCalc.Infrastructure.Persistence;

/// <summary>Compares two stored snapshots; all deltas are newer minus older.</summary>
public sealed record QuoteHistoryComparisonResult(
    bool LikelySamePart,
    decimal SelfCostDeltaPerPart,
    decimal MinimumPriceDeltaPerPart,
    decimal RecommendedPriceDeltaPerPart,
    decimal MaterialCostDeltaPerPart,
    decimal MachineCostDeltaPerPart,
    decimal MachineMinutesDeltaPerPart,
    decimal CutLengthDeltaMm,
    IReadOnlyList<string> Warnings);

public static class QuoteHistoryComparison
{
    public static QuoteHistoryComparisonResult Compare(QuoteHistoryEntry older, QuoteHistoryEntry newer)
    {
        ArgumentNullException.ThrowIfNull(older);
        ArgumentNullException.ThrowIfNull(newer);

        var warnings = new List<string>();
        var oldName = Path.GetFileNameWithoutExtension(older.SourceFile);
        var newName = Path.GetFileNameWithoutExtension(newer.SourceFile);
        var sameName = string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase);
        var sameMaterial = string.Equals(older.Material, newer.Material, StringComparison.OrdinalIgnoreCase);
        var sameThickness = Math.Abs(older.ThicknessMm - newer.ThicknessMm) <= 0.01m;
        var sameOrientation = Math.Abs(older.WidthMm - newer.WidthMm) <= 0.1m &&
                              Math.Abs(older.HeightMm - newer.HeightMm) <= 0.1m;
        var rotated = Math.Abs(older.WidthMm - newer.HeightMm) <= 0.1m &&
                      Math.Abs(older.HeightMm - newer.WidthMm) <= 0.1m;
        var sameArea = older.NetAreaMm2.HasValue && newer.NetAreaMm2.HasValue &&
                       Math.Abs(older.NetAreaMm2.Value - newer.NetAreaMm2.Value) <= 1.0m;
        var sameLength = Math.Abs(older.CutLengthMm - newer.CutLengthMm) <= 0.5m;
        var likelySame = sameName && sameMaterial && sameThickness && (sameOrientation || rotated) && sameArea && sameLength;

        if (!sameName) warnings.Add("Dateinamen unterscheiden sich; identisches Werkstück ist nicht bestätigt.");
        if (!sameMaterial) warnings.Add("Werkstoffe unterscheiden sich.");
        if (!sameThickness) warnings.Add("Blechdicken unterscheiden sich.");
        if (!sameOrientation && !rotated) warnings.Add("Außenmaße unterscheiden sich.");
        if (!sameArea) warnings.Add("Nettoteilflächen fehlen oder weichen um mehr als 1 mm² ab.");
        if (!sameLength) warnings.Add("Schnittlängen weichen um mehr als 0,5 mm ab.");
        if (!likelySame) warnings.Insert(0, "Achtung: Das Werkstück ist nicht sicher als identisch bestätigt. Preisdelta nicht als belastbaren Preisvergleich verwenden.");

        var oldMachineMinutes = older.ActualMachineMinutesPerPart ?? older.EstimatedMachineMinutesPerPart;
        var newMachineMinutes = newer.ActualMachineMinutesPerPart ?? newer.EstimatedMachineMinutesPerPart;
        return new QuoteHistoryComparisonResult(
            likelySame,
            newer.CostPerPart - older.CostPerPart,
            newer.MinimumPricePerPart - older.MinimumPricePerPart,
            newer.RecommendedPricePerPart - older.RecommendedPricePerPart,
            newer.MaterialCostPerPart - older.MaterialCostPerPart,
            newer.MachineCostPerPart - older.MachineCostPerPart,
            newMachineMinutes - oldMachineMinutes,
            newer.CutLengthMm - older.CutLengthMm,
            warnings.AsReadOnly());
    }
}
