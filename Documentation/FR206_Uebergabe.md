# FR2-06 – Finanzen und Jahresabschluss / Übergabe

**Verifizierte Basis, 22.11.2026:** `work/ap02a-shadow-20261121` bei `fd3a5cb24d1ed6ac4d5f3cd893a6bf14fd778fef`.
`main` liegt bei `52590e9dfa721c675bac98ddd1aa3d77cf4a26fa` und ist mit dem Arbeitsbranch divergiert.
DemoDB `ymxhszxfutjnljiuciyj`: 285 Migrationen, zuletzt `20261122190917`.
ProduktivDB `itjcabiibuodkxayhvjq`: 255 Migrationen, zuletzt `20261121155022`.

## Verbindliche Schutzgrenzen

WPF bleibt ausgeschlossen. **FR2-05-MAUI-E2E läuft parallel.** Deshalb jetzt keinerlei Änderungen an DemoDB, ProduktivDB, Testkonten, Fachrechten in Remote-Datenbanken oder Deployments. Die beiden Katalogrechte `jahresabschluss.finalisieren` und `jahresabschluss.korrigieren` bleiben **inaktiv und fachlich gesperrt**. Eine spätere DemoDB-Migration setzt den erfolgreichen MAUI-Abschluss und die ausdrückliche Nutzerfreigabe voraus; die ProduktivDB eine weitere separate Freigabe.

## Bestandsanalyse

1. Der Fachrechtekatalog enthält die sechs vorgesehenen Rechte. Die zentrale DB-Prüfung `ap02_has_effective_right` berücksichtigt Hauptrollen, Zusatzrollen, Vereinsfunktionen und individuelle Overrides mit Verweigerungsvorrang. Die inaktiven Abschlussrechte können so nicht allein durch Admin oder Einzelgewährung aktiv werden.
2. Die RLS-Finanzpolicies nutzen zum Teil `FOR ALL` und trennen Lesen, Rechnungserfassung, Vorbereitung und Finalisierung bislang unzureichend. Status-Trigger schützt zwar die direkte Finalisierung, aber der Zustandsautomat ist nicht vollständig ausgearbeitet.
3. MAUI öffnet die gesamte Jahresabschlussverwaltung bisher nur mit `jahresabschluss.finalisieren`, das gesperrt ist. Berechtigte Rechnungs-/Kostenartenbearbeiter und vorbereitende Rollen erreichen ihre Bereiche daher nicht.
4. `SupabaseService.ReplaceJahresabschlussPositionenAsync` löscht bisher Positionen und fügt sie in einem **zweiten** HTTP-Aufruf erneut ein. Bei Abbruch verbleibt gegebenenfalls ein teilweise aktualisierter Entwurf.
5. In der Web-Anwendung existiert im geprüften Branch keine fachlich ausgebaute Jahresabschlussoberfläche. Die bestehenden Web-Funktionen dürfen dabei nicht über pauschale Adminrechte legitimiert werden.
6. Die DemoDB enthielt bei der lesenden Prüfung keine Jahresabschluss-Datensätze.

## Vorbereiteter Änderungsumfang

- `supabase/migrations/20261122235600_fr2_06_finance_rights_cutover.sql`: Berechtigungssplit bei RLS, Status-/Quell-/Snapshot-Sperren, und neuer invoker-RPC `fr206_replace_jahresabschluss_positionen`, der Löschung und Einfügen in einer Transaktion kapselt und identische Wiederholungen ohne neue Positions-IDs beantwortet. **SQL noch nicht gegen PostgreSQL ausgeführt.**
- `KGV.Core/Security/PermissionChecks.cs`: separate benannte Rechte für Finanzen lesen, Rechnungen, Kostenarten, Abschluss vorbereiten/finalisieren/korrigieren.
- `KGV.Maui/AdminShell.cs`, `KGV.Maui/Pages/JahresabschlussPage.cs`, `KGV.Maui/Pages/JahresabschlussPruefungPage.cs`: getrennte Menü- und Aktionsfreigaben; endgültiger Abschluss bleibt gesperrt.
- `KGV.Infrastructure/Services/SupabaseService.cs`: ersetzt den zweistufigen DELETE+INSERT durch den RPC-Aufruf.
- `KGV.Tests/Fr206FachrechteTests.cs`: vier xUnit-Fälle zur unabhängigen Rechtezuordnung und negativen Altbit-Probe.
- `KGV.Web/security/fr206-contract.test.mjs`: fünf statische Contracts.
- `tools/FR206/`: bereitgestellte Demo-Freigabevorprüfung und authentifizierter HTTP-Test (beide später und nur mit expliziten Schaltern).

## Test- und Freigabestatus

| Test/Aktion | Status |
|---|---|
| GitHub- und DB-Bestand, Katalogsperren | LESEND BESTÄTIGT |
| Node-Testdatei syntaktisch geprüft (`node --check`) | BESTANDEN im Paketerstellungscontainer |
| SQL-Migration in isoliertem PostgreSQL | AUSSTEHEND – kein Docker/Postgres in der Erstellungsumgebung |
| RLS/RPC mit echten JWTs, Scope/Denies/Idempotenz/Konkurrenz | AUSSTEHEND |
| .NET-Tests und MAUI-Build | AUSSTEHEND – kein dotnet/Android-Workload in der Erstellungsumgebung |
| Web-Build | AUSSTEHEND – im lokalen Installationsskript vorgesehen |
| Git-Worktree, Commit, Push | AUSSTEHEND – GitHub-Connector verweigerte Branch-Create (403); Windows-Dateisystem ist nicht erreichbar |
| DemoDB HTTP-E2E, inkl. positiver Fachoperation und unabhängiger Persistenz | **AUSSTEHEND – WARTET AUF MAUI-ABSCHLUSS UND MIGRATIONSFREIGABE** |
| Weitere positive RPC-/Retry-/Wiederherstellungstests | **AUSSTEHEND – WARTET AUF MAUI-ABSCHLUSS UND MIGRATIONSFREIGABE** |
| Echte FR2-06 Web-/MAUI-UI-E2E | **AUSSTEHEND – WARTET AUF MAUI-ABSCHLUSS UND MIGRATIONSFREIGABE** |
| Produktivmigration | NICHT FREIGEGEBEN |

Der vorbereitete HTTP-Skriptbestand deckt den Rechnungs-POSITIV- und Negativpfad mit erneutem Login ab. Er ist **kein vollständiger Ersatz** für sämtliche geforderten E2E-Fälle. Diese müssen vor Freigabe ergänzt und separat nachgewiesen werden.

## Installation und spätere Schritte

1. ZIP in den Windows-Downloads speichern. Mit dem Ein-Befehl-Bootstrap entpacken; `FR206_Start.ps1` prüft alle SHA-256-Prüfsummen sowie den PowerShell-Parserstatus.
2. `FR206_Install.ps1` kontrolliert den vorhandenen Branch, den exakten Basiscommit und alle Worktrees. Nur falls der FR2-06-Branch/-Worktree **noch nicht existiert**, wird `C:\Programmieren\KGV\KGV.fr2-06` auf dem verifizierten Commit erstellt. Originalbranch und Originaldateien bleiben unverändert.
3. Änderungen werden mittels exakter Suche (jeweils genau ein Treffer) eingespielt. Jede Abweichung führt zum Abbruch. Contract-Test, .NET-Tests, MAUI- und Web-Build werden ausgeführt; vor Commit `git diff --check`.
4. Commit und Push nur mit ausdrücklichem `-CommitAndPush` und nach erfolgreichem lokalen Testlauf. Fehlschläge werden als Fehlstatus im JSON-Protokoll dokumentiert.
5. Danach **isolierte lokale PostgreSQL-Validierung** nachholen: Migration gegen komplette 285er-Basis; mehrfach ausführen nur in jeweils zurückgesetzten lokalen Datenbanken; RLS, Trigger, RPC und konkurrierende Transaktionen mit echten lokalen Identitäten prüfen. Diese Stufe ist momentan nicht automatisiert oder bestanden.
6. Erst **nach** MAUI-Abschluss und ausdrücklicher Demo-Freigabe: neue Bestandsaufnahme; einzigen ausstehenden Migrationsstand prüfen; abschließendes lokales SQL- und HTTP-Testpaket fertigstellen; dann einen gesondert freigegebenen Remote-Migrationsvorgang ausführen. Die mitgelieferte `FR206_DemoMigration.ps1` ist absichtlich **fail closed** und führt auch mit `-Apply` noch keinen Remote-DB-Push aus. Freigabe muss separat ergänzt werden.
7. Danach echte positive authentifizierte Finanz-/Vorbereitungsoperationen, negative Rollen- und Objektscope-Proben und unabhängige Persistenz einschließlich Idempotenz, Wiederholungen, Parallelität und Rollback testen.
8. Gesondert Produktivfreigabe einholen. Bis dahin keine Änderung an der VDB.

## Noch offene fachlich-technische Risiken

Die vorbereitete SQL-Migration ist noch **nicht** auf PostgreSQL geprüft; Fehler in DDL/RPC/RLS sind möglich. Die Anwendung sichert die Berechnung durch einen atomaren Positions-RPC ab, aber das vorherige Schreiben des berechneten Entwurfsstatus bleibt eine separate Operation. Die gesperrten Finalisierung/Korrektur dürfen nicht als erfolgreiche Positivtests erscheinen. Warnungsbestätigungen, Saisonkonflikte, konkurrierende Bearbeiter und vollständige Rechnungs-/Vorauszahlungsketten erfordern eigenständige Prüfungen, bevor eine produktive Abschlussfreigabe überhaupt in Betracht kommt.
