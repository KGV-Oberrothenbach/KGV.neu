# Wartungsvertrag - Entwurfsanalyse

Stand: 21.10.2026. Diese Analyse beschreibt den vorhandenen Datenbestand; sie nimmt keine technische Änderung vor.

## 1. Vorhandene Datenbasis

`wartungsvertraege` enthält den Stammdatensatz eines Wartungsvertrags:

- `titel` als Bezeichnung bzw. Vertragsart,
- `beschreibung` als vollständige Leistungsbeschreibung,
- `bereich`, `bemerkung`, `aktiv` und `max_aktive_zuordnungen`,
- die Arbeitsstundenwirkung über `befreit_von_pflichtstunden` und `arbeitsstunden_gutschrift`.

Eine eigenständige Preis- oder Kosten-Spalte gibt es nicht. Nach Abstimmung gehört eine Kostenangabe auch nicht in die beiden Dokumententwürfe.

`wartungsvertrag_zuordnungen` verbindet einen Wartungsvertrag mit `hauptmitglied_id`. Die Zuordnung enthält `gueltig_ab`, optional `gueltig_bis` und `bemerkung`. Nebenmitglieder werden technisch auf das Hauptmitglied aufgelöst. Eine direkte Parzellenreferenz existiert nicht; die Oberflächen ermitteln Gartennummern über die aktuelle Parzellenbelegung des Mitglieds.

Die vollständige Leistungsbeschreibung liegt daher in `wartungsvertraege.beschreibung`; zugehörige ergänzende Hinweise liegen in `wartungsvertraege.bemerkung`.

## 2. Aktueller Saison- und Verlängerungsstand

Es existiert kein Saison-Fremdschlüssel in beiden Wartungsvertragstabellen. Der Zeitraum einer Zuordnung ist frei über `gueltig_ab` und `gueltig_bis` definierbar; eine offene Zuordnung (`gueltig_bis = NULL`) ist ausdrücklich zulässig und wird in den aktuellen Zuordnungsoberflächen verwendet.

Eine Verlängerungsentscheidung, eine Ursprungsvertragsreferenz, ein Nichtverlängerungsmerkmal und eine Verlängerungsmitteilung werden aktuell nicht fachlich geführt. Der bestehende Dokumentworkflow wird beim Anlegen einer Zuordnung erzeugt und ist auf einen Signaturvorgang für `wartungsvertrag` ausgerichtet.

Damit sind Kalenderjahr 01.01.-31.12., Verlängerungsentscheidung und Mitteilung im Entwurf als gewünschte fachliche Regelung enthalten, aber aus den vorhandenen Tabellen noch nicht vollständig befüllbar oder nachweisbar.

## 3. Entwürfe und verwendete Platzhalter

Die beiden PDF-Entwürfe verwenden ausschließlich Angaben, die entweder vorhanden sind oder vom Auftrag ausdrücklich als Dokumentinhalt gefordert werden:

| Dokumentangabe | Quelle / Ableitung | Stand |
| --- | --- | --- |
| Mitglied, Mitgliedsnummer | `mitglied`, verknüpft über `hauptmitglied_id` | vorhanden |
| Parzelle | aktuelle Parzellenbelegung des Mitglieds | ableitbar, nicht direkt gespeichert |
| Wartungsvertragsart | `wartungsvertraege.titel` | vorhanden |
| Leistungsbeschreibung | `wartungsvertraege.beschreibung` | vorhanden |
| Bereich, Bemerkung | `wartungsvertraege.bereich`, `bemerkung` | vorhanden |
| Arbeitsstundenwirkung | Befreiung/Gutschrift im Stammdatensatz | vorhanden |
| Saisonjahr | aus Zuordnung nicht zuverlässig ableitbar | fachliche Regelung, technisch offen |
| Beschlussdatum und Dokumentdatum der Verlängerung | keine Quelle vorhanden | technisch offen |

## 4. Bestehende Oberflächen und Logik

Vorhanden sind Verwaltungs- und Detailoberflächen für Wartungsverträge sowie Zuordnungsoberflächen im Web und in MAUI. Die Zuordnung prüft aktive Verträge, Überschneidungen, Hauptmitgliedskontext und das Kontingent. Sie erlaubt aber auch unbegrenzte Laufzeiten und implementiert keine saisonale Verlängerung.

## 5. Noch abzustimmende rein textliche Punkte

1. Soll die Verlängerungsentscheidung ausschließlich im Vorstandsbeschluss dokumentiert werden, oder soll ein dokumentierter Beschlussverweis Bestandteil der Mitteilung sein?
2. Soll die Parzelle als Momentaufnahme in den Dokumenten festgehalten werden, wenn die Zuordnung selbst keine Parzelle speichert?
3. Soll der Auftrag für jede Zuordnung eine zwingend kalenderjahresgenaue Laufzeit erzwingen? Der aktuelle Datenbestand erlaubt abweichende und offene Zeiträume.

Die Vorlagen enthalten keine neue Signaturarchitektur, keine Datenbankänderung und keine Versand- oder Verlängerungslogik.

## 6. Verbindliche Einordnung der Verlängerungsmitteilung

Die Verlängerungsmitteilung bleibt ein unsigniertes Informationsdokument und wird derzeit nicht produktiv automatisiert. Sie gehört nicht zum Prepared-Contract-Signaturworkflow. Für eine spätere Automatisierung ist ein eigenes persistentes Fachmodell für Verlängerungsentscheidung und Entscheidungsdatum erforderlich.
