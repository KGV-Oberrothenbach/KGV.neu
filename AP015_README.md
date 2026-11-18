# KGV Produktionsreife – AP-01.5/F1

**Basis-Commit:** `8a8f5b00e04d2e21828218a5d33c6c334325d9a3`
**Stand:** 18.11.2026

## Inhalt

- `supabase/migrations/20261118203000_ap015_secure_contract_snapshot_rpc.sql`
- `supabase/tests/20261118203000_ap015_contract_snapshot_privileges.sql`

Der Migrationsname wurde entsprechend der bestehenden Zeitstempelkonvention im Repo gewählt. **Die CLI konnte in der Arbeitsumgebung nicht ausgeführt werden; die Datei wurde daher nicht mit `supabase migration new` erzeugt.** Vor dem Anwenden auf einen gemeinsam genutzten Branch auf Versionskonflikte prüfen.

## Befund und Grenzen

Der öffentlich zugängliche `SECURITY DEFINER`-RPC `ensure_vertragsversand_snapshot(bigint)` kann interne Vertrags-Snapshots erstellen. Das `EXECUTE`-Recht wird für `PUBLIC`, `anon` und `authenticated` entzogen; `service_role` und `postgres` bleiben berechtigt. Der Trigger `freeze_vertragsversand_snapshot()` und die Definer-Versandroutinen sind Eigentum von `postgres`, so dass deren innere Aufrufe nach aktuellem Datenbankstand weiterhin berechtigt sind.

Die geplante Migration wurde in der **DemoDB nur transaktional und mit ROLLBACK** vorgetestet, auch durch echte direkte negative Aufrufe als `anon`/`authenticated`. Kein produktiver Datensatz wurde verändert.

**Noch NICHT nachgewiesen:** dauerhafte Demo-Migration, Regression eines vollständig finalisierten synthetischen Vertrags inklusive Dispatch, Web-/MAUI-Builds und produktive Bereitstellung. Die Migration darf nicht ohne diese Endabnahme produktiv angewendet werden.

## Empfohlener Ablauf

1. Git `main` und sauberen Ausgangscommit prüfen, lokalen Prüfbranch anlegen.
2. Dateien unter denselben Pfaden in das lokale Repository kopieren.
3. `supabase migration list --linked` gegen **DemoDB** und `supabase db push --linked --dry-run` (nur AP015 darf ausstehen).
4. In die DemoDB installieren; Postmigration-Test `supabase/tests/...sql` über psql oder den SQL Editor ausführen.
5. Synthetische vollständige Vertragsabschluss-/Versand-Regression in sicherer Rollback-Testumgebung; Builds und GitHub-Review.
6. Erst nach Freigabe und gesicherter ProduktivDB dieselbe Migration kontrolliert in die ProduktivDB einspielen.

Nicht `supabase db reset`, `migration repair` oder unkontrollierte DB-Pushes auf Produktion ausführen.
