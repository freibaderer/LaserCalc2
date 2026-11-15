using System.Text;
using System.IO.Compression;
using LaserCalc.Domain.Calculation;
using LaserCalc.Domain.Geometry;
using LaserCalc.Domain.Offers;
using LaserCalc.Infrastructure.Export;
using LaserCalc.Infrastructure.Import;
using LaserCalc.Infrastructure.Persistence;
using LaserCalc.Infrastructure.Offers;
using LaserCalc.Infrastructure.Customers;

const string GeoRectangle = """
#~1
1.03
1
01.01.2026
0.000000000 0.000000000 0.000000000
10.000000000 10.000000000 0.000000000
100.000000000
1
0.001000000
0
1
##~~
#~31
P
1
0.000000000 0.000000000 0.000000000
|~
P
2
10.000000000 0.000000000 0.000000000
|~
P
3
10.000000000 10.000000000 0.000000000
|~
P
4
0.000000000 10.000000000 0.000000000
|~
##~~
#~33

1 24 0
0
0.000000000 0.000000000 -1.000000000
0.000000000 0.000000000 0.000000000
10.000000000 10.000000000 0.000000000
5.000000000 5.000000000 0.000000000
100.000000000
0
##~~
#~331
LIN
1 0
1 2
|~
LIN
1 0
2 3
|~
LIN
1 0
3 4
|~
LIN
1 0
4 1
|~
#~KONT_END
#~END
#~EOF
""";

const string GeoCircle = """
#~1
1.03
1
01.01.2026
0.000000000 0.000000000 0.000000000
10.000000000 10.000000000 0.000000000
78.539816340
1
0.001000000
0
1
##~~
#~31
P
1
0.000000000 0.000000000 0.000000000
|~
##~~
#~33

1 24 0
0
0.000000000 0.000000000 -1.000000000
-5.000000000 -5.000000000 0.000000000
5.000000000 5.000000000 0.000000000
0.000000000 0.000000000 0.000000000
78.539816340
0
##~~
#~331
CIR
1 0
1
5.000000000
|~
#~KONT_END
#~END
#~EOF
""";

const string DxfR12Rectangle = """
0
SECTION
2
HEADER
9
$INSUNITS
70
4
0
ENDSEC
0
SECTION
2
ENTITIES
0
LINE
8
OUTER_PROFILE
10
0
20
0
11
10
21
0
0
LINE
8
OUTER_PROFILE
10
10
20
0
11
10
21
10
0
LINE
8
OUTER_PROFILE
10
10
20
10
11
0
21
10
0
LINE
8
OUTER_PROFILE
10
0
20
10
11
0
21
0
0
ENDSEC
0
EOF
""";



const string DxfRectangleWithCircularHole = """
0
SECTION
2
HEADER
9
$INSUNITS
70
4
0
ENDSEC
0
SECTION
2
ENTITIES
0
LINE
8
OUTER_PROFILE
10
0
20
0
11
20
21
0
0
LINE
8
OUTER_PROFILE
10
20
20
0
11
20
21
20
0
LINE
8
OUTER_PROFILE
10
20
20
20
11
0
21
20
0
LINE
8
OUTER_PROFILE
10
0
20
20
11
0
21
0
0
CIRCLE
8
HOLE
10
10
20
10
40
5
0
ENDSEC
0
EOF
""";



const string DxfClassicPolylineBulgeWithHole = """
0
SECTION
2
HEADER
9
$INSUNITS
70
4
0
ENDSEC
0
SECTION
2
ENTITIES
0
POLYLINE
8
0
66
1
70
1
10
0
20
0
30
0
0
VERTEX
8
0
10
0
20
0
30
0
42
0
0
VERTEX
8
0
10
72
20
0
30
0
42
0
0
VERTEX
8
0
10
72
20
60
30
0
42
0
0
VERTEX
8
0
10
0
20
60
30
0
42
1
0
SEQEND
0
CIRCLE
8
0
10
36
20
30
40
18.25
0
ENDSEC
0
EOF
""";



const string DxfInchRectangle = """
0
SECTION
2
HEADER
9
$INSUNITS
70
1
0
ENDSEC
0
SECTION
2
ENTITIES
0
LINE
8
OUTER_PROFILE
10
0
20
0
11
1
21
0
0
LINE
8
OUTER_PROFILE
10
1
20
0
11
1
21
1
0
LINE
8
OUTER_PROFILE
10
1
20
1
11
0
21
1
0
LINE
8
OUTER_PROFILE
10
0
20
1
11
0
21
0
0
ENDSEC
0
EOF
""";

const string DxfUnsupportedSpline = """
0
SECTION
2
HEADER
9
$INSUNITS
70
4
0
ENDSEC
0
SECTION
2
ENTITIES
0
SPLINE
8
CUT
70
8
71
3
72
8
73
4
74
0
0
ENDSEC
0
EOF
""";

var passed = 0;
var failed = 0;

void Check(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS  {name}");
        passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL  {name}: {ex.Message}");
        failed++;
    }
}

var engine = new CalculationEngine();
var sample = new CalculationInputs(
    MaterialPricePerKg: 3.20m,
    DensityKgPerM3: 8000m,
    ThicknessMm: 6m,
    BoundingWidthMm: 102.012764841m,
    BoundingHeightMm: 60m,
    NetPartAreaMm2: 4688.135881m,
    ContourLengthMm: 412.941441m,
    EstimatedPierceCount: 2,
    FeedRateMetersPerMinute: 12m,
    MachineRatePerHour: 100m,
    SetupMinutes: 0m,
    Quantity: 1,
    MinimumMarginPercent: 5m,
    TargetMarginPercent: 20m,
    MaterialBorderMm: 10m,
    PierceSeconds: 1m);

Check("10-mm-Rand wird je Seite addiert", () =>
{
    var result = engine.Calculate(sample);
    if (Math.Abs(result.BlankWidthMm - 122.01m) > 0.000001m || result.BlankHeightMm != 80m)
        throw new InvalidOperationException($"Unerwartete Rohteilmaße: {result.BlankWidthMm} × {result.BlankHeightMm} mm");
});

Check("Materialausnutzung und Verschnittfläche werden berechnet", () =>
{
    var result = engine.Calculate(sample);
    var blankArea = (sample.BoundingWidthMm + 20m) * (sample.BoundingHeightMm + 20m);
    var expectedUtilization = sample.NetPartAreaMm2!.Value / blankArea * 100m;
    var expectedScrap = blankArea - sample.NetPartAreaMm2.Value;
    if (result.MaterialUtilizationPercent != decimal.Round(expectedUtilization, 2, MidpointRounding.AwayFromZero))
        throw new InvalidOperationException("Materialausnutzung ist falsch.");
    if (result.ScrapAreaMm2 != decimal.Round(expectedScrap, 2, MidpointRounding.AwayFromZero))
        throw new InvalidOperationException("Verschnittfläche ist falsch.");
});

Check("Maschinenzeit aus Schnittlänge und Einstichen", () =>
{
    var result = engine.Calculate(sample);
    if (Math.Abs(result.CuttingMinutesPerPart - 0.07m) > 0.00001m)
        throw new InvalidOperationException($"Unerwartete Zeit: {result.CuttingMinutesPerPart} min");
});

Check("Zielmarge ist Anteil am Verkaufspreis, kein Kostenaufschlag", () =>
{
    var result = engine.Calculate(sample with
    {
        MaterialPricePerKg = 0m,
        MachineRatePerHour = 60m,
        SetupMinutes = 0m,
        ContourLengthMm = 60000m,
        FeedRateMetersPerMinute = 1m,
        EstimatedPierceCount = 0,
        MinimumMarginPercent = 0m,
        TargetMarginPercent = 20m
    });
    if (result.CostPerPart != 60m || result.RecommendedSellingPricePerPart != 75m)
        throw new InvalidOperationException($"20-%-Marge falsch: Kosten {result.CostPerPart}, Verkauf {result.RecommendedSellingPricePerPart}.");
    var achievedMargin = (result.RecommendedSellingPricePerPart - result.CostPerPart) / result.RecommendedSellingPricePerPart * 100m;
    if (achievedMargin != 20m)
        throw new InvalidOperationException($"Tatsächliche Zielmarge beträgt {achievedMargin}% statt 20%.");
});

Check("Mindestpreis bei 5-%-Marge wird korrekt rückwärts berechnet", () =>
{
    var result = engine.Calculate(sample with
    {
        MaterialPricePerKg = 0m,
        MachineRatePerHour = 60m,
        SetupMinutes = 0m,
        ContourLengthMm = 60000m,
        FeedRateMetersPerMinute = 1m,
        EstimatedPierceCount = 0,
        MinimumMarginPercent = 5m,
        TargetMarginPercent = 20m
    });
    if (result.MinimumSellingPricePerPart != 63.16m)
        throw new InvalidOperationException($"Mindestpreis bei 5-%-Marge unerwartet: {result.MinimumSellingPricePerPart} EUR.");
});

Check("Interne Maschinen-Vollkosten werden exakt in Selbstkosten übernommen", () =>
{
    var result = engine.Calculate(sample with
    {
        MaterialPricePerKg = 0m,
        MachineRatePerHour = 120m,
        ActualOrConfirmedMachineMinutes = 30m,
        SetupMinutes = 0m
    });
    if (result.MachineCostPerPart != 60m || result.CostPerPart != 60m)
        throw new InvalidOperationException($"Interne Vollkosten falsch: Maschinenkosten {result.MachineCostPerPart}, Selbstkosten {result.CostPerPart}.");
});

Check("Drei Preisstufen aus 6-mm-VA-Referenz", () =>
{
    var result = engine.Calculate(sample);
    if (result.CostPerPart != 1.61m || result.MinimumSellingPricePerPart != 1.70m || result.RecommendedSellingPricePerPart != 2.02m)
        throw new InvalidOperationException($"Preiswerte unerwartet: {result.CostPerPart} / {result.MinimumSellingPricePerPart} / {result.RecommendedSellingPricePerPart} EUR");
});

Check("Bestätigte Maschinenzeit überschreibt Geometrie-Schätzung", () =>
{
    var result = engine.Calculate(sample with { ActualOrConfirmedMachineMinutes = 4m });
    if (result.CuttingMinutesPerPart != 4m || !result.TimeBasis.Contains("bestätigte", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Die bestätigte Fertigungszeit wurde nicht verwendet.");
});

Check("Eingetragene bestätigte Maschinenzeit darf nicht null sein", () =>
{
    try
    {
        engine.Calculate(sample with { ActualOrConfirmedMachineMinutes = 0m });
        throw new InvalidOperationException("Null wurde als bestätigte Maschinenzeit akzeptiert.");
    }
    catch (ArgumentOutOfRangeException)
    {
        // Expected.
    }
});

Check("Rüstzeit wird auf die Stückzahl verteilt", () =>
{
    var result = engine.Calculate(sample with { Quantity = 10, SetupMinutes = 5m });
    var expectedPerPart = 5m / 10m * 100m / 60m;
    if (Math.Abs(result.SetupCostPerPart - decimal.Round(expectedPerPart, 2, MidpointRounding.AwayFromZero)) > 0.000001m)
        throw new InvalidOperationException("Der Rüstkostenanteil je Teil ist falsch.");
    if (result.OrderCostTotal != result.CostPerPart * 10m)
        throw new InvalidOperationException("Auftragssumme enthält die Rüstkosten nicht genau einmal.");
});

Check("Angebotsmenge verteilt Rüstkosten pro Teil korrekt neu", () =>
{
    var onePart = engine.Calculate(sample with { Quantity = 1, SetupMinutes = 8m });
    var fourParts = engine.Calculate(sample with { Quantity = 4, SetupMinutes = 8m });
    if (fourParts.SetupCostPerPart >= onePart.SetupCostPerPart ||
        Math.Abs(fourParts.SetupCostPerPart * 4m - onePart.SetupCostPerPart) > 0.02m)
        throw new InvalidOperationException("Der Rüstkostenanteil wurde bei geänderter Angebotsmenge nicht korrekt verteilt.");
    if (Math.Abs(fourParts.MaterialCostPerPart - onePart.MaterialCostPerPart) > 0.001m ||
        Math.Abs(fourParts.MachineCostPerPart - onePart.MachineCostPerPart) > 0.001m)
        throw new InvalidOperationException("Material- oder Maschinenkosten je Stück wurden durch die Rüstkostenänderung verändert.");
});

Check("Unmögliche Fläche wird abgewiesen", () =>
{
    try
    {
        engine.Calculate(sample with { NetPartAreaMm2 = 100_000m });
        throw new InvalidOperationException("Geometrie wurde nicht abgewiesen.");
    }
    catch (ArgumentOutOfRangeException)
    {
        // Expected.
    }
});

Check("TruTops-GEO: geschlossene Rechteckkontur wird erkannt", () =>
{
    var geometry = new TruTopsClassicGeoImporter().ImportText(GeoRectangle, "fixture.GEO");
    if (!geometry.IsSafeForPreliminaryCalculation || geometry.ContourCount != 1 || geometry.OpenContourCount != 0)
        throw new InvalidOperationException("GEO-Rechteck wurde nicht als geschlossene Kontur erkannt.");
    if (Math.Abs(geometry.CutLengthMm - 40.0) > 0.000001 || Math.Abs((geometry.NetAreaMm2 ?? 0.0) - 100.0) > 0.000001)
        throw new InvalidOperationException($"Falsche GEO-Werte: {geometry.CutLengthMm} mm / {geometry.NetAreaMm2} mm².");
});

Check("TruTops-GEO: einzelner Kreis liefert korrekte Fläche, Schnittlänge und Abmessungen", () =>
{
    var geometry = new TruTopsClassicGeoImporter().ImportText(GeoCircle, "circle.GEO");
    var expectedArea = Math.PI * 25.0;
    var expectedLength = 2.0 * Math.PI * 5.0;
    if (!geometry.IsSafeForPreliminaryCalculation || geometry.ContourCount != 1 || geometry.OpenContourCount != 0)
        throw new InvalidOperationException("GEO-Kreis wurde nicht als geschlossene Kontur erkannt.");
    if (Math.Abs(geometry.NetAreaMm2!.Value - expectedArea) > 0.00001 || Math.Abs(geometry.CutLengthMm - expectedLength) > 0.00001)
        throw new InvalidOperationException($"GEO-Kreiswerte falsch: Fläche {geometry.NetAreaMm2} mm², Schnittlänge {geometry.CutLengthMm} mm.");
    if (Math.Abs(geometry.WidthMm - 10.0) > 0.00001 || Math.Abs(geometry.HeightMm - 10.0) > 0.00001)
        throw new InvalidOperationException("GEO-Kreisabmessungen sind falsch.");
});

Check("TruTops-GEO: abweichende Flächenmetadaten verlangen manuelle Prüfung", () =>
{
    var mismatched = GeoRectangle.Replace("\n100.000000000\n1\n", "\n102.000000000\n1\n", StringComparison.Ordinal);
    var geometry = new TruTopsClassicGeoImporter().ImportText(mismatched, "area-mismatch.GEO");
    if (!geometry.RequiresAreaVerification)
        throw new InvalidOperationException("Die Flächenabweichung wurde nicht erkannt.");
    if (!geometry.IsSafeForPreliminaryCalculation)
        throw new InvalidOperationException("Die geometrisch geschlossene Kontur wurde fälschlich als defekt markiert; die Freigabe muss über die manuelle Flächenprüfung erfolgen.");
});

Check("DXF-R12: geschlossene Rechteckkontur wird nicht als Verzweigung markiert", () =>
{
    var geometry = new AsciiDxfImporter().ImportText(DxfR12Rectangle, "fixture-r12.dxf");
    if (!geometry.IsSafeForPreliminaryCalculation || geometry.ContourCount != 1 || geometry.OpenContourCount != 0)
        throw new InvalidOperationException("DXF-Rechteck ist nicht kalkulationsfähig oder wurde fälschlich als verzweigt erkannt.");
    if (Math.Abs(geometry.CutLengthMm - 40.0) > 0.000001 || Math.Abs((geometry.NetAreaMm2 ?? 0.0) - 100.0) > 0.000001)
        throw new InvalidOperationException($"Falsche DXF-Werte: {geometry.CutLengthMm} mm / {geometry.NetAreaMm2} mm².");
});

Check("DXF-Einheit Zoll wird korrekt in Millimeter umgerechnet", () =>
{
    var geometry = new AsciiDxfImporter().ImportText(DxfInchRectangle, "fixture-inch.dxf");
    if (!geometry.IsSafeForPreliminaryCalculation || geometry.RequiresUnitVerification)
        throw new InvalidOperationException("DXF mit expliziter Zoll-Einheit wurde nicht sicher erkannt.");
    if (Math.Abs(geometry.WidthMm - 25.4) > 0.000001 || Math.Abs(geometry.HeightMm - 25.4) > 0.000001)
        throw new InvalidOperationException($"Falsche Zoll-Umrechnung: {geometry.WidthMm} × {geometry.HeightMm} mm.");
    if (Math.Abs(geometry.CutLengthMm - 101.6) > 0.000001 || Math.Abs((geometry.NetAreaMm2 ?? 0) - 645.16) > 0.000001)
        throw new InvalidOperationException("Schnittlänge oder Fläche nach Zoll-Umrechnung ist falsch.");
});

Check("Nicht unterstützte SPLINE-Geometrie sperrt die automatische Kalkulation", () =>
{
    var geometry = new AsciiDxfImporter().ImportText(DxfUnsupportedSpline, "fixture-spline.dxf");
    if (geometry.IsSafeForPreliminaryCalculation || geometry.UnknownEntityCount == 0)
        throw new InvalidOperationException("SPLINE wurde fälschlich als kalkulationsfähige Geometrie akzeptiert.");
});

Check("DXF-Kreisloch wird von der Nettofläche abgezogen", () =>
{
    var geometry = new AsciiDxfImporter().ImportText(DxfRectangleWithCircularHole, "fixture-hole.dxf");
    var expectedArea = 400m - (decimal)Math.PI * 25m;
    var expectedLength = 80m + (decimal)(2 * Math.PI * 5);
    if (!geometry.IsSafeForPreliminaryCalculation || geometry.ContourCount != 2)
        throw new InvalidOperationException("Außenkontur und Kreisloch wurden nicht als zwei geschlossene Konturen erkannt.");
    if (Math.Abs((decimal)(geometry.NetAreaMm2 ?? 0) - expectedArea) > 0.01m)
        throw new InvalidOperationException($"Falsche Nettofläche: {geometry.NetAreaMm2} mm², erwartet {expectedArea} mm².");
    if (Math.Abs((decimal)geometry.CutLengthMm - expectedLength) > 0.01m)
        throw new InvalidOperationException($"Falsche Schnittlänge: {geometry.CutLengthMm} mm, erwartet {expectedLength} mm.");
});

Check("Klassische DXF-POLYLINE mit Bulge-Bogen berechnet exakte Grenzen und Länge", () =>
{
    var geometry = new AsciiDxfImporter().ImportText(DxfClassicPolylineBulgeWithHole, "fixture-bulge.dxf");
    var expectedLength = 204m + (decimal)(30 * Math.PI) + (decimal)(2 * Math.PI * 18.25);
    var expectedArea = 72m * 60m + (decimal)(0.5 * Math.PI * 30 * 30) - (decimal)(Math.PI * 18.25 * 18.25);
    if (!geometry.IsSafeForPreliminaryCalculation || geometry.ContourCount != 2)
        throw new InvalidOperationException("Bulge-POLYLINE und Kreisloch wurden nicht als zwei sichere Konturen erkannt.");
    if (Math.Abs((decimal)geometry.WidthMm - 102m) > 0.001m || Math.Abs((decimal)geometry.HeightMm - 60m) > 0.001m)
        throw new InvalidOperationException($"Falsche Außenmaße: {geometry.WidthMm} × {geometry.HeightMm} mm.");
    if (Math.Abs((decimal)geometry.CutLengthMm - expectedLength) > 0.01m)
        throw new InvalidOperationException($"Falsche Schnittlänge: {geometry.CutLengthMm} mm, erwartet {expectedLength} mm.");
    if (Math.Abs((decimal)(geometry.NetAreaMm2 ?? 0) - expectedArea) > 0.01m)
        throw new InvalidOperationException($"Falsche Nettofläche: {geometry.NetAreaMm2} mm², erwartet {expectedArea} mm².");
});

Check("DXF/GEO-Abgleich akzeptiert um 90 Grad gedrehte gleiche Teile", () =>
{
    var dxf = new PartGeometry
    {
        SourceFile = "customer.dxf", Format = "DXF", WidthMm = 20, HeightMm = 10,
        NetAreaMm2 = 150, CutLengthMm = 60, ContourCount = 2, IsSafeForPreliminaryCalculation = true
    };
    var geo = new PartGeometry
    {
        SourceFile = "part.geo", Format = "GEO", WidthMm = 10, HeightMm = 20,
        NetAreaMm2 = 150, CutLengthMm = 60, ContourCount = 2, IsSafeForPreliminaryCalculation = true
    };
    var comparison = GeometrySourceComparer.Compare(dxf, geo);
    if (!comparison.IsConsistent)
        throw new InvalidOperationException(string.Join("; ", comparison.Warnings));
});

Check("DXF/GEO-Abgleich sperrt fehlende Flächenwerte", () =>
{
    var dxf = new PartGeometry
    {
        SourceFile = "customer.dxf", Format = "DXF", WidthMm = 20, HeightMm = 10,
        NetAreaMm2 = null, CutLengthMm = 60, ContourCount = 2, IsSafeForPreliminaryCalculation = true
    };
    var geo = new PartGeometry
    {
        SourceFile = "part.geo", Format = "GEO", WidthMm = 20, HeightMm = 10,
        NetAreaMm2 = 150, CutLengthMm = 60, ContourCount = 2, IsSafeForPreliminaryCalculation = true
    };
    if (GeometrySourceComparer.Compare(dxf, geo).IsConsistent)
        throw new InvalidOperationException("Fehlende Nettofläche wurde fälschlich als bestätigter Quellenabgleich gewertet.");
});

Check("DXF/GEO-Abgleich sperrt unterschiedliche Konturanzahl", () =>
{
    var dxf = new PartGeometry
    {
        SourceFile = "customer.dxf", Format = "DXF", WidthMm = 20, HeightMm = 10,
        NetAreaMm2 = 150, CutLengthMm = 60, ContourCount = 2, IsSafeForPreliminaryCalculation = true
    };
    var geo = new PartGeometry
    {
        SourceFile = "part.geo", Format = "GEO", WidthMm = 20, HeightMm = 10,
        NetAreaMm2 = 150, CutLengthMm = 60, ContourCount = 3, IsSafeForPreliminaryCalculation = true
    };
    if (GeometrySourceComparer.Compare(dxf, geo).IsConsistent)
        throw new InvalidOperationException("Unterschiedliche Konturanzahl wurde nicht erkannt.");
});

Check("DXF/GEO-Abgleich sperrt unsichere Eingangsgeometrie", () =>
{
    var dxf = new PartGeometry
    {
        SourceFile = "customer.dxf", Format = "DXF", WidthMm = 20, HeightMm = 10,
        NetAreaMm2 = 150, CutLengthMm = 60, ContourCount = 2, IsSafeForPreliminaryCalculation = false
    };
    var geo = new PartGeometry
    {
        SourceFile = "part.geo", Format = "GEO", WidthMm = 20, HeightMm = 10,
        NetAreaMm2 = 150, CutLengthMm = 60, ContourCount = 2, IsSafeForPreliminaryCalculation = true
    };
    if (GeometrySourceComparer.Compare(dxf, geo).IsConsistent)
        throw new InvalidOperationException("Unsichere Kundendatei wurde als verifizierte Referenz akzeptiert.");
});

Check("Kalkulationshistorie speichert und lädt neueste Einträge zuerst", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "LaserCalc-History-" + Guid.NewGuid().ToString("N"));
    try
    {
        var store = new QuoteHistoryStore(directory);
        QuoteHistoryEntry MakeEntry(DateTime when, string fileName) => new(
            when, fileName, "TruTops Classic GEO", "Stahl", 0.90m, 7850m, 3m, 1,
            100m, 50m, 4800m, 4800m, 400m, 1, 12m, 80m, 0m, 10m, 1m, null,
            1.20m, 0.50m, 0m, 0.60m, 1.70m, 1.79m, 2.13m, "Geometrie-Schätzung", false, false);
        store.AppendAsync(MakeEntry(DateTime.Now.AddMinutes(-10), "alt.geo")).GetAwaiter().GetResult();
        store.AppendAsync(MakeEntry(DateTime.Now, "neu.geo")).GetAwaiter().GetResult();
        var loaded = store.LoadRecentAsync(1).GetAwaiter().GetResult();
        if (loaded.Count != 1 || loaded[0].SourceFile != "neu.geo")
            throw new InvalidOperationException("Die Historie liefert nicht den neuesten Eintrag zuerst.");
    }
    finally
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
});

Check("Historienvergleich erkennt identische Werkstücke und berechnet Preisänderungen", () =>
{
    QuoteHistoryEntry Entry(DateTime time, decimal cost, decimal recommendation, string file = "Frontblech.geo") => new(
        time, file, "TruTops Classic GEO", "Stahl", 0.90m, 7850m, 4m, 1,
        750m, 700m, 374444.70m, 376037.61m, 23330.088m, 203, 12m, 80m, 0m,
        10m, 1m, null, 20m, 5m, 0m, 18m, cost, cost / 0.95m,
        recommendation, "Geometrie-Schätzung", true, false);
    var older = Entry(DateTime.Now.AddDays(-5), 20m, 25m);
    var newer = Entry(DateTime.Now, 22m, 27.50m);
    var comparison = QuoteHistoryComparison.Compare(older, newer);
    if (!comparison.LikelySamePart || comparison.SelfCostDeltaPerPart != 2m ||
        comparison.RecommendedPriceDeltaPerPart != 2.50m)
        throw new InvalidOperationException("Der Vergleich gleicher Teile oder die Preisdelta-Berechnung ist falsch.");
    var different = QuoteHistoryComparison.Compare(older, Entry(DateTime.Now, 22m, 27.50m, "AnderesTeil.geo"));
    if (different.LikelySamePart || different.Warnings.Count == 0)
        throw new InvalidOperationException("Unterschiedliche Teile wurden nicht als unsicherer Vergleich gekennzeichnet.");
});

Check("Kalkulationshistorie exportiert CSV mit deutschem Trennzeichen und korrekt maskierten Texten", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "LaserCalc-Csv-" + Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "history.csv");
    try
    {
        var entry = new QuoteHistoryEntry(
            DateTime.Now, "Teil; \"Front\".geo", "TruTops Classic GEO", "Edelstahl / VA",
            3.20m, 8000m, 6m, 2, 102m, 60m, 4688.135m, 4624.419m, 412.941m, 2,
            12m, 100m, 0m, 10m, 1m, null, 1.50m, 0.11m, 0m, 0.68m,
            1.61m, 1.70m, 2.02m, "Geometrie-Schätzung", true, false);
        new QuoteHistoryCsvExporter().Export(path, new[] { entry });
        var text = File.ReadAllText(path, Encoding.UTF8);
        if (!text.Contains(";", StringComparison.Ordinal) ||
            !text.Contains("\"Teil; \"\"Front\"\".geo\"", StringComparison.Ordinal) ||
            !text.Contains("Edelstahl / VA", StringComparison.Ordinal) ||
            !text.Contains("1,61", StringComparison.Ordinal))
            throw new InvalidOperationException("CSV-Trennzeichen, Textmaskierung oder deutsches Zahlenformat ist fehlerhaft.");
    }
    finally
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
});

Check("Schnittparameter werden material- und dickengenau aufgelöst", () =>
{
    var presets = new List<CuttingParameterPreset>
    {
        new() { MaterialName = "Stahl", ThicknessMm = 4m, FeedRateMetersPerMinute = 3.5m, PierceSeconds = 1.8m },
        new() { MaterialName = "Edelstahl / VA", ThicknessMm = 6m, FeedRateMetersPerMinute = 1.2m, PierceSeconds = 2.5m }
    };
    var steel = CuttingParameterResolver.Find(presets, "stahl", 4.005m);
    var wrongThickness = CuttingParameterResolver.Find(presets, "Stahl", 5m);
    var wrongMaterial = CuttingParameterResolver.Find(presets, "Aluminium", 4m);
    if (steel is null || steel.FeedRateMetersPerMinute != 3.5m || steel.PierceSeconds != 1.8m ||
        wrongThickness is not null || wrongMaterial is not null)
        throw new InvalidOperationException("Der Werkstoff-/Dickenabgleich ist fehlerhaft.");
});

Check("Datensicherung stellt Materialstamm und Kunden vollständig wieder her", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "LaserCalc-Backup-" + Guid.NewGuid().ToString("N"));
    var backup = Path.Combine(Path.GetTempPath(), "LaserCalc-Backup-" + Guid.NewGuid().ToString("N") + ".zip");
    try
    {
        var settingsStore = new AppSettingsStore(directory);
        var initial = settingsStore.Load();
        initial.Materials.Single(x => x.Name == "Stahl").PricePerKg = 0.91m;
        settingsStore.Save(initial);
        var customers = new CustomerBookStore(directory);
        customers.Upsert(new CustomerRecord("Backup-Kunde GmbH", "Einkauf", "Werkstraße 1", "93426", "Roding", "info@example.invalid", DateTime.Now));

        var backupService = new AppDataBackupService(directory);
        backupService.CreateBackup(backup);

        var changed = new AppSettingsStore(directory).Load();
        changed.Materials.Single(x => x.Name == "Stahl").PricePerKg = 1.25m;
        new AppSettingsStore(directory).Save(changed);
        new CustomerBookStore(directory).Upsert(new CustomerRecord("Neuer Kunde GmbH", "", "Neue Straße 2", "93000", "Regensburg", "", DateTime.Now));

        var restored = backupService.RestoreBackup(backup);
        var restoredSettings = new AppSettingsStore(directory).Load();
        var restoredCustomers = new CustomerBookStore(directory).Load();
        if (restoredSettings.Materials.Single(x => x.Name == "Stahl").PricePerKg != 0.91m ||
            restoredCustomers.Count != 1 || restoredCustomers[0].Name != "Backup-Kunde GmbH" ||
            string.IsNullOrWhiteSpace(restored.SafetyBackupPath) || !File.Exists(restored.SafetyBackupPath))
            throw new InvalidOperationException("Wiederherstellung oder Sicherheitsbackup ist unvollständig.");
    }
    finally
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        if (File.Exists(backup)) File.Delete(backup);
    }
});

Check("Datensicherung weist Pfadmanipulationen und unbekannte Dateien ab", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "LaserCalc-BackupSecurity-" + Guid.NewGuid().ToString("N"));
    var backup = Path.Combine(Path.GetTempPath(), "LaserCalc-BackupSecurity-" + Guid.NewGuid().ToString("N") + ".zip");
    try
    {
        Directory.CreateDirectory(directory);
        using (var stream = File.Create(backup))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            var manifest = archive.CreateEntry("manifest.json");
            using (var writer = new StreamWriter(manifest.Open()))
                writer.Write("{\"Product\":\"LaserCalc 2.0\",\"FormatVersion\":\"1\",\"CreatedAt\":\"2026-11-13T00:00:00+00:00\",\"Files\":[\"settings.json\"]}");
            archive.CreateEntry("../settings.json");
        }
        var rejected = false;
        try { new AppDataBackupService(directory).RestoreBackup(backup); }
        catch (InvalidDataException) { rejected = true; }
        if (!rejected) throw new InvalidOperationException("Eine Datensicherung mit Pfadmanipulation wurde akzeptiert.");
    }
    finally
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        if (File.Exists(backup)) File.Delete(backup);
    }
});

Check("Materialstamm, Preise und Werkstoff-Dicken-Parameter bleiben nach Neustart gespeichert", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "LaserCalc-Settings-" + Guid.NewGuid().ToString("N"));
    try
    {
        var firstStore = new AppSettingsStore(directory);
        var settings = firstStore.Load();
        var steel = settings.Materials.Single(x => x.Name == "Stahl");
        steel.PricePerKg = 0.95m;
        steel.DensityKgPerM3 = 7850m;
        steel.MachineRatePerHour = 82m;
        settings.CuttingParameters.Add(new CuttingParameterPreset
        {
            MaterialName = "Stahl", ThicknessMm = 3m,
            FeedRateMetersPerMinute = 8.5m, PierceSeconds = 0.8m
        });
        firstStore.Save(settings);

        var reloaded = new AppSettingsStore(directory).Load();
        var savedSteel = reloaded.Materials.Single(x => x.Name == "Stahl");
        var savedCutting = reloaded.CuttingParameters.Single();
        if (savedSteel.PricePerKg != 0.95m || savedSteel.MachineRatePerHour != 82m ||
            savedCutting.ThicknessMm != 3m || savedCutting.FeedRateMetersPerMinute != 8.5m)
            throw new InvalidOperationException("Material- oder Schnittparameter wurden nicht dauerhaft gespeichert.");
    }
    finally
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
});

Check("Angebotsrechnung berechnet Rabatt, Umsatzsteuer und Bruttosumme korrekt", () =>
{
    var pricing = OfferPricingCalculator.Calculate(new OfferPricingInput(100m, 3, 10m, 19m));
    if (pricing.LineNetBeforeDiscount != 300m || pricing.DiscountAmount != 30m ||
        pricing.NetTotal != 270m || pricing.VatAmount != 51.30m || pricing.GrossTotal != 321.30m ||
        pricing.EffectiveUnitNetPrice != 90m)
        throw new InvalidOperationException("Die Angebotsrechnung liefert falsche Summen.");
});

Check("Angebotsrechnung blockiert ungültige Rabatte und Mengen", () =>
{
    var discountRejected = false;
    try { OfferPricingCalculator.Calculate(new OfferPricingInput(100m, 1, 100m, 19m)); }
    catch (ArgumentOutOfRangeException) { discountRejected = true; }
    var quantityRejected = false;
    try { OfferPricingCalculator.Calculate(new OfferPricingInput(100m, 0, 0m, 19m)); }
    catch (ArgumentOutOfRangeException) { quantityRejected = true; }
    if (!discountRejected || !quantityRejected)
        throw new InvalidOperationException("Ungültiger Rabatt oder Menge wurde akzeptiert.");
});

Check("Angebotsnummern laufen fort und bleiben nach Neustart erhalten", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "LaserCalc-Offers-" + Guid.NewGuid().ToString("N"));
    try
    {
        var store = new OfferSettingsStore(directory);
        var settings = store.Load();
        var first = store.ReserveNextNumber(settings);
        var reloaded = new OfferSettingsStore(directory);
        var secondSettings = reloaded.Load();
        var second = reloaded.ReserveNextNumber(secondSettings);
        if (!first.EndsWith("0001", StringComparison.Ordinal) || !second.EndsWith("0002", StringComparison.Ordinal))
            throw new InvalidOperationException($"Sequenz ist nicht fortlaufend: {first}, {second}");
    }
    finally
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
});

Check("Kundenstamm speichert Adressen lokal und aktualisiert identische Kunden statt Duplikate anzulegen", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "LaserCalc-Customers-" + Guid.NewGuid().ToString("N"));
    try
    {
        var store = new CustomerBookStore(directory);
        store.Upsert(new CustomerRecord("Beispiel GmbH", "Einkauf", "Hauptstraße 1", "93426", "Roding", "einkauf@example.invalid", DateTime.Now));
        store.Upsert(new CustomerRecord("BEISPIEL GMBH", "Max Mustermann", "Werkstraße 2", "93426", "Roding", "max@example.invalid", DateTime.Now.AddMinutes(1)));
        var reloaded = new CustomerBookStore(directory).Load();
        if (reloaded.Count != 1 || reloaded[0].Contact != "Max Mustermann" || reloaded[0].Street != "Werkstraße 2")
            throw new InvalidOperationException("Kunde wurde nicht aktualisiert oder doppelt gespeichert.");
    }
    finally
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
});

Check("Kundenangebot erzeugt ein gültiges PDF mit Kunde, Angebotsnummer und Summen", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "LaserCalc-OfferPdf-" + Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "angebot.pdf");
    try
    {
        var offer = new OfferDocument(
            "ANG-2026-0001", new DateTime(2026, 11, 13), new DateTime(2026, 11, 27),
            new OfferCompanyProfile { CompanyName = "Mages mechanische Fertigung GmbH", Street = "Musterstraße 1", PostalCode = "93426", City = "Roding" },
            "Beispielkunde GmbH", "Einkauf", "Kundenstraße 2", "93000", "Regensburg", "einkauf@example.invalid",
            "Angebot Laserzuschnitt", "Laserschneidteil Frontblech", "Stahl", 4m, 3, 25m, 0m, 19m,
            "Liefertermin nach Vereinbarung", "14 Tage netto", "Wir freuen uns auf Ihre Bestellung.", 15m, 16m, 20m);
        new CustomerOfferPdfExporter().Export(path, offer);
        var pdf = Encoding.ASCII.GetString(File.ReadAllBytes(path));
        if (!pdf.StartsWith("%PDF-1.4", StringComparison.Ordinal) || !pdf.Contains("ANG-2026-0001", StringComparison.Ordinal) ||
            !pdf.Contains("Beispielkunde GmbH", StringComparison.Ordinal) || !pdf.Contains("Gesamtbetrag netto", StringComparison.Ordinal) ||
            !pdf.Contains("%%EOF", StringComparison.Ordinal))
            throw new InvalidOperationException("Das Angebots-PDF enthält nicht alle erwarteten Angaben.");
        if (pdf.Contains("15,00 EUR", StringComparison.Ordinal) || pdf.Contains("16,00 EUR", StringComparison.Ordinal) ||
            pdf.Contains("20,00 EUR", StringComparison.Ordinal))
            throw new InvalidOperationException("Interne Selbstkosten oder Preisstufen wurden in das Kundenangebot übernommen.");
    }
    finally
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
});

Check("Kundenangebot-PDF weist Rabatt, Nettosumme, Umsatzsteuer und Brutto korrekt aus", () =>
{
    var directory = Path.Combine(Path.GetTempPath(), "LaserCalc-OfferDiscountPdf-" + Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "angebot-rabatt.pdf");
    try
    {
        var offer = new OfferDocument(
            "ANG-2026-0002", new DateTime(2026, 11, 15), new DateTime(2026, 11, 29),
            new OfferCompanyProfile { CompanyName = "Mages mechanische Fertigung GmbH", Street = "Musterstraße 1", PostalCode = "93426", City = "Roding" },
            "Beispielkunde GmbH", "Einkauf", "Kundenstraße 2", "93000", "Regensburg", "einkauf@example.invalid",
            "Angebot Laserzuschnitt", "Laserschneidteil Frontblech", "Stahl", 4m, 3, 25m, 10m, 19m,
            "Liefertermin nach Vereinbarung", "14 Tage netto", "Wir freuen uns auf Ihre Bestellung.", 15m, 16m, 20m);
        new CustomerOfferPdfExporter().Export(path, offer);
        var pdf = Encoding.ASCII.GetString(File.ReadAllBytes(path));
        foreach (var expected in new[] { "75,00 EUR", "7,50 EUR", "67,50 EUR", "12,83 EUR", "80,33 EUR" })
            if (!pdf.Contains(expected, StringComparison.Ordinal))
                throw new InvalidOperationException($"Erwarteter Angebotsbetrag fehlt im PDF: {expected}.");
        if (!pdf.Contains(@"Rabatt \(10,00 %\)", StringComparison.Ordinal))
            throw new InvalidOperationException("Rabattzeile fehlt oder wird falsch formatiert.");
    }
    finally
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
});

Check("PDF-Kalkulationsblatt hat gültige PDF-Struktur", () =>
{
    var geometry = new PartGeometry
    {
        SourceFile = "fixture.GEO", Format = "TruTops Classic GEO", WidthMm = 10, HeightMm = 10,
        NetAreaMm2 = 100, CutLengthMm = 40, ContourCount = 1, EstimatedPierceCount = 1,
        IsSafeForPreliminaryCalculation = true,
        Primitives = Array.Empty<GeometryPrimitive>()
    };
    var inputs = new CalculationInputs(0.90m, 7850m, 3m, 10m, 10m, 100m, 40m, 1, 12m, 80m, 0m, 1, 5m, 20m);
    var result = engine.Calculate(inputs);
    var path = Path.Combine(Path.GetTempPath(), "LaserCalc-Verification-" + Guid.NewGuid().ToString("N") + ".pdf");
    try
    {
        new CalculationPdfExporter().Export(path, geometry, inputs, result, "Stahl");
        var bytes = File.ReadAllBytes(path);
        var pdf = Encoding.ASCII.GetString(bytes);
        if (bytes.Length < 500 || !pdf.StartsWith("%PDF-1.4", StringComparison.Ordinal) || !pdf.Contains("%%EOF", StringComparison.Ordinal))
            throw new InvalidOperationException("PDF-Signatur oder Endmarker fehlt.");
    }
    finally
    {
        if (File.Exists(path)) File.Delete(path);
    }
});

Console.WriteLine();
Console.WriteLine($"Ergebnis: {passed} bestanden, {failed} fehlgeschlagen.");
return failed == 0 ? 0 : 1;

