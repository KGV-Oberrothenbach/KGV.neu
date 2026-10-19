# FORM REF1 Feldübersicht

## Mitgliedsantrag v2 gegenüber v1

| Änderung | Feld | Fachliche Bedeutung |
| --- | --- | --- |
| neu | `vereinsunterlagen_nachweis` | Mehrzeiliger Nachweis für Satzung, Kleingartenordnung, Beitragsordnung und Arbeitsordnung einschließlich Fassung, Aushändigungsdatum und Art der Aushändigung. |
| entfernt | `bank_kontoinhaber`, `bank_name`, `bank_iban`, `bank_bic` | Bankverbindung gehört in Abrechnung und gegebenenfalls ein separates SEPA-Mandat, nicht in den Mitgliedsantrag. |
| geändert | Anerkennung und Aufnahme | Der Antrag enthält jetzt die Anerkennung von Satzung und Vereinsordnungen sowie den satzungsgemäßen Hinweis auf die Aufnahmeentscheidung durch den Vorstand. |
| ersetzt | `vertreter_*` | Die Angaben der zweiten Person sind jetzt neutral als `zweite_partei_*` angelegt: einsetzbar für ein Nebenmitglied oder – bei Minderjährigen – die gesetzliche Vertretung. |
| neu | `zweite_partei_bezeichnung` | Dynamische Rollenbezeichnung, zum Beispiel „Nebenmitglied“ oder „gesetzliche Vertretung minderjähriger Antragsteller/in“. |
| ersetzt | `unterschrift_vertreter` | Neutrale Signatur `unterschrift_zweite_partei` mit dynamischer Beschriftung `unterschrift_zweite_partei_bezeichnung`. |
| neu | `vereins_unterzeichner` | Vorbereitetes Feld für die zeichnende Person des Vereins; eine echte Auswahlliste wird erst durch eine spätere Formular-/Workflow-Erweiterung möglich. |
| neu | Seite 2: Datenschutzinformation | Vollständige Hinweise nach Art. 13 DSGVO mit Verantwortlichem, Zwecken, Rechtsgrundlagen, Empfängern, Speicherdauer, Betroffenenrechten, Beschwerdemöglichkeit und Erforderlichkeit der Daten. |
| neu | `mitglied_einwilligung_*`, `zweite_partei_einwilligung_*` | Getrennte freiwillige Einwilligungen für Vereinsinformationen per E-Mail, Rechnungen per E-Mail und WhatsApp – jeweils mit eigener Unterschrift. |
| geändert | Fußzeile Seite 1 | Vereinsregister, Vorstandsvorsitzende, ladungsfähige Anschrift und Vereins-E-Mail sind aufgenommen. |
| unverändert | übrige Kommunikations-, Beitrags- und Signaturfelder | Bestehende Inhalte bleiben erhalten. Die vorhandenen Signaturen bestätigen zusätzlich den Erhalt der Vereinsunterlagen. |

## Pachtvertrag v3 gegenüber v2

| Änderung | Feld | Fachliche Bedeutung |
| --- | --- | --- |
| neu | `paechter_geburtsdatum` | Geburtsdatum der Pächterin oder des Pächters. |
| neu | `zweite_partei_geburtsdatum` | Geburtsdatum der zweiten Partei (Mitpächterin/Mitpächter oder gesetzliche Vertretung). |
| neu | `altvertrag_hinweis` | Mehrzeiliger Hinweis, dass die Urkunde einen Altvertrag ersetzt, ohne dessen ursprünglichen Beginn zu verändern. Bei Neuvertrag leer. |
| neu | `pacht_pro_qm` | Pachtpreis je Quadratmeter zusätzlich zur Jahrespacht. |
| neu | `probephase_bis` | Ende der Probe- und Beobachtungsphase; der Vertrag bleibt unbefristet. |
| neu | `vereinsunterlagen_nachweis` | Mehrzeiliger Nachweis über Satzung, Kleingartenordnung, Beitragsordnung und Arbeitsordnung. |
| entfernt | `befristung` | Der Vertrag ist unbefristet; ersetzt durch `probephase_bis`. |
| entfernt | `kleingartenordnung_fassung` | Bestandteil des gemeinsamen Nachweises der Vereinsunterlagen. |
| entfernt | `kleingartenordnung_ausgehaendigt_am` | Bestandteil des gemeinsamen Nachweises der Vereinsunterlagen. |
| entfernt | `anlage_1` bis `anlage_4` | Keine Vertragsanlagen-Checkboxen mehr. Übergabeprotokoll und Wertermittlung sind eigenständige Vorgänge. |
| unverändert | `weitere_vereinbarungen` | Technischer Name bleibt erhalten; sichtbare Bezeichnung lautet nun „Individuelle Zusatzvereinbarungen“. |
| unverändert | `faelligkeit` | Bleibt ein dynamisches Feld ohne fest im Template hinterlegten Wert. |
| unverändert | `uebergabe_datum` | Bestehendes Feld bleibt erhalten. |
| unverändert | `vereins_unterzeichner` | PDF-Feld bleibt erhalten; später automatische Befüllung aus dem zeichnungsberechtigten Vorstandsmitglied, keine manuelle Vertragseingabe. |
