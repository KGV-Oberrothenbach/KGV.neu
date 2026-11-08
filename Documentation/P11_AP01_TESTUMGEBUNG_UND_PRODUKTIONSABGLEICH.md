# P11 – AP-01: Testumgebung und lesender Produktionsabgleich

Stand: 08.11.2026
AP-00-Referenz: `P11_FACHREGELMATRIX_TESTKATALOG.md`
Geltung: Voraussetzung für jede spätere Supabase-Änderung aus P11.

## Produktivabgleich

Der Abgleich erfolgt ausschließlich lesend. Ausdrücklich ausgeschlossen sind `supabase db push --linked`, Datenänderungen, Function-Deployments und jede Änderung an Produktionsdaten.

Am 08.11.2026 wurde gegen das verknüpfte Projekt `ymxhszxfutjnljiuciyj` geprüft:

| Prüfpunkt | Ergebnis |
| --- | --- |
| `npx supabase migration list --linked` | Lokaler und Remote-Stand stimmen vollständig überein; letzter Stand `20261105230000`. |
| `npx supabase db push --linked --dry-run` | Erfolgreich; keine ausstehenden Migrationen, Seeds oder Rollenänderungen. |
| `npx supabase functions list` | 13 aktive Functions inventarisiert: Login-OTP, Foto-/Dokument-Upload, Invite/Delete User, Vertrags-/Export-Erzeugung, Vereinsdokumentversand sowie vorbereitete Vertragsvorgänge und Signatur-Upload. |
| RLS, RPCs, Trigger, Indizes | Vollständiger Remote-Schema-Dump in dieser Sitzung nicht möglich, da die Supabase-CLI für den Dump eine laufende Docker-Laufzeit benötigt. Der nachfolgende lokale Neuaufbau ist der verbindliche Vergleichspfad. |

Vor jeder zukünftigen Migration sind die ersten beiden Befehle erneut auszuführen. Eine Abweichung blockiert die Erstellung oder Anwendung einer neuen Migration, bis sie dokumentiert und aufgelöst ist.

### Reparatur der historischen Export-Metadatenkette

Beim lokalen Neuaufbau scheiterte `20260923223000_export_arbeitsstunden_uebersicht.sql` mit `relation "public.app_export_definition" does not exist`. Die erste Exportmigration schreibt bereits in `app_export_definition`, `app_export_filter_definition` und `app_export_column_definition`; keine vorhandene Migration erzeugte diese drei Tabellen.

Ein ausschließlich lesender Produktionsschema-Dump bestätigte, dass die Tabellen produktiv vorhanden sind, einschließlich Primär- und eindeutiger Schlüssel, FK-Kaskaden von Filter- und Spaltendefinitionen auf die Exportdefinition, Aktualisierungs-Trigger und RLS-Lesepolicies. Die fehlende, zeitlich korrekte Foundationsmigration `20260923210000_export_metadata_foundation.sql` rekonstruiert daher den damaligen Basisstand unmittelbar vor dem ersten Verbraucher. Sie enthält bewusst weder die G14-Spalten noch die späteren Serienbriefspalten; diese bleiben bei ihren bestehenden späteren Migrationen.

Die Reparatur ist lokal durch den vollständigen Migrationsneuaufbau zu verifizieren. Sie wird nicht mit `db push --linked` ausgeführt und verändert keine Produktionsdaten.

Der erste vollständige Lauf legte anschließend einen zweiten, unabhängigen lokalen Replayfehler in `20260923234500_export_jahresablesung_status.sql` offen: `COALESCE` kombinierte den Enum `ablesung_pruefstatus` direkt mit Text. Da die fehlerhafte Funktionsdefinition selbst nicht erstellt werden kann, ist ein späterer Korrekturauftrag technisch unmöglich. Die bereits produktiv verzeichnete Migration wird deshalb nur an dieser Stelle semantisch unverändert präzisiert: `a.pruefstatus::text`. Es werden keine Daten, Rechte oder Funktionsabläufe verändert.

Der nächste Replayfehler betraf `20260924230000_historische_saisons.sql`: Die Migration kopiert Finanz- und Demo-Felder aus der Saison 2025, obwohl deren Spalten lokal nicht angelegt waren. Der Produktionsschema-Dump bestätigt diese Felder. `20260924220000_saison_finance_foundation.sql` legt nur diese vor dem Verbraucher benötigten Spalten an, füllt für den verpflichtenden Pachtwert ausschließlich bestehende Nullwerte mit `0` und setzt danach die vorhandene Produktionsinvariante `NOT NULL`. Spätere Saisonfelder, insbesondere `pacht_faellig_am`, werden nicht vorgezogen.

Die vollständige Kette erreichte danach die PDF-Template-Kataloge. Diese historischen Migrationen prüfen Storage-Metadatensätze, während die zugehörigen Template-Binärdateien nachweislich außerhalb der SQL-Kette hochgeladen wurden. `20261017124000_local_template_storage_replay_fixture.sql` erzeugt deshalb ausschließlich für den lokalen Fresh-Replay idempotente Storage-Metadatensätze zu den vorhandenen versionierten Templatepfaden. Es ersetzt keine realen PDFs, führt keinen Upload aus und verändert das produktive Storage nicht; eine Produktivveröffentlichung bleibt weiter der explizite Template-Publish-Prozess.

Die spätere Neudefinition desselben Jahresendablesungs-Exports in `20261031170000_jea_rfid_generic_exports.sql` enthielt dieselbe Enum/Text-Kombination und erhielt denselben semantisch neutralen Cast. Beide Anpassungen sind erforderlich, weil jede Migration bei einem Fresh-Replay ihre Funktionsdefinition selbst kompilieren muss.

## Isolierter, reproduzierbarer Neuaufbau

Die Testdatenbank entsteht ausschließlich lokal aus dem gesamten versionierten Migrationsstand. `supabase/seed.sql` wird nur bei `supabase db reset --local` eingespielt und enthält keine produktiven Daten.

```powershell
pwsh -File supabase/tests/rebuild-p11-ap01-test-db.ps1
```

Der Ablauf startet bei Bedarf den lokalen Supabase-Stack, führt `supabase db reset --local` aus und listet anschließend den lokalen Migrationsstand. Er nutzt weder die verknüpfte Datenbank noch Produktionszugangsdaten. Jeder native Supabase-Befehl wird auf seinen Exit-Code geprüft; bei einem Fehler wird der Ablauf ohne Erfolgsmeldung abgebrochen.

## Fixtures

`public.p11_ap01_test_fixture` ist eine lokale, durch `seed.sql` angelegte Fixture-Tabelle. Sie enthält keine realen Personen- oder Produktionsdaten und deckt die für Folgepakete benötigten Szenarien ab:

| Fixture | AP-00-Regeln / spätere Prüffälle |
| --- | --- |
| `haupt-nebenmitglied` | P11-R06/R07; P11-T05 |
| `passive-mitgliedschaft` | P11-R06; P11-T05 |
| `pachtwechsel` | P11-R07/R08/R09; P11-T06/T07 |
| `nachsorge` | P11-R08/R10; P11-T06/T08 |
| `zaehlerwechsel` | P11-R14/R15; P11-T10/T11 |
| `leerstand` | P11-R15; P11-T11 |
| `vereinsgarten` | P11-R15; P11-T11 |
| `individuelle-rechte` | P11-R01/R02; P11-T01/T02 |

Die Fixture-IDs und die Referenzen auf die AP-00-Regeln werden durch `supabase/tests/p11-ap01-fixtures.contract.test.mjs` offline geprüft. Die Fachtests der Folgepakete verwenden diese IDs als stabile, synthetische Ausgangsdaten; sie ersetzen keine Fachlogik und keine RLS-/RPC-Tests.

## Rückfallverhalten

Schlägt ein lokaler Neuaufbau fehl, wird keine Migration erstellt oder angewendet. Der Fehler, die letzte erfolgreich angewendete Migration und die betreffende Fixture werden dokumentiert. Ein fehlender Docker-Dienst ist ein Infrastrukturblocker, kein Grund, auf die verknüpfte oder produktive Datenbank auszuweichen.

## AP-01-Abnahme

AP-01 ist abgeschlossen, wenn der Remote-Migrationsabgleich grün ist, der lokale Neuaufbau bei verfügbarer Docker-Laufzeit durchläuft und die Fixtures vorhanden sind. Die Abnahme am 08.11.2026 ist erfolgt: `rebuild-p11-ap01-test-db.ps1` hat die vollständige Kette bis `20261105230000` sowie den lokalen AP-01-Seed erfolgreich ausgeführt. Die drei Export-Metadatentabellen enthielten danach 7 Definitionen, 11 Filter und 76 Spalten; die Seed-Tabelle `p11_ap01_test_fixture` enthielt 8 Fixtures. Die AP-01-Vertragstests (5/5) und die vorhandene Supabase-Testsuite (87/87) waren erfolgreich. Es wurde kein `db push --linked` ausgeführt und keine Produktivdaten wurden verändert.

Der ergänzende Lauf von `dotnet test KGV.Tests/KGV.Tests.csproj --no-restore` baute `KGV.Core` und `KGV.Infrastructure` erfolgreich, endete jedoch mit sechs vorbestehenden, nicht durch diese Datenbankreparatur verursachten Testfehlern (422/428 erfolgreich): drei Demo-Filter-Tests, ein Test für technische PDF-Dateinamen und zwei Mitgliedsantrag-Template-Regressionen. Die Web-Build- sowie Berechtigungs-, SQL-Berechtigungs- und Capability-Vertragstests waren erfolgreich; der Web-Build meldete nur die bestehende Chunk-Größenwarnung.
