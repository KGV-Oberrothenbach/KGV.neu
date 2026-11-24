# KGV – FR2-08 Dokumentenverwaltung – technischer Paketstand

**Basis:** GitHub `work/fr2-07-arbeitsstunden-arbeitseinsaetze-20261124` @ `c5843f9bf9aeeda3f09317d6795802551b88561e` (unabhängig geprüft 24.11.2026).
**Ziel:** `work/fr2-08-dokumentenverwaltung-20261124`; gesonderter Worktree `C:\Programmieren\KGV\KGV.fr2-08`.

## Live-Inventur (read-only, 24.11.2026)

- DemoDB `ymxhszxfutjnljiuciyj`: **287 Migrationen**, zuletzt FR2-07 `20261124155223`.
- ProduktivDB `itjcabiibuodkxayhvjq`: **255 Migrationen**, zuletzt `20261121155022`. **Kein Datenbank-Write in diesem Paket.**
- Vier private Storage-Buckets: `dokumente`, `pdf-templates`, `prepared-contract-signatures`, `vereinsregelwerke`.
- DemoDB: 24 `dokument`-Zeilen, darunter 19 Mitglieder- und 5 Parzellendokumente; alle 24 mit Google-Drive-ID, 7 mit Vertragsprovenienz. Storage-Zeilen: 42 Vorlagen, 11 Vertragsunterschriften, 4 Vereinsunterlagen. Dies sind Ausgangszahlen, keine Abschlusszählung.
- Alte Storage-Policies auf `vereinsregelwerke` benutzen `current_app_user_context().role` ohne individuelle Fachrechtsentzüge. `pdf-templates` ist über `is_productive_admin_or_vorstand()` (liefert in Demo `false`) fehlkonfiguriert.
- `save_dokumenttyp` ließ bei allein `dokumenttyp.verwalten` das Löschen und Neusetzen der Wirkungen zu.
- `dokument_select_effective` machte `dokument.verwalten` zu einem impliziten Leserecht; Edge-Downloads benutzten eine eigene Service-Role-Berechtigungsnachbildung. Der Web-Storage-Fallback konnte signierte URLs erzeugen.
- `dokument`/`dokumenttyp` und weitere Tabellen hatten unnötige `TRUNCATE`, `TRIGGER`, `REFERENCES`-Privilegien für `authenticated`.

## Geplante, noch **nicht** in Demo angewandte Migration

`fr2_08_document_security_cutover.sql` wird im **neuen lokalen Worktree** über `npx --yes supabase@2.121.0 migration new` versioniert. Die Migration führt nur Rechte-, Policy-, Funktions- und Triggeränderungen durch; kein `DELETE`/`UPDATE` an bestehenden Dokumentdaten oder Storage-Objekten. Ein vollständiger Backup, DDL-Review und unabhängige SQL-/HTTP-Nachweise sind vor der Freigabe nötig.

Abgrenzung der Fachrechte:

| Fachrecht | Autorisierung |
|---|---|
| `dokument.lesen` | Dokumente lesen (zusätzlich eng definierter Eigen-/Historienzugriff) |
| `dokument.verwalten` | Dokument hochladen/bearbeiten mit verifiziertem Objektbezug, nicht automatisch lesen |
| `dokument.archivieren` | Geschützte Archivaktion mit eigenem Recht, Passwort und Objekt-Guard |
| `dokumenttyp.verwalten` | Typ-Stammdaten ändern, nicht automatisch Wirkungen |
| `dokumentwirkung.verwalten` | Typ-Wirkungen ändern, aber nicht für einen bereits belegten Dokumenttyp |
| `vorlage.verwalten` | Private Vorlagen lesen und als neue unveränderliche Datei anlegen |

`vereinsunterlagen.verwalten` bleibt **zusätzlich**, unabhängig von den sechs Rechten, die Voraussetzung für Pflege von Vereinsregelwerken. `vereinsdaten.alle_lesen` gibt den entsprechenden Administratoren explizite übergreifende Leserechte, ohne Schreibrecht.

Die SQL schützt bestehende `dokument`-Datensätze, `pachtvertragsfassung`, `vereinsdokument_fassung`, `dokumenttyp_wirkung` für belegte Typen und registrierte Storage-Dateien. Unter `fr208_can_archive_dokument` werden verknüpfte historische und signierte Verträge ausdrücklich von der allgemeinen Archivaktion ausgenommen.

## Grenzen / Restrisiken / Abnahmesperren

1. **Diese Datei ist eine Implementierungs- und Prüfvorbereitung, keine Freigabebescheinigung.** Weder SQL-Migration noch Git-Push, E2E-Tests oder Produktionsdeployment werden von ChatGPT remote durchgeführt (GitHub-Schreibzugriff: 403).
2. **.NET, MAUI, Web-Produktionsbuild** müssen lokal mit dem beigefügten Installationsskript ausgeführt werden; der Container besitzt weder dotnet noch pwsh, und hat keinen Checkout.
3. Die Cloud-Edge-Funktion muss **gesondert** mit der neuen Version deployed werden. Ohne Edge-Deployment dürfen die neuen Upload-/Archivpfade nicht produktiv verwendet werden; ohne SQL-Migration darf die neue Edge nicht aktiviert werden. Eine koordinierte Demo-Rollout-Reihenfolge ist nötig.
4. `storage.objects`-Policy- und RPC-Änderungen sind noch nicht mit authentifizierten Sessions getestet. Besonders SQL-Grants, die `storage`-API und SEC-DEF Rechte sind über HTTP zu kontrollieren.
5. **HTTP-E2E gesammelt nach allen FR2**: positiver/negativer JWT, echte Rechteentzüge, Fremdobjekt, Upload/Download von Google Drive und Supabase Storage, private Bucket-URL, Archivierung/Passwort, vorhandene Vorlagen, Wirkungstrennung, Versionen, Signatur-Recovery, Historien-Workflow MA+PV/PV, Hashwerte, Idempotenz, parallele Anfragen, unabhängige Persistenzkontrolle.
6. Der im SQL-Entwurf enthaltene Entzug direkter `INSERT/UPDATE/DELETE`-Rechte auf `dokument` ist vor dem Demo-Rollout gegenüber sämtlichen RPCs und direkten MAUI-Aufrufstellen zu verifizieren. `SECURITY DEFINER` und Service-Role-Flows bleiben getrennt legitimiert. Bestehende Freigabe-/Signatur-Workflows dürfen dadurch nicht ausfallen.
7. Revisions- und Storage-Konkurrenzschutz für neu hochgeladene Vereinsunterlagen sowie E2E-Nachweise zu Fehler-Recovery bleiben offen. Kein produktiver Einsatz ohne diese Nachweise.
8. Historische Verträge bleiben fachlich MA+PV (Erstvertrag) oder PV (vorhandenes Mitglied). Der neue Sicherheits-Cutover darf diesen Ablauf nicht aufteilen.

## Arbeitsschritte nach der lokalen Paketinstallation

1. Ergebnisprotokoll des Installers überprüfen: vollständiger Patch, Node-Vertragstests, .NET-Tests, Web-Build, MAUI Android-Release, `git diff --check`.
2. Patch und SQL sorgfältig diffen, insbesondere bisherige Vertragssignaturen, Templates, bestehende Archivierung und Core/MAUI.
3. Vollständige Demo-Sicherung + unabhängigen aktuellen Demo-Schema- und Migrationsabgleich durchführen.
4. **DemoDB-Migration nur nach kontrollierter Freigabe anwenden**. ProduktivDB nicht ändern.
5. Neu entstandene effektive RLS-/Storage-Policies, ACLs, Trigger, Migrationseintrag und unveränderte Bestandszahlen in unabhängigen SQL-Queries kontrollieren.
6. Die Authenticated-HTTP-E2E-Prüfungen erst in der vereinbarten gemeinsamen Abschlussphase aller FR2 durchführen.
7. Erst dann FR2-09 (Vorgänge und Kommunikation) beginnen. FR2-09 muss gemeinsame Dokument- und Versand-Snapshots auf konsistente Rechte prüfen.

## FR2-08 Abschlusskorrekturpaket (24.11.2026; Prüfung vor Ort erforderlich)

Die externe Sicherheitsanalyse bestätigte am 24.11.2026 vor der Migration die 287 DemoDB- und
255 ProduktivDB-Migrationen sowie 24 vorhandene Dokumentzeilen. Branch-HEAD war
`77bd3bd63ce3fee1575504006cda62628469a665`. Dies sind **Ausgangswerte**, kein Abnahmenachweis.

Das Paket ergänzt: (1) Wartungsvertragszuordnungs-RLS mit Dokument- und Zuordnungsbezug,
(2) service_role-only transaktionale Passwort-geschützte Archivierung über Edge,
(3) Locks gegen Archiv/Vertrag-Konkurrenz und physische Dokumentlöschung,
(4) Historien-/Typwirkungssperren einschließlich alter/neuer Typ-ID,
(5) sichere serverseitige Dokumenttyp-Verwendungsabfrage,
(6) getrennte Metadaten-/Wirkungs-RPCs mit Expected-State-Abgleich,
(7) Web-UI- und MAUI-Kompatibilitätskorrektur (Delete-Schnittstelle verweigert sicher),
(8) zusätzliche statische Tests und HTTP-E2E-Matrix.

**Freigabesperren:** SQL-Semantik, Migration, Deployment und lokale Builds müssen durch den
mitgelieferten PowerShell-7-Installer erfolgreich belegt werden. Die vorab aus einem Linux-
Container erstellten Dateien besitzen noch keinen lokalen .NET-/MAUI-/Deno- oder Supabase-
Laufzeitnachweis. Die Benutzerentscheidung zur Verschiebung der authentifizierten HTTP-E2E-
Tests bleibt bestehen. Keine ProduktivDB-Änderung und kein Merge nach `main`.

Der Installationsbericht ist in `%TEMP%\\KGV_FR208_Abschlussberichte` zu finden. Erst seine
überprüften Resultate in einen FR2-09-Startprompt übernehmen, niemals behauptete Wunschwerte.

## Zusätzlicher Storage-Befund: DELETE/Versionsschutz

Die alte direkte Storage-DELETE-Policy für unregistrierte Vereinsunterlagen
wird durch die ursprüngliche FR2-08-Migration ersetzt. Die neue Variante
enthält jedoch eine nicht ausreichend sichere `NOT EXISTS`-Abfrage gegen die
RLS-geschützte Fassungstabelle. Bei fehlendem Leserecht kann eine vorhandene
Fassung unsichtbar bleiben. Deshalb entfernt die Abschlusskorrektur auch die
neue direkte DELETE-Policy. Registrierte Fassungen sind unveränderlich;
fehlgeschlagene unregistrierte Uploads erfordern eine explizite gesicherte
serverseitige Bereinigung statt eines ausweitbaren Endnutzer-Löschrechts.
