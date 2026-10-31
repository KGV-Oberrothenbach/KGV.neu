# G8 – Serienbriefvorschau

## Laufzustand

G8 hält einen transienten Laufzustand mit `selectedRows`, `freifeldWerte` und `previewIndex`. Ausschließlich die in G7 ausgewählten gültigen Zeilen werden verwendet. Die erste Vorschau startet bei Index 0; die Navigation bleibt an den Grenzen deaktiviert.

## Daten und Platzhalter

Freifelder werden einmalig pro Lauf vor der Vorschau eingegeben; Pflichtfelder blockieren den Start. Text, Datum und Zahl verwenden die G6-Typen. Datumswerte werden deutsch formatiert, Zahlen mit deutscher Standardformatierung.

Export- und Freifeldplatzhalter werden über das einheitliche `{{feldschluessel}}`-Format aufgelöst. Unbekannte oder leere verwendete Werte werden sichtbar als Fehler gemeldet und nie unbemerkt leer ersetzt. Ersetzungswerte werden im HTML kontextgerecht escaped und die Ergebnisansicht wird mit dem bestehenden DOMPurify-Sanitizer bereinigt.

## Empfänger und Anrede

Die Anschrift wird für jeden ausgewählten `mitglied_id` über den Mitgliedsstamm geladen. Die zentrale Anredefunktion verwendet ausschließlich ein explizites Anredefeld (`frau`/`herr`), falls es aus dem Mitgliedsvertrag verfügbar ist; sie leitet kein Geschlecht aus Vornamen ab und verwendet sonst den Gartenfreund/in-Fallback. Der aktuelle Mitgliedsstamm enthält noch kein solches Anredefeld, daher greift in der Live-Vorschau derzeit der Fallback.

## Grenzen

Vorschau und Laufdaten bleiben read-only und lokal. G8 erzeugt keine PDFs, speichert keine Vorlagen oder Läufe und führt keinen Versand aus. G9 kann die Vorlagenspeicherung anschließen.
