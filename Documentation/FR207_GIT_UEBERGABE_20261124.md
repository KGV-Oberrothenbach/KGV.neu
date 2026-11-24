# FR2-07 – Git-Übergabe

**Stand:** 24.11.2026

## Umsetzung

- Fachrechte 2.0 für Arbeitsstunden und Arbeitseinsätze.
- Core, MAUI, Web, Tests und Supabase angepasst.
- WPF unverändert und ausgeschlossen.
- Basiscommit: 33ff4ef6ff5f8178910e4f0876b937c49b1d11bf.

## Verifizierter Prüfstand vor Git-Commit

- .NET: 443 Tests bestanden.
- Web: Produktionsbuild bestanden.
- MAUI Android: Release-Build bestanden.
- FR2-07: 10 Contracttests bestanden.
- DemoDB: Migration 20261124155223 installiert.
- DemoDB: 287 Migrationen insgesamt.
- Sechs vorhandene Arbeitsstunden unverändert.
- Datenfingerabdruck: cc3d463e6775d01fab584dbc8bc540b6.
- RLS-Policies, Audit-Trigger und RPC-Ausführungsrechte geprüft.

## Bewusst zurückgestellte Prüfungen

- Authentifizierte HTTP-E2E-Tests und zugehörige Persistenzprüfungen.
- Vollständige Web- und MAUI-UI-E2E-Abnahme.
- Diese Tests erfolgen gesammelt nach Abschluss aller FR2-Arbeitspakete.
- Contracttests und Builds ersetzen diese Nachweise ausdrücklich nicht.

## Freigabestatus

- Fachrechte-Code und Demo-Migration umgesetzt.
- Keine FR2-07-Migration auf der ProduktivDB.
- Kein Produktionsrelease oder Deployment.
- Vollständige fachliche Abnahme wegen zurückgestellter E2E-Tests offen.
