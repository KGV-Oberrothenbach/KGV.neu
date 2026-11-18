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
ProduktivDB: 243 Migrationen.

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

## Abgeschlossene Freigabeschritte

- Fast-Forward-Merge nach main erfolgreich.
- Beide Migrationen in DemoDB und ProduktivDB installiert.
- Beide Datenbanken auf 243 Migrationen.
- Tatsächliche EXECUTE-Rechte beider Funktionen geprüft.
- Unberechtigte Direktaufrufe in ProduktivDB abgewiesen.
- Auth-Hook-Tests für zugelassene und nicht zugelassene E-Mails bestanden.
- Funktionsdefinitionen zwischen DemoDB und ProduktivDB identisch.

Produktivbackup: VDB_2026-11-18_20-45-23.
Sicherungsbericht erfolgreich; vollständiger Restore nicht nachgewiesen.

Die Sicherheitskorrektur AP-01.5/F1 ist abgeschlossen.
Die allgemeine Produktionsreifeprüfung läuft weiter.

Nicht durch diese Prüfung abgedeckt:
- Vollständige Web-/MAUI-End-to-End-Tests.
- Tatsächlicher E-Mail-Versand.
- Vollständige End-to-End-Tests aller Vertragsarten.

Die allgemeine Produktionsfreigabe der KGV-Software
ist damit noch nicht erteilt.