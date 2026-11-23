# FR2-06 – DemoDB-Migration / Durchfuehrungsnachweis

- Datum: 2026-11-24 00:09:54 +01:00
- Ziel: DemoDB ymxhszxfutjnljiuciyj (ausschliesslich; VDB nicht veraendert)
- Migrationsdatei: 20261122235600_fr2_06_finance_rights_cutover.sql
- SHA-256 der SQL-Migration: b755be741c79d429f24a35bfcddf372a339cf30d1de80a2bc330f966888759d8
- Supabase CLI: 2.121.0
- Vorpruefung: exakt eine ausstehende Migration im db push --dry-run
- Backup vor Migration: oeffentliches Schema und Daten im lokalen, nicht versionierten KGV-Berichtsordner
- Migration: db push --linked mit Exitcode 0
- Nachpruefung: kein ausstehendes Migrationsskript; Remote-Schema-Dump enthaelt sechs gepruefte FR2-06-Marker
- FR2-06 Node-Contracttests: lokal vor dem Push bestanden
- .NET 435/435, Web-Produktionsbuild, MAUI-Release-Build: vorher durch Nutzer bestaetigt; in diesem Ablauf nicht wiederholt
- Authentifizierte HTTP-E2E, Idempotenz-/Parallelitaetspruefungen: **ausstehend**
- MAUI-Geraete-UI-E2E: wird im Gesamtpaket geprueft
- jahresabschluss.finalisieren und jahresabschluss.korrigieren: nicht durch Migration aktiviert
- Keine VDB-Migration, kein Deployment, keine WPF-Aenderungen

**Hinweis:** Erfolgreicher Migrationslauf und Strukturabgleich sind keine vollstaendige fachliche HTTP-E2E-Abnahme.
