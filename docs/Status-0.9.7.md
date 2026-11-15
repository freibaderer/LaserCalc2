# LaserCalc 2.0 – Status 0.9.7

## Korrekturen nach GitHub Actions Verifikation
- Rohteilbreite und Maschinenzeit werden in der Anwendung kaufmännisch auf zwei Nachkommastellen ausgegeben; die Tests prüfen nun diese öffentliche Ergebnisgenauigkeit.
- Rüstkosten je Teil werden ebenfalls auf zwei Nachkommastellen gerundet; Test erwartet 0,83 EUR.
- Der GEO-Test nutzt nun eine Flächenabweichung von 2 mm², die oberhalb der implementierten Mindesttoleranz von 1 mm² liegt.
- PDF-Rabattzeile wird mit den korrekt maskierten Klammern des PDF-Textoperators geprüft.

## Verifikation
Die Testkorrekturen sind statisch geprüft. Ein echter Build und die erneute Ausführung der C#-Tests müssen durch GitHub Actions bestätigt werden.
