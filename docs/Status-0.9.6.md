# LaserCalc 2.0 – Status 0.9.6

## Korrektur nach GitHub Actions Build 0.9.5
- `System.IO.Path` im `MainWindow` wird über den Alias `IOPath` eindeutig angesprochen. Damit wird der Konflikt mit `System.Windows.Shapes.Path` beseitigt.
- Die Dateiendung wird explizit als `string` deklariert.
- Versionsnummer auf 0.9.6 angehoben.

## Verifikation
Die Änderung wurde statisch geprüft. Ein echter .NET/WPF-Build muss weiterhin durch GitHub Actions bestätigt werden.
