# AP-01.5 – Sicherheitsprüfung

Stand: 18.11.2026

## Änderungen

1. Vertragsversand-Snapshot-RPC für direkte Aufrufe
   durch anon und authenticated gesperrt.
2. Auth-Allowlist-Hook für direkte Aufrufe
   durch anon und authenticated gesperrt.
3. Interne Dienstberechtigungen erhalten.

## Migrationen

- 20261118203000_ap015_secure_contract_snapshot_rpc.sql
- 20261118210651_ap015_restrict_auth_allowlist_hook.sql

## Prüfungen

DemoDB: 243 Migrationen.
ProduktivDB: 241 Migrationen.

Bestanden:
- Berechtigungsprüfungen der beiden Funktionen.
- Direkte Aufrufe durch unberechtigte Rollen abgewiesen.
- Interne Service-Berechtigungen erhalten.
- Auth-Hook akzeptiert zugelassene und verweigert
  nicht zugelassene E-Mail-Adressen.
- Synthetische Aufnahmebestätigung mit Finaldokument,
  Versand-Snapshot, Empfänger und Versandinstanz.
- Wiederholter Versandaufruf ist idempotent.
- Synthetischer Test vollständig zurückgerollt.

## Ausstehende Freigabeschritte

- Git-Review und Merge nach main.
- Kontrollierte Übernahme in ProduktivDB.
- Berechtigungs- und Regressionstests nach Übernahme.

Nicht durch diese Prüfung abgedeckt:
- Vollständige Web-/MAUI-End-to-End-Tests.
- Tatsächlicher E-Mail-Versand.
- Vollständige End-to-End-Tests aller Vertragsarten.

Die allgemeine Produktionsfreigabe der KGV-Software
ist damit noch nicht erteilt.