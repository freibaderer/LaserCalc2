using System.Globalization;
using System.Text;
using LaserCalc.Domain.Calculation;
using LaserCalc.Domain.Geometry;

namespace LaserCalc.Infrastructure.Export;

/// <summary>
/// Creates a one-page A4 internal costing sheet using only .NET base libraries.
/// The document is intentionally labelled as an internal costing sheet, not a
/// customer offer, until customer/address/terms fields are implemented.
/// </summary>
public sealed class CalculationPdfExporter
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public void Export(string outputPath, PartGeometry geometry, CalculationInputs input,
        CalculationResult result, string materialName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(materialName);

        var lines = BuildLines(geometry, input, result, materialName);
        var content = BuildPageContent(lines);
        WritePdf(outputPath, content);
    }

    private static IReadOnlyList<(string Text, bool Bold, int Size)> BuildLines(
        PartGeometry geometry, CalculationInputs input, CalculationResult result, string materialName)
    {
        var lines = new List<(string, bool, int)>
        {
            ($"Interne Kalkulation | {DateTime.Now:dd.MM.yyyy HH:mm}", false, 9),
            ($"Referenz: LC-{DateTime.Now:yyyyMMdd-HHmmss}", false, 9),
            ("", false, 6),
            ("DATEI UND WERKSTUECK", true, 10),
            ($"Datei: {geometry.SourceFile}", false, 9),
            ($"Quelle: {geometry.Format}", false, 9),
            ($"Material: {materialName} | Dicke: {F(input.ThicknessMm)} mm | Menge: {input.Quantity} Stueck", false, 9),
            ($"Abmessungen: {F(input.BoundingWidthMm)} x {F(input.BoundingHeightMm)} mm", false, 9),
            ($"Nettoteilflaeche: {(input.NetPartAreaMm2.HasValue ? F(input.NetPartAreaMm2.Value) + " mm2" : "nicht ermittelt")}", false, 9),
            ($"Schnittlaenge: {F(input.ContourLengthMm / 1000m)} m | Konturen/Einstiche: {geometry.ContourCount}/{input.EstimatedPierceCount}", false, 9),
            ("", false, 6),
            ("MATERIAL UND FERTIGUNG", true, 10),
            ($"Materialrechteck inkl. Rand: {F(result.BlankWidthMm)} x {F(result.BlankHeightMm)} mm (Rand je Seite: {F(input.MaterialBorderMm)} mm)", false, 9),
            ($"Rohteilgewicht: {F(result.BlankWeightKg)} kg | Fertigteilgewicht: {(result.FinishedPartWeightKg.HasValue ? F(result.FinishedPartWeightKg.Value) + " kg" : "nicht ermittelt")}", false, 9),
            ($"Materialausnutzung: {(result.MaterialUtilizationPercent.HasValue ? F(result.MaterialUtilizationPercent.Value) + " %" : "nicht ermittelt")} | Verschnittflaeche im Rohteilrechteck: {(result.ScrapAreaMm2.HasValue ? F(result.ScrapAreaMm2.Value) + " mm2" : "nicht ermittelt")}", false, 9),
            ($"Materialpreis: {Money(input.MaterialPricePerKg)}/kg | Interne Maschinen-Vollkosten: {Money(input.MachineRatePerHour)}/h", false, 9),
            ($"Zeitbasis: {result.TimeBasis}", false, 9),
            ($"Maschinenzeit je Teil: {F(result.CuttingMinutesPerPart)} min | Vorschub: {F(input.FeedRateMetersPerMinute)} m/min | Einstich: {F(input.PierceSeconds)} s", false, 9),
            ($"Ruestzeit Auftrag: {F(input.SetupMinutes)} min", false, 9),
            ("", false, 6),
            ("KOSTEN UND PREISE (NETTO)", true, 10),
            ($"Materialkosten je Teil: {Money(result.MaterialCostPerPart)}", false, 9),
            ($"Maschinenkosten je Teil: {Money(result.MachineCostPerPart)}", false, 9),
            ($"Ruestkostenanteil je Teil: {Money(result.SetupCostPerPart)}", false, 9),
            ($"Selbstkosten je Teil: {Money(result.CostPerPart)}", true, 11),
            ($"Mindestverkaufspreis je Teil ({F(input.MinimumMarginPercent)} % Marge): {Money(result.MinimumSellingPricePerPart)}", false, 9),
            ($"Empfohlener Verkaufspreis je Teil ({F(input.TargetMarginPercent)} % Marge): {Money(result.RecommendedSellingPricePerPart)}", true, 10),
            ($"Auftrag gesamt: Selbstkosten {Money(result.OrderCostTotal)} | Mindestpreis {Money(result.MinimumSellingPriceTotal)} | Empfehlung {Money(result.RecommendedSellingPriceTotal)}", true, 9)
        };

        var warnings = geometry.Warnings.Concat(result.Warnings).Distinct().Take(3).ToList();
        if (warnings.Count > 0)
        {
            lines.Add(("", false, 6));
            lines.Add(("PRUEFHINWEISE", true, 9));
            lines.AddRange(warnings.Select(w => ("- " + w, false, 8)));
        }

        lines.Add(("", false, 5));
        lines.Add(("Hinweis: Interne Kalkulationsunterlage, keine verbindliche Kundenangebots- oder Auftragsbestaetigung.", false, 8));
        return lines;
    }

    private static string BuildPageContent(IReadOnlyList<(string Text, bool Bold, int Size)> lines)
    {
        var sb = new StringBuilder();
        // Clean white A4 page and a restrained blue company header.
        sb.AppendLine("q 0.055 0.13 0.20 rg 0 760 595 82 re f Q");
        DrawText(sb, 42, 811, "MAGES MECHANISCHE FERTIGUNG GMBH", 10, true, true);
        DrawText(sb, 42, 783, "LASERCALC 2.0  |  KALKULATIONSBLATT", 17, true, true);
        sb.AppendLine("0.20 0.52 0.82 RG 42 748 m 553 748 l S");

        var y = 724.0;
        foreach (var line in lines)
        {
            if (y < 48) break;
            if (line.Text.Length == 0) { y -= 8; continue; }
            DrawText(sb, 45, y, line.Text, line.Size, line.Bold, false);
            y -= line.Size >= 10 ? 19 : 16;
        }
        sb.AppendLine("0.72 0.77 0.82 RG 42 37 m 553 37 l S");
        DrawText(sb, 45, 22, "Mages mechanische Fertigung GmbH  |  LaserCalc 2.0  |  Alle Betraege netto in EUR", 7, false, false);
        return sb.ToString();
    }

    private static void DrawText(StringBuilder sb, double x, double y, string text, int size, bool bold, bool white)
    {
        var font = bold ? "F2" : "F1";
        var colour = white ? "1 1 1 rg" : "0.10 0.16 0.22 rg";
        var safe = ToPdfAscii(text);
        if (safe.Length > 112) safe = safe[..109] + "...";
        sb.AppendLine($"BT {colour} /{font} {size} Tf 1 0 0 1 {x.ToString("0.##", CultureInfo.InvariantCulture)} {y.ToString("0.##", CultureInfo.InvariantCulture)} Tm ({EscapePdfString(safe)}) Tj ET");
    }

    private static string ToPdfAscii(string value)
    {
        var replacements = new Dictionary<char, string>
        {
            ['ä'] = "ae", ['ö'] = "oe", ['ü'] = "ue", ['Ä'] = "Ae", ['Ö'] = "Oe", ['Ü'] = "Ue",
            ['ß'] = "ss", ['€'] = "EUR", ['²'] = "^2", ['–'] = "-", ['—'] = "-", ['·'] = "|",
            ['Δ'] = "Delta", ['×'] = "x", ['„'] = "\"", ['“'] = "\"", ['”'] = "\"", ['’'] = "'"
        };
        var sb = new StringBuilder();
        foreach (var ch in value)
        {
            if (replacements.TryGetValue(ch, out var replacement)) sb.Append(replacement);
            else if (ch is >= ' ' and <= '~') sb.Append(ch);
            else sb.Append('?');
        }
        return sb.ToString();
    }

    private static string EscapePdfString(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("(", "\\(", StringComparison.Ordinal)
        .Replace(")", "\\)", StringComparison.Ordinal);

    private static string F(decimal value) => value.ToString("N2", German);
    private static string Money(decimal value) => value.ToString("N2", German) + " EUR";

    private static void WritePdf(string path, string content)
    {
        var stream = Encoding.ASCII.GetBytes(content);
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
            $"<< /Length {stream.Length} >>\nstream\n{content}endstream"
        };

        using var memory = new MemoryStream();
        var header = Encoding.ASCII.GetBytes("%PDF-1.4\n% LaserCalc 2.0\n");
        memory.Write(header);
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(memory.Position);
            var bytes = Encoding.ASCII.GetBytes($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            memory.Write(bytes);
        }
        var xrefOffset = memory.Position;
        var xref = new StringBuilder();
        xref.AppendLine("xref");
        xref.AppendLine($"0 {objects.Length + 1}");
        xref.AppendLine("0000000000 65535 f ");
        for (var i = 1; i < offsets.Count; i++)
            xref.AppendLine($"{offsets[i]:D10} 00000 n ");
        xref.AppendLine("trailer");
        xref.AppendLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
        xref.AppendLine("startxref");
        xref.AppendLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
        xref.Append("%%EOF");
        var xrefBytes = Encoding.ASCII.GetBytes(xref.ToString());
        memory.Write(xrefBytes);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var temp = path + ".tmp";
        File.WriteAllBytes(temp, memory.ToArray());
        File.Move(temp, path, overwrite: true);
    }
}
