# P11 – AP-04 Pacht-Arbeitsplan

## AP-04.1 – Pachtende, Mitgliedschaftsstatus und Pächterbeteiligungen

### Befund und Umsetzung

Das rechtliche Pachtende wird im bestehenden Modell durch
`pachtverhaeltnis.beendet_am` gespeichert; `parzellen_belegung` ist nur die
getrennte operative Zuordnung. Vor AP-04.1 fehlte ein autorisierter,
transaktionaler Abschlussweg, der das Vertragsende, die
Beteiligungshistorie und P11-R06 gemeinsam verarbeitet.

Migration `20261113120000_p11_ap04a_lease_end_membership_status.sql` führt
`beende_pachtverhaeltnis` ein. Sie verlangt effektives
`CanCreateMitglied` (das bestehende Pachtvertrags-Fachrecht), ein aktuelles
Enddatum und einen Grund, beendet die Vertragsbeziehung mit Audit und schreibt
die Beteiligungs-Enddaten. Nur ein Hauptpächter ohne weitere wirksame
Pachtbeteiligung wird passiv (`aktiv=false`); `mitglied_ende`, App-Zugang und
Nebenmitgliedschaft bleiben unverändert. Gleiches Enddatum ist idempotent,
ein abweichendes zweites Enddatum wird abgewiesen. Ein Mitpächter übernimmt
keinen Altvertrag; die bestehende Erstvertragslogik verlangt weiterhin einen
neuen Vertrag mit Hauptpächter.

### Nachweis

Der lokale Neuaufbau einschließlich Seed bestand mit 217 Migrationen. Die
rückrollbare `authenticated`-Fixture
`20261113120000_p11_ap04a_lease_end_membership_status_funktionstest.sql`
bestätigt fehlendes Recht, die Passivstellung nach letzter Hauptpacht,
erhaltenes Mitgliedschaftsende/App-Zugang/Nebenmitglied, Audit und Retry.

**Abnahme AP-04.1:** vollständig abgenommen. Nachsorge, Übergabeprotokolle,
historische Korrekturen und weitere Pachtwechsel bleiben spätere AP-04-Pakete.
B-01 bleibt offen.

## AP-04.2 – Nachsorge und Übergabeprotokolle

### Befund und Umsetzung

Die Nachsorgegrundstruktur aus `20261113130000` wurde gezielt ergänzt. Die
Abschluss-RPC `beende_pacht_nachsorge` bindet die Entscheidung an das
bestehende effektive Pachtvertragsrecht (`CanCreateMitglied`), verlangt Art
und Begründung und schreibt Akteur/Zeitpunkt sowohl an der Nachsorge als auch
unveränderlich in `pacht_nachsorge_ereignis`. Bereits abgeschlossene
Nachsorgen sind nicht reaktivierbar; Wiederholungen sind idempotent.

Die neue Triggerkette beendet offene Nachsorge ausschließlich innerhalb der
tatsächlichen Anlage bzw. Wirksamwerdung einer Hauptpächterbeteiligung auf
derselben Parzelle. Künftige Beteiligungen bleiben wirkungslos. Die
serverseitige Zuordnung eines vorhandenen abgeschlossenen Rückgabe- oder
Übernahmeprotokolls prüft Parzelle und rechtlich beendetes Pachtverhältnis und
auditert die Zuordnung, ohne Zählerstände, Fotos oder Dokumentreferenzen zu
kopieren oder zu verändern. Mitgliedschaft, App-Zugang und der bereits
beendete Altvertrag werden durch diese Wege nicht verändert.

### Nachweis

Vollständiger lokaler Supabase-Neuaufbau einschließlich Seed erfolgreich;
die aktiven Migrationen reichen bis
`20261113133000_p11_ap04b_aftercare_audit_field_correction.sql`. Die
rückrollbare `authenticated`-Fixture
`20261113131000_p11_ap04b_aftercare_funktionstest.sql` bestätigt fehlendes
Recht einschließlich Admin-Entzug, Bindung an einen rechtlich beendeten
Altvertrag, Zweijahresgrenze, abgewiesene geplante Übernahme, automatischen
Abschluss bei wirksamer Hauptpächterübernahme, Audit, Protokollzuordnung und
idempotenten Wiederholungsabschluss.

**Abnahme AP-04.2:** Datenbankvertrag und RLS-Nachweis abgenommen; die
bestehende Parzellenprotokoll-Oberfläche wird weiterverwendet. B-01 bleibt
offen.

## AP-04.3 – Pächterwechsel und nahtlose Folgebelegung

### Befund und Umsetzung

Die Vertragsregistrierung erzeugte bereits einen neuen eigenständigen
Pachtvertrag mit Haupt-/Mitpächterbeteiligungen, aber die operative
`parzellen_belegung` war nicht verbindlich an dieses neue Vertragsverhältnis
gebunden. Migration `20261113140000_p11_ap04c_tenant_change_successor_occupancy.sql`
ergänzt deshalb ausschließlich für neue Hauptpächterbeteiligungen eine
serverseitige, eindeutig zugeordnete operative Folgebelegung. Eine
bereichsbezogene Exclusion-Constraint verhindert überlappende Belegungen;
das rechtliche Ende schließt die zugeordnete Belegung im selben Vorgang.

Der Altvertrag bleibt unverändert historisch, der Folgepächter entsteht nur
als neuer Vertrag. Die vorhandenen Mitgliedschaftsprüfungen für Pächter, die
AP-04.1-Statusfolge und die AP-04.2-Nachsorgekette bleiben maßgeblich.
Geplante Beteiligungen schließen Nachsorge nicht, erst die wirksam gewordene
Hauptpächterbeteiligung tut dies transaktional. R10 bleibt über die bestehende
wirksame Nachfolgebelegung durchgesetzt.

### Nachweis

Vollständiger lokaler Neuaufbau einschließlich Seed erfolgreich. Die
rückrollbare `authenticated`-Fixture
`20261113140000_p11_ap04c_tenant_change_funktionstest.sql` bestätigt
individuelle Gewährung, Admin-Entzug, rechtliches Altvertragsende,
Folgebelegungsabschluss, geplante versus tatsächliche Übernahme,
Nachsorgeabschluss und abgewiesene operative Parallelbelegung.

**Abnahme AP-04.3:** vollständig abgenommen. B-01 bleibt offen.

## AP-04.4 – Vertragsänderungen und historische Korrekturen

Migration 20261113150000_p11_ap04d_contract_change_history.sql sperrt direkte Änderungen von Fläche und Anschlüssen während laufender Pacht. Die autorisierte RPC verlangt eine abgeschlossene, zum Verhältnis gehörende Vertragsfassung und schreibt Vorher/Nachher, Begründung, Akteur und Zeitpunkt in den unveränderlichen Auditnachweis. Abgeschlossene Pachtverhältnisse und Beteiligungen sind gegen stille Updates geschützt. Der lokale Neuaufbau und die rückrollbare authenticated-Guard-Fixture bestanden.

**Abnahme AP-04.4:** offen – der positive End-to-End-Nachweis einer real vollständig signierten Vertragsänderung ist noch nicht als Fixture abgebildet.

### AP-04.4 – WIP: bindende signierte Änderung und historische Berichtigung

Migration `20261113160000_p11_ap04d_signed_change_authorization.sql` ergänzt den
laufenden Prepared-Ersatzvertragsweg um einen serverseitig eingefrorenen
Änderungsauftrag. Seine Werte und ihr SHA-256-Digest werden vor dem Start der
Signaturrevision in den Prepared-Entwurf gebunden. Die beim Abschluss erzeugte
Vertragsfassung wird nur bei vollständigen Pflichtsignaturen und exakt passender
Bindung dem Auftrag zugeordnet. Die Anwendung akzeptiert anschließend keine
frei übergebenen Fachwerte mehr; sie liest ausschließlich den eingefrorenen
Stand. Das bisherige, vom Aufrufer setzbare GUC-Kennzeichen ist nicht mehr
Autorisierungsnachweis. Ein privater, nur von der autorisierten RPC erzeugter
Transaktionsnachweis schützt den Parzellen-Trigger.

Historische Pachtverhältnisse, Beteiligungen, Belegungen und Vertragsfassungen
werden als Originale erhalten. `pacht_historische_berichtigung` speichert den
vorherigen und berichtigten Stand, Originalreferenz, Akteur, Zeitpunkt und
Begründung unveränderlich. Der Änderungsweg ist ergänzend; er überschreibt kein
historisches Original. Dieser WIP ist **nicht abgenommen**, weil die geforderte
lokale Supabase-Neuaufbau-/Signatur-E2E-Ausführung in der bereitgestellten
Umgebung noch nicht möglich war.

Nachprüfung 26.2: Der Docker-freie PGlite-Regressionstest wurde nach
Installation der versionierten `supabase/tests`-Abhängigkeiten tatsächlich
ausgeführt: `npm test --prefix supabase/tests` meldete **92 bestanden, 0
fehlgeschlagen**. Das ist ein realer SQL-/Historien-Regressionstest, deckt die
vollständige Supabase-Auth-/Storage- und die neue AP-04.4-Signaturkette jedoch
nicht ab. Der Web-Produktionsbuild (`npm run build --prefix KGV.Web`) bestand.
Core und Infrastructure konnten nicht gebaut werden, weil die lokal vorhandene
ImageSharp-Version 4.1.3 einen nicht bereitgestellten Lizenzschlüssel verlangt;
zusätzlich war NuGet im Sandbox-Netz nicht erreichbar. Docker Desktop war
vorhand, sein Daemon war jedoch nicht über die lokale Named Pipe erreichbar;
Supabase CLI und `psql` fehlen. Deshalb wurden lokaler Supabase-Neuaufbau,
AP-04.1–04.4-SQL-Fixtures, echte Prepared-Signatur-E2E-, RLS- und
Parallelitätstests nicht als bestanden dokumentiert.

Migration `20261113161000_p11_ap04d_signature_plan_and_document_binding.sql`
schließt zwei beim Review der WIP-Migration gefundene Lücken: Die vertraglich
sichtbare Klausel mit Fläche/Anschlüssen/Begründung wird in
`draft.additionalAgreements` eingefroren und damit vom bestehenden
PDF-Generator in `weitere_vereinbarungen` übernommen. Außerdem wird die
Bindung getrennt in zusammengesetzte PL/pgSQL-Variablen geladen und verlangt
explizit die Signaturpositionen `lease.member`, `vorstand_1`, `vorstand_2`
sowie bei Bedarf `lease.second_party` in genau der eingefrorenen Revision.

### AP-04.4 – CI-Nachweis 26.3 und Blocker

Der isolierte GitHub-Actions-Workflow
`.github/workflows/p11-ap04d-validation.yml` wurde auf GitHub tatsächlich
ausgeführt. Im Lauf
[`43435090662`](https://github.com/KGV-Oberrothenbach/KGV.neu/actions/runs/43435090662)
bestand der Job `pglite-and-web`: PGlite-Regressionssuite und
Web-Produktionsbuild waren grün. Der lokale Supabase-Neuaufbau begann auf einem
GitHub-hosted Docker-Runner und wandte die Migrationen chronologisch an.

Er scheiterte deterministisch in der bereits eingecheckten Migration
`20261113160000_p11_ap04d_signed_change_authorization.sql` mit PostgreSQL
`SQLSTATE 42601`: `record variable cannot be part of multiple-item INTO list`.
Auslöser ist genau `select c.*, k.* into v_change, v_process` in
`bind_pacht_vertragsaenderung_an_fassung`. Die spätere Migration
`20261113161000...` enthält zwar die korrekte getrennte Zuweisung, kann wegen
des Fehlers in der Vorgängermigration nie erreicht werden.

Damit besteht ein unauflösbarer Konflikt zwischen der geforderten vollständigen
Neuaufbauprüfung und der Vorgabe, historische Migrationen niemals zu ändern:
Eine neue chronologische Migration kann einen Parsefehler einer früheren
Migration nicht beheben. Bis zu einer ausdrücklichen Entscheidung, ob
`20261113160000...` ausnahmsweise korrigiert/rebaset werden darf, bleiben der
Supabase-Neuaufbau, die AP-04.4-Signatur-E2E, die SQL-Fixtures und die
PostgreSQL-/RLS-/Parallelitätsnachweise offen.

### AP-04.4 – Genehmigte Migrationsreparatur 26.5

Der Auftraggeber hat die einmalige, auf die Migrationsausführbarkeit begrenzte
Ausnahme für `20261113160000_p11_ap04d_signed_change_authorization.sql`
erteilt. Es wurden ausschließlich zwei technische Defekte korrigiert:

- Die ungültige Mehrfach-`INTO`-Zuweisung wurde in zwei gesperrte,
  zusammengesetzt typisierte Abfragen aufgeteilt.
- Der Berechtigungsentzug nennt nun die tatsächlich vorhandene Sechs-Parameter-
  Überladung. Diese alte RPC wird anschließend ohne `CASCADE` gelöscht, damit
  nur die neue Ein-Parameter-RPC als Änderungsweg verbleibt.

Die temporäre CI-Manipulation wurde entfernt. Der folgende CI-Lauf prüft damit
wieder ausschließlich den eingecheckten Migrationsbestand. Eine Prüfung einer
bereits dauerhaft betriebenen Datenbank wurde nicht durchgeführt: Es besteht
keine dafür autorisierte oder angebundene Datenbankverbindung. Für einen
Bestand, in dem die fehlerhafte Migration wider Erwarten bereits als angewandt
registriert ist, ist vor einem Rollout ein separates Kompatibilitätskonzept
erforderlich.

Der ungepatchte CI-Lauf
[`43455308904`](https://github.com/KGV-Oberrothenbach/KGV.neu/actions/runs/43455308904)
bestätigte anschließend, dass alle versionierten Migrationen – einschließlich
der beiden korrigierten Stellen und `20261113161000` – vollständig chronologisch
angewandt werden. Der Neuaufbau scheiterte erst beim bestehenden lokalen Seed:
`supabase/seed.sql` erstellt und befüllt `p11_ap01_test_fixture`, doch der
Supabase-Seed-Runner meldete beim Batchversand `relation
public.p11_ap01_test_fixture does not exist` (`42P01`). Deshalb wurden die
AP-04-Fixtures noch nicht erreicht. Dieser Seed-Fehler liegt außerhalb der
genehmigten Migrationsreparatur; keine Änderung daran wurde vorgenommen.

Nachlauf 26.7: Die getrennten Seed-Dateien bestanden im CI; der Neuaufbau und
die Prüfung auf acht AP-01-Fixtures waren erfolgreich. Die erste AP-04.1-
Regression deckte anschließend einen Triggerfehler auf: die gemeinsame
OR-Bedingung in `guard_abgeschlossene_pachtdaten` referenzierte für einen
Beteiligungs-Trigger `old.beendet_am`, das dort nicht existiert. Migration
`20261113162000_p11_ap04d_historical_guard_table_safe.sql` verwendet deshalb
tabellenspezifische Zweige und erhält die unveränderte fachliche Sperre.

Der CI-Lauf `43457509925` bestand anschließend vollständig: lokaler
Supabase-Neuaufbau, beide Seed-Dateien, acht AP-01-Fixtures sowie AP-04.1 bis
AP-04.4-Funktionstests, PGlite-Regressionen und Web-Produktionsbuild. Die
CI-Seed-Prüfung wurde danach von einer bloßen Boolean-Ausgabe in eine
fehlschlagende PostgreSQL-Assertion überführt.

