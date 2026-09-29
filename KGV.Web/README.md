# vinext-starter

A clean full-stack starter running on [vinext](https://github.com/cloudflare/vinext), with optional Cloudflare D1 and Drizzle support.

## Prerequisites

- Node.js `>=22.13.0`
- Portable: Windows, macOS, or Linux; no Bash required
- Managed Linux: managed Linux runtime with Bash, `flock`, `curl`, `sha256sum`, and GNU `timeout`
- Git is required only for publishing

## Portables Windows-Testpaket ueber OneDrive

Mit `npm run package:test` wird unter `outputs/KGV-BrowserApp-Test` ein
portables Testpaket erzeugt. Den gesamten Ordner in OneDrive ablegen und mit
den Testern teilen. Der Tester startet `KGV BrowserApp starten.cmd` per
Doppelklick. Eine separate Installation von Node.js ist nicht erforderlich.

Beim ersten Start wird der unveraenderliche Paketinhalt nach
`%LOCALAPPDATA%/KGV-BrowserApp-Test` entpackt. Laufende Server- und
Zwischendateien werden daher nicht ueber OneDrive synchronisiert. Die App
laeuft nur auf dem jeweiligen PC unter `http://127.0.0.1:5173/`; der PC des
Entwicklers muss nicht eingeschaltet sein.

Mehrere Tester koennen das geteilte Paket gleichzeitig verwenden. Jeder PC
betreibt eine eigene lokale Instanz, waehrend alle Instanzen die freigegebenen
Supabase-Daten verwenden. Der Versionsordner in OneDrive darf waehrend einer
Testrunde nicht ueberschrieben werden. Fuer eine neue Version wird ein neuer
freigegebener Ordner bereitgestellt.

Die Browser-App verwendet zentrale serverseitige Bearbeitungssperren. Beim
Oeffnen eines vorhandenen Datensatzes wird eine zehn Minuten gueltige Sperre
angefordert und waehrend der Bearbeitung alle vier Minuten verlaengert. Beim
Verlassen der Ansicht oder beim Logout wird sie freigegeben. Jeder Aenderungs-
und Loeschvorgang prueft die Sperre zusaetzlich unmittelbar vor dem Schreiben.
Die Sperrfunktion qualifiziert ihre Tabellenfelder explizit; dadurch kollidiert
die Rueckgabespalte `expires_at` nicht mit dem gleichnamigen Datenbankfeld.
Mitglieder und Arbeitsstunden synchronisieren dabei die bereits von MAUI und
WPF verwendeten Sperrfelder. Die Supabase-Migration
`20260927120000_browser_edit_locks.sql` wurde am 27.09.2026 gezielt im
Vereinsprojekt installiert und in der Migrationshistorie als angewendet
eingetragen. Andere noch offene lokale Migrationen wurden dabei nicht
veroeffentlicht.

Nach 15 Minuten ohne Maus-, Tastatur-, Touch- oder Scrollaktivitaet meldet die
Browser-App den Benutzer automatisch ab, gibt seine Bearbeitungssperren frei
und widerruft die Supabase-Sitzung. Aktivitaet in einem weiteren Browser-Tab
der gleichen Anmeldung verlaengert den Inaktivitaetszeitraum ebenfalls.

Die Browser-App verwendet fuer die Anmeldung bewusst E-Mail und Passwort.
Der Browser kann diese Zugangsdaten speichern und das automatische Einfuegen
unter Windows mit Windows Hello/PIN freigeben. Eine eigene Windows-Hello-,
Passkey-, OTP- oder Passwort-Neuvergabe-Oberflaeche gehoert daher derzeit nicht
zum festgelegten Browserumfang.

Im Arbeitskontext zeigt die App den Vor- und Nachnamen des ausgewaehlten oder
angemeldeten Mitglieds. Die technische Mitgliedsnummer erscheint nur noch als
Rueckfall, falls die Namensdaten nicht geladen werden koennen.

Demo- und Echtdaten bleiben getrennt: Ein regulaeres Vereinskonto laedt aus
allen entsprechend gekennzeichneten Fachtabellen ausschliesslich Datensaetze
mit `is_demo = false`. Nur ein ausdruecklich als Demo-Konto markierter Zugang
erhaelt stattdessen die Demodatensaetze. Damit entspricht die Browser-App dem
Filterverhalten der MAUI-App.

Die Desktop-Navigation folgt der Reihenfolge der MAUI-`AdminShell`:
`Startseite`, `Impressum`, `Ablesen`, `Parzellenverwaltung`,
`Wartungsvertraege`, `Arbeitsstunden freigeben`, `Export`, `Verwaltung` und
`Mitglieder suchen`. `Ablesen` enthaelt eingerueckt `Foto-Uploads` und
`Zaehlerwechsel`. `Verwaltung` enthaelt wie MAUI `Saisonverwaltung`,
`Jahresabschluss` und `Vereinskonfiguration`. Erst nach einer Mitgliedsauswahl
erscheinen unter `Mitglieder suchen` die eingerueckten mitgliedsbezogenen
Bereiche in der MAUI-Reihenfolge. Nicht aktive aufklappbare Bereiche bleiben
geschlossen.

Der Name des ausgewaehlten Mitglieds wird ausschliesslich innerhalb dieser
aufgeklappten Gruppe `Mitglieder suchen` oberhalb der eingerueckten Unterpunkte
angezeigt. Einen zusaetzlichen globalen Kontextkasten gibt es nicht.

## Seitenabgleich mit MAUI

Der vollstaendige View-fuer-View-Abgleich, die fachlichen Restarbeiten und der
nummerierte weitere Ablauf werden zentral in
`../Browser_Umsetzungsablauf.md` gepflegt. Die folgende Tabelle bleibt die
kompakte Uebersicht fuer die Browser-App.

Die fachlichen Inhalte der MAUI-Seiten sind die Referenz. Ihre mobile
Einzelseiten-Navigation wird am PC jedoch nicht kopiert: Listen, Auswahl,
Detailansicht und Editor duerfen dort gleichzeitig in einer breiten
Master-Detail-Ansicht stehen.

| Bereich | Browser-Stand | Ziel fuer die PC-Darstellung |
| --- | --- | --- |
| Startseite | WPF-Aufbau mit Verwaltungszugang, benutzerbezogener Pflichtstundenuebersicht, drei Inhaltsspalten und vollstaendigen Detaildialogen umgesetzt | Bezieht sich immer auf den angemeldeten Benutzer; eine Verwaltungsauswahl wird nicht angezeigt oder verwendet |
| Startseiten-Verwaltung | Drei getrennte Master-Detail-Arbeitsbereiche fuer Arbeitseinsaetze, Termine und Bekanntmachungen mit MAUI-/WPF-Feldumfang, Eingabepruefungen und Rueckkehr zur Startseite | Die gebuendelte mobile `HomeManagementPage` ist am PC bewusst auf die groessere Arbeitsflaeche verteilt |
| Arbeitseinsaetze | Chronologische Master-Detail-Verwaltung mit vollstaendigem Editor, Datensatznavigation, Teilnehmern, Kapazitaets-/Fristpruefung, Absagen, Loeschen und `Speichern + naechste Schicht` | Fachlich auf Stand der MAUI-/WPF-Verwaltung; Liste und Editor bleiben am PC gleichzeitig sichtbar |
| Termine | Chronologische Master-Detail-Verwaltung mit vollstaendigem Editor, Datensatznavigation, MAUI-Standardzeiten, Sichtbarkeitspruefung, Deaktivieren und Loeschen | Liste und Editor bleiben am PC gleichzeitig sichtbar |
| Bekanntmachungen | Geordnete Master-Detail-Verwaltung mit HTML-Einfuegehilfen, abgeschotteter Live-Vorschau, vollstaendigen Validierungen, Deaktivieren und Loeschen | Liste, HTML-Editor und Vorschau nutzen die breite PC-Arbeitsflaeche |
| Impressum | Vereinskopf, Verantwortliche, dynamische Vorstands-/Bauausschusskontakte sowie Datenschutzdialog umgesetzt | Vereinsangaben links, Kontaktfunktionen rechts; auf kleinen Bildschirmen untereinander |
| Ablesen | Gemeinsamer PC-Arbeitsbereich mit Normal-/JEA-Erfassung, manueller oder optionaler NFC-Auswahl, Plausibilitaetspruefung, Eichfristtabelle, WLAN-gesteuerter Foto-Warteschlange und vierstufiger Pruefung samt Namen und Verlauf | Fachlich fuer 3.1 bis 3.3 angeglichen; eigenstaendige RFID-Einrichtung und vollstaendiger Scanworkflow bleiben unter 3.5/3.6 getrennt offen |
| Parzellenverwaltung | WPF-artige Master-Detail-Verwaltung mit Suche, Status/Pächter, Stammdaten, Anschlüssen, Zählern, Ablesungen, Dokumenten und Belegungsverlauf; mitgliedsbezogene Gartenansicht und Zuordnung integriert | 4.1 bis 4.5 fachlich angeglichen; Liste bleibt links und der vollständige Detailkontext rechts |
| Parzellenprotokolle | Dreistufiger PC-Ablauf mit Grunddaten, Ablesungsbezug, bis zu zehn Fotoanlagen, vier Unterschriften, Entwurf und serverseitiger PDF-Ablage | 4.6 im Code abgeschlossen; geänderte Supabase-Funktion bereitstellen und PDF-/Drive-Ablage produktiv testen |
| Wartungsvertraege | globale und mitgliedsbezogene Master-Detail-Ansicht vorhanden | Aufbau beibehalten und MAUI-Felder/-Regeln vollstaendig abgleichen |
| Arbeitsstunden freigeben | Liste und Pruefung gleichzeitig vorhanden | PC-Aufbau beibehalten; Mitgliedsnamen statt technischer IDs anzeigen |
| Export | Definition, Filter, Ergebnistabelle und CSV vorhanden | breite Ergebnistabelle, feste Filterleiste und PDF-Ausgabe angleichen |
| Saisonverwaltung | Liste und Editor vorhanden | PC-Master-Detail beibehalten |
| Jahresabschluss | nur Menueziel | Rechnungen, Stammdaten/Umlagen und Pruefung als dreigeteilten PC-Arbeitsbereich umsetzen |
| Vereinskonfiguration | Editor vorhanden | Felder wie in MAUI in klaren Abschnitten bzw. Reitern gruppieren |
| Mitgliedersuche | Reine Such- und Auswahlseite für Namen, E-Mail, Mitgliedsnummer und aktuell zugeordnete Gartennummer; inaktive Mitglieder sind standardmäßig ausgeblendet und über einen Schalter zuschaltbar; aktive Parzellenbelegungen werden ohne unzulässigen Demo-Direktfilter verknüpft, Auswahl wird nicht sitzungsübergreifend gespeichert | Nach Auswahl stehen die Mitgliedsbereiche eingerückt im Menü bereit |
| Stammdaten | Eigene Seite unter dem ausgewählten Mitglied mit Bearbeitung, Neuanlage, Nebenmitglied und Mitgliedschaftsende | Weitere MAUI-Felder, Alters-/Pflichtstundenregeln und Mitgliedsantrag-Aktionen noch fachlich ergänzen |
| Dokumente | Liste, Upload, Oeffnen, Archivieren und Vertragserzeugung teilweise vorhanden | MAUI-Funktionen fuer Signatur und geschuetzte Archivierung abgleichen |
| Protokolle | nur Menueziel | Protokollart, Parzelle, Ablesungen, Fotos, Unterschriften, Entwurf und PDF uebernehmen |
| Nebenmitglied | Anlage/Bearbeitung teilweise in Stammdaten eingebaut; Untermenueziel noch ohne Seite | eigene PC-Detailseite auf demselben Datenpfad |
| Gaerten des Mitglieds | nur Menueziel; Zuordnung global teilweise vorhanden | Zuordnungsliste links und Parzellen-Detail rechts |
| Admin-Menue | Rollen und Fachrechte vorhanden | MAUI-Ablesungsfreigabe und kontobezogene Aktionen ergaenzen |
| Arbeitsstunden des Mitglieds | Soll/Ist-Uebersicht, Tabelle, Editor und Verlauf vorhanden | PC-Aufbau beibehalten und MAUI-Regeln vollstaendig pruefen |

Die groessten fachlichen Restbloecke sind damit: `Impressum`, vollstaendige
`Stammdaten`, `Jahresabschluss`, `Protokolle`, `Nebenmitglied`, `Gaerten des
Mitglieds` sowie die noch fehlenden Detailfunktionen in `Ablesen`, `Dokumente`
und `Admin-Menue`.

Das portable Paket ist fuer Windows vorgesehen. Auf iPhone, iPad oder Mac wird
fuer Tests ausserhalb des Heimnetzes spaeter eine HTTPS-Testadresse benoetigt.

## Sites Lifecycle

The Sites initializer copies the shared starter and selects managed-linux only when `SITES_MANAGED_LINUX_CONTAINER=1`; otherwise it selects portable. It saves the selection only in ignored `.sites-runtime/execution-profile.json`. Both profiles copy/configure first, then use the plugin's separate `install-dependencies.mjs` step to measure installation independently. Edit source under `app/` and follow the Sites skill for installation, preview, builds, and publishing.

Run `node <plugin-root>/scripts/configure-execution-profile.mjs` only when the profile is unknown for the current checkout and environment. Profile changes do not alter tracked source or require reinstalling otherwise-valid dependencies; restart an existing preview to use the new selection. Do not commit or upload `.sites-runtime/`.

This starter does not use `wrangler.jsonc`.

`install:ci` runs `npm ci` once against the shared lockfile, disables parent-workspace discovery, and includes required dev/optional dependencies despite production/omit settings. Sharp defaults to prebuilt binaries unless explicitly configured otherwise. Do not overlap installers.

- **Portable:** Preserve host HOME, npm cache, registry, proxy, temporary paths, retry/concurrency settings, and lifecycle-script policy. Use `--prefer-offline --no-audit --no-fund`.
- **Managed Linux:** Use the existing project-local HOME/cache/tmp setup and Linux install lock, tarball preflight, and timeout. Restore the image-seeded npm cache only when its lockfile hash matches; retain network fallback. Builds keep their existing timeout. These helpers are not invoked by the portable profile.

`scripts/sites-env.mjs` preserves the caller's HOME, npm cache, proxy, XDG, and temporary-directory configuration while defaulting Wrangler and Miniflare state to the checkout. If npm reports an unwritable cache, select a writable path with `npm_config_cache` for that install. The `dev` and `start` scripts also keep Wrangler logs inside the checkout. Generated `.sites-runtime/` and `.wrangler/` directories are disposable and ignored by Git.

On portable, `npm run dev` uses `vinext dev` with HMR, starting at port 5173. Vinext records the running server in ignored `.vinext/` state, rejects an ordinary duplicate launch, and recovers stale state after a stopped process; exactly simultaneous starts can race. Pass `--port <port>` or `--hostname <host>` after `npm run dev --` when needed; keep portable previews on loopback.

For browser QA on managed Linux, use `sites-preview start`. The project's dev script runs Vite and accepts the supervisor's `--host 0.0.0.0 --port 4173 --strictPort` arguments. The internal browser uses `http://terminal.local:4173/`; it is not a user-facing URL. The supervisor owns the preview lifecycle. The ignored local profile survives the supervisor's cleared process environment.

The portable profile simulates ChatGPT sign-in only for loopback development requests. Visit `/signin-with-chatgpt?return_to=/` to sign in as `local_seedy` (`seedy@sites.test`, display name `Seedy`) and `/signout-with-chatgpt?return_to=/` to sign out. The development cookie preserves that identity across server restarts. Mock auth is disabled in the managed-linux profile and is not included in production builds; hosted authentication remains dispatch-owned.

The Worker uses `vinext/server/fetch-handler`, including Vinext's config-aware image handling. After building, `npm start` runs that Worker locally through Wrangler on `127.0.0.1`, sharing `.wrangler/state` with dev preview and local D1 migrations; it does not deploy the site or simulate sign-in. Use the URL printed by the server. Pass `npm start -- --port <port>` to select a different built-preview port.

Local previews use Miniflare's placeholder `Request.cf` metadata without a network lookup. Set `CLOUDFLARE_CF_FETCH_ENABLED=true` to opt into fetching preview metadata; this setting does not change hosted request metadata.

Local tool usage metrics are disabled by default. Set `WRANGLER_SEND_METRICS=true` to opt in.

## Included Shape

- edit site code under `app/`
- `app/chatgpt-auth.ts` provides optional dispatch-owned ChatGPT sign-in helpers
- `.openai/hosting.json` declares optional Sites D1 and R2 bindings
- `vite.config.ts` simulates declared bindings for local development
- `db/index.ts` reads the D1 binding from the Cloudflare Worker environment
- `db/schema.ts` starts intentionally empty
- `@cloudflare/workers-types` provides Worker types; `cloudflare-env.d.ts` declares optional `DB`/`BUCKET` bindings—update these declarations if binding names change
- `examples/d1/` contains an optional D1 example surface
- `drizzle.config.ts` supports local migration generation when needed

## Workspace Auth Headers

Signed-in visitors receive both `oai-authenticated-user-id` and `oai-authenticated-user-email`. Private Sites require every visitor to sign in; public Sites may also have anonymous visitors, for whom neither header is present.

The user ID is stable for the same user on the same Site and different across Sites. Use it as the durable user key; use email and name for display or contact purposes.

SIWC-authenticated workspace sites may also receive `oai-authenticated-user-full-name` when the user's SIWC profile has a non-empty `name` claim. The full-name value is percent-encoded UTF-8 and is accompanied by `oai-authenticated-user-full-name-encoding: percent-encoded-utf-8`.

Treat the full name as optional and fall back to email when it is absent:

```tsx
import { headers } from "next/headers";

export default async function Home() {
  const requestHeaders = await headers();
  const userId = requestHeaders.get("oai-authenticated-user-id");
  const email = requestHeaders.get("oai-authenticated-user-email");
  const encodedFullName = requestHeaders.get("oai-authenticated-user-full-name");
  const fullName =
    encodedFullName &&
    requestHeaders.get("oai-authenticated-user-full-name-encoding") ===
      "percent-encoded-utf-8"
      ? decodeURIComponent(encodedFullName)
      : null;

  const displayName = fullName ?? email;
  // ...
}
```

## Optional Dispatch-Owned ChatGPT Sign-In

Import the ready-to-use helpers from `app/chatgpt-auth.ts` when the site needs optional or required ChatGPT sign-in:

- Use `getChatGPTUser()` for optional signed-in UI.
- Use the returned `userId` as the stable user key for user-owned records; do not use email as a durable identifier.
- Use `requireChatGPTUser(returnTo)` for server-rendered pages that should send anonymous visitors through Sign in with ChatGPT.
- In a Server Component, start sign-in with `<a href={chatGPTSignInPath(returnTo)} target="_top">`. The auth helper module is server-only; do not import it into a Client Component.
- Do not use `fetch`, XHR, a client-side router, or a framework link that can prefetch the sign-in route. SIWC must start as a top-level navigation.
- Never request the AuthAPI authorization endpoint directly. The dispatch-owned `/signin-with-chatgpt` route must start the SIWC flow.
- Use `chatGPTSignOutPath(returnTo)` for browser sign-out links or actions.
- Pass a same-origin relative `returnTo` path for the destination after sign-in or sign-out. The helper validates and safely encodes it.
- Mark protected pages with `export const dynamic = "force-dynamic"` because they depend on per-request identity headers.

Dispatch owns `/signin-with-chatgpt`, `/signout-with-chatgpt`, `/callback`, the OAuth cookies, and identity header injection. Do not implement app routes for those reserved paths. Routes that do not import and call the helper remain anonymous-compatible.

SIWC establishes identity only; it does not prove workspace membership. Use the Sites hosting platform's access policy controls for workspace-wide restrictions, or enforce explicit server-side membership or allowlist checks.

Use SIWC for account pages, user-specific dashboards, saved records, and write actions tied to the current ChatGPT user. Leave public content anonymous.

## Local D1 migrations

For a D1-backed local preview, generate SQL with `npm run db:generate`. Build once through the Sites skill's build entrypoint (or `npm run build` for standalone use) to generate `dist/server/wrangler.json`, rebuilding if bindings change. From the project root, apply each pending migration in order:

```sh
node --import ./scripts/sites-env.mjs ./node_modules/wrangler/bin/wrangler.js d1 execute DB --local --config dist/server/wrangler.json --persist-to .wrangler/state --file drizzle/0000_example.sql
```

Replace the filename with the pending migration and `DB` with your D1 binding name if different. Use `.wrangler/state`, not `.wrangler/state/v3`; Wrangler adds the versioned directories. Do not replay migrations already applied locally. This updates only the preview database; publishing applies production migrations separately.

## Diagnostic Commands

- `npm run install:ci`: perform the one locked dependency install
- `npm run dev`: start the Vite/Vinext development server
- `npm run build`: build the deployable Sites artifact
- `npm run start`: preview the built Worker locally with D1/R2 support
- `npm run db:generate`: generate Drizzle migrations after schema changes

When using the Sites plugin, follow its skill instructions for installation, builds, and publishing. These npm commands remain available for standalone use.

The portable build runs Vinext directly without a host `timeout` command. The managed-linux build uses `scripts/build-verified.sh` and its existing `SITES_BUILD_TIMEOUT` setting.

## Learn More

- [vinext Documentation](https://github.com/cloudflare/vinext)
- [Drizzle D1 Guide](https://orm.drizzle.team/docs/get-started/d1-new)
