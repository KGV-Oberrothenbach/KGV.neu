# P11 – Demo-Bereinigung: Arbeitsplan D0

## Zweck und verbindliche Zielarchitektur

VereinsDB und DemoDB sind zwei vollständig getrennte Supabase-Projekte. Die
VereinsDB enthält ausschließlich Vereinsdaten, die DemoDB ausschließlich
synthetische Daten. Beide Projekte erhalten dasselbe fachliche Schema und
denselben Anwendungscode. Daraus folgt:

- `is_demo`, `is_demo_account` und `allow_demo_access` sind keine fachlichen
  Daten und werden aus dem aktiven Schema und Anwendungscode entfernt.
- Der Zugang wird ausschließlich durch Authentifizierung, Rollenbasis,
  individuelle Gewährungen und individuelle Entzüge entschieden. Die
  effektiven Rechte aus P11 AP-02A bleiben unverändert maßgeblich.
- Objekt-, Eigentümer-, Mitgliedschafts-, Archiv- und Statusschutz bleiben
  bestehen. Sie werden nicht durch die Projekttrennung ersetzt.
- `is_protected` ist kein Demo-Merkmal und bleibt erhalten.

Dieser Plan betrifft ausschließlich die Demo-Bereinigung. Der lokale,
uncommittete AP-02A-Arbeitsstand, insbesondere
`20261108120000_p11_ap02a_effective_permissions.sql`, wird weder geändert,
zurückgesetzt noch vorgezogen. Historische Migrationen bleiben unverändert.

## Festgestellte Ausgangslage

### Aktive Schemaelemente

Die folgenden aktiven Spalten sind zu entfernen, nachdem ihre Abhängigkeiten
umgestellt wurden:

| Tabelle | Demo-Spalte |
| --- | --- |
| `app_user` | `is_demo_account` |
| `auth_allowlist` | `allow_demo_access` |
| `arbeitseinsatz`, `bekanntmachung`, `historical_contract_operation`, `mitglied`, `parzelle`, `saison`, `termin`, `wartungsvertraege` | `is_demo` |
| `fachnummer_reservierung` | `is_demo` (Teil des Primär- und eines Eindeutigkeitsschlüssels) |

Aktiv verwendet werden zudem insbesondere die Helfer
`is_demo_user`, `can_access_demo_scope`, `is_demo_app_user_row`,
`is_demo_or_reviewer`, `is_productive_admin_or_vorstand`,
`is_restricted_demo_admin_or_vorstand`, die `is_demo_*_scope`-Funktionen
sowie `is_demo_mitglied_id`, `is_demo_parzelle_id` und `is_demo_zaehler_id`.
Sie hängen in RLS-Policies, Triggern und SECURITY-DEFINER-Funktionen.

Betroffen sind unter anderem Vertrags-, Dokument-, historische Vertrags-,
Arbeitsstunden-, Termin-, Export-, Zähler- und Nummern-RPCs. Die konkrete
Objektliste wird je Arbeitspaket aus der bestehenden Inventur abgearbeitet;
kein Paket darf eine globale Text-Ersetzung verwenden.

### Nachweis der Projekttrennung und offener Betriebs-Blocker

Der Code leitet Web-REST- und Web-Edge-Aufrufe aus `ClubContext.supabaseUrl`
und `ClubContext.supabasePublishableKey` ab. MAUI löst den Verein im
Vereinsregister auf, speichert dessen URL und Publishable Key im
`Vereinskontext` und erzeugt den Supabase-Client aus diesem Kontext. Damit
ist die beabsichtigte Auswahl zweier Projekte im Anwendungscode vorhanden.

Edge Functions verwenden hingegen pro Deployment die Umgebungsvariablen
`SUPABASE_URL`, `SUPABASE_ANON_KEY` beziehungsweise
`SUPABASE_SERVICE_ROLE_KEY`. Der Repository-Stand kann nicht nachweisen,
welche Werte bei der Bereitstellung in VereinsDB und DemoDB gesetzt sind.

**Sicherheitskritischer Freigabe-Blocker B-01:** Vor dem ersten Rollout der
Bereinigung ist je Projekt außerhalb des Quellcodes lesend nachzuweisen:

1. Jede Function ist in beiden Projekten separat bereitgestellt.
2. `SUPABASE_URL` und alle projektgebundenen Schlüssel gehören jeweils nur
   zum selben Projekt.
3. Kein Service-Role-Schlüssel des einen Projekts ist als Secret im anderen
   Projekt hinterlegt.
4. Keine Function übernimmt eine Ziel-URL oder einen Service-Schlüssel aus
   Request-Body, Query, Header oder Client-Kontext.
5. Token eines Projekts werden gegen Auth desselben Projekts validiert.

Bis zu diesem Nachweis darf die Entfernung der in der Datenbank bestehenden
Demo-Scope-Barrieren nicht produktiv ausgerollt werden. Zugangsdaten werden
nicht ausgegeben oder in Tests protokolliert.

## Gemeinsame Migrationsregeln

1. Neue, chronologisch nach AP-02A liegende Migrationen verwenden; keine
   historische Migration und keine uncommittete AP-02A-Migration umschreiben.
2. Jede Migration ist auf einer frisch aufgebauten lokalen Datenbank zu
   prüfen. Produktiv erfolgt keine automatische Ausführung und kein
   `db push --linked`.
3. Kein `DROP CASCADE`. Vor jedem Entfernen sind abhängige Policies,
   Views, Trigger, Funktionen, Grants und Indizes explizit aufgeführt und
   kontrolliert neu erstellt oder entfernt.
4. Der Abbau erfolgt in zwei Phasen: zuerst neutrale Rechte-/Scope-Logik und
   kompatibler Anwendungscode, zuletzt physisches Entfernen der Spalten und
   Helfer. Dadurch entsteht kein Zwischenstand mit kaputten RLS- oder
   SECURITY-DEFINER-Funktionen.
5. Fachnummern, Vertragsnummern und alle Referenzen bleiben erhalten;
   Nummern werden nie automatisch umnummeriert. Effektive Rechte,
   `is_protected`, Eigentümer- und Statusprüfungen bleiben fachlich
   unverändert.

### Sonderfall `fachnummer_reservierung`

Der gegenwärtige Primärschlüssel und die Quell-Eindeutigkeit enthalten
`is_demo`. Diese Spalte darf nicht einfach entfernt werden, weil sich nach
dem Zusammenfall der beiden Namensräume Konflikte ergeben könnten.

Die dafür vorgesehene Migrationsreihenfolge lautet:

1. Eine Vorprüfungsfunktion bzw. ein migrationsinterner Check ermittelt
   Konflikte für den künftigen fachlichen Schlüssel `nummer` und für die
   künftige Quelle `(quelle_art, quelle_id)`. Der Check muss mit einer
   verständlichen Ausnahme abbrechen, falls es doppelte Werte gibt.
2. Bei Konflikt wird **keine** Zeile gelöscht, zusammengeführt oder
   umnummeriert. Die Konfliktauflösung ist ein separates, fachlich
   freizugebendes Datenbereinigungspaket mit dokumentierter Zuordnung.
3. Nur bei konfliktfreiem Bestand werden zuerst neue explizite
   Eindeutigkeitsindizes ohne `is_demo` angelegt. Anschließend werden die
   bisherigen Schlüssel gezielt ersetzt, alle Fremdschlüssel und Aufrufer
   geprüft und erst dann `is_demo` entfernt.
4. `reserve_fachnummer(text, text, bigint, boolean)` wird erst in demselben
   freigegebenen Paket durch einen Vertrag ohne Boolean ersetzt, nachdem alle
   SQL- und Anwendung-Aufrufer angepasst sind. Ein Übergangs-Wrapper darf
   das Flag nicht mehr auswerten und ist nur zulässig, wenn er eindeutig
   abgekündigt und getestet wird.

## Arbeitspakete

### D1 – Projektverbindungsgrenze verbindlich absichern

**Ziel:** Den getrennten Projektvertrag vor Änderungen am Datenmodell
ausführbar nachweisen und gegen versehentliche projektübergreifende Aufrufe
testen.

| Aspekt | Festlegung |
| --- | --- |
| Betroffene Dateien/Objekte | `KGV.Infrastructure/Supabase/SupabaseClientFactory.cs`, `KGV.Maui/Settings/AppSettings.cs`, `KGV.Maui/Pages/VereinsauswahlPage.cs`, `KGV.Web/lib/supabase/client-context.ts`, `rest-client.ts`, `edge-client.ts`, `KGV.Web/services/auth/club-service.ts`; alle `supabase/functions/*/index.ts`, die Service Role oder `SUPABASE_URL` verwenden; Deployment-Konfiguration beider Projekte (nur lesend). |
| Änderungen | Einen nicht-geheimen Kontrakt-/Integrationstest ergänzen, der beweist: Ziel-URL und Publishable Key kommen ausschließlich aus dem ausgewählten Vereinskontext; Edge-Aufrufe verwenden dieselbe Ziel-URL; keine Request-Eingabe kann eine alternative Service-URL bestimmen. Deployment-Nachweis B-01 dokumentieren. Keine Datenbankmigration. |
| Abhängigkeiten | Keine. D1 ist Voraussetzung für jeden produktiven Abbau von Demo-Scope-Schutz. |
| Risiken | Ein falsch konfiguriertes Edge-Deployment könnte bei entferntem Demo-Scope Daten eines anderen Projekts bedienen. Lokal ist das nicht vollständig beweisbar. |
| Tests | Bestehende Web-/MAUI-Kontexttests, neuer URL-Bindungstest, Quellprüfung der Function-Umgebungsvariablen, manueller lesender Nachweis B-01. |
| Abnahme | Zwei verschiedene Kontext-URLs bleiben in REST- und Edge-Aufrufen isoliert; keine Service-Role-Verbindung ist aus Clientdaten bestimmbar; B-01 ist je Projekt bestätigt oder als Stop-Blocker offen. |

#### D1-Umsetzung und lokaler Prüfstand (2026-11-08)

**Status: lokal technisch abgenommen; B-01 offen, daher nicht vollständig
betriebsseitig abgeschlossen.**

- **Web:** REST-, RPC-, Storage- und Edge-Clients verwenden weiter
  ausschließlich die URL und den Publishable Key des expliziten
  `ClubContext`. Die globale Browser-Sitzungsablage wurde durch
  vereinsbezogene Schlüssel ersetzt. Beim Vereinswechsel wird die Sitzung
  des aktuell gewählten Vereins vor der neuen Auswahl verworfen. Arbeits-
  und Sitzungsdaten anderer Vereine können damit nicht als Kandidat für den
  neuen Verein geladen oder aktualisiert werden. Die fünf verbliebenen
  direkten Edge-Aufrufe (Vertragsabschluss, Mitgliedsantragsabschluss,
  Vertragsverwerfung, Vereinsdokumentversand und zentraler PDF-Export)
  verwenden nun ebenfalls den vorhandenen generischen Edge-Transport. Der
  PDF-Export behält dabei den erforderlichen `Accept: application/pdf`-
  Vertrag.
- **MAUI/Core/Infrastructure:** Der bestehende Vereinswechsel meldet ab,
  leert transienten Zustand und Vereinskontext und beendet den Prozess,
  sodass Singleton-Clients nicht in einen anderen Verein wechseln. Zusätzlich
  wurde `PhotoUploadTestService` vom statischen `appsettings`-Fallback auf
  `ISupabaseClientFactory` umgestellt; Upload und Download verwenden damit
  URL und Publishable Key des ausgewählten Vereinskontexts.
- **Edge Functions:** Der Quellcheck aller Functions mit Service Role hat
  bestätigt, dass URL und Service Role aus Deployment-Umgebungsvariablen
  beziehungsweise deren lokalem `required`-Helfer stammen. Ein Request kann
  weder Ziel-URL noch Service Role übergeben. Die Functions authentifizieren
  ihren Aufrufer mit einem Client desselben konfigurierten Projekts.
- **B-01:** Der lokale Quellcheck ersetzt nicht den Nachweis der tatsächlichen
  Deployments und Secret-Zuordnungen. Zugriff auf diese Betriebsinformationen
  war im Repository nicht vorhanden; B-01 bleibt als manueller, lesender
  Freigabe-Blocker offen. Es wurden keine Secrets gelesen, protokolliert oder
  geändert.

Ausgeführt:

| Prüfung | Ergebnis |
| --- | --- |
| `node --test security/project-connection-boundary.contract.test.mjs` | bestanden: 3/3 (Kontext-URL/Key, getrennte Sitzungen und Caches, MAUI-Wechsel, Edge-Quellbindung) |
| `node --test security/supabase-transport.contract.test.mjs` | bestanden: 5/5; alle Repositories und Services verwenden den expliziten generischen Transport |
| `npm run build` in `KGV.Web` | bestanden |
| `dotnet build KGV.Maui/KGV.Maui.csproj --no-restore` | bestanden, 0 Fehler; bestehende Nullable-, Obsoleszenz- und ImageSharp-Sicherheitswarnungen |

Die tatsächliche Prüfung mit einem Token des jeweils anderen Projekts und
ein isolierter Edge-Function-Integrationstest erfordern zwei separat
bereitgestellte, nichtproduktive Supabase-Projekte inklusive deren
Deployment-Secrets. Sie bleiben zusammen mit B-01 für die manuelle
Betriebsfreigabe offen; gegen Produktivprojekte wurden keine destruktiven
oder verbindenden Tests ausgeführt.

### D2 – Datenbanklogik auf projektneutrale Rechte und Scopes umstellen

**Ziel:** Demo-Sonderentscheidungen aus RLS, Triggern und SECURITY-DEFINER-
Funktionen entfernen, ohne Spalten schon zu löschen.

| Aspekt | Festlegung |
| --- | --- |
| Betroffene Objekte | Alle Policies und Trigger mit `is_demo` oder Demo-Helfern; die oben genannten Demo-Helfer; `current_app_user_context`; `mark_meter_defective`, `end_membership`, Arbeitsstunden-/Arbeitseinsatz-RPCs, `enforce_appointment_announcement_demo_scope`, `acquire_browser_edit_lock_core`, Export-RPCs, historische Vertrags-/Dokument-RPCs, vorbereitete Vertrags-RPCs, Wartungsvertrags-RPCs und Nummern-RPCs. |
| Änderungen | Neue Migration erstellt jede betroffene Policy/Funktion vollständig und explizit neu: effektive Rechte, Eigentümer-/Mitglieds-/Archiv-/Status- und `is_protected`-Schutz bleiben; nur die Datenbankinterne Unterscheidung Demo/Echt entfällt. SECURITY DEFINER behält festen `search_path`, minimale EXECUTE-Grants und authentifizierten Akteur. |
| Abhängigkeiten | D1 mit bestätigtem B-01 für produktiven Rollout; AP-02A-Migration muss lokal davor angewendet bleiben. |
| Risiken | Unvollständig erneuerte Policies können Lesen/Schreiben sperren oder Rechte erweitern; historische Funktionssignaturen und AP-02A-RPC-Tests dürfen nicht brechen. |
| Tests | Lokaler Neuaufbau; RLS-Positiv-/Negativtests pro Objektgruppe; effektive Rechte (Gewährung, Entzug, `CanManageRoles`); RPC-1 bis RPC-6 und deren Schutztests; dokument-/archiv-/eigentümerbezogene Regressionen. |
| Abnahme | Keine aktive Policy, Triggerbedingung oder SECURITY-DEFINER-Funktion trifft mehr eine fachliche Entscheidung anhand von Demo-Status; alle bestehenden Schutzentscheidungen sind negativ getestet. |

#### D2 – lokaler Umsetzungs- und Abnahmestand (09.11.2026)

**Status:** lokal technisch abgeschlossen. Der produktive Rollout bleibt wegen
des offenen betrieblichen Nachweises B-01 gesperrt.

Angewendete Migrationen:

- `20261109090000_p11_demo_cleanup_d2_project_neutral_scope.sql` normalisiert
  die noch vorhandenen Kompatibilitätsfelder auf `false`, neutralisiert die
  bisherigen Demo-Helfer, entfernt Demo-Vollzugriffspolicies und erstellt die
  betroffenen RLS-Regeln für effektive Rechte, Eigentümer-, Mitglieds-, Status-
  und Zeitfensterprüfungen neu. Die Kompatibilitätstrigger verhindern neue
  datensatzinterne Demo-Markierungen.
- `20261109100000_p11_demo_cleanup_d2_project_neutral_constraints.sql` sichert
  die projektneutrale Invariante zusätzlich mit zehn validierten
  `CHECK`-Constraints. Sie gilt damit auch für Wartungssitzungen, die
  Benutzertigger überspringen.
- `20261109110000_p11_demo_cleanup_d2_project_neutral_numbers.sql` führt neue
  Nummernreservierungen ausschließlich im projektlokalen Normal-Namensraum
  aus. Der historische Boolean-Parameter bleibt bis D3/D4 nur aus
  Aufrufkompatibilitätsgründen bestehen und beeinflusst weder Sequenz noch
  Reservierung. Der konfliktgeprüfte Schlüssel- und Signaturwechsel bleibt
  ausdrücklich D4 vorbehalten.

Die physischen Spalten, alten Rückgabefelder und Demo-Helfernamen bleiben bis
D3/D5 erhalten, treffen aber keine Demo/Echt-Entscheidung mehr. Insbesondere
werden alle entsprechenden Datensatzwerte durch Trigger und Constraints auf
`false` festgelegt. `fachnummer_reservierung.is_demo` bleibt bis D4 Bestandteil
der historischen Schlüssel; D2 erzeugt dort ausschließlich `false`-Einträge.

Lokale Abnahme auf einem vollständigen `supabase db reset --local`:

- alle drei D2-Migrationsversionen wurden in Reihenfolge angewendet und der
  Seed lief erfolgreich durch;
- `20261109090000_p11_demo_cleanup_d2_funktionstest.sql` bestanden:
  Neutralisierung der Alt-Felder, Constraint-Schutz bei übersprungenen
  Triggern, RLS ohne Demo-Scope, individuelle Gewährung, individueller Entzug
  auch bei `admin` und projektlokale/idempotente Nummernreservierung;
- AP-02A-RPC-Tests 1 bis 6 einschließlich echter Ersatzvertragsneuanlage:
  vollständig bestanden;
- bestehende Supabase-Regressionssuite: **87/87 bestanden**;
- Web-Sicherheits-/Verbindungsvertragstests: **10/10 bestanden**;
- Web-Produktionsbuild: bestanden (nur bestehender Hinweis auf große Chunks);
- MAUI-Android-Build: bestanden, 0 Fehler; bestehende `NU1902`/`NU1903`-
  Hinweise zu `SixLabors.ImageSharp 2.1.13` bleiben fachfremd offen.

B-01 wurde nicht als erledigt markiert. Es erfolgten kein produktiver Zugriff,
kein Deployment und kein `db push --linked`.

#### Git-Checkpoint nach D2

Der gemeinsame Zwischenstand aus AP-02A, D0, D1 und D2 wird auf dem
dauerhaften Arbeitsbranch `work/p11-ap02a-demo-bereinigung` unter der
Commit-Bezeichnung
`wip(p11): effective permissions and demo cleanup through D2` gesichert.
Dieser Checkpoint ist ausdrücklich keine AP-02A-Gesamtabnahme und keine
Freigabe für D3 bis D6. B-01 bleibt als betrieblicher Nachweis offen. Der
versionierte GitHub-Actions-Workflow führt bei Push lediglich Builds aus und
enthält weder ein produktives Deployment noch einen Supabase-Migrationspush.

### D3 – Core, Infrastruktur, Web, MAUI und Edge-Verträge bereinigen

**Ziel:** DTOs, Filter und Clients verwenden keine Demo-Felder und reichen
keine Demo-Parameter mehr an die Datenbank weiter.

| Aspekt | Festlegung |
| --- | --- |
| Betroffene Dateien | `KGV.Core/Models/*Record*.cs` für Arbeitseinsatz, Bekanntmachung, Fachnummerreservierung, Mitglied, Parzelle, Termin und Wartungsvertrag; `OperationalDataFilter`; `KGV.Infrastructure/Models/AppUser*.cs`, `Services/UserContextService.cs`, `Services/SupabaseService.cs`; betroffene Web-Modelle, Repositories und Services für Benutzer, Bekanntmachungen, Termine, Verträge, Wartung und Vereinsfunktionen; betroffene MAUI-Seiten/Services; `supabase/functions/kgv-upload-document/index.ts` sowie weitere Functions mit `is_demo*`. |
| Änderungen | Felder `IsDemo`, `IsDemoAccount`, `AllowDemoAccess` und Scope-Filter aus Datenverträgen entfernen; normale Objekt- und Berechtigungsprüfungen erhalten; Edge Functions lesen/filtern keine Demo-Markierung mehr und benutzen weiterhin den authentifizierten, projektlokalen Client. |
| Abhängigkeiten | D2 stellt den neutralen Serververtrag bereit. |
| Risiken | Serialisierungs-/Select-Fehler, unerkannte UI-Filter oder ein Edge-Function-Service-Client können nach dem Spaltendrop fehlschlagen. |
| Tests | .NET-Unit-/Contract-Tests, Web-Contract-Tests, Edge-Function-Tests, Web- und MAUI-Build; API-Tests mit einem Vereins- und einem Demo-Projektkontext. |
| Abnahme | Kein produktiver Client-Select, Insert, Update, RPC-Parameter oder DTO erwartet ein entferntes Demo-Feld; beide Anwendungen funktionieren mit demselben fachlichen Schema. |

### D4 – Nummernverwaltung konfliktfrei entkoppeln

**Ziel:** `fachnummer_reservierung` ohne Demo-Anteil schlüsselfähig machen,
ohne Nummern oder Quellbeziehungen zu verlieren.

| Aspekt | Festlegung |
| --- | --- |
| Betroffene Objekte/Dateien | `fachnummer_reservierung`, deren Primär- und Eindeutigkeitsindizes, Foreign Keys, `reserve_fachnummer`, `trg_reserve_vertragsnummer`, `reserve_historische_vertragsnummer` und alle SQL-/C#/TS-Aufrufer; Seed- und Demo-Daten. |
| Änderungen | Neue Migration mit Konflikt-Preflight, neuen nicht-Demo-Eindeutigkeiten, gezieltem Schlüsselwechsel und neuer RPC-Signatur. Alle Aufrufer werden atomar auf den neuen Vertrag umgestellt. |
| Abhängigkeiten | D2, D3. Vor produktivem Rollout muss der Konflikt-Preflight in jedem Projekt konfliktfrei sein. |
| Risiken | Doppelte historische Nummern, abhängige Fremdschlüssel und unentdeckte Boolean-Aufrufer. Kein automatisches Auflösen. |
| Tests | Direkte SQL-Tests für konfliktfreien und absichtlich kollidierenden Bestand; Vertrags-/historische Vertrags-/Wartungsvertrags-Nummernregressionen; lokale Neuaufbauten mit Vereins- und Demo-Seed. |
| Abnahme | Schlüssel enthalten `is_demo` nicht mehr, alle vorhandenen Fachnummern und Referenzen bleiben unverändert, jeder Nummern-RPC arbeitet ohne Demo-Parameter und ein Kollisionsbestand bricht sicher ohne Datenänderung ab. |

### D5 – Physisches Schema und Demo-Helfer entfernen

**Ziel:** Nach abgeschlossener Vertragsumstellung alle Demo-Spalten,
Demo-Funktionen, obsolete Policies/Trigger und Demo-Sonderindizes aus dem
aktiven Schema entfernen.

| Aspekt | Festlegung |
| --- | --- |
| Betroffene Objekte | Alle in „Aktive Schemaelemente“ genannten Spalten; Demo-Helfer; obsolete Demo-Policies/Trigger; Demo-spezifische Indizes und Constraints; `current_app_user_context`-Rückgabetyp ohne `is_demo_account`. |
| Änderungen | Neue explizite Migration in Abhängigkeitsreihenfolge: erst abhängige Funktionen/Policies/Grants, dann Indizes/Constraints, dann Spalten und zuletzt nicht mehr referenzierte Helfer. Vor jedem `DROP` werden Katalogprüfungen ausgeführt. Kein `DROP CASCADE`. |
| Abhängigkeiten | D2, D3 und D4 vollständig abgenommen. |
| Risiken | Verborgenes Objekt, View, Seed oder Function-Test referenziert alte Spalten; der Spaltendrop darf deshalb nicht vor Compile-/Katalogprüfungen erfolgen. |
| Tests | Frischer vollständiger Supabase-Neuaufbau, Katalogscan auf alle entfernten Namen, Schema-Contract-Test, Rechte- und Vertragsregressionen, Seed-Ausführung. |
| Abnahme | Der lokale Zielstand enthält keine aktive Spalte, Funktion, Policy, Trigger, Constraint oder View mit Demo-Daten-Scope; `is_protected` und alle fachlichen Schutzmechanismen bestehen fort. |

### D6 – Seeds, Tests, Dokumentation und Endabnahme

**Ziel:** Die DemoDB wird weiterhin mit synthetischen Daten aufgebaut, aber
ohne Datenzeilen als Demo zu markieren; der neue gemeinsame Schemavertrag ist
dauerhaft regressionsgesichert.

| Aspekt | Festlegung |
| --- | --- |
| Betroffene Dateien | `supabase/seed.sql`, `supabase/demo-data/*`, `supabase/tests/fixture.sql`, manuelle Demo-Skripte, SQL-Regressionen, AP-02A-RPC-1 bis RPC-6-Tests, Web-/Edge-Contract-Tests und P11-Dokumentationen. |
| Änderungen | Synthetische Seeds behalten, aber Demo-Spalten/Parameter entfernen; Tests auf Projekttrennung statt Datenflag umstellen; alte Demo-Schutztests durch Projektgrenzen-, effektive Rechte- und Objekt-Scope-Tests ersetzen; Dokumentation und Testkatalog aktualisieren. |
| Abhängigkeiten | D1 bis D5. |
| Risiken | Tests können scheinbar grün sein, wenn sie alte Fixtures nicht mehr ausführen; lokale Seeds dürfen keine Produktivdaten imitieren oder externe Dienste ansprechen. |
| Tests | Vollständiger lokaler Neuaufbau je Seed, alle Supabase-Regressionen, RPC-1 bis RPC-6, Edge-Tests, Web-/MAUI-Build und die in P11 geforderten Rechte-Regressionen. |
| Abnahme | Beide lokalen Projektkonfigurationen lassen sich mit gleichem Schema und jeweils passenden synthetischen Daten aufbauen; alle Tests verwenden keine Demo-Zeilenmarkierung mehr; D0-Blocker B-01 ist geschlossen. |

## Reihenfolge und Stop-Kriterien

Die Umsetzungsreihenfolge ist verbindlich: **D1 → D2 → D3 → D4 → D5 → D6**.
Ein Paket darf erst beginnen, wenn seine Abhängigkeiten abgenommen sind. Bei
einem offenen B-01 oder einem Fachnummernkonflikt wird nicht mit einem
produktiven Schemaabbau fortgefahren; die konkrete Ursache wird dokumentiert.

Während D2 bis D6 werden die bestehenden AP-02A-Tests für RPC 1 bis RPC 6 bei
jeder berührten Funktionsgruppe ausgeführt und, falls sie ausschließlich ein
entferntes Demo-Feld voraussetzen, kontrolliert auf den neuen Projektgrenzen-
Vertrag angepasst. Ihre effektiven Rechte-, Eigentümer-, Status- und
SECURITY-DEFINER-Negativtests dürfen nicht abgeschwächt werden.

## Nächster Codex-Auftrag: D1

**Erstes umsetzbares Paket:** D1 – Projektverbindungsgrenze verbindlich
absichern.

**Änderungsumfang:** Nur Kontrakt-/Integrationstests und diese Dokumentation
dürfen ergänzt werden. Voraussichtlich betroffen sind
`KGV.Web/lib/supabase/client-context.ts`, `rest-client.ts`, `edge-client.ts`
und deren Testdateien, die genannten MAUI-/Infrastruktur-Kontextklassen sowie
eine neue, secrets-freie Prüfung für Edge-Function-Zielbindungen. Es werden
keine Migrationen, RLS-Policies, RPCs, DTOs oder Demo-Spalten geändert.

**Direkt danach auszuführen:** Die neuen URL-/Kontext-Isolationstests, die
bestehenden Web-Supabase-Contract-Tests, betroffene .NET-Kontexttests sowie
eine lesende Deployment-Checkliste für B-01. Ergebnis muss ohne Secrets
dokumentiert werden.

**Ausdrücklich unberührt:** Die uncommittete AP-02A-Migration und alle
AP-02A-RPCs, effektive Rechte, `is_protected`, Fachnummern, Daten, RLS- und
SECURITY-DEFINER-Definitionen sowie sämtliche Produktionsprojekte.
