# G7 – Empfängerauswahl

## Auswahlmodell

Die Auswahl ist ein transienter Zustand des G3-Serienbrief-Einstiegs. Eine schlüsselbasierte `Set`-Auswahl enthält nur gültige Exportdatensätze; sie wird weder in Exportdefinitionen, Vorlagen oder Datenbanktabellen gespeichert.

Beim ersten Öffnen werden alle Datensätze mit eindeutiger `mitglied_id` ausgewählt. Konflikte bleiben nicht ausgewählt und ihre Checkbox ist deaktiviert. Die vorhandenen G3-Status und Konflikttexte bleiben führend.

## Identität und vollständige Treffermenge

Die Datensatzidentität wird deterministisch aus Mitglied-, Parzellen-, Status- und fachlichen Zeilenwerten erzeugt, nicht aus einem Listenindex. Mehrere Zeilen desselben Mitglieds, etwa für unterschiedliche Zähler, bleiben damit unabhängig auswählbar.

Alle Auswahlactions arbeiten gegen `context.datensaetze`, die vollständige G3-Ergebnismenge. Der abgeleitete Wert `selectedRows` enthält ausschließlich ausgewählte, gültige Datensätze und ist die Übergabe für G8 und spätere Schritte.

## Bedienung

- Einzelne gültige Zeilen sind per Checkbox wählbar.
- **Alle auswählen** wählt nur gültige Datensätze.
- **Auswahl aufheben** entfernt alle Auswahlzustände.
- Zähler zeigen Gesamtzahl, gültige Empfänger, Auswahl und Konflikte.
- Der Wechsel in den lokalen Editor und zurück bewahrt den Auswahlzustand, da er auf derselben Feature-Komponente liegt.

Der Weiter-Button bleibt ohne mindestens einen ausgewählten gültigen Datensatz deaktiviert. G7 öffnet noch keine Vorschau und fragt weder Freifeldwerte ab noch persistiert es Laufdaten. G8 kann direkt auf `selectedRows` aufbauen; dessen Umsetzung ist nicht Teil von G7.
