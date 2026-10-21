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

## Bestehender Produktstand

Web besitzt globale Übersicht, Detail-/Editorfluss, Zuordnung zu Vertrag,
mitgliedsbezogene Zuordnung und Beenden. Die Web-Service-Schicht klassifiziert
Zeitstände bereits für die Detaildarstellung und prüft Eingaben vor dem
speichernden Tabellenzugriff.

MAUI besitzt globale Liste, Detail, Editor, Vertragszuweisung und
mitgliedsbezogene Ansicht. Die zentralen Overview-/Detail-Methoden des
SupabaseService verdichten derzeit bewusst nur am heutigen Stichtag aktive
Zuordnungen.

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

## Nächste Teilblöcke

- W2: Globale Web-/MAUI-Übersicht und Detailansicht konsistent prüfen,
  Anzeige der Arbeitsstundenwirkung und Bearbeiten klar führen.
- W3: Zeitbezogene Zuordnungshistorie und Zukunft in den gemeinsamen
  Detail-/Mitgliedsmodellen ergänzen; Batch-Fehlerverhalten absichern.
- W4: Mitgliedsbezogene Zuordnung in Web und MAUI gegen dieselbe Zeitdarstellung
  und Hauptmitglied-Normalisierung abgleichen.
- W5: Regression der Pflichtstunden-, Gutschrift-, Befreiungs- und Exportpfade.
- W6: Fachliche DEMO-Abnahme und kontrollierter VDB-Rollout.
