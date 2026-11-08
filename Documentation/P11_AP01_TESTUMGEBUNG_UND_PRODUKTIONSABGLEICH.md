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

## Isolierter, reproduzierbarer Neuaufbau

Die Testdatenbank entsteht ausschließlich lokal aus dem gesamten versionierten Migrationsstand. `supabase/seed.sql` wird nur bei `supabase db reset --local` eingespielt und enthält keine produktiven Daten.

```powershell
pwsh -File supabase/tests/rebuild-p11-ap01-test-db.ps1
```

Der Ablauf startet bei Bedarf den lokalen Supabase-Stack, führt `supabase db reset --local` aus und listet anschließend den lokalen Migrationsstand. Er nutzt weder die verknüpfte Datenbank noch Produktionszugangsdaten. Die lokale Docker-Laufzeit war beim AP-01-Abschluss nicht verfügbar; deshalb wurde der vollständige Neuaufbau nicht vorgetäuscht und bleibt ein verpflichtender Ausführungsschritt vor AP-02A.

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

AP-01 ist abgeschlossen, wenn der Remote-Migrationsabgleich grün ist, der lokale Neuaufbau bei verfügbarer Docker-Laufzeit durchläuft und die Fixtures vorhanden sind. Der aktuelle Remote-Abgleich ist grün; der lokale Lauf ist wegen fehlender Docker-Laufzeit noch auszuführen, bevor eine Supabase-Änderung aus AP-02A freigegeben wird.
