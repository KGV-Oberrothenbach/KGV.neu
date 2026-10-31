# G6 – Freie Felder für Serienbriefvorlagen

## Umfang

Freie Felder sind lokale Definitionen der gerade bearbeiteten Vorlage. Sie werden in G6 weder an Supabase geschrieben noch mit konkreten Werten befüllt. Der Editorzustand enthält dafür `freieFelder` mit Schlüssel, Anzeigename, Typ, Pflichtkennzeichen und stabiler Anlagereihenfolge.

Unterstützte Typen: **Text**, **Datum** und **Zahl**.

## Schlüssel und Kollisionen

Aus dem Anzeigenamen wird ein editierbarer Vorschlag gebildet, etwa `Letzter Termin` zu `letzter_termin`. Zulässig sind nur Schlüssel nach `^[a-z][a-z0-9_]*$`.

Der Editor weist Kollisionen mit Exportfeldern, anderen freien Feldern und den reservierten Namen `mitglied_id`, `parzelle_id`, `serienbrief_empfaenger_status` sowie `briefanrede` zurück.

## Feldkatalog und Platzhalter

Freie Felder stehen im bestehenden Katalog in der Gruppe **Freie Felder** und werden in Anlagereihenfolge geführt. Sie verwenden unverändert die G5-Einfügelogik und das Format `{{feldschluessel}}` für Betreff und Rich-Text. Die G5-Normalisierung schützt damit auch freie Felder gegen HTML-Brüche.

## Bearbeiten und Löschen

Anzeigename, Typ und Pflichtkennzeichen sind bearbeitbar. Wird ein Schlüssel geändert, ersetzt der Editor alle vorhandenen Platzhalter im Betreff und HTML-Inhalt atomar durch den neuen Schlüssel. Ein verwendetes freies Feld kann nicht gelöscht werden; der Editor zeigt den Grund an. Nicht verwendete Felder werden direkt entfernt.

## Grenzen und Anschluss

G6 enthält keine Laufwerte, keine Persistenz und keine PDF- oder Massenerzeugung. G7 kann auf den lokalen Vorlagendefinitionen aufbauen; dessen Umsetzung ist nicht Teil dieses Schritts.
