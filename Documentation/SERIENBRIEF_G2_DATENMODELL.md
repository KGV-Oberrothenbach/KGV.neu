# Serienbrief G2 – Datenmodell und Export-Datenvertrag

Stand: 2026-10-31. Diese Festlegung ergänzt die Bestandsanalyse in
[`SERIENBRIEF_ANALYSE.md`](SERIENBRIEF_ANALYSE.md).

## Gewähltes Modell

Die Migration `20261031100000_serienbrief_foundation.sql` erweitert die
vorhandene Exportmetadatenarchitektur additiv:

- `app_export_definition.serienbrief_erlaubt` ist ein explizites Opt-in und
  startet für alle vorhandenen Exporte mit `false`.
- `serienbrief_mitglied_id_feld` benennt den technischen Roh-Ergebniswert mit
  der stabilen `mitglied_id`; ein aktivierter Serienbriefexport benötigt ihn.
- `serienbrief_parzelle_id_feld` benennt bei eindeutiger Zuordnung optional die
  technische `parzelle_id`.
- `app_export_column_definition.serienbrief_freigegeben` und
  `serienbrief_gruppe` steuern die später angebotenen fachlichen Felder. Die
  Gruppen sind reine Darstellung. `mitglied_id` und `parzelle_id` dürfen
  aufgrund einer Datenbank-Constraint nicht freigegeben werden.

Die technischen ID-Felder müssen nicht als sichtbare Exportspalten existieren;
sie werden nie aus Name, Mitgliedsnummer oder Gartennummer abgeleitet. Die
Fähigkeit eines konkreten Exports wird erst in G3 nach Prüfung seines
Roh-Ergebnisvertrags aktiviert. G2 aktiviert daher keinen Bestandsexport.

## Vorlagen

`serienbrief_vorlage` speichert exportunabhängig Name, Betreff-Vorlage,
Anredemodus `briefanrede`, HTML-Inhalt, Aktivstatus, Auditfelder und die
Grundlayoutreferenz `briefbogen-allgemein/v2`.

`serienbrief_vorlage_exportfeld` hält die ausdrücklich verwendeten fachlichen
Feldschlüssel separat vom HTML. Damit kann G3/G9 eine Vorlage gegen den
freigegebenen Feldkatalog eines gewählten Exports prüfen, ohne HTML parsen zu
müssen. Die zentrale fachliche Ableitung von **Briefanrede** wird nicht als
Exportspalte gespeichert; ihre Implementierung gehört in den späteren
gemeinsamen Daten-/Rendervertrag, nicht in Web oder MAUI separat.

`serienbrief_vorlage_freifeld` definiert freie Felder mit Schlüssel,
Anzeigename, Typ (`text`, `date`, `number`), Pflichtkennzeichen und Sortierung.
Konkrete Eingabewerte gehören bewusst nicht zur Vorlage.

Alle drei Tabellen sind per RLS auf Admin/Vorstand beschränkt. Es gibt noch
keine Serienbrief-Lauf-, Empfänger-, PDF-, Dokument- oder Versandtabellen.

## Späterer Versandanschluss

G2 legt keine Versandwarteschlange an. Ein späterer Lauf muss je erzeugtem
Brief mindestens `mitglied_id`, optionale `parzelle_id`, `dokument_id`, den
Mailfreigabe-Snapshot, Empfänger-E-Mail, Betreff, Standard-Mailtext-Snapshot
und den bestehenden Kommunikationsvorgang halten bzw. übergeben. Mailausgang
und Druckausgabe bleiben an `kommunikationsvorgang` und den vorhandenen
Versandvertrag angeschlossen.

Noch nicht umgesetzt sind UI, Vorlageneditor, Vorschau, HTML-Rendering,
Briefanredeableitung, PDF-Erzeugung, Laufpersistenz, Archivierung sowie
Mail-/Druckübergabe.
