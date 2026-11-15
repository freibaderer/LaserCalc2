# LaserCalc 2.0 – Status 0.9.2

## Änderung gegenüber 0.9.1

Die Angebots-PDF-Prüfung deckt jetzt zusätzlich einen vollständigen Rabattfall ab: 3 Stück zu 25,00 EUR netto, 10 % Rabatt, 19 % Umsatzsteuer. Erwartet werden 75,00 EUR Zwischensumme, 7,50 EUR Rabatt, 67,50 EUR netto, 12,83 EUR Umsatzsteuer und 80,33 EUR brutto. Damit wird nicht nur die Berechnungsfunktion, sondern auch die tatsächliche Ausgabe der Beträge im PDF-Text geprüft.

## Verifikation

- 38 automatisierte Prüffälle sind im C#-Prüfprojekt hinterlegt.
- Der neue Angebots-PDF-Fall prüft Rabattzeile und alle fünf Summenwerte.
- Die statische Projektprüfung kontrolliert XML, XAML-Eventhandler, Projektverweise, C#-Klammerstruktur, Testfallanzahl und das Fehlen von Kundendateien im Quellpaket.
- Ein echter C#-Build und WPF-Laufzeittest sind in dieser Umgebung weiterhin nicht möglich, da kein .NET-10-SDK/MSBuild verfügbar ist. Der Windows-GitHub-Actions-Workflow ist für den echten Build vorbereitet.

## Freigabestatus

Weiterhin Release Candidate, noch keine produktionsfertige EXE. Vor Einsatz auf dem Arbeitsrechner sind ein erfolgreicher Windows-CI-Build und die Prüfung mit freigegebenen Fertigungsreferenzen erforderlich.
