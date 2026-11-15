# LaserCalc 2.0 – Status 0.9.1

## Änderung gegenüber 0.9.0

Die GEO-Importprüfung markiert eine Flächenabweichung zwischen TruTops-Metadaten und rekonstruierter Geometrie weiterhin separat. Die Geometrie bleibt als geschlossene Kontur technisch verwendbar, aber die Oberfläche verlangt vor der Kalkulation eine manuelle Bestätigung oder den erfolgreichen Abgleich mit der zugehörigen Kunden-DXF. Dadurch bleibt der vorgesehene Prüf- und Freigabeschritt erhalten, statt eine manuelle Bestätigung technisch unmöglich zu machen.

## Verifikation

- 37 automatisierte Prüf-Fälle sind im C#-Prüfprojekt hinterlegt.
- Ein GEO-Kreis-Fixture prüft Fläche, Schnittlänge und Außenmaße gegen die exakten Kreisformeln.
- Ein weiterer Fall verändert den GEO-Kopf-Flächenwert eines geschlossenen Test-Rechtecks und prüft, dass `RequiresAreaVerification` gesetzt bleibt und die Geometrie als geschlossene Kontur eingestuft wird; die eigentliche Freigabe erfolgt anschließend durch die vorhandene manuelle Prüfung in der Oberfläche.
- Die statische Projektprüfung kontrolliert XML, XAML-Eventhandler, Projektverweise, C#-Klammerstruktur, Testfallanzahl und das Fehlen von Kundendateien im Quellpaket.
- Ein echter C#-Build und die Ausführung des WPF-Programms konnten in dieser Umgebung weiterhin nicht stattfinden, da kein .NET-10-SDK/MSBuild verfügbar ist. Der GitHub-Actions-Workflow ist dafür vorbereitet.

## Freigabestatus

Weiterhin Release Candidate, noch keine produktionsfertige EXE. Erst nach erfolgreichem Windows-CI-Build und Prüfung der importierten DXF/GEO-Dateien darf die EXE auf dem Arbeitsrechner eingesetzt werden.
