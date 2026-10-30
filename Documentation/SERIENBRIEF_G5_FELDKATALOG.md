# G5 – Exportfeldkatalog und Platzhalter

## Ergebnis

Der Vorlageneditor zeigt ausschließlich die für den gestarteten Export freigegebenen Serienbrieffelder. Der Katalog wird aus dem bereits beim Serienbrief-Einstieg aufgebauten Exportkontext gespeist; er führt weder eine weitere Datenabfrage aus noch enthält er fest verdrahtete Fachfelder.

Für den Referenzexport `zaehler_eichfaelligkeit` stammen die Felder damit aus `app_export_column_definition` mit `serienbrief_freigegeben = true`. Die technische Empfänger-ID, die technische Parzellen-ID und der Diagnosewert zum Empfängerstatus sind keine Vorlagenfelder.

## Darstellung und Reihenfolge

- Die Felder werden nach `serienbrief_gruppe` gruppiert. Eine fehlende oder leere Gruppe erscheint als **Sonstiges**.
- Innerhalb einer Gruppe gilt die bestehende fachliche Spaltensortierung `sortierung`.
- Angezeigt wird ausschließlich die Metadatenbezeichnung `label_kurz` (im Frontend als `anzeigename`).

## Platzhalter

Ein Klick auf ein Feld fügt den stabilen Schlüssel als `{{feldschluessel}}` an der Cursorposition ein:

- in den Betreff, wenn dieser zuletzt aktiv war;
- in den Rich-Text, wenn der Text zuletzt aktiv war.

Die Auswahl im aktiven Eingabefeld bleibt beim Klick auf den Feldkatalog erhalten. Im Rich-Text wird der Platzhalter über den Text-Einfügebefehl eingefügt; dadurch wird er nicht als HTML interpretiert oder verändert.

Ist noch kein Ziel gewählt, weist der Editor darauf hin. Der Katalog enthält keine freien G6-Felder und erzeugt keine persistierten Vorlagenänderungen.
