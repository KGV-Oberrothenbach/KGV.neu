# FR2-10 – Auswertung und technischer Zwischenstand (24.11.2026)

## Status

**Vorbereitungskandidat – weder migrationsbereit noch vollständig abgenommen.** Keine Mutation der DemoDB/ProduktivDB, kein Rollout, kein GitHub-Commit in diesem Chat. GitHub-Connector konnte `main` lesen, aber Arbeitsbranch-Anlage scheiterte mit HTTP 403 („Resource not accessible by integration“). Ein auf dem Benutzer-PC lokal ausgeführter Git-Workflow ist nicht ersetzt oder nachgewiesen.

## Live verifizierte Fakten

- `main`: `77357c8209c08b9de5b980bfc6401f49dfcbf198` (GitHub API).
- Demo `ymxhszxfutjnljiuciyj`: 289 Migrationen, letzte `20261124192619`, RLS auf den acht untersuchten FR2-10-Tabellen aktiviert.
- VDB `itjcabiibuodkxayhvjq` **nur gelesen**: 255 Migrationen, letzte `20261121155022`.
- Fachrechtekatalog mit 55 Rechten enthält alle fünf benannten FR2-10-Codes.
- Bestehende Termin/Bekanntmachungs-RLS unterscheidet sichtbare Mitgliedsinhalte und Management; keine DELETE-Policy, aber alte DELETE/DDL-ACLs.
- Saison-Migrationspolicy bisher `FOR ALL` und `saison.verwalten`; Vereinsfunktions-Tabellen bisher `FOR ALL` und `vereinsfunktion.verwalten`.
- Vereinskonfiguration hat serverseitig bereits eigenes `verein.konfiguration`-Recht; UI verwechselt es teilweise mit Saisonrecht.
- Direkte Rollen- und Konfigurationschecks in MAUI/Web/Core benötigen Trennung.
- Bestehende Altvertragserfassung und Signatur-Snapshots sind unverändert zu lassen.

## Vorgesehene lokale Änderungen (nicht ausgeführt)

- Core: fünf getrennte Rechtechecks statt alter unabhängiger Permission-Bits; gesonderte Berechtigung für Vereinskonfiguration; Verwaltungsnavigation aus einzelnen Fachrechten.
- Web: Saison und Club-Konfiguration getrennt, Termin/Bekanntmachung direkt anhand aktueller Fachrechtsantwort; passende Contract-Tests.
- MAUI: Vereinskonfigurationsseite und Menü auf eigenes Fachrecht umstellen.
- SQL: unangemessene ACLs (TRUNCATE/TRIGGER/REFERENCES/DELETE) entfernen, `FOR ALL`-Policies aufteilen, Vergangenheits- und Jahresabschlusssperre für Saison, historische Funktionsbesetzungen gegen Löschung schützen.
- Kein neues Fachrecht und keine Änderung an WPF.

## Verpflichtende noch fehlende Nachweise

1. Lokalen Skriptparser und vollständige Ausgangsanker-Prüfung (PWSh 7 ist hier nicht installiert).
2. Code-Änderungen in einem isolierten Worktree, tatsächliche .NET/Web/MAUI-Builds und Regressionen (dotnet fehlt hier).
3. DemoDB-Vollsicherung und unabhängige Wiederherstellbarkeit.
4. SQL-Transaktionstest mit Rollback; positiv/negativ RLS/ACL/Trigger, Konkurrenzfälle.
5. Supabase-CLI `npx --yes supabase@2.121.0`: Migration `new`, `dry-run`, kontrollierte Pushfolge, ausschließlich Demo.
6. Git-Commit und Push auf Arbeitsbranch; SHA/Remote-Nachweis; `main` nicht ohne neue Freigabe mergen.
7. Unabhängige Nachkontrolle von Datenfingerabdrücken und Migrationen.
8. Gesammelte authentifizierte HTTP-E2E- und UI-E2E-Tests erst nach FR2-11.

**Sicherheitsregel:** Paket enthält Kandidaten und wird nicht eigenmächtig auf einer Datenbank angewendet. Es existiert kein beweiskräftiger Test- oder Migrationsabschluss. Lokale Prüfung und Abnahme erforderlich.
