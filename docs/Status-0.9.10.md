# LaserCalc 2.0 – Status 0.9.10

## TRUMPF TLF 4000 Parameter
Aus den vom Anwender bereitgestellten TRUMPF-Tabellen wurden 34 Datensätze übernommen:
- Tabelle 28: Baustahl, Sauerstoff, große Kontur, normales Einstechen mit PCS.
- Tabelle 31: rostfreier Stahl, Stickstoff-Hochdruckschneiden, große Kontur, normales Einstechen mit PCS.
- Tabelle 34: Aluminium, Stickstoff-Hochdruckschneiden, große Kontur, normales Einstechen mit PCS.

Die Werte für Vorschub und PCS-Einstichzeit werden automatisch anhand von Material und Blechdicke geladen. Andere Prozessarten (mittlere/kleine Konturen, sanftes Einstechen, ohne PCS) sind bewusst nicht Teil dieses vereinfachten Startprofils.

Wichtig: Die Werte sind Referenzwerte aus der gelieferten Tabellensammlung. Insbesondere Gasdruck, Düse, Optik, Materialzustand, Maschinenoptionen und reale Schnittqualität sind vor produktiver Kalkulation/Fertigung an der Maschine zu verifizieren. Die aktuelle Kalkulation verwendet Vorschub und Einstichzeit; Gasdruck, Düse und Laserleistung werden noch nicht als Kosten-/Zeitfaktoren modelliert.

## Speicherung / Migration
Ältere Installationen, deren Schnittparameterliste noch leer ist, erhalten die 34 Referenzwerte beim Laden der Einstellungen. Bereits vorhandene, vom Betreiber bearbeitete Parameter werden nicht überschrieben.

## Benutzeroberfläche
Die Materialauswahl in der Hauptmaske verwendet größere Schrift, mehr Kontrast und besser lesbare Dropdown-Einträge.

## Verifikation
Ein zusätzlicher Prüffall deckt repräsentative Werte aus allen drei Materialtabellen ab. GitHub Actions muss den echten Windows-Build und alle 39 Prüfungen bestätigen.
