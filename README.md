# LaserCalc 2.0 – Entwicklungsstand 0.9.7

Windows-Desktopanwendung für Einzelteil-Kalkulation in der Blech-Lohnfertigung.

## Zielausgabe
- Windows 10/11 x64
- WPF / .NET 10
- Self-contained Single-File EXE (`LaserCalc.exe`)
- Keine Visual-Studio-Installation auf dem Firmenrechner
- Lokale Daten unter `%LocalAppData%\\Mages\\LaserCalc2`

## Entwicklungsablauf
Der Windows-Build ist für GitHub Actions vorbereitet. Der Build führt aus:
1. `dotnet restore`
2. `dotnet build -c Release`
3. Konsolen-Verifikationsprogramm mit Geometrie-, Kosten-, Angebot- und Backup-Prüfungen
4. Statische Projektstrukturprüfung
5. `dotnet publish` als self-contained Windows x64 Einzeldatei
6. ZIP-Paket, SHA-256-Prüfsumme und bei Versions-Tag eine GitHub Release-Datei

## Angebots-PDF
Rabatt, Nettosumme, Umsatzsteuer und Bruttosumme werden im Kundenangebot getrennt ausgewiesen. Interne Selbstkosten und interne Preisempfehlungen bleiben aus dem Kunden-PDF ausgeschlossen.

## Finanzmodell
Die Materialkosten basieren auf dem Rohteilrechteck mit 10 mm Materialrand je Seite.
Das Fertigteilgewicht basiert auf der rekonstruierten Nettofläche.
Maschinenstundensätze sind interne Maschinen-Vollkosten, keine Kunden-Verkaufssätze.
Mindest- und Zielpreis verwenden eine Zielmarge als Anteil am Nettoverkaufspreis:
`Verkaufspreis = Selbstkosten / (1 - Marge)`.

## Wichtiger Freigabestatus
Der Quellcode enthält ein eigenständiges Verifikationsprojekt. Ein erfolgreicher Windows-CI-Build und
die Ausführung aller C#-Prüfungen sind Voraussetzung für ein als fertig bezeichnetes EXE-Paket.
Die Software ist bis zur Prüfung mit realen, freigegebenen Fertigungsreferenzen nicht als produktiv
freigegebene Angebotskalkulation einzusetzen.
