# LaserCalc 2.0 – Status 0.9.11

Korrektur des Verifikationstests nach Einführung der 34 TRUMPF TLF 4000 Referenzdatensätze.

Ursache: Der bisherige Persistenztest erwartete genau einen Schnittparameter (`Single()`), legte aber nach der Einführung der Referenzdaten einen zusätzlichen Stahl-3-mm-Datensatz an. Das führte zu „Sequence contains more than one element“ und war ein Fehler in der Testannahme, nicht im eigentlichen Speichervorgang.

Korrektur: Der Test bearbeitet nun den vorhandenen Stahl-3-mm-Datensatz und prüft nach erneutem Laden genau diesen Datensatz sowie den Erhalt aller 34 Referenzwerte.

Der tatsächliche Windows-Build und alle 39 Prüfungen müssen erneut durch GitHub Actions bestätigt werden.
