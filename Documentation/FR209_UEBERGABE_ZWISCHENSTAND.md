# FR2-09 – Vorgänge und Kommunikation – Zwischenstand

Stand: 24.11.2026. **Dies ist KEIN Abschluss und KEINE Freigabe.**

## Verifizierter Ausgangsstand

- GitHub: `KGV-Oberrothenbach/KGV.neu`, `main` SHA `52590e9dfa721c675bac98ddd1aa3d77cf4a26fa`.
- FR2-08 `work/fr2-08-dokumentenverwaltung-20261124` SHA `a0ad301f0d9d090b352ead254c54a718505db33a`.
- DemoDB `ymxhszxfutjnljiuciyj`: 288 Migrationen, letzte `20261124173955`.
- ProduktivDB `itjcabiibuodkxayhvjq`: 255 Migrationen, letzte `20261121155022`; **nicht verändert**.
- Unabhängige DemoDB-Abfrage: 55 Fachrechte, 8 Kommunikationsvorgänge, je 0 Versandinstanzen/Snapshots und 0 Serienbriefvorlagen.
- Demo Edge-Funktionen `kgv-send-contract-documents` v3, `kgv-send-club-documents` v7, `kgv-upload-document` v18 ACTIVE.

## Sicherheitsbefunde

1. `kommunikationsvorgang` besitzt `FOR ALL`-RLS bei `vorgang.bearbeiten` und auch gefährliche Grants `DELETE`, `TRUNCATE`, `TRIGGER`, `REFERENCES` an `authenticated`.
2. Sechs Versand-/Snapshot-Tabellen besitzen überbreite Rechte für `anon` und `authenticated` einschließlich `TRUNCATE`. RLS verhindert TRUNCATE nicht.
3. Versand-Snapshot-RLS prüft `kann_vertragsversand_ausfuehren()` unabhängig vom zugehörigen Vorgang.
4. Übernahme unzugeordneter Vorgänge geschieht aus Web und MAUI direkt über REST-PATCH; vorhandene RLS autorisiert den ursprünglichen Zustand `zugewiesen_an IS NULL` nicht sauber.
5. Serienbrief-UI nutzt `CanManageCommunicationWorkflows` statt `serienbrief.erstellen`; MAUI `CanManageCommunicationWorkflows` verwendet noch alte Permissions.
6. `kgv-send-club-documents`: unabhängige Versandberechtigung und Objekt-/Empfängerprüfung fehlen; das SMTP-Senden erfolgt vor der Nachweisspeicherung. **Doppelversandrisiko**.
7. `kgv-send-contract-documents`: `claim_contract_dispatch` reaktiviert nach 15 Minuten; SMTP-Erfolg und nachfolgendes DB-Update können auseinanderfallen, wodurch Double-Send möglich bleibt.

## Vorbereitete, ausdrücklich **nicht ausgeführte** Änderung

- SQL **Kandidat** `fr209_security_cutover.sql`: separierte RLS, eingeschränkte Grants, guarded status writes, objektgebundene Versandhistorie und atomare Übernahme-/Freigabe-RPCs.
- `apply-fr209.mjs`: bytegenaues Git-Blob-Preflight und Fail-closed-Änderungen an Core, Infrastructure, Web. Web-Serienbriefrechte werden getrennt; beide Clients nutzen neue Übernahme-/Freigabe-RPCs.
- Noch keine produktive sichere SMTP-Claim-/Finalize-Architektur implementiert. `kgv-send-club-documents` und `kgv-send-contract-documents` dürfen für FR2-09 nicht als abschließend gehärtet gelten.
- `FR209_E2E_TESTMATRIX.md`: 40 Szenarien; bewusst **später auszuführen**.
- `FR209_POST_MIGRATION_AUDIT.sql`: lesende Kontrollabfragen.

## Lokale Grenzen und Sperren

GitHub-Anlage des Arbeitsbranches per Connector wurde mit **HTTP 403** abgelehnt. Lokaler Windows-Arbeitsbaum, vorhandene Worktrees, DPAPI-Testgeheimnisse und Git-Konfiguration sind für diesen Chat nicht zugänglich; kein tatsächlicher lokaler .NET-/Web-/MAUI-Build und kein PowerShell-Parserlauf möglich. Daher ein ZIP zur **kontrollierten lokalen Vorbereitung** statt eines behaupteten Abschlusses. Der Installer darf **nicht** eigenmächtig die DemoDB ändern oder Mails versenden. SQL-Kandidat benötigt vor Migration zusätzlichen Review und echte Laufzeit-/SQL-Prüfungen. Kein Commit/Push wurde bisher durch diesen Chat durchgeführt.

Nach lokalem erfolgreichen Prüflauf müssen ergänzend: Live-Review des gesamten Mail-Sendeprotokolls (Idempotenz, Provider-Akzeptanz, retry-sichere Zustandsmaschine), SQL-Funktionstests und echte PostgreSQL-17-Konkurrenztests, zusammenhängende Sicherung/Dry-Run/Demo-Rollout, Remote-Audit, Git-Commit/Push und später gemeinsame HTTP-E2E-Tests erfolgen.

**Historie unverändert:** MA+PV bei Erstvertrag, PV bei bestehendem Mitglied; mehrere Parzellen und 80xxx-Nachweise beibehalten.
