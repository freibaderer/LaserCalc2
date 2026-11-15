# LaserCalc 2.0 – Status 0.9.9

## GEO-Flächenabweichungstest
Der vorherige Test verwendete eine Text-Ersetzung, die nicht zuverlässig den Header-Flächenwert an der vom Importer ausgewerteten Zeile änderte. Nun wird der normalisierte GEO-Text in Zeilen zerlegt und der Headerwert an Index 6 gezielt auf 102 mm² gesetzt, während die Geometrie 100 mm² bleibt. Damit prüft der Test wirklich die Abweichung zwischen Dateikopf und rekonstruierter Kontur.

## Verifikation
Die statische Projektprüfung muss durch GitHub Actions erneut laufen. Ein echter .NET/WPF-Build kann hier nicht ausgeführt werden.
