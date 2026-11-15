# Referenzabgleich der bereitgestellten Dateien

Die Werte unten wurden aus den Originaldateien unabhängig mit einem Python-Referenzskript und `ezdxf` ausgewertet. Sie prüfen die Annahmen des C#-Importers; sie ersetzen keinen Build- oder Laufzeittest des WPF-Programms.

## TruTops-Classic-GEO-Stichprobe

| Datei | Punkte | Konturblöcke | Schnittlänge rekonstruiert | Fläche aus Geometrie | Fläche aus GEO-Kopf |
|---|---:|---:|---:|---:|---:|
| 3mmStahlEnglmeier | 5 | 1 | 662,949 mm | 10.302,00 mm² | 10.302,00 mm² |
| 4mmStahFrontblech16+36 | 1.206 | 203 | 23.330,088 mm | 374.444,70 mm² | 376.037,61 mm² |
| 6mmVAMarkl3-880-46-11281 | 6 | 2 | 412,941 mm | 4.688,14 mm² | 4.624,42 mm² |
| 10_135_262 | 6 | 1 | 762,368 mm | 34.641,00 mm² | 34.641,00 mm² |
| FCB-1 | 872 | 20 | 8.025,341 mm | 280.702,58 mm² | 280.662,79 mm² |
| FCB2 | 1.350 | 88 | 18.489,296 mm | 250.274,72 mm² | 250.847,09 mm² |
| FCB3 | 2.193 | 161 | 20.950,372 mm | 260.555,31 mm² | 261.007,85 mm² |
| FCB4 | 4 | 1 | 1.350,000 mm | 113.906,25 mm² | 113.906,25 mm² |
| FCB-Rekordmeister | 1.459 | 19 | 7.589,836 mm | 284.165,75 mm² | 284.143,52 mm² |
| SAB S250227 | 202 | 154 | 9.243,719 mm | 45.777,61 mm² | 42.931,41 mm² |

Alle in diesen Beispielen geprüften Punktreferenzen sind vorhanden. Die Segment-Endpunkte in den GEO-Konturfolgen schließen in der Stichprobe innerhalb 0,05 mm. Die beobachteten ARC-Radien sind innerhalb der Rundungsgenauigkeit konsistent.

**Interpretation:** Die gespeicherten Flächenwerte der GEO-Konturblöcke summieren sich in der Stichprobe auf den GEO-Dateikopf. Diese gespeicherten Werte sind jedoch nicht durchgehend identisch mit der neu berechneten Fläche der Punkt-/Linien-/Kreisbogen-Geometrie. Die Datei-Metadaten dürfen daher nicht stillschweigend als physische Fertigteilfläche übernommen werden.

## DXF/GEO-Paarvergleiche

| Referenzpaar | DXF-Inhalt | DXF-Schnittlänge | GEO-Schnittlänge | Ergebnis |
|---|---|---:|---:|---|
| 4-mm-Frontblech | 404 LINE, 400 ARC, 2 CIRCLE | 23.330,087610 mm | 23.330,087610 mm | praktisch deckungsgleich |
| 6-mm-VA-Markl | 1 geschlossene POLYLINE, 1 CIRCLE | 412,941441 mm | 412,941441 mm | praktisch deckungsgleich |

Für beide Paare stimmt die aus der Geometrie rekonstruierte DXF-Nettofläche ebenfalls mit der GEO-Geometriefläche auf die numerische Rundung überein. Die im GEO-Kopf gespeicherten Flächen weichen dagegen ab (siehe Tabelle oben). Beim 6-mm-DXF sind außerdem `$EXTMIN`/`$EXTMAX` nicht als verlässliche Teilegrenzen zu verwenden: Aus den tatsächlichen Geometrieelementen ergeben sich 102,0128 × 60,0000 mm – passend zum GEO-Kopf. Das ist der Grund, warum LaserCalc die Grenzen aus der Geometrie berechnet und die gespeicherten Datei-Header nur als Kontrollinformation behandelt.

## Vorläufige Kalkulationsprobe: 6-mm-VA-Markl

Mit 6 mm VA, 10 mm Materialrand je Seite, 3,20 €/kg, 8.000 kg/m³, 100 €/h internen Maschinen-Vollkosten, 12 m/min, 2 Einstichen und 1 s je Einstich ergibt die aktuelle Modellformel ungefähr:

- Rohteilrechteck: 122,013 × 80,000 mm
- Rohteilgewicht: 0,469 kg
- Materialkosten: 1,50 €
- Geometrie-/Einstichzeit: 0,0677 min je Stück
- Maschinenkosten: 0,11 €
- Selbstkostenmodell: 1,61 € je Stück (Material + bestätigte interne Maschinen-Vollkosten; ohne Rüstzeit)
- Mindestpreis bei 5 % Marge: 1,70 €
- Empfehlung bei 20 % Marge: 2,02 €

Dies ist nur eine Rechenprobe der aktuellen Formel, keine freigegebene Angebotskalkulation. Die Margen von 5 % und 20 % sind veränderbare Startwerte und müssen noch als betriebliche Preisregeln bestätigt werden.


## DXF-Konturverfolgung – zusätzlicher Regressionstest

Beim Prüfen der gelieferten Frontblech-DXF-Datei wurde ein Fehlalarm in der bisherigen Graph-Verfolgung gefunden: Das erste Segment einer geschlossenen Kontur war noch nicht als benutzt markiert. Dadurch zählte der Startknoten sein Startsegment und den regulären Nachbarn fälschlich als Verzweigung. Der Algorithmus startet jetzt mit dem Seed-Segment und prüft danach verbleibende Kanten. Eine separate Abschlussprüfung erkennt echte zusätzliche Kanten am Startpunkt.

Unabhängige Nachrechnung der beiden bereitgestellten DXF/GEO-Paare:

| Paar | DXF-Schnittlänge | GEO-Schnittlänge | DXF-Nettofläche | GEO-Geometriefläche | Abmessungen | Konturen |
|---|---:|---:|---:|---:|---:|---:|
| 4-mm-Frontblech | 23.330,087610 mm | 23.330,087610 mm | 374.444,696681 mm² | 374.444,696681 mm² | 750 × 700 mm | 203 |
| 6-mm-VA-Markl | 412,941441 mm | 412,941441 mm | 4.688,135881 mm² | 4.688,135881 mm² | 102,0128 × 60 mm | 2 |

Für die beiden DXF/GEO-Paare ergibt die korrigierte Konturverfolgung keine offenen Konturen und keine mehrdeutigen Verzweigungen. Die Werte sind eine unabhängige Referenzrechnung aus den gelieferten Beispieldateien; der C#-Build und ein Laufzeittest in Windows stehen weiterhin aus.


## Quellenvergleich v0.4.1
Der DXF/GEO-Vergleich ist als `GeometrySourceComparer` aus der Oberfläche herausgelöst und separat prüfbar. Vorläufige Grenzen: 0,5 mm Schnittlänge, 1,0 mm² Nettofläche und 0,2 mm Außenmaß. Bei den Außenmaßen wird eine 90°-Drehung akzeptiert. Der Abgleich gilt nur als erfolgreich, wenn beide Importer ihre Geometrie als sicher einstufen, beide Nettoflächen vorhanden sind, die Konturanzahl übereinstimmt und alle Messwerte innerhalb der Grenzen liegen. Fehlende Werte werden nicht mehr als erfolgreiche Prüfung gewertet.

Der C#-Verifikationstestbestand umfasst nun 14 Fälle. Die Testfälle sind im Quellcode vorbereitet, konnten in dieser Umgebung mangels .NET SDK jedoch noch nicht ausgeführt werden.


## Materialausnutzung – Beispiel 6-mm-VA-Markl
Mit den rekonstruierten Außenmaßen 102,0128 × 60,0000 mm und 10 mm Rand je Seite beträgt das kalkulierte Rohteilrechteck 122,0128 × 80,0000 mm bzw. 9.761,02 mm². Gegenüber der geometrischen Nettofläche von 4.688,14 mm² ergibt das rund 48,03 % Materialausnutzung und 5.072,89 mm² rechnerische Verschnittfläche im Rechteck. Der Wert beschreibt die Rechteckkalkulation, nicht eine optimierte Blechtafelverschachtelung.
