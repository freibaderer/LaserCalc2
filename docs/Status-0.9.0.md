# LaserCalc 2.0 – Status 0.9.0

## Neu in 0.9.0
- Drei finanzielle Regressionstests:
  - 20 % Zielmarge ist exakt 20 % vom Nettoverkaufspreis.
  - 5 % Mindestmarge wird korrekt rückwärts berechnet.
  - Maschinen-Vollkosten werden als interne Selbstkosten angesetzt.
- Windows-Pipeline erzeugt self-contained Single-File EXE und ZIP.
- SHA-256 der EXE wird im Paket abgelegt.
- GitHub Release wird bei Versions-Tags `v*` erzeugt.
- Installationshinweise explizit ohne Visual Studio.

## Noch nicht verifiziert
- Tatsächlicher Windows-Build und Ausführung der C#-Prüfungen.
- WPF-Laufzeitverhalten und Dateidialoge auf Windows 10/11.
- PDF-Darstellung mit langen Firmen-/Kundenangaben.
- Geometrie- und Zeitabgleich an weiteren freigegebenen realen Teilen.

## Benutzeraufwand
Für den Firmen-PC ist weiterhin kein Visual Studio vorgesehen. Ziel ist nur das Entpacken und Starten
der fertigen `LaserCalc.exe` nach bestandener Freigabe.
