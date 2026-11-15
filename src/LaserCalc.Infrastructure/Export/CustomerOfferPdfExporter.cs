using System.Globalization;
using System.Text;
using LaserCalc.Infrastructure.Offers;

namespace LaserCalc.Infrastructure.Export;

/// <summary>Creates a one-page German customer offer without external PDF dependencies.</summary>
public sealed class CustomerOfferPdfExporter
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public void Export(string outputPath, OfferDocument offer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(offer);
        Validate(offer);

        var content = BuildContent(offer);
        WritePdf(outputPath, content);
    }

    private static void Validate(OfferDocument offer)
    {
        if (string.IsNullOrWhiteSpace(offer.OfferNumber)) throw new ArgumentException("Angebotsnummer fehlt.", nameof(offer));
        if (string.IsNullOrWhiteSpace(offer.Company.CompanyName) || string.IsNullOrWhiteSpace(offer.Company.Street) ||
            string.IsNullOrWhiteSpace(offer.Company.PostalCode) || string.IsNullOrWhiteSpace(offer.Company.City))
            throw new ArgumentException("Bitte die vollständigen Absenderdaten des Unternehmens hinterlegen.", nameof(offer));
        if (string.IsNullOrWhiteSpace(offer.CustomerName) || string.IsNullOrWhiteSpace(offer.CustomerStreet) ||
            string.IsNullOrWhiteSpace(offer.CustomerPostalCode) || string.IsNullOrWhiteSpace(offer.CustomerCity))
            throw new ArgumentException("Bitte Name und vollständige Anschrift des Kunden eintragen.", nameof(offer));
        if (string.IsNullOrWhiteSpace(offer.PositionDescription)) throw new ArgumentException("Positionsbeschreibung fehlt.", nameof(offer));
        if (offer.ValidUntil.Date < offer.OfferDate.Date) throw new ArgumentException("Die Angebotsgültigkeit liegt vor dem Angebotsdatum.", nameof(offer));
    }

    private static string BuildContent(OfferDocument offer)
    {
        var sb = new StringBuilder();
        // Brand treatment: dark navy header, framed M symbol, restrained blue rule.
        sb.AppendLine("q 0.055 0.13 0.20 rg 0 730 595 112 re f Q");
        sb.AppendLine("q 1 1 1 rg 42 761 43 43 re f Q");
        sb.AppendLine("q 0 0 0 RG 42 761 43 43 re S Q");
        DrawText(sb, 52, 771, "M", 29, true, false);
        DrawText(sb, 98, 798, offer.Company.CompanyName, 12, true, true);
        DrawText(sb, 98, 778, "BLECH. FORM. FUNKTION.", 8, true, true);
        DrawText(sb, 98, 754, JoinNonEmpty(" | ", offer.Company.Phone, offer.Company.Email, offer.Company.Website), 7, false, true);
        DrawText(sb, 405, 799, "ANGEBOT", 18, true, true);
        DrawText(sb, 405, 778, offer.OfferNumber, 10, true, true);
        DrawText(sb, 405, 758, $"Datum: {offer.OfferDate:dd.MM.yyyy}", 8, false, true);

        // Sender line / recipient block.
        DrawText(sb, 42, 708, JoinNonEmpty(" | ", offer.Company.CompanyName, offer.Company.Street,
            offer.Company.PostalCode + " " + offer.Company.City), 7, false, false);
        var y = 680.0;
        var customerLines = new List<string> { offer.CustomerName };
        if (!string.IsNullOrWhiteSpace(offer.CustomerContact)) customerLines.Add(offer.CustomerContact);
        customerLines.Add(offer.CustomerStreet);
        customerLines.Add(offer.CustomerPostalCode + " " + offer.CustomerCity);
        if (!string.IsNullOrWhiteSpace(offer.CustomerEmail)) customerLines.Add(offer.CustomerEmail);
        foreach (var line in customerLines)
        {
            DrawText(sb, 42, y, line, 10, line == offer.CustomerName, false);
            y -= 15;
        }

        DrawText(sb, 385, 681, "ANGEBOTSDATEN", 8, true, false);
        DrawText(sb, 385, 663, $"Angebotsdatum: {offer.OfferDate:dd.MM.yyyy}", 8, false, false);
        DrawText(sb, 385, 648, $"Gültig bis: {offer.ValidUntil:dd.MM.yyyy}", 8, false, false);

        var subjectY = Math.Min(y - 20, 595);
        DrawText(sb, 42, subjectY, offer.Subject, 14, true, false);
        DrawText(sb, 42, subjectY - 23, "Sehr geehrte Damen und Herren,", 9, false, false);
        DrawText(sb, 42, subjectY - 41, "vielen Dank für Ihre Anfrage. Gerne bieten wir Ihnen folgende Leistung an:", 9, false, false);

        var tableTop = subjectY - 72;
        sb.AppendLine($"q 0.90 0.93 0.96 rg 42 {N(tableTop - 26)} 511 26 re f Q");
        DrawText(sb, 50, tableTop - 17, "POS.", 8, true, false);
        DrawText(sb, 83, tableTop - 17, "BESCHREIBUNG", 8, true, false);
        DrawText(sb, 355, tableTop - 17, "MENGE", 8, true, false);
        DrawText(sb, 415, tableTop - 17, "EINZELPREIS", 8, true, false);
        DrawText(sb, 505, tableTop - 17, "GESAMT", 8, true, false);

        var rowY = tableTop - 48;
        DrawText(sb, 52, rowY, "1", 9, false, false);
        var descriptionLines = Wrap(offer.PositionDescription, 46).Take(3).ToList();
        var descriptionY = rowY;
        foreach (var line in descriptionLines)
        {
            DrawText(sb, 83, descriptionY, line, 9, false, false);
            descriptionY -= 13;
        }
        var detailText = $"{offer.Material}, Blechdicke {F(offer.ThicknessMm)} mm";
        DrawText(sb, 83, descriptionY - 2, detailText, 8, false, false);
        DrawText(sb, 355, rowY, offer.Quantity.ToString(German) + " Stk.", 9, false, false);
        DrawText(sb, 415, rowY, Money(offer.UnitNetPrice), 9, false, false);
        DrawText(sb, 505, rowY, Money(offer.Pricing.LineNetBeforeDiscount), 9, false, false);

        var totalsY = tableTop - 112 - (descriptionLines.Count - 1) * 13;
        sb.AppendLine($"0.78 0.82 0.86 RG 360 {N(totalsY + 16)} m 553 {N(totalsY + 16)} l S");
        DrawText(sb, 370, totalsY, "Zwischensumme netto", 8, false, false);
        DrawText(sb, 490, totalsY, Money(offer.Pricing.LineNetBeforeDiscount), 8, false, false);
        var nextY = totalsY - 16;
        if (offer.DiscountPercent > 0)
        {
            DrawText(sb, 370, nextY, $"Rabatt ({F(offer.DiscountPercent)} %)", 8, false, false);
            DrawText(sb, 490, nextY, "- " + Money(offer.Pricing.DiscountAmount), 8, false, false);
            nextY -= 16;
        }
        DrawText(sb, 370, nextY, "Gesamtbetrag netto", 9, true, false);
        DrawText(sb, 490, nextY, Money(offer.Pricing.NetTotal), 9, true, false);
        nextY -= 16;
        DrawText(sb, 370, nextY, $"Umsatzsteuer ({F(offer.VatPercent)} %)", 8, false, false);
        DrawText(sb, 490, nextY, Money(offer.Pricing.VatAmount), 8, false, false);
        nextY -= 24;
        sb.AppendLine($"q 0.055 0.13 0.20 rg 360 {N(nextY - 8)} 193 30 re f Q");
        DrawText(sb, 370, nextY + 2, "GESAMTBETRAG BRUTTO", 8, true, true);
        DrawText(sb, 488, nextY + 2, Money(offer.Pricing.GrossTotal), 9, true, true);

        var conditionsY = nextY - 43;
        DrawText(sb, 42, conditionsY, "KONDITIONEN", 9, true, false);
        DrawText(sb, 42, conditionsY - 18, "Lieferung: " + offer.DeliveryTerms, 8, false, false);
        DrawText(sb, 42, conditionsY - 33, "Zahlung: " + offer.PaymentTerms, 8, false, false);
        DrawText(sb, 42, conditionsY - 48, "An dieses Angebot halten wir uns bis zum " + offer.ValidUntil.ToString("dd.MM.yyyy") + " gebunden.", 8, false, false);
        var closingLines = Wrap(offer.ClosingText, 92).Take(2).ToList();
        var closingY = conditionsY - 78;
        foreach (var line in closingLines)
        {
            DrawText(sb, 42, closingY, line, 8, false, false);
            closingY -= 13;
        }
        DrawText(sb, 42, 76, "Mit freundlichen Grüßen", 8, false, false);
        DrawText(sb, 42, 61, offer.Company.CompanyName, 9, true, false);

        // Footer contains company and tax/bank details only when configured.
        var footer = new List<string> { offer.Company.CompanyName, offer.Company.Street,
            offer.Company.PostalCode + " " + offer.Company.City, offer.Company.Phone, offer.Company.Email,
            offer.Company.VatId, offer.Company.TaxNumber, offer.Company.BankName,
            offer.Company.Iban, offer.Company.Bic };
        var footerText = JoinNonEmpty(" | ", footer.ToArray());
        DrawText(sb, 42, 34, footerText, 6, false, false);
        sb.AppendLine("0.13 0.48 0.78 RG 42 47 m 553 47 l S");
        return sb.ToString();
    }

    private static IEnumerable<string> Wrap(string text, int maxCharacters)
    {
        foreach (var paragraph in (text ?? string.Empty).Replace("\r", "").Split('\n'))
        {
            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = new StringBuilder();
            foreach (var word in words)
            {
                if (line.Length > 0 && line.Length + word.Length + 1 > maxCharacters)
                {
                    yield return line.ToString();
                    line.Clear();
                }
                if (line.Length > 0) line.Append(' ');
                line.Append(word);
            }
            if (line.Length > 0) yield return line.ToString();
        }
    }

    private static void DrawText(StringBuilder sb, double x, double y, string text, int size, bool bold, bool white)
    {
        var font = bold ? "F2" : "F1";
        var colour = white ? "1 1 1 rg" : "0.10 0.16 0.22 rg";
        var safe = ToPdfAscii(text ?? string.Empty);
        if (safe.Length > 115) safe = safe[..112] + "...";
        sb.AppendLine($"BT {colour} /{font} {size} Tf 1 0 0 1 {N(x)} {N(y)} Tm ({EscapePdfString(safe)}) Tj ET");
    }

    private static string ToPdfAscii(string value)
    {
        var map = new Dictionary<char, string>
        {
            ['ä']="ae", ['ö']="oe", ['ü']="ue", ['Ä']="Ae", ['Ö']="Oe", ['Ü']="Ue",
            ['ß']="ss", ['€']="EUR", ['²']="^2", ['–']="-", ['—']="-", ['·']="|",
            ['„']="\"", ['“']="\"", ['”']="\"", ['’']="'", ['×']="x"
        };
        var result = new StringBuilder();
        foreach (var ch in value)
        {
            if (map.TryGetValue(ch, out var replacement)) result.Append(replacement);
            else if (ch is >= ' ' and <= '~') result.Append(ch);
            else result.Append('?');
        }
        return result.ToString();
    }

    private static string EscapePdfString(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal);
    private static string JoinNonEmpty(string separator, params string[] values) => string.Join(separator, values.Where(x => !string.IsNullOrWhiteSpace(x)));
    private static string F(decimal value) => value.ToString("N2", German);
    private static string Money(decimal value) => value.ToString("N2", German) + " EUR";
    private static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

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
        memory.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n% LaserCalc 2.0 Customer Offer\n"));
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(memory.Position);
            memory.Write(Encoding.ASCII.GetBytes($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"));
        }
        var xrefOffset = memory.Position;
        var xref = new StringBuilder();
        xref.AppendLine("xref"); xref.AppendLine($"0 {objects.Length + 1}"); xref.AppendLine("0000000000 65535 f ");
        for (var i = 1; i < offsets.Count; i++) xref.AppendLine($"{offsets[i]:D10} 00000 n ");
        xref.AppendLine("trailer"); xref.AppendLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
        xref.AppendLine("startxref"); xref.AppendLine(xrefOffset.ToString(CultureInfo.InvariantCulture)); xref.Append("%%EOF");
        memory.Write(Encoding.ASCII.GetBytes(xref.ToString()));

        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temp = fullPath + ".tmp";
        File.WriteAllBytes(temp, memory.ToArray());
        File.Move(temp, fullPath, overwrite: true);
    }
}
