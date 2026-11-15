# LaserCalc 2.0 – Status 0.9.5

## Korrekturen nach Windows-Cloudbuild 0.9.4

- `System.IO` explizit in MainWindow und OfferWindow eingebunden, damit `File`, `Path` und `IOException` eindeutig aufgelöst werden.
- `System.Windows.Application.Current` voll qualifiziert, um eine Namensraumkollision mit `LaserCalc.Application` zu vermeiden.
- Build-Fehler aus GitHub Actions als konkrete Grundlage für diese Korrekturen verwendet.

## Prüfstatus

Die Änderungen wurden statisch am Quellcode vorgenommen. Ein erneuter .NET-Windows-Build wurde in dieser Umgebung nicht ausgeführt. Freigabe erst nach erfolgreichem GitHub Actions Lauf und Funktionstest.
