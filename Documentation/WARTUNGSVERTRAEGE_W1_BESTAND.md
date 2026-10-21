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

Die Vertragsdetailansichten zeigen Titel, Bereich, Beschreibung, Aktivstatus,
Kontingent, heutige Belegung, freie Plätze, Arbeitsstundenwirkung, Bemerkung
sowie aktuelle, zukünftige und historische Zuordnungen. Nur aktuelle
Zuordnungen zählen zur heutigen Belegung.

Die vorhandenen Bearbeitungspfade verwenden dieselben Vertragsfelder in Web
und MAUI: Bereich, Beschreibung, Kontingent, Befreiung oder Gutschrift,
Bemerkung und Aktivstatus. Die Infrastruktur übernimmt diese Felder sowohl
beim Anlegen als auch beim Aktualisieren. Die aktuelle Belegung entsteht aus
dem gemeinsamen Stichtags-Bundle im SupabaseService; die Oberflächen rechnen
sie nicht selbst nach.

## W1-Feststellungen und Status nach W5

1. Die Zeitlisten Aktuell, Zukünftig und Historisch wurden in W3 ergänzt und in
W4 für Mitgliedsdetails in Web und MAUI an dieselbe Klassifikation
angeschlossen.
2. Web und MAUI verwenden weiterhin ihre bestehenden Batch-Ergebnismodelle.
Teilfehler werden angezeigt und nach Teilerfolgen werden Mitgliedslisten
aktualisiert; eine neue DB-RPC oder clientseitige Rückabwicklung gibt es
nicht.
3. Die Arbeitsstundenwirkung ist saisonüberlappend (Intervallschnitt), nicht
taggenau anteilig. Das ist der vorhandene fachliche Stand und darf ohne
Fachentscheidung nicht geändert werden. W5 hat diese zentrale Wirkung in
Pflichtstunden-, Web-/MAUI-Anzeige- und Exportpfaden regressionsgesichert.

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

## W4 – Mitgliedsbezogene Ansichten

Web und MAUI zeigen bei einem Mitglied dieselben drei Zeitbereiche: Aktuell,
Zukünftig und Historisch. Jede Zuordnung enthält Vertragsbezeichnung,
Gültigkeitsbeginn und -ende, Zeitstatus und die im Vertrag hinterlegte
Arbeitsstundenwirkung. Die Wirkung wird nur dargestellt; die Oberflächen
berechnen weder Pflichtstunden noch Gutschriften.

Die Listen verwenden inklusive Datumsgrenzen aus dem Zeitmodell. Aktuelle
Verträge sind nach Bezeichnung sortiert, zukünftige nach dem nächsten Beginn
und historische nach dem zuletzt beendeten Vertrag. Infrastruktur und Web
verwenden die vorhandene Hauptmitgliedsauflösung; für ein Nebenmitglied wird
dieselbe Hauptmitgliedszuordnung angezeigt und nicht als zweite Zuordnung
dupliziert.

Die Mitgliedszuordnung bleibt an die vorhandenen Speicherpfade und das W3-
Batchergebnis gebunden. Nach Voll- und Teilerfolg werden die Mitgliedslisten
erneut geladen; W3-Fehlerdetails bleiben sichtbar. Die W4-Contracts prüfen
Grenztage, Sortierung, Hauptmitgliedspfad, Batchmodell, reine Anzeige der
Vertragswirkung und die MAUI-/Web-Servicebindung.

## W5 – Arbeitsstunden- und Exportregressionen

Die alleinige fachliche Berechnung bleibt
`fn_berechne_pflichtstunden_status`; Web, MAUI und Exporte lesen deren Werte
aus `v_pflichtstunden_uebersicht`. W5 ändert weder Datenbank noch Produktiv-
Berechnung und ergänzt keine clientseitigen Pflichtstundenregeln.

Der zentrale Saisonbezug prüft inklusive Überschneidung der Zuordnung mit dem
Kalenderjahr der Saison (`gueltig_ab <= 31.12.` und `gueltig_bis >= 01.01.`,
offenes Ende eingeschlossen). Eine Zuordnung, die die Saison auch nur teilweise
schneidet, wirkt in dieser Saison mit ihrer vollen Vertragswirkung; sie wird
nicht taganteilig berechnet. Das SQL prüft keinen zusätzlichen heutigen
Stichtag: Eine zukünftige Zuordnung innerhalb desselben Saisonjahrs zählt
bereits zum Ergebnis dieser Saison, auch wenn der Vertragsbeginn aus Sicht des
heutigen Tags noch bevorsteht. Eine vollständig außerhalb der Saison liegende
zukünftige Zuordnung zählt nicht. Gleiches gilt für beendete Zuordnungen,
sofern ihr Beginn spätestens am Saisonende und ihr Ende mindestens am
Saisonbeginn liegt. Diese Jahresregel ist vom taggenauen Zeitstatus der
Mitgliedsansicht zu unterscheiden und vor einer fachlichen DEMO-Abnahme zu
bestätigen; W5 ändert sie nicht.

Ein aktiver befreiender Vertrag setzt Soll und offene Stunden auf null und
unterdrückt die Gutschrift. Andernfalls summiert die Funktion
Gutschriftbeträge aktiver, saisonüberlappend zugeordneter Verträge einmal je
Vertrags-ID und begrenzt die Summe auf das Soll. Mehrere Verträge folgen
ausschließlich dieser vorhandenen SQL-Regel; W5 erfindet keine neue
Kombinationslogik.

Die Arbeitsstundenübersicht im Export übernimmt Soll, geleistete Stunden,
Gutschrift, offen, Vertragsstatus und Regelgrund aus der zentralen View. Der
Export ergänzt nur die Vertragsbezeichnungen und Detailzeilen. Web- und
MAUI-Arbeitsstundenanzeigen geben View-Werte wieder; auch die
PDF-Exportdarstellung berechnet keine Befreiung oder Gutschrift erneut.
Regressionstests verankern die SQL-Intervall-, Befreiungs-, Gutschrift- und
Mehrfachvertragsregeln sowie die zentralen Web-, MAUI- und Exportpfade.

## Nächste Teilblöcke

- W6: Fachliche DEMO-Abnahme und kontrollierter VDB-Rollout.
