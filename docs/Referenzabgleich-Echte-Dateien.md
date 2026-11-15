# Referenzabgleich mit den bereitgestellten TruTops-Classic- und Kunden-DXF-Dateien

Dieser Bericht fasst eine unabhängige technische Sichtprüfung der hochgeladenen Originaldateien zusammen. Die Zahlen wurden aus den GEO-Punktreferenzen/Primitiven und den DXF-Entitäten mathematisch ausgewertet. Sie sind **kein Ersatz für einen Build- und Laufzeittest des C#-Importers**, der in der aktuellen Umgebung mangels .NET-SDK nicht ausführbar ist.

## 4-mm-Frontblech

- DXF-Entitäten: 404 LINE, 400 ARC, 2 CIRCLE.
- GEO: 203 Konturblöcke, 1206 referenzierte Punkte, 404 LIN, 400 ARC, 2 CIR.
- Aus beiden Quellen rekonstruierte Gesamtschnittlänge: rund **23.330,0876 mm**.
- TruTops-GEO-Kopfmaß: 750 × 700 mm.
- Aus den GEO-Konturen rekonstruierte Nettoteilfläche: rund **374.444,6967 mm²**.
- Im GEO-Kopf gespeicherter Flächenwert: **376.037,6127 mm²**. Das ist ein eigenständiger Metadatenwert und weicht von der aus Konturen rekonstruierten Fläche ab; LaserCalc hält beide Werte getrennt und zeigt den Prüfhinweis.

## 6-mm-VA-Teil

- DXF-Entitäten: eine geschlossene klassische POLYLINE mit drei Geraden und einem Bulge-Kreisbogen sowie ein CIRCLE.
- GEO: 2 Konturblöcke, 6 Punkte, 3 LIN, 1 ARC, 1 CIR.
- Aus beiden Quellen rekonstruierte Gesamtschnittlänge: rund **412,9414 mm**.
- Aus den GEO-Konturen rekonstruierte Nettoteilfläche: rund **4.688,1359 mm²**.
- Im GEO-Kopf gespeicherter Flächenwert: **4.624,4192 mm²**. Die Abweichung bleibt sichtbar und darf nicht stillschweigend als geometrisch bestätigte Fläche behandelt werden.
- Der Fall prüft insbesondere die korrekte Behandlung des klassischen POLYLINE-Bulge-Werts und eines inneren Kreises.

## Konsequenz für LaserCalc

1. Kunden-DXF bleibt die ursprüngliche Kundenquelle.
2. TruTops Classic GEO bleibt die bevorzugte Quelle nach der Maschinenprogrammierung.
3. Schnittlänge, Nettogeometriefläche und gespeicherte TruTops-Metadaten werden getrennt geführt.
4. Ein DXF/GEO-Paar wird nur als abgeglichen bewertet, wenn beide Geometrien sicher sind und Maße, Schnittlänge, Nettoteilfläche und Konturanzahl innerhalb der definierten Toleranzen übereinstimmen.
5. Der Import einer Kundendatei mit nicht unterstützten Elementen wie INSERT/SPLINE darf nicht stillschweigend einen scheinbar sicheren Angebotspreis erzeugen.

**Hinweis:** Die 10-mm-Materialzugabe ist in der Kalkulation als Zugabe je Seite umgesetzt. Die Einstichzahl bleibt eine Näherung aus geschlossenen Schnittkonturen, solange keine eindeutigen TruTops-Prozessdaten für tatsächliche Anstechvorgänge vorliegen.
