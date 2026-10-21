# Wartungsverträge – W1 Bestandsprüfung und fachliche Basis

Stand: 21.10.2026

## Datenmodell und Datenbankregeln

Die Tabelle wartungsvertraege beschreibt den Vertrag mit Titel, Beschreibung,
Bereich, maximalen aktiven Zuordnungen, Pflichtstundenbefreiung,
Arbeitsstundengutschrift, Aktivstatus, Bemerkung, Zeitstempeln und is_demo.
Das Kontingent ist mindestens eins; eine Arbeitsstunden-Gutschrift ist nicht
negativ. Befreiung und positive Gutschrift schließen sich per Check aus.

Die Tabelle wartungsvertrag_zuordnungen hält Vertrag, Hauptmitglied, gueltig_ab,
optional gueltig_bis, Bemerkung und Zeitstempel. Ein offenes Ende ist zulässig.
Die DB verwendet inklusive Datumsgrenzen. Eine Zuordnung ist aktuell, wenn ihr
Intervall den Stichtag enthält; sie ist zukünftig, wenn der Beginn später
liegt, sonst historisch.

Die Triggerfunktion validate_wartungsvertrag_zuordnung prüft serverseitig:

- Existenz und Hauptmitglied-Kontext,
- keine überlappende Zuordnung desselben Vertrags zum selben Hauptmitglied,
- die Spitzenbelegung im gesamten beantragten Zeitraum gegen das Kontingent,
- gültige Zeitintervalle.

Kapazität ist daher nicht nur UI-Logik. Web und MAUI führen zusätzlich eine
Vorschauprüfung aus; bei Parallelzugriff bleibt der Trigger verbindlich.
RLS ist vorhanden: Schreiben erfolgt nur für admin/vorstand; lesend sind neben
der Verwaltung die eigenen zugeordneten Verträge zulässig.

## Arbeitsstundenwirkung

Die zentrale DB-Funktion fn_berechne_pflichtstunden_status ist die einzige
fachliche Quelle. Sie berücksichtigt ausschließlich aktive Verträge mit einer
Zuordnung, die das Saisonintervall schneidet:

- ein befreiender Vertrag setzt Soll und offene Stunden auf null;
- sonst werden Gutschriften aktiver Verträge einmal je Vertrags-ID addiert
  und höchstens bis zum Pflichtstundensoll angerechnet;
- historische oder zukünftige Zuordnungen ohne Überschneidung mit der Saison
  wirken nicht;
- Arbeitsstunden selbst werden weiterhin separat nur aus freigegebenen
  Arbeitsstunden summiert.

Web, MAUI und Export lesen das Ergebnis aus v_pflichtstunden_uebersicht; sie
berechnen keine zweite Wartungsvertragswirkung.

## Produktstand nach W2

Web und MAUI zeigen in der globalen Übersicht einheitlich den heutigen Stand:
Titel, Aktivstatus, aktuelle Belegung gegen das verpflichtende Kontingent und
die Arbeitsstundenwirkung. Das Datenmodell kennt kein unbegrenztes Kontingent;
es gilt stets mindestens ein Platz.

Die Detailansichten zeigen Titel, Bereich, Beschreibung, Aktivstatus,
Kontingent, heutige Belegung, freie Plätze, Arbeitsstundenwirkung, Bemerkung
und ausschließlich die aktuell zugeordneten Mitglieder. Zukunft und Historie
sind bewusst nicht Teil von W2.

Die vorhandenen Bearbeitungspfade verwenden dieselben Vertragsfelder in Web
und MAUI: Bereich, Beschreibung, Kontingent, Befreiung oder Gutschrift,
Bemerkung und Aktivstatus. Die Infrastruktur übernimmt diese Felder sowohl
beim Anlegen als auch beim Aktualisieren. Die aktuelle Belegung entsteht aus
dem gemeinsamen Stichtags-Bundle im SupabaseService; die Oberflächen rechnen
sie nicht selbst nach.

## Festgestellte Lücken

1. Globale und mitgliedsbezogene Core-/MAUI-Übersichten zeigen über das
gemeinsame Bundle aktuell nur den heutigen Stand. Zukunft und Historie müssen
in W3 explizit als zeitliche Listen ergänzt werden, ohne die Kapazitäts- oder
Arbeitsstundenregeln zu verändern.
2. Mehrfachzuordnungen werden in den Web- und MAUI-Batchpfaden nicht über
eine eigene DB-RPC als fachliche Sammeltransaktion ausgeführt. Der einzelne
Tabellen-Insert ist serverseitig validiert; bei einer späteren
Batch-Fehlersituation muss der UI-Status transparent bleiben. Das ist W3 zu
klären, nicht im W1-Fundament durch eine neue Regel zu ersetzen.
3. Die Arbeitsstundenwirkung ist saisonüberlappend (Intervallschnitt), nicht
taggenau anteilig. Das ist der vorhandene fachliche Stand und darf ohne
Fachentscheidung nicht geändert werden.

## W3 – Zeitdarstellung und Batch-Ergebnisse

W3 ergänzt die gemeinsame Zeitklassifikation Aktuell, Zukünftig und Historisch
mit inklusiven Datumsgrenzen. Vertragsdetails zeigen alle drei Bereiche; nur
aktuelle Zuordnungen zählen zur heutigen Belegung.

Mehrfachzuordnungen werden bewusst als einzelne Speicheroperationen ausgeführt.
Jede Operation liefert Mitglied, Vertrag und ein verständliches Ergebnis. Ein
Fehler hält spätere Operationen nicht auf; bereits erfolgreiche Zuordnungen
bleiben bestehen. Es gibt weder eine fachliche RPC-Sammeltransaktion noch einen
clientseitigen Rollback. Die Datenbank bleibt für Hauptmitglied, Zeitraum,
Doppelzuordnung und Kapazität maßgeblich.

## Nächste Teilblöcke

- W4: Mitgliedsbezogene Zuordnung in Web und MAUI gegen dieselbe Zeitdarstellung
  und Hauptmitglied-Normalisierung abgleichen.
- W5: Regression der Pflichtstunden-, Gutschrift-, Befreiungs- und Exportpfade.
- W6: Fachliche DEMO-Abnahme und kontrollierter VDB-Rollout.
