# Infrastructure

Aktueller Stand:
- `TruTopsClassicGeoImporter`: GEO-Import anhand bereitgestellter TruTops-Classic-Beispiele.
- `AsciiDxfImporter`: ASCII-DXF für LINE, ARC, CIRCLE, LWPOLYLINE sowie POLYLINE/VERTEX.
- `DxfLoopAnalyzer`: schließt lineare/gebogene Segmente anhand Endpunkttoleranz zu Konturen und berechnet Nettofläche.
- `AppSettingsStore`: lokale JSON-Einstellungen.
- `QuoteHistoryStore`: lokale Kalkulationshistorie.

Offen: SQLite-Repository, vollständige DXF-Block-/Spline-Unterstützung, PDF-Angebot, aussagekräftige Historienanalyse und strukturierte Protokollierung.

- `QuoteHistoryCsvExporter`: Exportiert gefilterte Historien-Snapshots als UTF-8-CSV mit Semikolon-Trennung und deutschem Zahlenformat.
