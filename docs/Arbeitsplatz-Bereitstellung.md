# Bereitstellung auf dem Firmenrechner

## Ziel
Der Firmenrechner wird erst verwendet, wenn LaserCalc fachlich und technisch ausreichend getestet und als Release paketiert ist. Auf dem Firmenrechner ist keine Entwicklungsumgebung vorgesehen.

## Vorgesehene Ausgabe
- Windows 10/11, x64
- Self-contained .NET-WPF-Anwendung
- Eine startbare `LaserCalc.exe` als Single-File-Paket
- Lokale Einstellungen und Historie unter `%LocalAppData%\Mages\LaserCalc2`
- Kein Visual Studio und keine separate .NET-Runtime auf dem Arbeitsplatz erforderlich

## Qualitätsschranke vor Übergabe
1. Windows-Runner baut die komplette Lösung ohne Fehler.
2. Automatische Rechenprüfungen laufen fehlerfrei.
3. Die EXE wird in einer sauberen Windows-Umgebung gestartet.
4. GEO-/DXF-Import wird mit allen bereitgestellten Referenzdateien geprüft.
5. Schnittlänge, Fläche, Gewicht, Einstiche und Preise werden mit Referenzwerten verglichen.
6. Die Anwendung zeigt bei nicht unterstützten Entitäten oder unklaren Einheiten eine Sperre/Warnung und berechnet nicht stillschweigend einen Preis.

**Status:** Der Windows-Cloud-Build ist vorbereitet, aber noch nicht ausgeführt. Die EXE ist daher noch nicht freigegeben.


## Vorgabe des Betriebs
Der spätere Arbeitsplatz-PC wird bis zur Freigabe nicht als Entwicklungsplatz verwendet. Dort sind weder Visual Studio noch das .NET SDK zu installieren. Entwicklungs-, Test- und Build-Schritte sollen außerhalb des betrieblichen Rechners erfolgen. Ausgeliefert werden soll erst ein Windows-x64-Paket mit selbstständiger EXE.


## Entwicklungsablauf ohne Eingriffe am Firmen-PC
Der Firmen-PC wird während der Entwicklung nicht benötigt. Dort werden weder Visual Studio noch das .NET-SDK installiert. Die Entwicklung und die vorläufigen Tests erfolgen in der Entwicklungsumgebung. Vor der betrieblichen Nutzung muss ein Windows-x64-Build auf einer Windows-Build-Umgebung erfolgreich erstellt und getestet werden. Erst das fertige self-contained Paket wird später auf den Firmen-PC übertragen.
