# LaserCalc 2.0 – Status 0.9.8

## Buildkorrektur
GitHub Actions meldete CS0219 in `LaserCalc.Verification/Program.cs`: `expectedPerPart` wurde berechnet, aber nicht verwendet. Der Test vergleicht nun `SetupCostPerPart` mit dem auf zwei Nachkommastellen kaufmännisch gerundeten Erwartungswert.

## Verifikation
Statische Projektprüfung ausstehend. Ein echter .NET-Build und die Funktionstests müssen von GitHub Actions bestätigt werden.
