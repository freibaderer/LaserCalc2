# LaserCalc 2.0 – Status 0.9.4

## Korrekturen nach dem zweiten Windows-Cloudbuild

- `HistoryWindow.xaml.cs`: `System.ComponentModel` für `ICollectionView` eingebunden.
- `LaserCalc.Verification/Program.cs`: GEO- und DXF-Rechtecktests verwenden jetzt `double`-Literale passend zu den Geometriedaten.
- Desktop-Assemblyversion auf 0.9.4 gesetzt.

## Noch offen

Der Windows-Build muss erneut in GitHub Actions ausgeführt werden. Diese Quellkorrekturen wurden hier nicht mit dem .NET SDK kompiliert. Erst nach erfolgreichem Build und bestandenen Verifikationstests gilt die Version als gebaut.
