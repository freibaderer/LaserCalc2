# LaserCalc 2.0 – Status 0.9.3

## Anlass
Erster echter Windows/.NET-10-Cloudbuild hat Compilerfehler in den Importern offengelegt.

## Korrekturen
- GEO-Importer: fehlende Kopf-Abmessungen werden nun als `null` statt als Standardwert `0` repräsentiert; dadurch sind `??`, `HasValue` und `Value` typkorrekt und fehlende Werte bleiben von echten Nullwerten unterscheidbar.
- DXF-Importer: ungenutzte lokale Variable `end` entfernt.
- Versionsnummer auf 0.9.3 angehoben.

## Prüfstatus
Die Korrekturen adressieren die im GitHub-Actions-Lauf angezeigten Compilerfehler. Ein erneuter Windows-Build ist erforderlich; Erfolg wird erst nach einem grünen Actions-Lauf bestätigt.
