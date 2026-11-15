# Kundenangebot in LaserCalc 2.0

1. DXF und/oder TruTops-Classic-GEO importieren. Wenn beide vorliegen, wird die Kundendatei gegen das bearbeitete GEO abgeglichen.
2. Material, Dicke und Menge wählen; die Kalkulation zeigt Selbstkosten, Mindestpreis und Empfehlung.
3. `Kundenangebot erstellen` öffnen. Die Empfehlung wird als editierbarer Netto-Stückpreis vorgeschlagen.
4. Absenderdaten der Mages mechanische Fertigung GmbH einmalig ergänzen. Diese werden lokal gespeichert.
5. Kundendaten, Betreff, Beschreibung, Rabatt, Umsatzsteuer, Gültigkeit sowie Zahlungs- und Lieferbedingungen ergänzen.
6. LaserCalc zeigt Netto, Steuer und Brutto live an. Ein Preis unter Selbstkosten oder Mindestpreis erfordert eine ausdrückliche Bestätigung.
7. Das Kunden-PDF wird lokal gespeichert. Interne Selbstkosten und Preisstufen bleiben ausschließlich in der internen Kalkulation und erscheinen nicht im Kundenangebot.

## Angebotsnummern und lokale Daten

Die Angebotsnummern laufen jährlich als `ANG-JJJJ-NNNN`. Absenderprofil, Zähler und Angebotsdefaults werden in `%LocalAppData%\Mages\LaserCalc2\offer-settings.json` gespeichert.

## Noch offen

Die aktuelle Angebotsversion enthält bewusst eine Angebotsposition pro Einzelteil-Kalkulation. Mehrpositionenangebote, Kundenstammdaten mit Wiederverwendung, Angebotsstatus und ein eigener Angebotsverlauf sind geplante Erweiterungen.
