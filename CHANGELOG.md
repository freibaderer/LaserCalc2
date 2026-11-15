## 0.9.5 – Korrekturen nach zweitem Windows-Cloudbuild

- HistoryWindow: fehlender Namespace für ICollectionView ergänzt.
- Verifikationsfälle für GEO- und DXF-Rechtecke: numerische Literale an double-Geometriedaten angepasst.
- Versionsnummer auf 0.9.5 angehoben.

## 0.8.0 – Datensicherung und sichere Ergebnisfreigabe
- Lokale Datensicherung als ZIP für Einstellungen, Materialstamm, Angebotsprofil, Kunden und Kalkulationshistorie.
- Wiederherstellung prüft Manifest, Dateiliste, Dateigrößen, erlaubte Dateinamen und typisierte JSON-Strukturen; Pfadmanipulationen und unbekannte Dateien werden abgewiesen.
- Vor Wiederherstellung wird der vorhandene Datenstand automatisch als Sicherheitskopie archiviert.
- Nach Wiederherstellung wird die Anwendung in der gepackten LaserCalc.exe automatisch neu gestartet.
- Kalkulationsabhängige Eingabefelder invalidieren ein vorhandenes Ergebnis; PDF- und Angebotsausgabe werden bis zur erneuten Berechnung gesperrt.
- 32 Verifikationsfälle im C#-Prüfprojekt, einschließlich Datensicherungs-/Wiederherstellungsfall.
- Projektversion auf 0.8.0 angehoben.
- Kein .NET SDK bzw. Windows-Runner in der aktuellen Arbeitsumgebung: tatsächlicher C#-Build und WPF-Laufzeittest stehen weiterhin aus.

## 0.7.0 – Materialstamm und lokaler Kundenstamm
- Eigener Materialstamm-Dialog zum Hinzufügen, Bearbeiten und Löschen von Werkstoffen.
- Materialpreis/kg, Dichte und interne Maschinen-Vollkosten werden gemeinsam validiert und dauerhaft gespeichert.
- Beim Materialstammwechsel bleibt der zuvor ausgewählte Werkstoff erhalten, sofern er noch vorhanden ist.
- Lokales Kundenadressbuch für den Angebotsdialog; vorhandene Kunden können anhand von Firma und Ort ausgewählt werden.
- Nach erfolgreicher PDF-Erstellung werden Kundendaten lokal gespeichert bzw. anhand Firma/PLZ/Ort aktualisiert.
- Fehler beim Speichern des Kundenstamms verhindern nicht mehr die Erfolgsmeldung für ein bereits erzeugtes PDF.
- Angebotsmengenänderungen lösen eine erneute Verteilung der Rüstkosten aus; manuell angepasste Angebotspreise bleiben erhalten und werden gegen die neu kalkulierten Kosten geprüft.
- Verifikationsprojekt um Tests für Material-/Schnittparameter-Persistenz, Kundenstamm-Deduplizierung und mengenabhängige Rüstkosten ergänzt.
- Projektversion auf 0.7.0 angehoben.
- Weiterhin kein .NET SDK in der aktuellen Umgebung: C#-Build und Windows-WPF-Laufzeittest sind nicht ausgeführt.

## 0.6.0 – Kundenangebote und Schnittparameter
- Eigenständiger Angebotsdialog aus einer abgeschlossenen Einzelteil-Kalkulation.
- Fortlaufende Angebotsnummern im Format ANG-JJJJ-NNNN; Sequenz und Absenderprofil werden lokal gespeichert.
- Kundenanschrift, Ansprechpartner, Betreff, Positionsbeschreibung, Angebotsdatum, Gültigkeit, Liefer-/Zahlungsbedingungen und Schlusstext sind im Angebot editierbar.
- Netto-Stückpreis kann von der Kalkulationsempfehlung abweichen; Rabatt, Umsatzsteuer und Bruttosumme werden live berechnet.
- Warn- und Bestätigungsdialog, wenn der effektive Preis nach Rabatt unter Selbstkosten oder Mindestpreis liegt.
- Eigenständiges einseitiges Kundenangebots-PDF mit MAGES-Kopf, Angebotssumme und Konditionen. Interne Selbstkosten, Mindestpreis und Empfehlung werden nicht ins Kunden-PDF übernommen.
- Verwaltung material- und dickenspezifischer Schnittparameter (Vorschub und Einstichzeit); vorhandene Kennwerte werden bei Material-/Dickenänderung automatisch geladen.
- Allgemeine Startwerte bleiben Fallback, wenn kein exakter Werkstoff-/Dickenkennwert gepflegt ist. Keine erfundenen Maschinenkennfelder.
- Verifikationsprojekt um Tests für Angebotssteuer, Umsatzsteuer, Rabatt, Angebotsnummern-Persistenz, Kunden-PDF und Schnittparameterauflösung erweitert.
- Projektversion auf 0.6.0 angehoben.
- Kein .NET-SDK in dieser Linux-Umgebung: C#-Kompilierung und Windows-WPF-Laufzeittest bleiben offen.

# Changelog

## 0.5.2 – Historische Kalkulationen vergleichen
- In der Historie können zwei Einträge per Strg+Klick markiert und direkt verglichen werden.
- Das Programm prüft Dateiname, Material, Dicke, Außenmaße (einschließlich 90°-Drehung), Nettofläche und Schnittlänge, bevor es das Werkstück als wahrscheinlich identisch kennzeichnet.
- Differenzen bei Selbstkosten, Mindestpreis, Empfehlung, Materialkosten, Maschinenkosten und Zeit werden angezeigt.
- Regressionstest für gleiche und unterschiedliche Werkstücke ergänzt.

## 0.5.1 – Historie exportieren
- Gefilterte Kalkulationshistorie kann als UTF-8-CSV mit Semikolon-Trennzeichen für deutsches Excel exportiert werden.
- CSV enthält Geometrie-, Material-, Zeitbasis-, Kosten-, Preis- und Prüfstatusfelder.
- Regressionstest für deutsches Zahlenformat und korrekt maskierte CSV-Texte ergänzt.

## 0.5.0 – Durchsuchbare Kalkulationshistorie
- Teilehistorie aus dem Seitenmenü öffnet eine eigene Übersicht mit Datum, Datei, Werkstoff, Dicke, Menge und den drei Preisstufen.
- Historie kann nach Datei, Werkstoff, Quellformat, Zeitbasis und Datum durchsucht werden.
- Neueste Kalkulationen stehen zuerst; Zeitbasis und Maschinenparameter sind in der Detailzeile sichtbar.
- Regressionstest für Speicherung, Sortierung und Laden der Historie ergänzt.

## 0.4.3 – DXF-Einheiten und Sicherheitsprüfungen
- Tests für die Zoll-zu-Millimeter-Umrechnung und das sichere Sperren nicht unterstützter SPLINE-Geometrie ergänzt.

## 0.4.2 – DXF-Bulge-Validierung
- Regressionstest für klassische POLYLINE mit Bulge-Kreisbogen, exakter Bounding-Box, Schnittlänge und Kreisloch-Nettofläche ergänzt.

## 0.4.1 – Quellenvergleich und Materialausnutzung
- DXF/GEO-Vergleich in einen separat testbaren Dienst ausgelagert.
- Fehlende Nettoflächen werden nicht mehr als erfolgreicher Quellenabgleich behandelt.
- Beide Quellen müssen sichere Geometrie liefern; Konturanzahl wird mitgeprüft.
- Um 90° gedrehte, ansonsten identische Teile werden beim Außenmaßvergleich akzeptiert.
- Neue Regressionstests für Kreisloch-/Nettoflächenberechnung, gedrehte Quellpaare, fehlende Flächen, unsichere Quelldaten, abweichende Konturanzahl und ungültige Null-Maschinenzeit.
- Der bestätigte Materialrand von 10 mm pro Seite wird nicht mehr als vorläufiger Prüfhinweis ausgegeben.
- Materialausnutzung und rechnerische Verschnittfläche werden pro Teil berechnet, in der Oberfläche angezeigt und im PDF ausgewiesen.
- Zusatztest für die Materialausnutzungs- und Verschnittformel ergänzt.
- Weiterhin kein erfolgreicher Windows-Build in dieser Arbeitsumgebung; .NET-SDK/WPF-Toolchain fehlt.

## 0.2 – Erster integrierter Kalkulations-Prototyp
- WPF-Oberfläche für Einzelteil-Kalkulation modernisiert.
- TruTops-Classic-GEO-Import mit LIN/ARC/CIR, Punktreferenzen und Konturprüfung.
- ASCII-DXF-Import für LINE/ARC/CIRCLE/LWPOLYLINE/POLYLINE inklusive Bulge-Bögen.
- DXF/GEO-Quellenabgleich für Schnittlänge, Nettofläche und Abmessungen.
- GEO-Metadatenflächenabweichung wird sichtbar gemacht; manueller Prüfschritt oder DXF-Abgleich erforderlich.
- Materialverbrauch, Maschinenzeit, Einstiche und drei Preisstufen kalkulierbar.
- Materialpreise, Dichten, Maschinen-Kalkulationssätze und Zeitwerte speicherbar.
- Kalkulationshistorie speichert Eingaben und Ergebnisse als lokale JSON-Snapshots.
- Exakte Bounding-Box-Ermittlung für Linien, Kreise und Kreisbögen.
- Außen-/Innenkonturen aus GEO werden in der Vorschau unterschiedlich dargestellt.
- Plausibilitätsprüfung sperrt Flächenwerte oberhalb des umschließenden Rechtecks.
- Paketfreies Verifikationsprojekt für die zentralen Kosten- und Preisformeln.
- Build-Skript für Windows hinzugefügt.

## Noch nicht enthalten
- Vollständiges Kundenangebotslayout mit Kundenstammdaten, Zahlungsbedingungen und Angebotsnummer
- Wiederaufnahme einer gespeicherten Kalkulation als neue Kalkulationsvorlage
- Vollständige SQLite-Datenbank
- Komplexe DXF-Elemente wie SPLINE, INSERT/Blockreferenzen, ELLIPSE und HATCH
- Maschinenkennfelder, Beschleunigungs-/Eckenlogik und Prozessgas-/Gemeinkosten
- Automatische historische Preisempfehlung / KI-Auswertung


## 0.3.0 – Deployment-Vorbereitung
- Maschinenstundensätze im Modell ausdrücklich als interne Maschinen-Vollkosten dokumentiert.
- Irreführenden Warnhinweis zur noch offenen Einordnung des Maschinenstundensatzes entfernt.
- Windows-x64-Publish-Profil für self-contained Single-File-Ausgabe ergänzt.
- Windows-Runner-Workflow vorbereitet: Restore, Build, Rechenprüfungen, Publish und ZIP-Artefakt.
- Margen-Defaults ausdrücklich als vorläufig und konfigurierbar markiert.
- Kein Einsatz von Visual Studio auf dem späteren Arbeitsrechner vorgesehen.

- UI-Texte auf bestätigte interne Maschinen-Vollkosten angepasst; überholten Bestätigungshinweis entfernt.
- Bereitstellungsanleitung ergänzt: Firmenrechner bleibt bis zum geprüften Release außen vor.


## 0.4.0 – DXF-Konturprüfung und PDF-Kalkulationsblatt
- Fehler in der DXF-Konturverfolgung behoben: Startsegmente einer normalen geschlossenen Kontur werden nicht mehr als Verzweigung fehlinterpretiert.
- Zusätzliche Prüfung auf echte Verzweigungen am Konturstart ergänzt.
- Interner PDF-Export des Kalkulationsblatts (A4, Kostenaufschlüsselung, Zeitbasis, Preise und Prüfhinweise) ohne zusätzliche PDF-Bibliothek ergänzt.
- PDF-Export steht erst nach erfolgreicher Kalkulation zur Verfügung.
- Verifikationsprojekt um GEO-Rechteck, DXF-R12-Rechteck, bestätigte Maschinenzeit, Rüstkostenverteilung und PDF-Signaturprüfung erweitert.
- Maschinenstundensätze bleiben ausdrücklich interne Vollkosten.
- Kein Visual-Studio- oder .NET-SDK-Einsatz auf dem späteren Firmen-PC vorgesehen.

## 0.9.0 – Release-Candidate-Vorbereitung
- Finanzielle Regressionstests ergänzt: Zielmarge als Anteil des Nettoverkaufspreises, Mindestmarge und interne Maschinen-Vollkosten.
- Windows-CI baut und veröffentlicht ein selbstständiges Windows-x64-Einzeldatei-Paket.
- Versionierte Tags erzeugen eine Release-ZIP-Datei; SHA-256-Prüfsumme wird beigelegt.
- Lese-Mich-Anleitung beschreibt Start, lokale Datenablage und Datensicherung ohne Visual Studio.
- Keine Kunden-DXF/GEO-Beispieldateien werden in den Quellcode übernommen.

## 0.9.1 – GEO-Sicherheitsprüfung

- GEO-Metadatenabweichungen bleiben als eigener Prüfstatus erhalten. Die Oberfläche verlangt vor der Kalkulation eine manuelle Bestätigung oder einen erfolgreichen Abgleich mit der Kunden-DXF.
- Regressionstest prüft, dass die Flächenabweichung erkannt wird und die Geometrie als geschlossene Kontur erhalten bleibt.
- GEO-Kreisimport mit Regressionstest für Fläche, Schnittlänge und Außenmaße ergänzt.
- Verifikationsumfang auf 37 Fälle erweitert.

## 0.9.2 – Angebots-PDF-Prüfung

- Zusätzlicher Regressionstest prüft die vollständige Angebots-PDF-Ausgabe mit Rabatt, Nettosumme, Umsatzsteuer und Bruttosumme anhand eines konkreten Rechenfalls.
- Dokumentation des Angebots-PDFs präzisiert: interne Selbstkosten und Preisempfehlungen bleiben aus dem Kundenangebot ausgeschlossen.
- Verifikationsumfang auf 38 Fälle erweitert.


## 0.9.3 – Korrekturen nach erstem Windows-Cloudbuild
- GEO-Importer: optionale Header-Abmessungen korrekt nullable modelliert.
- DXF-Importer: ungenutzte lokale Variable entfernt.
- Versionsnummer auf 0.9.3 angehoben.

## 0.9.5
- System.IO-Namespaces ergänzt und WPF Application.Current voll qualifiziert nach Fehleranalyse des Windows-Cloudbuilds.

## 0.9.6 – Korrektur mehrdeutiger Path-Referenzen
- System.IO.Path im Desktop-Hauptfenster über eindeutigen Alias referenziert.
- Dateiendungsvariable explizit als string typisiert.

## 0.9.7 – Korrektur der Verifikationsfälle
- Prüferwartungen an die kaufmännische Rundung der Ergebniswerte angepasst.
- GEO-Flächenabweichung im Test auf einen Wert oberhalb der Toleranz erhöht.
- PDF-Rabattzeile im Test unter Berücksichtigung der PDF-Escapes geprüft.
