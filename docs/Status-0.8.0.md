# LaserCalc 2.0 – Status 0.8.0

## Neue Funktionen in diesem Stand
- Einzelteil-Kalkulation mit DXF-/TruTops-Classic-GEO-Import, gemeinsamer Geometrieprüfung und Vorschau.
- Materialstammverwaltung für Materialpreis/kg, Dichte und interne Maschinen-Vollkosten.
- Werkstoff-/dickenabhängige Schnittparameter, Kundenangebot mit fortlaufender Nummer, Rabatt- und Umsatzsteuerberechnung sowie Kunden-PDF.
- Lokaler Kundenstamm mit Wiederverwendung von Adressen.
- Kalkulationshistorie mit Suche, Vergleich und CSV-Ausgabe.
- Lokale Datensicherung und validierte Wiederherstellung von Einstellungen, Materialstamm, Kunden, Angebotsprofil und Kalkulationshistorie.
- Kalkulationsergebnisse werden bei Änderung wesentlicher Eingaben oder der manuellen Geometrie-Freigabe entwertet; PDF- und Angebotsausgabe bleiben bis zur Neuberechnung gesperrt.

## Prüflage
- Strukturprüfung: 12 XML/XAML-/Projektdateien geprüft, 43 WPF-Ereignisse geprüft, 31 C#-Dateien auf Klammerstruktur geprüft, 8 Projektverweise geprüft.
- Im C#-Prüfprojekt sind 32 Rechen- und Persistenztests angelegt.
- Die 32 C#-Tests wurden in dieser Umgebung nicht ausgeführt; sie sind nicht als bestanden zu verstehen.
- Diese Laufzeitumgebung enthält kein .NET SDK und keine Windows-WPF-Toolchain. Ein echter Windows-Build und ein GUI-Laufzeittest fehlen noch.
- Der Windows-x64-Buildworkflow ist vorbereitet. Für die spätere EXE ist ein Windows-Buildrunner erforderlich; auf dem Firmenrechner wird weder Visual Studio noch das .NET SDK benötigt.

Dieser Stand ist ein Entwicklungsstand und keine produktiv freigegebene Software.
