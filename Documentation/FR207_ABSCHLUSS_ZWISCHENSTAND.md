# FR2-07 – Implementierungs-Zwischenstand (Vorbereitung)

**Datum:** 24.11.2026 · **Abhängigkeit:** FR2-06 `33ff4ef6ff5f8178910e4f0876b937c49b1d11bf`.

## Aktuelle, unabhängig kontrollierte Basis

- GitHub: `work/fr2-06-finanzen-jahresabschluss-20261122` → `33ff4ef6ff5f8178910e4f0876b937c49b1d11bf`.
- GitHub-Vergleich zur früheren AP-02-Basis `fd3a5cb…`: genau ein Commit voraus, ohne Rückstand.
- DemoDB `ymxhszxfutjnljiuciyj`: **286 Migrationen**, letzte `20261122235600` (FR2-06).
- ProduktivDB: nicht verändert; keine neue Freigabe.
- Verbindliche technische Arbeitsregeln aus `main` gelesen (Stand 22.11.2026).
- GitHub-Branch-Erstellung via aktuelle ChatGPT-GitHub-Verbindung: **403 – Integration ohne Schreibberechtigung**. Der FR2-07-Branch muss lokal per mitgeliefertem Skript erstellt werden.
- `WPF` strikt ausgeschlossen.

## Codeänderungen (Paketinhalt; nach Installation auf FR2-07-Worktree)

- `KGV.Core/Security/PermissionChecks.cs`: `arbeitsstunden.lesen`, `.bearbeiten`, `.freigeben` und `arbeitseinsatz.verwalten` getrennt über `UserContext.HasFachrecht` prüfen, ohne Legacy-Rollenbit als Berechtigung.
- `KGV.Maui/AdminShell.cs`, `UserShell.cs`: Freigabemenüs ausschließlich nach `arbeitsstunden.freigeben` anzeigen.
- `KGV.Maui/Pages/ArbeitsstundenEditorPage.cs`: administrativ sofort genehmigt nur bei `bearbeiten` **und** `freigeben`, sonst offen; ausgewählter Mitgliedskontext nur mit `bearbeiten`.
- `KGV.Maui/Pages/ArbeitseinsatzTeilnehmerPage.cs`: eigenständige Freigabeberechtigung für Einsatzstunden.
- `KGV.Maui/Pages/ArbeitsstundenReviewDetailPage.cs`: Korrigieren/Löschen nur mit Bearbeiten + Freigeben.
- `KGV.Web/app/page.tsx`: Navigation und Aktionstypen nach vier getrennten Fachrechten.
- Web-Arbeitsstundenservice, -Repository und Komponenten: Offene Erfassung allein mit Bearbeiten, sofortige Genehmigung nur mit zusätzlicher Freigabe; Korrektur-/Löschgates strikt.
- Web-Arbeitseinsatz-Management und Teilnehmeransicht: Freigaberecht explizit getrennt von Einsatzverwaltungsrecht.
- `KGV.Tests/PermissionServiceTests.cs`: alte Bit-Testannahmen sind als Nichtautorisierung zu prüfen, Bit-Konstanten bleiben stabil.
- Neue `KGV.Tests/Fr207FachrechteTests.cs` und `KGV.Web/security/fr207-contract.test.mjs`.

## Vorbereitete SQL-Migration

Das Paket enthält `fr2_07_work_hours_assignment_rights_cutover.sql`. Das Installationsskript lässt die **gepinnt feste CLI** `npx --yes supabase@2.121.0 migration new fr2_07_work_hours_assignment_rights_cutover` den Dateinamen erstellen. **Die Installation führt keine Migration aus.**

Die SQL-Datei führt erst nach gesonderter Freigabe/Kontrolle aus:

1. FR2-06- und Fachrechte-Basisguards, Abbruch bei fehlenden RPC-Quellankern.
2. RLS-Trennung: Lesen, offene administrative Erfassung, offene Bearbeitung, Freigabe-/Prüfqueue.
3. Eigene offene Arbeitsstunden bleiben auf `current_mitglied_id()` beschränkt; fremde offene Bearbeitung erfordert Bearbeitungsrecht und aktiven eigenen Browser-Lock.
4. Geprüfter Trigger-Cutover: eigenständige RPC für Einsatzstundenbestätigung mit `arbeitseinsatz.verwalten` + `arbeitsstunden.freigeben`, ohne pauschalen Bearbeitungsrecht-Zwang; direkte genehmigte Neuanlage benötigt beide Stundenrechte.
5. Idempotente Einreichung/Bestätigung von Einsatzstunden bei identischem bereits vorhandenem Beleg; abweichende Wiederholung bleibt gesperrt.
6. Review-RPC: Ablehnung/Freigabe mit Freigaberecht; Korrektur/Löschen nur zusätzlich mit Bearbeiten, Audit und nicht-retrybarer finaler Zustand (SQLSTATE `23514`, nicht `40001`).
7. Schutz von Saison- und Einsatzdaten sowie Verhinderung historisch destruktiver Einsatzlöschungen.
8. Privates Audit für Stunden/Einsätze/Anmeldungen, keine Client-CRUD-Rechte.
9. Arbeitsstundenexport nur mit `arbeitsstunden.lesen` **und** `export.ausfuehren`, keine öffentliche RPC-Ausführung.
10. `TRUNCATE`, `TRIGGER`, `REFERENCES` für normale Anwendungssessions auf betroffenen Tabellen entziehen.

## Test- und Freigabestatus

| Prüfstand | Status |
|---|---|
| Statische Basiskontrolle GitHub | unabhängig verifiziert |
| FR2-06-Demo-Migration | unabhängig verifiziert installiert |
| ZIP-/Datei-Formatierung, Node-Syntax | im Paket vorgeprüft |
| FR2-07-Worktree lokal | Installationsskript vorhanden; **noch nicht ausgeführt** |
| FR2-07 SQL-Migration | **vorbereitet, nicht angewendet** |
| .NET-Unit-Tests | **nicht ausgeführt** |
| Web-Contracttests | **nach Installation ausführbar, noch nicht ausgeführt** |
| Web-Produktionsbuild | **nicht ausgeführt** |
| MAUI-Android-Release | **nicht ausgeführt** |
| Authentifizierte HTTP-E2E + SQL-Persistenz | **nicht ausgeführt** |
| Git-Commit/Push/Deployment | **nicht erfolgt** |

## Risiko-/Freigabegates

- Die DemoDB hatte bei der vorherigen Prüfung für fünf App-Identitäten **kein** effektives Bearbeiten-/Freigeben-/Einsatzverwaltungsrecht. Die aktuellen Rechte vor HTTP-E2E erneut prüfen. Keine generische Admin-Ausnahme.
- Demo-Migration ausschließlich nach SQL-Review, Prüfung der konkurrierenden Arbeitspakete und Migrationsliste (nicht pauschal `db push` gegen unbekannte Migrationen).
- Synthetischen E2E-Mitgliedskontext vor jedem Schreiben ausdrücklich identifizieren und alle negativen Varianten zuerst testen.
- Selbstservice und historische Pflichtstundenberechnung nicht durch angenommene Rollen oder wechselnde Tageswerte ersetzen.
- Genehmigte Stunden bleiben unveränderlich; ein nachträglicher Korrektur-/Rücknahmeprozess benötigt eine fachliche Entscheidung.
- Die SQL-Quellanker der bestehenden RPCs sind auf dem aktuellen Demostand zu validieren; bei Abweichung ohne Teilausführung abbrechen.
- Kein Produktiveinsatz ohne reale HTTP- und unabhängige SQL-Persistenznachweise.

**Übergabe:** FR2-07 ist fachlich/technisch vorbereitet, aber kein bestandenes Arbeitspaket, bis die oben genannten Tests und Freigaben erfolgreich dokumentiert sind.
