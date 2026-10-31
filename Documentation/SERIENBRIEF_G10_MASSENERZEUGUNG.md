# G10 – Renderjobs für Serienbriefe

`bereiteSerienbriefeVor` erzeugt je ausgewählter Exportzeile einen typisierten Renderjob. Der Service verwendet direkt die G8-Platzhalterauflösung, Briefanrede und Freifeldprüfung; Vorschau und Batch teilen damit denselben Resolver.

Globale Fehler (keine Auswahl, fehlende Pflichtfelder) blockieren den Lauf. Fehler eines Mitglieds oder einer Anschrift bleiben dagegen zeilenbezogen und stoppen andere Jobs nicht. Empfängerdaten werden als vorab geladene Map übergeben, damit der Aufrufer gebündelt laden kann. Der Job enthält Dateinamenskontext, schreibt aber keine PDF, Dokumente oder Versanddaten. G11 übernimmt erst das Rendering.
