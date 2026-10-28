# KGV_Fortschrittslog_kompakt

Diese Fassung verdichtet das ausführliche Fortschrittslog auf die fachlich tragenden Informationen:
- **was umgesetzt wurde**
- **welche Wirkung das hatte**
- **welcher Rest offen blieb**
- **welche Validierung wiederholt genutzt wurde**

Nicht mehr einzeln aufgeführt sind die vielen wiederholten Zwischenstände wie:
- wiederholte Git-/Repo-Prüfungen
- identische Dateilisten pro Mini-Block
- mehrfach gleiche Buildaufrufe
- schrittweise Diagnose-Iterationen, wenn das Ergebnis später sauber zusammengeführt werden konnte

## Validierungsstandard

Sofern nicht anders erwähnt, wurden die betroffenen Blöcke mit den jeweils relevanten Builds abgeschlossen, typischerweise über:
- `KGV.Core`
- `KGV.Maui`

## 2026-10-28 – Phase 10 E2E Wartungsvertrag

- Die vorbereiteten Phase-10-Änderungen für Retry mit tatsächlicher
  Vertragspartei, idempotenten Abschluss derselben Verlängerungsmitteilung und
  Abbruch-/Neustart-Historie grundlegender Änderungen geprüft und beibehalten.
- Der erneute E2E-Test ist 5/5 grün; die bestehenden Wartungsvertrags-
  Contracttests sind 10/10 grün; gezieltes ESLint blieb ohne Befund. Die
  Abbruchregression prüft ausdrücklich separate Vertragsdefinition und
  Folge-90xxx; die Nummer des abgebrochenen Versuchs bleibt erhalten.
- Aus dem unmittelbar vorausgegangenen CX-Lauf übernommen: 10 fokussierte
  .NET-Tests sowie Web-Build erfolgreich; MAUI-Build mit 48 fachfremden
  Fehlern, kein bekannter Fehler in geänderten Wartungsvertragsdateien.
- Die vier lokalen v2-PDF-/Field-Spec-SHAs stimmen mit den bestehenden
  Katalogwerten überein. Die neue Migration ist ausschließlich versioniert;
  kein Migration Push, kein Upload, keine Templateaktivierung und kein
  Deployment.
- `supabase db push --linked --dry-run` listete ausstehende Migrationen und
  wendete keine an. `supabase migration list --linked` scheiterte an der
  Datenbank-Passwortauthentifizierung; der Remote-Migrationsstand war daher
  nicht separat abrufbar.

## 2026-10-27 – MAUI-Biometrieangebot vor App-Wechsel

- Den Passwortloginablauf eng korrigiert: nach gültiger User-ID, geladenem
  UserContext und validiertem Mitgliedskontext wird das vorhandene
  Biometrieangebot vor dem Wechsel auf die App-Shell ausgeführt.
- Ungültiger Login oder ungültiger KGV-Zugang bietet keine Biometrie an.
  Speicherung bei Zustimmung, Ablehnungsentscheidung, SecureStorage,
  Scope-Trennung, Vereinswechsel-Löschung und späterer Biometrie-Login bleiben
  unverändert.
- Der Regressionstest prüft `GetUserContextAsync` → Mitgliedskontextprüfung →
  `OfferBiometricsAfterValidatedAccessAsync` → `SwitchToUserContextAsync` und
  kontrolliert die bestehenden Zustimmungs-/Ablehnungs- und Resume-Pfade.
- Verifikation: der direkte Node-Test
  `security/maui-valid-access.contract.test.mjs` sowie vier
  `BiometricSessionScopeTests` bestanden; Core-Build erfolgreich (bestehende
  ImageSharp-NU1902/NU1903-Warnungen). Das npm-Script
  `test:maui-valid-access` ist nicht registriert. MAUI-Build bleibt an 53
  vorhandenen Compilerdiagnosen (106 ausgegebene Fehlermeldungen) blockiert;
  `LoginPage.xaml.cs` ist fehlerfrei, bekannte `LineBreakMode`-Diagnosen in
  `WartungsvertragFortfuehrungPage.cs` bleiben unverändert.

## 2026-10-27 – NB-2B-B4 Wartungsvertragsnummern

- CX' lokale v2-Templateänderungen und der vorbereitete Generator-/Service-
  Umbau wurden beibehalten und fachlich vervollständigt. Ergänzt wurden die
  stabile Nummernquelle pro Wartungsvertrag, der Nummerntrigger bei
  `zur_signatur`, Nummernanzeige in PDF/Dateiname/Titel/Metadaten und die
  Nummer in Web-/MAUI-Vertragsansichten.
- Reguläre Verlängerungen halten dieselbe Wartungsvertragsdefinition und
  90xxx bei; Verlängerungsmitteilungen beziehen sich auf dieselbe Nummer,
  reservieren aber keine eigene.
- Die neue ausdrückliche Aktion für grundlegende Änderungen legt einen
  separaten inaktiven Vertrag und Prepared-Signaturvorgang an. Erst nach allen
  erforderlichen Signaturen und erfolgreicher Dokumentfinalisierung werden
  neue Zuordnung/Vertragsaktivierung wirksam und die alte Laufzeit nötigenfalls
  beendet. Vorgängerzuordnung, unique retry binding und idempotenter Abschluss
  sichern Herkunft und Wiederholung; es gibt keine Feldvergleichs-Heuristik.
- Eine Vertragsanbindungsprüfung wurde auf die tatsächliche Vertragspartei
  korrigiert. Test-Fakes wurden an die bestehenden Prepared-Signaturinterfaces
  angepasst. Wartungsvertrags-ID-Fallbacks in Fortführungs-, Mitglieds- und
  Zuweisungsansichten wurden durch die 90xxx bzw. neutrale Bezeichnungen
  ersetzt.
- Die PDF-Finalisierung prüft die Mitgliedssignatur gegen die Vertragspartei
  aus dem signierten Vorgang und kann damit auch den neuen Grundänderungs-
  vorgang ohne aktive Zuordnung abschließen.
- Der Dokumentworkflow-Validator unterscheidet Erstvertrag und
  Verlängerungsmitteilung anhand ihrer Vorgangsart; ein Regressionstest sichert
  beide Vorgänge ab.
- Schema- und Templatekatalogmigration sowie Wartungsvertrag-/Verlängerungs-
  v2-Artefakte sind vorbereitet. Katalogeinträge bleiben `draft`, `aktiv=false`;
  keine Migration wurde angewendet und kein Template hochgeladen/aktiviert.
- Verifikation: 46 gezielte Web-/Generator- und 11 fokussierte .NET-Tests
  bestanden (57 gesamt).
  Web-, Core- und Infrastructure-Build erfolgreich. MAUI-Build scheitert mit
  53 vorhandenen unterschiedlichen Compilerfehlern (106 Meldungen über zwei
  Target Frameworks), darunter zwei unveränderte `LineBreakMode`-Verwendungen
  in der Fortführungsseite; keine B4-spezifischen Compilerfehler. Vorhandene
  ImageSharp- und Web-Chunk-Warnungen bleiben.
- Keine Produktionsdatenänderung, kein Supabase-Migrationslauf, kein
  Template-Upload/-Aktivierung und kein Deployment.

## 2026-10-21 – Wartungsvertrags-Dokumentworkflow technisch vorbereitet

- Die vorhandene Prepared-Contract-Architektur mit `kommunikationsvorgang`,
  Revisionen, Signaturstart, letzter Prüfung, Finaldokument-Provenienz und den
  vorhandenen Web-/MAUI-Signaturoberflächen wurde als Wiederverwendungspfad
  ausgewählt. Eine Wartungsvertragsvorlage und konkrete Unterzeichnerrollen
  existieren noch nicht; deshalb wurde keine tatsächliche PDF-Erzeugung oder
  Signaturfreigabe implementiert.
- Eine neue Migration bindet Kommunikationsvorgänge und finale Dokumente
  eindeutig an `wartungsvertrag_zuordnungen`; eindeutige Indizes und
  DB-Validierungstrigger sichern die Zuordnung und das normalisierte
  Hauptmitglied. Ein Insert-Trigger und eine Admin/Vorstand-RPC erstellen
  idempotent einen Status `dokument_ausstehend` und ermöglichen die explizite
  Nachholung bestehender Workfloweinträge. Die Migration ist vorbereitet,
  aber nicht angewendet.
- Die Infrastructure hängt Dokumentstatus/-fehler an die vorhandenen W3-
  Einzelresultate an. Web und MAUI rufen den gemeinsamen Workflow nach
  erfolgreichen Einzelzuordnungen auf; Batch-Teilerfolge erzeugen
  Workfloweinträge nur für tatsächlich gespeicherte Zuordnungen. Fehler beim
  Workflow werden protokolliert und nicht als Zuordnungsfehler ausgegeben.
- Web- und MAUI-Mitgliedsansichten bieten eine manuelle Status-/Vorbereitungs-
  aktion für bereits gespeicherte Zuordnungen ohne Massenerzeugung. Die
  Oberfläche weist darauf hin, dass die Vorlage fehlt und das Dokument daher
  noch nicht erstellt werden kann.
- Noch zu entscheiden sind: freigegebene Vorlage und deren konkrete
  Platzhalter mit Datenquellen/Snapshotfeldern; Mitglieds- und
  Vereinsunterzeichnerrollen einschließlich Reihenfolge und Vertretung;
  konkrete Erweiterung des Prepared-Contract-Generators und der vorhandenen
  Signaturplanung für Wartungsverträge.
- Keine Supabase-Verknüpfung, kein `db push`, keine Demo-/VDB-Datenänderung,
  kein Commit und kein Push.
- Validierung: 16 gezielte Wartungsvertrags-, Batch-, Zeitmodell-,
  Arbeitsstunden- und Export-Contracts bestanden. Infrastructure-Build und
  Web-Produktionsbuild erfolgreich. MAUI-Build scheitert nur in den
  blockfremden Dateien `MemberClubDocumentsSection.cs`,
  `VereinsunterlagenPage.cs` und `VereinsfunktionenPage.cs`; die geänderten
  Wartungsvertrags-Pages verursachen keine Compilerfehler. `KGV.Tests` wird
  durch den bekannten fehlenden Parameter `vereinsunterlagenService` in
  `PreparedMembershipApplicationSigningServiceTests.cs` blockiert. Der
  Web-Typecheck zeigt nur bereits bekannte Fehler in Vertrags-, Dokument-
  und Kommunikationsdateien außerhalb dieses Blocks. ImageSharp-Warnungen
  blieben unberührt.

## 2026-10-21 – Wartungsverträge W6: Abschlussprüfung

- W1–W5-Contracts für Zeitstatus, inklusive Randtage, Sortierung,
  Hauptmitgliedsbezug, Batch-Teilfehler, Web-/MAUI-Ansichten,
  Arbeitsstundenwirkung und Export gemeinsam ausgeführt: 24 Tests bestanden.
- Repo-weite Suche über Web, MAUI, Core, Infrastructure und Export bestätigt:
  Pflichtstunden, Befreiung und Gutschrift stammen aus der zentralen
  DB-Funktion/View; Export formatiert zentrale Werte. React-Komponenten und
  MAUI-Pages bauen keine eigene Zeitklassifikation oder Vertragsberechnung.
  Web-Service und Core enthalten die jeweiligen sprachspezifischen
  Klassifizierer, abgesichert durch die gemeinsamen Grenzfall-Contracts.
- Im Git-Vergleich der W1–W5-Commits mit dem W1-Ausgangsstand ist keine
  Supabase-Migration hinzugekommen oder geändert worden. W6 ändert keine
  Migration und keine Daten; daher ist für diesen Block kein DB-Rollout
  erforderlich. Kein Remote-Projekt wurde verbunden oder abgefragt.
- Wartungsvertrags-Abschlussdokument und knappe DEMO-Prüfliste erstellt;
  keine ausgeführte DEMO-/VDB-Abnahme behauptet. W6 ist aus `ToDo.md` entfernt.
  Der separate Dokumentworkflow je Wartungsvertragszuordnung bleibt als
  eigenständiger offener Vorgang bestehen.

## 2026-10-21 – Wartungsverträge W5: Arbeitsstunden-, Befreiungs- und Exportregressionen

- Die Repo-Prüfung hat bestätigt, dass
  `fn_berechne_pflichtstunden_status` die einzige fachliche Berechnung ist.
  `v_pflichtstunden_uebersicht` liefert Soll, geleistete/offene Stunden,
  Befreiung, Vertragsindikator, Wartungsvertragsgutschrift und Fehlbetrag an
  die Clients und die Export-RPC.
- W5 ergänzt Regressionstests gegen die vorhandene SQL-Funktion:
  saisonübergreifende Zuordnungsüberschneidung mit inklusiven Jahresgrenzen,
  vollständig außerhalb liegende Historie/Zukunft, anteilige Saisonintervalle,
  Befreiung, Vertragsgutschrift, Mehrfachzuordnung und Soll-Obergrenze.
- Festgehalten wurde die bestehende Saisonregel ohne Neuinterpretation:
  jede Überschneidung mit 01.01.–31.12. zählt als ganze Saisonwirkung, auch
  wenn eine Zuordnung erst später im selben Jahr beginnt oder innerhalb des
  Jahres endet. Das SQL verwendet keinen Tagesstichtag. Befreiung überstimmt
  Gutschrift; Credits werden je Vertrags-ID addiert und auf das Soll begrenzt.
- Daraus folgt, dass ein aus heutiger Sicht erst künftig beginnender Vertrag
  im selben Saisonjahr laut bestehender DB-Regel schon im Jahresergebnis wirkt.
  Dieser Unterschied zum taggenauen Zeitstatus der Mitgliederlisten ist
  regressionsgetestet und für die fachliche DEMO-Abnahme offengelegt, nicht
  eigenmächtig verändert.
- Web-/MAUI-Arbeitsstundenanzeigen und der PDF-/RPC-Export lesen weiterhin
  zentrale View-Felder. Kein eigener Pflichtstunden- oder WV-Rechenweg wurde
  ergänzt. Dokumentation fortgeschrieben und W5 aus `ToDo.md` entfernt.
- Validierung: 13 zielgerichtete W5-, W4- und bestehende Export-/Work-Hours-
  Contracts bestanden. Infrastructure-/Web-Build waren bereits im W4-Block
  erfolgreich. W5 wiederholte keine unnötigen Komplettbuilds; die bekannten
  blockfremden MAUI-, .NET-Test- und Web-Typecheck-Fehler bleiben unverändert.

## 2026-10-21 – Wartungsverträge W4: Mitgliedsbezogene Zeitlisten

- Web und MAUI zeigen Mitgliedszuordnungen in den Bereichen Aktuell,
  Zukünftig und Historisch mit Vertragsname, inklusiver Gültigkeit, Status und
  Vertrags-Arbeitsstundenwirkung.
- Infrastructure klassifiziert alle Mitgliedszuordnungen über
  `WartungsvertragZeitmodell` und verwendet die gleiche Hauptmitgliedsauflösung
  beim Lesen, bei Kandidatenlisten und beim Speichern. Web verwendet dafür den
  bestehenden Mitgliedsauflösungs-Service. UI-seitige Arbeitsstunden-
  oder Zeitberechnungen wurden nicht ergänzt.
- Beide Clients sortieren Aktuelles nach Vertragstitel, Zukünftiges nach
  nächstem Beginn und Historisches nach zuletzt beendetem Zeitraum.
- Teilerfolge aktualisieren die Mitgliedslisten; die bestehenden Web- und
  MAUI-W3-Ergebnismodelle einschließlich Einzelergebnissen bleiben erhalten.
- Tests: neun Wartungsvertrags-Contracts bestanden. Infrastructure-Build und
  Web-Produktionsbuild erfolgreich. MAUI-Build blockiert durch blockfremde
  fehlende MAUI-Usings in `MemberClubDocumentsSection.cs`,
  `VereinsunterlagenPage.cs` und `VereinsfunktionenPage.cs`; es gab keine
  W4-Fehler in `MemberWartungsvertraegePage.cs`. Der fokussierte
  `KGV.Tests`-Lauf scheitert an bereits bestehenden Fehlern in
  `PreparedMembershipApplicationSigningServiceTests.cs`. Der Web-Typecheck
  scheitert an bereits bestehenden Typfehlern in den Vertrags-, Dokument- und
  Kommunikationsbereichen außerhalb W4. ImageSharp-Warnungen bleiben ignoriert.

## 2026-10-17 – MAUI: Android-PDF-Buildfehler

- Die drei fehlerhaften `DocumentInfo.Titel`-Verwendungen auf `DocumentInfo.Title` korrigiert; keine zweite Modelleigenschaft ergänzt.
- Die Variablen des zentralen PDF-Exportzweigs (`centralFileName`/`centralFilePath`) umbenannt, ohne Download-, Cache- oder Rückgabeverhalten zu ändern.
- Validierung: `dotnet build .\KGV.Maui\KGV.Maui.csproj -c Release -f net10.0-android` erfolgreich mit 0 Fehlern und 26 bestehenden Warnungen.

## 2026-10-11 – Web: Ablesehistorie im Mitgliedsgarten

- Den Web-Fehler behoben, der aus dem Mitgliedsgarten in den globalen Ablesemodus sprang. Mitglied, Parzelle und konkrete Belegung bleiben im aktiven Workspace.
- Die Mitgliedsansicht ruft den vorhandenen G6-Reading-Service auf. Dieser lädt die Zähler der gewählten Parzelle und deren Ablesungen über die bereits vorhandenen Meter-/Reading-Repositories.
- Die Historie wird für aktive und historische Pachten inklusiv auf `von_datum` bis `bis_datum` begrenzt. Dadurch werden Ablesungen außerhalb der konkreten Belegung ausgeschlossen; bei mehreren Zählern bleiben alle zugehörigen tatsächlichen Datensätze sichtbar.
- Eine gespeicherte Ablesung exakt am Pachtbeginn wird angezeigt, falls vorhanden. Andernfalls ist der früheste sichtbare Datensatz die erste tatsächliche Ablesung ab Pachtbeginn; ein synthetischer Startdatensatz wird nicht erzeugt. MAUI zeigt weiterhin seine bestehende, nach Parzelle/Medium geladene und absteigend sortierte Historie ohne Belegungszeitfilter.
- Validierung: sieben gezielte Regressionstests, `npx tsc --noEmit`, Web-Produktionsbuild und `git diff --check` erfolgreich. ESLint auf den geänderten Dateien unter Ausschluss der bereits im Bestand verletzten Regel `react-hooks/set-state-in-effect` ohne Fehler (19 Warnungen); die neuen History-/Feature-Dateien bestehen ESLint ohne Override. Der reguläre Lauf bestätigt bestehende Verstöße in nicht angepassten Effect-Callbacks der Workspace-Dateien.

## 2026-10-05 – Vereinszeit Europe/Berlin

- Zentrale DB-Funktion `public.kgv_local_now()` ergänzt; sie liefert `timestamp without time zone` aus `now() AT TIME ZONE 'Europe/Berlin'`.
- Sichtbarkeitsviews, relevante RLS-Policies und die Anmeldungsvalidierung verwenden die zentrale Vereinszeit.
- Fachliche Web-, MAUI-,und Infrastructure-Defaults wurden vereinheitlicht; UTC-Felder und bestehende Daten blieben unverändert.
- Core, Infrastructure, MAUI und Web wurden erfolgreich validiert. Der WPF-Build bleibt wegen bereits bestehender Fehler außerhalb dieses Blocks offen.

## Aktueller Gesamtstand (Stand 2026-04-08)

- **WPF und MAUI** sind in den großen Fachbereichen deutlich näher zusammengezogen.
- **Arbeitsstunden** inkl. Prüf-/Freigabeprozess, Review-Lock und mobilem Nachzug sind produktiv ausgebaut.
- **Termine, Bekanntmachungen und Arbeitseinsätze** laufen über echte Verwaltungs-/Editorpfade statt Platzhalter.
- **Dokumente** sind produktiv an den Google-Drive-Unterbau angebunden; normale Nutzer bleiben view-only.
- **Demo-/Play-Store-Konten** können im Adminpfad jetzt auf echte Demo-Daten begrenzt werden, ohne produktive Vereinsdaten freizugeben.
- **Mitglieder-/Nebenmitglied-Flows** wurden stark ausgebaut: Neuanlage, Nebenmitglied anlegen/bearbeiten, Mitgliedschaft beenden mit Folgeentscheid.
- **Wartungsverträge** sind auch für Nebenmitglieder geöffnet.
- **Rechte-/Rollenmodell** wurde auf `app_user.role` als führende Quelle gezogen; benutzerspezifische Fachrechte inkl. Ablese-Freigaben sind aufgebaut.
- **Android/MAUI** wurde in mehreren Blöcken für Release, Logging, Timeout/Resume, Foto-Uploads und Startstabilität gehärtet.

## 2026-04-16 – Mitgliedsantrag: Ortfelder, Signatur- und Vertreter-Mapping verschärft; Layoutanpassungen

- Ziel: Vermeiden falscher Füllungen in Orts- und Signaturfeldern; Vertreterfelder nur bei echten gesetzlichen Vertretern füllen. Werte etwas tiefer in den Feldzellen platzieren, Checkboxen zentrieren.
- Änderungen (minimal-invasiv):
  - `KGV.Core/Models/MitgliedsantragTemplateData.cs`: `DokumentOrt` und `UnterschriftOrt` ergänzt.
  - `KGV.Core/Utilities/MitgliedsantragDokumentFactory.cs`: `dokument_ort` verwendet nun `DokumentOrt`; `unterschrift_ort` nur gesetzt, wenn vorhanden. Signaturfelder (`unterschrift_*`, `datenschutz_unterschrift_*`) werden nicht automatisch befüllt. Vertreterfelder bleiben leer, wenn kein Vertreter-Snapshot vorhanden ist. Fallback durch `TryFallbackNameForRect` wurde beschränkt, keine Fallbacks für Ort/Signatur/Vertreter.
  - Layout: Einzeilige Werte untenbündig, mehrzeilige Werte beginnen unterhalb des Labels; Checkbox-Häkchen grafisch zentriert.
- Test: `tools/MitgliedsantragTest` ausgeführt, Minor/Adult Test-PDFs neu erzeugt.
- Build: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

## 2026-04-18 – Mitgliedsantrag: Signaturflow mobil fertiggestellt

- Ziel: Den bestehenden mobilen Signaturfluss für den Mitgliedsantrag produktiv abschließen. Nutzung des vorhandenen `VertragsSignaturPage` und des gemeinsamen `SignedVertragsdokumentPdfBuilder` sowie des Servicepfads `CreateSignedMitgliedsantragDokumentAsync`.
- Änderungen:
  - `KGV.Maui/Pages/VertragsSignaturPage.cs`: UI-Buttons geordnet, finaler Save-Button `Speichern` am Ende platziert und `Leeren`/`Abbrechen` beibehalten. Anzeige des Signaturtitels oben erfolgt per Übergabe-Parameter.
  - `KGV.Maui/Pages/MemberDetailPage.cs`: Flow nutzt bereits die sequentielle Signaturabfolge (Antragsteller -> ggf. gesetzlicher Vertreter) und ruft `CreateSignedMitgliedsantragDokumentAsync` zur finalen Speicherung auf.
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
- **ReleaseManager / AWR** wurden für reale WPF-/Android-Releases deutlich ausgebaut.

## Chronologischer Kurzverlauf

## 2026-04-13
- Den begonnenen Pachtvertrag auf dem echten Stand von `main` produktiv vom alten PDF-AcroForm-/Zusatzseiten-Pfad auf den neuen HTML-Template-Ansatz umgestellt, ohne Mitgliedsantrag, Rechtearchitektur, Verwaltungslogik oder Navigation breiter umzubauen.
- Der Istzustand wurde zuerst gegen den realen Working Tree geprüft; gelesen und weiterverwendet wurden nur die direkt betroffenen Dateien des Pachtvertragsblocks in `KGV.Core`, `KGV.Infrastructure` und `KGV.Maui`.
- Die lokal bereits vorhandenen Template-Dateien `PachtvertragTemplate.html`, `PachtvertragTemplateData`, `PachtvertragTemplateRenderer` und `PachtvertragTemplateFactory` wurden nicht neu erfunden, sondern in den bestehenden Produktivpfad eingebunden; `KGV.Core.csproj` bindet das HTML-Template jetzt zusätzlich als Embedded Resource ein.
- `PachtvertragDokumentFactory` lädt nun die HTML-Vorlage, baut den Template-Kontext aus den bestehenden Produktivdaten `vereinskonfiguration`-Snapshot, Hauptpächter, optional zweitem Pächter, Parzelle, Saison und Vertragsbeginn, rendert das HTML vollständig aus und erzeugt daraus das PDF über den neuen `PachtvertragHtmlPdfRenderer`.
- Der neue Renderer wertet die gerenderte HTML-Struktur produktiv aus und bildet daraus die Vertragsseiten, den Einzel-/Doppelpächtermodus (`single` / `dual`), den Bankblock, den Kostenblock und die sichtbaren Signaturbereiche nach; bei einem Pächter bleiben zweite Blöcke und zweite Signaturen unsichtbar.
- Der Kostenblock des Vertrags enthält jetzt nur noch `Pacht für laufendes Jahr`; berechnet wird wie gefordert 1/12 des Jahresbetrags je angefangenem Restmonat des laufenden Jahres. `Mitgliedsbeitrag`, `Gesamt` und der alte AcroForm-Hauptpfad sind im führenden Erzeugungsweg entfernt.
- Im Servicepfad wurde die veraltete Pflichtabhängigkeit des Pachtvertrags von `saison.mitgliedsbeitrag` entfernt; Vorschau und finales Dokument laufen weiter über denselben bestehenden Request-/Signatur-/Speicherpfad.
- Der bestehende MAUI-Aufrufer blieb fachlich erhalten und wurde nur textlich auf den Template-Pfad geglättet; `Vorschau -> Unterschrift -> finales Speichern` bleibt unverändert bestehen.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

## 2026-04-20
- Export: Export-Schema und PDF-Konsolidierung abgeschlossen.
  - Repo-Check auf `main` durchgeführt; nur die Export-bezogenen Dateien wurden betrachtet.
  - Export-Definitions-/Column-/Filter-Modelle an reales Supabase-Schema (`app_export_*`) angepasst und im Servicepfad integriert.
  - `ExportPdfBuilder` (KGV.Core) konsolidiert: kompakter Header, Adresse/Kontakt-Gruppierung, verbesserte Spaltengewichtung für A4-Landscape, Trunkierung und Zeilenhöhen-Berechnung stabilisiert.
  - MAUI-Exportseite (`KGV.Maui/Pages/ExportPage.cs`) und `ExportViewModel` bereinigt: Picker zeigt nun sinnvolle Titel/Fallbacks, Buttons werden korrekt aktiviert, CSV/PDF-Exporter testweise angestoßen.
  - Smoke-Test (lokal): Picker lädt Einträge; `mitgliederliste` und `rfid_status` auswählbar; CSV/PDF-Export generieren Cache-Dateipfade ohne sofortigen Fehler.
  - Validierung: `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- 2026-04-14: Mitgliedsantrag produktiv auf PDF-Formularvorlage umgestellt (`Mitgliedsantrag_Vorlage_Formularfelder.pdf`). Alte HTML- und DOCX-Erzeugungspfade sowie die lokale `soffice`-Konversion wurden aus dem produktiven Erzeugungspfad entfernt. `KGV.Core` bindet die neue PDF-Vorlage als EmbeddedResource ein und füllt die AcroForm-Felder programmgesteuert im `MitgliedsantragDokumentFactory`. Build-Validierung (Core) erfolgreich.

- Den kleinen Pachtvertrag-Korrekturblock auf dem echten Stand von `main` minimal abgeschlossen, ohne neuen Formular- oder Verwaltungsblock zu starten.
- Ursache war der bestehende Produktivpfad in `PachtvertragDokumentFactory`: dort wurde der Mitgliedsbeitrag weiterhin separat in die PDF-Vorlage geschrieben und zusätzlich in den Gesamtbetrag des Pachtvertrags eingerechnet, obwohl dieser fachlich bereits über den Mitgliedsantrag geführt wird.
- Der bestehende Pachtvertragspfad wurde deshalb minimal geglättet: `member_fee_display` bleibt leer, der im Vertrag ausgewiesene Gesamtbetrag entspricht nur noch dem Pachtzins, und die unnötige harte Abhängigkeit des Pachtvertrags von `saison.mitgliedsbeitrag` entfällt.
- Wirkung: Der Pachtvertrag zeigt den separaten Mitgliedsbeitrag nicht mehr produktiv im PDF-Pfad an; der Block bleibt auf die bestehende Pachtvertragslogik begrenzt und baut keine Nebenarchitektur auf.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bereits begonnenen Mitgliedsantrag-HTML-Template-Block auf dem echten lokalen Stand von `main` als Abschlusslauf sauber geschlossen, ohne Pachtvertrag, Navigation, Rechtearchitektur oder neue Verwaltungslogik anzufassen.
- Der Working Tree wurde vorab eingeordnet; behandelt wurden nur die direkt betroffenen Mitgliedsantrag-/Template-/PDF-/Service-Dateien samt den zugehörigen Snapshot-/Request-Modellen und Logdateien.
- Die lokal bereits vorhandenen Dateien `MitgliedsantragTemplate.html`, `MitgliedsantragTemplateData`, `MitgliedsantragTemplateRenderer`, `MitgliedsantragDokumentFactory`, `MitgliedsantragHtmlPdfRenderer`, `MitgliedsantragDokumentRequest`, `MitgliedsantragBankverbindungSnapshot` und `SupabaseService` wurden bewusst weiterverwendet statt neu erfunden.
- Als echter Restfehler im begonnenen HTML->PDF-Pfad blieb nur noch der Renderer übrig: ein unerreichbarer `XCData`-Fall verursachte Compilerprobleme, und die Abschnittsprüfung der gerenderten HTML-Struktur war vor den festen Section-Indizes noch nicht strikt genug.
- Dieser Rest wurde minimal behoben; der produktive Pfad bleibt: Template laden, Template-Daten rendern, HTML prüfen, daraus PDF für Vorschau erzeugen und denselben Snapshot-/PDF-Pfad für die signierte Endfassung weiterverwenden.
- Wirkung: Der Mitgliedsantrag läuft jetzt technisch sauber über den begonnenen HTML-Template-Pfad mit Daten aus Mitglied, Vertreter-Snapshot, aktiver `vereinskonfiguration` und Saisonwerten inkl. `aufnahmegebuehr`; technische Altinhalte werden dabei nicht neu in den Antrag eingeführt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bereits umgesetzten Verwaltungsblock `Vereinskonfiguration` / `Saisonverwaltung` auf dem echten Stand von `main` in einem reinen Abschlusslauf technisch sauber geschlossen, ohne neuen Fachumfang zu starten.
- Der Working Tree wurde vorab eingeordnet; blockfremde lokale Änderungen aus dem laufenden Mitgliedsantrag-/Template-/Vertreter-Umfeld blieben bewusst unberührt.
- Die geforderte Validierung wurde auf den echten Verwaltungsdateien durchgeführt: `dotnet build KGV.Wpf/KGV.Wpf.csproj` lief direkt erfolgreich.
- Im anschließenden MAUI-Build blieb nur ein echter technischer Restfehler des neuen Verwaltungsblocks offen: In `VereinskonfigurationPage` fehlte noch der Namespace für `Keyboard`.
- Dieser Rest wurde minimal durch `using Microsoft.Maui;` geschlossen; danach baute auch `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.
- Wirkung: Der Verwaltungsblock ist jetzt für WPF und MAUI technisch validiert, ohne Dokumentpfade, Mitgliedsantrag-/Template-Logik, Pachtvertragspfade oder Rechtearchitektur weiter anzufassen.
- Offener Rest dieses Abschlusslaufs blieb nur noch der Git-Abschluss auf die direkt relevanten Verwaltungsdateien.

- Den Workspace auf dem echten Stand von `main` vor dem nächsten Verwaltungsblock bewusst nur technisch stabilisiert, ohne bereits neue Verwaltungs-/Bearbeiten-Logik für Saison oder Vereinskonfiguration zu beginnen.
- Der aktuelle Working-Tree wurde eingeordnet: der laufende Mitgliedsantrag-/Template-/Vertreter-Block war bereits zusammenhängend vorhanden, inklusive neuem HTML-Template als eingebetteter Ressource, Template-Datenmodell und Renderer.
- Zur Plausibilisierung wurde der Dateitext von `MitgliedsantragTemplateData` gegen den Symbol-/Compilerzustand geprüft; dabei gab es keinen Widerspruch mehr zwischen Workspace-Datei und Buildsicht.
- Weil WPF und MAUI mit diesem Zwischenstand bereits sauber buildeten, wurden an den begonnenen Mitgliedsantrag-/Template-/Snapshot-/Signaturdateien keine neuen fachlichen Umbauten mehr vorgenommen.
- Als einziger echter Vorgriff auf den noch ausstehenden Admin-Bearbeiten-Block wurde die vorzeitig erweiterte Modellfläche in `VereinskonfigurationRecord` wieder auf den bestehenden produktiven Bank-/Aktiv-Unterbau zurückgenommen.
- Wirkung: der Workspace ist vor dem Verwaltungsblock wieder konsistent, buildfähig und frei von halbfertig gestarteter Admin-Modelllogik; der bestehende Mitgliedsantrag-Unterbau bleibt unverändert als nächster fachlicher Ausgangspunkt erhalten.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

## 2026-04-11
- Den bestehenden Mitgliedsantrag auf dem echten Stand von `main` auf das bereits angelegte HTML-Template gezogen, ohne neuen Fachblock zu starten.
- Dafür wurden die vorhandenen Dateien `KGV.Core/Templates/MitgliedsantragTemplate.html`, `KGV.Core/Models/MitgliedsantragTemplateData.cs` und `KGV.Core/Utilities/MitgliedsantragTemplateRenderer.cs` produktiv in den bestehenden Dokumentpfad eingebunden statt nur ungenutzt im Projekt zu liegen.
- `KGV.Core` bindet die HTML-Vorlage jetzt als eingebettete Ressource ein, damit der gemeinsame Core-Pfad die Vorlage stabil laden kann.
- Im gemeinsamen Mitgliedsantrag-Factorypfad werden jetzt die Template-Daten aus Mitglied, Vertreter-Snapshot, Bankdaten-Snapshot und Vereinsbranding aufgebaut, die HTML-Vorlage geladen, über den vorhandenen Renderer mit echten Werten befüllt und auf vollständige Platzhalterauflösung geprüft.
- Der Mitgliedsantrag läuft damit nicht mehr nur über den bisherigen allgemeinen Vereinsdokument-Builder, sondern über einen templategetriebenen Produktivpfad; der bestehende Ablauf `Bearbeiten -> Vorschau -> Unterschrift -> finales Speichern` bleibt dabei unverändert erhalten.
- Rechte, Routing, Dialoge und der Signatur-/Finalspeicherpfad wurden in diesem Block bewusst nicht neu gebaut und nicht erweitert.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Die bestehende HTML-Vorlage des Mitgliedsantrags auf dem echten Stand von `main` minimal geglättet, ohne neuen Fachblock zu starten.
- Im Template `KGV.Core/Templates/Mitgliedsantrag.html` wurden die bisherigen kombinierten Kontaktfelder `Telefon / Handy` sowohl für Antragsteller/in als auch für gesetzliche Vertretung in getrennte Felder `Telefon` und `Mobil` aufgeteilt.
- Zusätzlich wurde die obere Unterschriftszeile im Minderjährigen-Fall rein layoutseitig so nachgeschärft, dass die Unterschriftslinien von Mitglied und gesetzlicher Vertretung auf gleicher Höhe stehen; nach dem ersten Lauf wurde die Beschriftung dafür noch auf feste statt nur minimale Höhe gezogen, damit das zweizeilige Vertreter-Label die Linie nicht mehr nach oben verschiebt.
- Es wurden bewusst keine Änderungen an Servicepfaden, Snapshot-Logik, Preview-/Signaturablauf oder Routing vorgenommen.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen gemeinsamen Restfehler im Ladepfad `vereinskonfiguration` auf dem echten Stand von `main` minimal korrigiert, ohne neuen Fachblock zu starten.
- Die reale Ursache des Fehlers `GetAktiveVereinskonfigurationAsync failed` lag nicht in fehlenden SQL-Daten, sondern im bisher verwendeten Bool-Predicate `.Where(x => x.Aktiv)`, das im verwendeten PostgREST-/Supabase-Client nicht parsebar war.
- Der gemeinsame Produktivpfad `GetAktiveVereinskonfigurationAsync()` nutzt deshalb jetzt statt des unären Bool-Predicates einen expliziten Vergleich `.Where(x => x.Aktiv == true)`.
- Sortierung und Auswahl des neuesten aktiven Datensatzes bleiben unverändert; die bestehende fachliche Fehlermeldung bei wirklich fehlender Konfiguration wurde bewusst nicht geändert.
- Wirkung: der Mitgliedsantrag-/Pachtvertrag-Unterbau kann die aktive `vereinskonfiguration` jetzt wieder über den bestehenden gemeinsamen Servicepfad laden, statt schon am Filterparser zu scheitern.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bestehenden MAUI-Pachtvertrag auf dem echten Stand von `main` auf denselben Vertreter-/Snapshot-/Vereinskonfigurations-Standard wie den Mitgliedsantrag gezogen, ohne neuen Fachblock zu starten.
- Dafür wurde ein kleiner gemeinsamer `PachtvertragDokumentRequest` ergänzt; Vertreter- und Vereinsbankdaten laufen jetzt als Snapshot durch den bestehenden Preview-/Signatur-/Save-Pfad statt über verdeckte Nachlade- oder Altquellen.
- Im gemeinsamen Servicepfad wurden request-basierte Pachtvertrag-Methoden ergänzt, die Minderjährigkeit zum Vertragsbeginn prüfen, einen aktiven gesetzlichen Vertreter vorbelegen, ein bestehendes Mitglied oder manuelle Vertretererfassung unterstützen und die Vereinsbankdaten produktiv aus `vereinskonfiguration` laden.
- Fehlt eine aktive `vereinskonfiguration` oder fehlt eines der Pflichtfelder `kontoinhaber`, `bankname`, `iban` oder `bic`, bricht der Pachtvertrag jetzt mit klarer fachlicher Meldung ab und läuft nicht still weiter.
- Für Minderjährige wird der gesetzliche Vertreter im finalen Erfolgsfall verarbeitet: eine manuell erfasste Person wird erst dann als Nebenmitglied angelegt, und erst dann wird auch die gesetzliche Vertreter-Verknüpfung gespeichert.
- Die Vorschau und das finale Dokument verwenden dabei denselben Snapshot-Stand für Vertreter und Bankdaten; dadurch bleibt der historische Dokumentinhalt stabil.
- Der bestehende gemeinsame Pachtvertrag-Builder bleibt führend und nutzt weiter die offizielle Vorlage; zusätzlich wird nur minimal eine kompakte Ergänzungsseite mit Bankdaten-Snapshot und – bei Minderjährigen – Vertreterblock angehängt.
- Im MAUI-Flow wurde kein neuer Großpfad gebaut: `PachtvertragFlowHelper`, `PachtvertragPreviewPage` und `VertragsSignaturPage` bleiben führend, ergänzt um eine kleine `PachtvertragDialogPage` für Vorbelegung/Vertreterwahl.
- Beide bestehenden mobilen Einstiege aus `MemberParzellenDetailPage` und `MemberGardenAssignPage` laufen weiter über denselben gemeinsamen erweiterten Pachtvertragspfad.
- Für Minderjährige ist im finalen Signaturpfad jetzt zusätzlich die digitale Unterschrift des gesetzlichen Vertreters verpflichtend; ohne vollständige notwendige Signaturen erfolgt keine finale Speicherung.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den offenen Mitgliedsantrag-Bankdatenrest auf dem echten Stand von `main` minimal-invasiv produktiv geschlossen, ohne neuen Fachblock zu starten.
- Dafür wurde ein kleines gemeinsames Modell `vereinskonfiguration` ergänzt und im bestehenden Servicepfad eine produktive Lade-Methode für den aktiven Datensatz aufgebaut.
- Der Mitgliedsantrag lädt die Vereinsbankdaten jetzt nicht mehr aus Annahmen oder Altquellen, sondern produktiv aus `vereinskonfiguration`.
- Die Pflichtfelder `kontoinhaber`, `bankname`, `iban` und `bic` werden im gemeinsamen Resolve-Pfad validiert; fehlt eine aktive Konfiguration oder fehlt eines der Pflichtfelder, bricht der Flow mit klarer fachlicher Meldung ab und läuft nicht still weiter.
- Die geladenen Bankdaten werden einmalig als `MitgliedsantragBankverbindungSnapshot` im bestehenden `MitgliedsantragDokumentRequest` festgehalten und danach für Vorschau und finales signiertes Dokument identisch weiterverwendet.
- Dadurch bleibt der bestehende Ablauf `Bearbeiten -> Vorschau -> Unterschrift -> finales Speichern` unverändert, verwendet aber jetzt stabil denselben Bankdatenstand in Preview und Enddokument.
- Im gemeinsamen PDF-Pfad `MitgliedsantragDokumentFactory` wird zusätzlich ein kompakter Bankdatenblock mit Kontoinhaber, Bankname, IBAN und BIC ausgegeben.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den Shared-/Infrastructure-Unterbau für den nächsten Vertreter-/Dokumentblock auf dem echten Stand von `main` minimal-invasiv ergänzt, ohne in diesem Block bereits UI oder Dokumentlayouts umzubauen.
- Im Saison-Unterbau wurde `mitgliedsbeitrag_nebenmitglied` im Modell und im bestehenden Lade-/Speicherpfad ergänzt, damit der neue DB-Wert produktiv bis in `SaisonRecord`, Saison-Normalisierung und `SaveSaisonAsync(...)` durchläuft.
- Für `mitglied_gesetzlicher_vertreter` wurden neue gemeinsame PostgREST-Modelle angelegt, passend zur produktiven SQL-Struktur mit Minderjährigen-Mitglied, Vertreter-Mitglied, Gültigkeitszeitraum, Bemerkung sowie Meta-Zeitfeldern.
- Im gemeinsamen Servicepfad wurden minimale Produktivmethoden ergänzt, um den aktuell aktiven gesetzlichen Vertreter eines Mitglieds zu laden, eine aktive Vertreter-Verknüpfung zu setzen/zu ändern und eine vorbereitete fachliche Auflösung für spätere Dokumentflows zu liefern.
- Die Vertreter-Beziehung bleibt dabei fachlich sauber getrennt von Haupt-/Nebenmitglied. Beim Wechsel des aktiven Vertreters wird die bisher offene Verknüpfung geschlossen und eine neue aktive Verknüpfung angelegt.
- Zusätzlich wurde ein kleiner gemeinsamer `GesetzlicherVertreterResolver` ergänzt, der Minderjährigkeit bestimmt und Vorbelegungsdaten des aktiven Vertreters direkt aus dem bestehenden `mitglied`-Datensatz ableitet; dadurch kann der spätere Mitgliedsantrag-/Pachtvertrag-Block darauf aufsetzen, ohne jetzt schon die Dokumenterzeugung anzufassen.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den weiterhin offenen Android-PdfSharpCore-Restfehler auf dem echten Stand von `main` im gemeinsamen Font-Initialisierungspfad geschlossen, ohne neuen Fachblock zu starten.
- Die Restanalyse zeigte, dass der vorherige Fix zwar vor `new XFont(...)` in den gemeinsamen Buildern initialisierte, aber den gemeinsamen Resolver noch nicht garantiert vor dem echten allerersten PdfSharp-Fontzugriff setzte.
- Zusätzlich lag in `PdfSharpFontResolverInitializer` noch ein zu später Zugriff über `GlobalFontSettings.FontResolver ??= ...`; dadurch wurde der Resolver vor dem Setzen zunächst lesend berührt, was auf Android den problematischen Erstpfad nicht sicher ausschloss.
- Der gemeinsame Fix erzwingt die Resolver-Initialisierung jetzt per `ModuleInitializer` bereits beim Laden des `KGV.Core`-Moduls. Gleichzeitig wird der Resolver nicht mehr per `??=` geprüft, sondern direkt einmalig gesetzt.
- Die Android-Schriftquellen über `/system/fonts/Roboto-*` bzw. Fallbacks bleiben unverändert; der fachliche Ablauf `Bearbeiten -> Vorschau -> Unterschrift -> finales Speichern` wurde nicht verändert.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen gemeinsamen Android-/PdfSharpCore-Fontfix auf dem echten Stand von `main` umgesetzt, ohne neuen Fachblock zu starten.
- Die Ist-Analyse zeigte, dass der bereits umgestellte mobile Mitgliedsantrag-Preview nicht an Routing, Beitragslogik oder Rechten scheiterte, sondern schon beim PDF-Aufbau mit `TypeInitializationException` aus `PdfSharpCore.Utils.FontResolver` abbrach.
- Ursache war der gemeinsame PDF-Pfad: `VereinsdokumentPdfBuilder` und `SignedVertragsdokumentPdfBuilder` erzeugten `XFont("Arial", ...)`, ohne auf Android vorab einen gültigen PdfSharpCore-FontResolver zu initialisieren.
- Der Fix liegt jetzt zentral in `PdfSharpFontResolverInitializer`: Der Resolver wird nur einmalig gesetzt und löst plattformabhängig echte Schriftdateien auf; auf Android werden dafür echte Systemschriften wie `Roboto` bzw. `NotoSans` aus `/system/fonts/` genutzt, statt auf Desktop-Annahmen zu bauen.
- Die gemeinsame Initialisierung wird vor jeder relevanten Font-Nutzung in den gemeinsamen PDF-Buildern ausgeführt; damit bleibt der bestehende fachliche Ablauf `Vorschau -> Unterschrift -> finales Speichern` unverändert, läuft aber auf Android jetzt über einen stabilen gemeinsamen Fontpfad.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bestehenden MAUI-Pachtvertrags-Flow auf dem echten Stand von `main` fachlich korrekt auf `Preview -> Unterschrift -> finales Speichern` umgestellt, ohne neuen Fachblock zu starten.
- Die Ist-Analyse zeigte, dass der mobile Pachtvertrag in `MemberParzellenDetailPage` und im Folgepfad nach Parzellenzuweisung bisher direkt über `CreatePachtvertragDokumentAsync(...)` im offiziellen Dokumentpfad gespeichert wurde; damit erfolgte die Persistierung zu früh, noch vor Dokumentprüfung und vor digitaler Unterschrift.
- Der mobile Flow erzeugt den Pachtvertrag jetzt zunächst nur temporär als vollständige PDF-Vorschau. Dafür wurde eine kleine `PachtvertragPreviewPage` ergänzt, die das komplette generierte Dokument über einen lokalen Temp-/Cache-Pfad zur Prüfung öffnet und zugleich `Zurück`, `Abbrechen` und `Weiter zur Unterschrift` anbietet.
- Der technische mobile Ablauf ist dabei in einem kleinen gemeinsamen `PachtvertragFlowHelper` gekapselt, damit sowohl der bestehende Einstieg im mitgliedsbezogenen Parzellen-Detail als auch der Folgepfad nach erfolgreicher Parzellenzuweisung denselben sauberen Preview-/Signatur-/Save-Pfad nutzen.
- Erst nach erfolgreicher digitaler Unterschrift wird der finale signierte Pachtvertrag im offiziellen Dokumentpfad gespeichert; bei Abbruch vor oder während der Unterschrift erfolgt keine endgültige Ablage.
- Der bestehende gemeinsame Pachtvertrags-Builder bleibt führend: die Vorschau wird aus demselben Produktivpfad erzeugt, und der finale Save nutzt weiter den vorhandenen gemeinsamen Dokumentpfad; temporäre Vorschau und finale Ablage bleiben sauber getrennt.
- Die bestehende MAUI-Signaturseite mit Querformat auf mobilen Geräten blieb unverändert erhalten.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bestehenden MAUI-Mitgliedsantrag-Flow auf dem echten Stand von `main` fachlich korrekt auf `Preview -> Unterschrift -> finales Speichern` umgestellt, ohne neuen Fachblock zu starten.
- Die Ist-Analyse zeigte, dass `MemberDetailPage` den Antrag bisher direkt nach Beitragsbestätigung über den offiziellen Dokumentpfad gespeichert hat; damit erfolgte die Persistierung zu früh, noch vor Dokumentprüfung und vor der digitalen Unterschrift.
- Der mobile Flow erzeugt den Mitgliedsantrag jetzt zunächst nur temporär als vollständige PDF-Vorschau. Dafür wurde eine kleine `MitgliedsantragPreviewPage` ergänzt, die das komplette generierte Dokument über einen lokalen Temp-/Cache-Pfad zur Prüfung öffnet und zugleich `Zurück`, `Abbrechen` und `Weiter zur Unterschrift` anbietet.
- `Zurück` führt wieder in den Bearbeitungsdialog, `Abbrechen` beendet den Flow ohne Persistierung, und erst nach erfolgreicher digitaler Unterschrift wird der finale Mitgliedsantrag im offiziellen Mitglieds-Dokumentpfad gespeichert.
- Der bestehende gemeinsame Dokumentpfad bleibt führend: die Vorschau kommt aus demselben gemeinsamen Mitgliedsantrag-Builder, und der finale Save nutzt weiterhin den gemeinsamen Uploadpfad; temporäre Vorschau und finale Ablage bleiben sauber getrennt.
- Die bestehende MAUI-Signaturseite mit Querformat auf mobilen Geräten blieb erhalten; der gemeinsame PDF-Signaturbuilder wurde nur textlich so geöffnet, dass er neben Vertragsdokumenten auch den signierten Mitgliedsantrag fachlich sauber ergänzt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bereits begonnenen Saisonverwaltungs-Block auf dem echten Stand von `main` produktiv in WPF und MAUI vervollständigt, ohne neuen Fachblock zu starten.
- Der gemeinsame Servicepfad `saison` wurde dabei sauber geschlossen: `GetSaisonRecordsAsync()` lädt die Saisondaten produktiv, `SaveSaisonAsync(...)` normalisiert `id` und `jahr` auf das Kalenderjahr und speichert die Felder der bestehenden Saisonstruktur produktiv zurück.
- In WPF wurden die bereits vorhandenen Bausteine `SaisonverwaltungViewModel` und `SaisonverwaltungView` jetzt wirklich an die Navigation angeschlossen; der Einstieg ist für Admin sichtbar als `Verwaltung` mit Unterpunkt `Saisonverwaltung`.
- In MAUI wurde die fehlende `SaisonverwaltungPage` ergänzt und in `MauiProgram`, `ShellRouteRegistrar` sowie `AdminShell` produktiv verdrahtet; auch dort erscheint der Einstieg nur für Admin unter `Verwaltung` / `Saisonverwaltung`.
- Fachlich bleibt der Block klein und konsistent: vorhandene Saisons werden angezeigt, neue Saisonvorschläge übernehmen die Vorjahreswerte, `id` und `jahr` entsprechen dem Kalenderjahr, vergangene Jahre bleiben schreibgeschützt und der Speichern-Button steht am Ende des Formulars.
- Der temporäre harte rote Sichtbarkeitstest aus `MemberDetailPage` wurde im selben Abschlusslauf wieder sauber zurückgebaut, ohne dort die Fachlogik zu ändern.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen Analyse-/Routingblock `memberdetails` auf dem echten Stand von `main` sauber aufgelöst und minimal korrigiert, ohne neuen Fachblock zu starten.
- Der Pfad aus der Mitgliedersuche schreibt das ausgewählte Mitglied korrekt in den `MemberContextState` und aktiviert danach bevorzugt die sichtbare Shell-Route `memberdetails`.
- Die eigentliche Abweichung lag im `AdminShell`: Dort war `memberdetails` im echten Stand nicht auf `MemberDetailPage`, sondern auf `MeineDatenPage` verdrahtet.
- Dadurch öffnete die sichtbare Stammdatenseite zwar fachlich korrekt den Stammdatenpfad, technisch aber nicht die erwartete `MemberDetailPage`; deshalb erschienen dort weder der harte rote Marker noch der Mitgliedsantragspfad dieser Seite.
- Minimaler Fix: `AdminShell` zeigt `memberdetails` jetzt auf `MemberDetailPage`; zusätzlich wurde der direkte Fallback aus `MemberSearchPage` ebenfalls auf `MemberDetailPage` gezogen, damit bevorzugter Routepfad und Ausweichpfad denselben Zieltyp verwenden.
- `UserShell` blieb bewusst unverändert, weil dort für den Eigenkontext weiterhin der getrennte Pfad `mydetails` gilt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen temporären MAUI-Härte-/Sichtbarkeitstest auf dem echten Stand von `main` umgesetzt, ohne neuen Fachblock zu starten.
- In `MemberDetailPage` wurde der vorhandene Laufzeit-Hinweis ganz oben zu einem bewusst auffälligen roten Testblock ausgebaut, damit auf einem Screenshot sofort erkennbar ist, ob wirklich dieser Seiten-Code läuft.
- Der Block zeigt jetzt explizit `TEST MemberDetailPage aktiv`, `Build main`, einen klaren Marker sowie die wichtigsten Laufzeitwerte `PageType`, `member.Id`, `_isCreateMode` und `CanCreateMitglied` direkt auf der Seite an.
- Die eigentliche Fachlogik für Rechte, Dokumente und die Sichtbarkeit des Mitgliedsantrag-Buttons blieb unverändert; der Block dient nur dem eindeutigen harten Laufzeittest.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen temporären MAUI-Diagnoseblock zur Laufzeitidentität auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Im betroffenen Stammdatenpfad `MemberDetailPage` wird jetzt direkt sichtbar angezeigt, dass technisch wirklich `MemberDetailPage` geöffnet ist.
- Zusätzlich zeigt der temporäre Hinweis App-Version und Buildnummer aus dem bestehenden MAUI-Versionspfad, einen Build-Marker mit Zeitstempel/Git-Kennung sowie den aktuellen Page-Typ und den aktuellen Shell-/Seitenpfad an.
- Ziel des Blocks ist nur die eindeutige Laufzeitidentifikation des tatsächlich installierten Builds und des tatsächlich geöffneten Seitenpfads; Rechte, Dokumentpfade und Fachlogik blieben unverändert.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen temporären MAUI-Diagnoseblock für den im Lauf weiterhin fehlenden sichtbaren Button `Mitgliedsantrag als PDF` auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- In `KGV.Maui/Pages/MemberDetailPage.cs` wurde direkt an der bestehenden Sichtbarkeitsberechnung ein leicht wieder entfernbarer Diagnosehinweis ergänzt.
- Der Hinweis zeigt zur Laufzeit genau die drei relevanten Bedingungen des Buttons an: Page-Modus (`_isCreateMode`), geladene `member.Id` und Ergebnis von `PermissionChecks.CanCreateMitglied(...)`; zusätzlich wird die aktuelle Rolle und ein kurzer Begründungstext für einen unsichtbaren Button ausgegeben.
- Die eigentliche Fachlogik wurde bewusst nicht geändert; der Block dient nur dazu, sichtbar zu machen, welche Bedingung im echten MAUI-Lauf greift.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen MAUI-Abschlussblock `CreateMitglied`/Mitgliedsantrag gegen den echten Branch-Stand `feature/formularverwaltung` geprüft, ohne neuen Fachblock zu starten.
- Die fachliche Gegenprüfung zeigte, dass der sichtbare mobile Einstieg `Mitgliedsantrag als PDF` im bestehenden `MemberDetailPage`-Pfad bereits korrekt an `PermissionChecks.CanCreateMitglied(...)` hängt; der frühere Dokumentrechtepfad `CanManageDocuments(...)` ist dort nicht mehr aktiv.
- Auch die direkt angrenzenden mobilen Onboarding-/Verpachtungspfade sind bereits konsistent: Parzellenzuweisung in `MemberGardensPage` und `Pachtvertrag als PDF` in `MemberParzellenDetailPage` nutzen ebenfalls `CanCreateMitglied(...)`.
- Ergebnis: Für diesen Abschlusslauf war kein weiterer Codeeingriff nötig; es wurden bewusst keine WPF-Änderungen, keine neue Dokumentlogik und keine neue Rechte-/Schattenlogik ergänzt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bereits begonnenen Datumsfix im realen Save-/Serializer-Pfad auf dem echten Branch-Stand `feature/formularverwaltung` sauber abgeschlossen, ohne neuen Fachblock zu starten.
- Der reale Restfehler lag nicht mehr in den Modell-Convertern, sondern im bisherigen `.Set(...)`-Updatepfad des Services: Dort wurden rohe `DateTime`-Werte geschrieben und die erweiterten Modell-Converter damit umgangen.
- Der zentrale Fix liegt jetzt ausschließlich in `KGV.Infrastructure/Services/SupabaseService.cs`: Arbeitseinsatz und Termin nutzen für `Insert` und `Update` denselben sicheren gemeinsamen Write-Pfad.
- Dabei werden reine Datumswerte explizit als `yyyy-MM-dd` und `timestamp without time zone` explizit ohne `Z`/Offset an PostgREST geschrieben, damit keine ungewollte UTC-/Offset-Verschiebung mehr in den DB-Speicherpfad hineinläuft.
- Es wurden bewusst keine UI-Workarounds, keine neue Fachlogik und keine weiteren Modell-Experimente ergänzt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

## 2026-04-09
- Den bereits umgesetzten Block `Mitgliedsantrag mit Mitgliedsbeitrag` auf dem echten Branch-Stand `feature/formularverwaltung` im Abschlusslauf fachlich und technisch sauber geschlossen, ohne neuen Fachblock zu starten.
- Der Mitgliedsantrag nutzt dafür weiter den bestehenden Saisonpfad `SaisonRecord.Mitgliedsbeitrag`; die Vorschlagsregel bleibt wie begonnen konsistent in WPF und MAUI: Beginn vor `01.07.` voller Jahresbeitrag, Beginn ab `01.07.` halber Jahresbeitrag.
- Der vorgeschlagene Beitrag bleibt in beiden Erzeugungsdialogen vor dem finalen Erzeugen manuell editierbar; der final verwendete Betrag wird sichtbar im PDF ausgegeben.
- Der Mitgliedsantrag bleibt fachlich rein mitgliedsbezogen; parzellenbezogene Inhalte werden dort nicht ausgegeben und bleiben weiterhin dem Pachtvertrag vorbehalten.
- Im Abschlusslauf wurde nur noch der kleine technische MAUI-Buildrest des bereits umgesetzten Dialogpfads geschlossen, indem die numerische Tastatur explizit auf `Microsoft.Maui.Keyboard.Numeric` gezogen wurde.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen MAUI-Block `Mitgliedsdokumente im Mitgliedskontext` auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- In der mobilen mitgliedsbezogenen Navigation gibt es jetzt den sichtbaren Menüpunkt `Dokumente` direkt zwischen `Stammdaten` und `Wartungsverträge`; er führt nicht auf eine globale Dokumente-Startseite, sondern direkt in die bestehende Dokumentansicht des aktuell ausgewählten Mitglieds.
- Dafür wurden keine neuen Dokumentservices und kein neues MAUI-ViewModel eingeführt: die vorhandene `DokumentePage` bleibt führend und nutzt weiter die bestehenden Produktivpfade `GetMitgliedDokumenteAsync(...)`, `GetParzelleDokumenteAsync(...)` und `ResolveDokumentOpenUrlAsync(...)`.
- Die bisher falsche Kopplung von reiner Lesbarkeit an Dokument-Verwaltungsrechte wurde im mobilen Mitgliedskontext gelöst: Dokumente sehen und öffnen folgt dort jetzt dem ausgewählten Mitgliedskontext, während Upload, Löschen, signierten Scan ablegen und digitale Signatur weiter nur mit dem bestehenden Fachrecht `CanManageDocuments` sichtbar bleiben.
- Ergebnis: Die mitgliedsbezogene MAUI-Dokumentansicht zeigt jetzt die echten Dokumente des aktuell ausgewählten Mitglieds an; dazu gehören auch `Mitgliedsantrag` und `Pachtvertrag`, wobei der Pachtvertrag fachlich im Mitglied sichtbar bleibt, auch wenn er aus einem Parzellenkontext stammt.
- Validierung: `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bereits begonnenen WPF-Updateprüfungsblock auf dem echten Branch-Stand `feature/formularverwaltung` fachlich sauber nachgeschärft, ohne neuen Fachblock zu starten.
- Die Updateprüfung startet jetzt nicht mehr nur allgemein nach erfolgreichem Login, sondern erst nachdem das `MainWindow` bzw. die Startseite erstmals sauber gerendert wurde.
- Dafür wurde der bestehende WPF-Startup-Pfad minimal von einem frühen Dispatcher-Start auf den vorhandenen Window-Lifecycle `ContentRendered` plus Dispatcher-Idle verschoben; die eigentliche Updateprüfungslogik bleibt unverändert.
- Ergebnis: Die WPF-Updateabfrage läuft jetzt erst nach erfolgreichem Login und nach sauber geladenem Startfenster an, statt den frühen Hauptfenster-/Startseitenaufbau zu unterbrechen.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den bereits begonnenen Rechteblock `CreateMitglied` auf dem echten Branch-Stand `feature/formularverwaltung` fachlich sauber abgeschlossen, ohne neuen Fachblock zu starten.
- Die Vererbungslogik wurde korrigiert: `CreateMitglied` wird nicht mehr automatisch an `Vorstand` vererbt.
- `Admin` behält das Recht weiterhin automatisch über die Rollenbasis; `Vorstand` kann `CreateMitglied` jetzt nur noch per expliziter Zuweisung erhalten.
- Der bestehende gemeinsame Check `CanCreateMitglied(...)` bleibt unverändert auf dem effektiven Rechte-Set und spiegelt damit den Sollzustand korrekt wider.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen Rechteblock `CreateMitglied` auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Dafür wurde im bestehenden Permission-Pfad ein eigenes Fachrecht `CreateMitglied` ergänzt; es ist kein neuer globaler Rollentyp, sondern ein gezielter Fachblock für Aufnahme-/Onboarding- und Verpachtungspflichten.
- Vorstand behält den bisherigen Zugriff bewusst weiter über die Rollenbasis; Admin bleibt ebenfalls voll funktionsfähig. Zusätzlich kann das neue Fachrecht jetzt gezielt vergeben werden, ohne fremde Adminbereiche wie Rechteverwaltung oder Saisonverwaltung mit freizuschalten.
- `PermissionChecks` kennt jetzt den gemeinsamen Check `CanCreateMitglied(...)`; daran hängen die direkt betroffenen sichtbaren Einstiege und Freigaben für `Mitglied anlegen`, `Mitgliedsantrag erstellen`, `Parzelle zuweisen` und `Pachtvertrag erstellen`.
- In MAUI wurden dafür der sichtbare Einstieg `Mitglied neu anlegen`, der Stammdaten-Folgepfad `Mitgliedsantrag als PDF`, die mobile Parzellenzuweisung und der Pachtvertragspfad auf das neue Recht umgestellt.
- In WPF wurden die entsprechenden Einstiege analog an denselben gemeinsamen Check gehängt; für die Parzellenzuweisung wurde der bestehende Mitglieds-Bearbeitungspfad minimal so geöffnet, dass der Onboarding-/Verpachtungspfad mit `CreateMitglied` nutzbar bleibt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen MAUI-UX-Fix für den Mitgliedsantrag im Stammdatenkontext auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Nach erfolgreicher Mitglieds-Neuanlage springt der mobile Pfad nicht mehr blind auf `MeineDatenPage`, sondern übernimmt den neu angelegten Datensatz direkt in den normalen Stammdatenkontext derselben Seite.
- Dadurch bleibt der Create-Modus nicht mehr hängen: Die Seite läuft danach als normale Stammdatenansicht weiter, und der bestehende Button `Mitgliedsantrag als PDF` ist für das gerade angelegte Mitglied mit Dokumentrechten direkt sichtbar und nutzbar.
- Der vorhandene Dialog `Mitgliedsantrag erstellen?` im Neuanlage-Flow bleibt bestehen; zusätzlich kann derselbe Mitgliedsantrag danach weiterhin klar aus den Stammdaten des gerade angelegten Mitglieds erzeugt werden.
- Ergebnis: Der mobile Mitgliedsantrag ist jetzt sowohl direkt im Neuanlage-Flow als auch anschließend alltagstauglich im normalen Stammdatenkontext erreichbar, ohne verwirrenden Seitensprung.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen MAUI-Bereinigungsblock für alte `Mitgliedsvertrag`-Reste auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Im mobilen Mitgliedsdetail wird kein eigenständiger sichtbarer Pfad `Mitgliedsvertrag als PDF` mehr angeboten; fachlich sichtbar bleibt dort nur noch `Mitgliedsantrag als PDF`.
- Der bereits angepasste mobile Neuanlage-Flow bleibt damit konsistent: Nach erfolgreicher Anlage wird weiter nur `Mitgliedsantrag erstellen?` angeboten; ein separater sichtbarer MAUI-Flow für `Mitgliedsvertrag` entfällt.
- Im mobilen Dokumentpfad wurden die sichtbaren Vertrags-Folgeaktionen zusätzlich auf fachlich gültige `Pachtvertrag`-Dokumente begrenzt, sodass alte `Mitgliedsvertrag`-Dokumente in MAUI keine gesonderten Vertrags-Folgeaktionen mehr anbieten.
- Bestehende gemeinsame Service- und Dokumentpfade bleiben unverändert nutzbar; es wurde keine neue Vertragslogik und kein neuer Signaturblock ergänzt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen MAUI-Formularblock für `Mitgliedsantrag` und `Pachtvertrag` auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Im mobilen Neuanlage-Flow des Mitglieds wird nach erfolgreicher Anlage jetzt `Mitgliedsantrag erstellen?` gefragt; bei Zustimmung nutzt der Pfad direkt den bestehenden Produktivpfad `CreateMitgliedsantragDokumentAsync(...)` und legt das Dokument im vorhandenen Mitglieds-Dokumentpfad ab.
- Wird der Antrag im Neuanlage-Moment noch nicht erzeugt, bleibt der bestehende mobile Folgepfad erhalten: Im Mitgliedskontext kann `Mitgliedsantrag als PDF` weiterhin nachträglich manuell erzeugt werden.
- Für den mitgliedsbezogenen Gartenpfad wurde eine kleine mobile Parzellenzuweisung ergänzt. Sie nutzt den bestehenden gemeinsamen Servicepfad `AssignParzelleToMitgliedAsync(...)`, erlaubt die Auswahl einer freien Parzelle sowie des Zuweisungsdatums und baut keine neue Parallelarchitektur.
- Nach erfolgreicher Parzellenzuweisung wird mobil jetzt direkt `Pachtvertrag erstellen?` angeboten; bei Zustimmung nutzt der bestehende gemeinsame Dokumentpfad `CreatePachtvertragDokumentAsync(...)` direkt die neue Zuordnung und speichert den Vertrag im vorhandenen Mitglieds-Dokumentpfad.
- Auch der nachträgliche Pachtvertragspfad bleibt erhalten: Im mitgliedsbezogenen Gärten-/Parzellen-Detail ist `Pachtvertrag als PDF` weiterhin verfügbar, sofern eine passende Parzellenzuweisung mit Startdatum vorhanden ist.
- Der bestehende MAUI-Dokumentpfad bleibt führend; Folgeaktionen wie Öffnen, signierten Scan ablegen oder digital signieren wurden in diesem Block nicht neu erfunden und nicht umgebaut.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den noch offenen Abschluss zum Datumsfix auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Nach dem bereits ergänzten gemeinsamen `date`-Pfad wurde jetzt auch der fehlende gemeinsame `Newtonsoft.Json`-Pfad für PostgreSQL-`timestamp without time zone` ergänzt, weil der reale Save-/PostgREST-/Supabase-Pfad hier über `Newtonsoft.Json` serialisiert.
- Damit sind jetzt nicht nur `datum`, sondern auch die Datums-/Zeitfelder `sichtbar_ab`, `sichtbar_bis` und `anmeldung_bis` im gemeinsamen Produktivpfad gegen ungewollte UTC-/Offset-Verschiebungen abgesichert.
- Die betroffenen Modellfelder in `Arbeitseinsatz` und `Termin` tragen dafür jetzt zusätzlich passende `Newtonsoft.Json.JsonConverter(...)`-Attribute; denselben gemeinsamen Timestamp-Pfad wurde minimal auch für `Bekanntmachung` mitgezogen.
- Ergebnis: Der reale Save-Pfad ist jetzt für beide relevanten PostgreSQL-Typen gemeinsam abgesichert: `date` und `timestamp without time zone`, jeweils für `System.Text.Json` und `Newtonsoft.Json`.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen WPF-Startup-Block zur Updateprüfung auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neuen Fachblock zu starten.
- Die bestehende Updateprüfung lief bisher noch vor dem Login im frühen App-Startup; dieser Pfad wurde jetzt hinter den erfolgreichen Login und hinter das Anzeigen des `MainWindow` verschoben.
- Dadurch bleibt der eigentliche Login-/Frühstartpfad schlanker, und die Updateabfrage kann den Start vor der Anmeldung nicht mehr vorzeitig beeinflussen.
- Ergänzend wurde der statische App-Pfad minimal mit Frühstart-Logging versehen, damit ein Absturz vor `OnStartup(...)` im WPF-Dateilog transparenter sichtbar wird.
- Die bestehende Updateprüfungslogik selbst bleibt erhalten; es wurde keine neue Update-Architektur und keine neue Login-Logik eingeführt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den begonnenen Datumsfix-Block für `Arbeitseinsätze` und `Termine` auf dem echten Branch-Stand `feature/formularverwaltung` sauber abgeschlossen, ohne neuen Fachblock zu starten.
- Fehlerursache lag im gemeinsamen DateOnly-Save-Pfad: der reale PostgREST-/Supabase-Pfad serialisiert die betroffenen `date`-Felder hier über `Newtonsoft.Json`, während die bisherigen Modelle nur `System.Text.Json`-Konverter trugen.
- Dadurch konnten reine Datumswerte im Produktivpfad zeitzonenbedingt um `-1 Tag` kippen; der Fehler lag also nicht nur im Rücklesen, sondern bereits im zentralen Speichern.
- Zentraler Fix: gemeinsame `Newtonsoft.Json`-Konverter für PostgreSQL-`date` ergänzt und an die betroffenen gemeinsamen `date`-Modelle angebunden; der vorhandene `System.Text.Json`-Pfad bleibt parallel erhalten.
- Der Fix gilt damit zentral für `Arbeitseinsätze` und `Termine`; Insert und Update nutzen denselben korrigierten DateOnly-Pfad ohne UI-Sonderlogik oder verstreute Workarounds.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den kleinen MAUI-Nachziehblock für Vertrags-Einstiege auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neue Formular- oder Signaturlogik zu starten.
- Auf der mobilen Stammdatenseite des Mitglieds gibt es jetzt zusätzlich den Einstieg `Mitgliedsvertrag als PDF`; der Button nutzt den bereits bestehenden Produktivpfad `CreateMitgliedsvertragDokumentAsync(...)`.
- Im mitgliedsbezogenen Pfad `Gärten` bzw. in der zugehörigen Parzellen-Detailansicht gibt es jetzt zusätzlich den Einstieg `Pachtvertrag als PDF`; auch dieser nutzt direkt den bestehenden Produktivpfad `CreatePachtvertragDokumentAsync(...)`.
- Der neue mobile Pachtvertragspfad baut keine Schattenlogik und keine Ersatzwerte auf: maßgeblich bleibt das im Parzellenkontext vorhandene Zuordnungs-Startdatum; fehlt es, wird mit klarer Meldung abgebrochen.
- Validierung: `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den begonnenen Block `Pachtvertrag nutzt Saisonwerte` auf dem echten Branch-Stand `feature/formularverwaltung` sauber abgeschlossen, ohne neuen Fachblock zu starten.
- Der bestehende Saison-/Pachtvertragspfad nutzt jetzt für den Zahlungskasten gemeinsam `pacht_pro_qm` und `mitgliedsbeitrag` der zum Vertragsjahr passenden Saison; Ersatzwerte oder Schattenlogik wurden nicht ergänzt.
- Die fachliche Berechnung lautet jetzt `Pachtzins = Parzellenfläche * Saison.Pacht_pro_qm` mit kaufmännischer Rundung auf zwei Nachkommastellen und `Gesamt = Pachtzins + Mitgliedsbeitrag`.
- Fehlende Pflichtwerte laufen nicht mehr still weiter: fehlende Saison, fehlendes `pacht_pro_qm` und fehlender `mitgliedsbeitrag` brechen den bestehenden Produktivpfad jetzt mit klaren Fehlermeldungen ab.
- Die bestehende PDF-Vorlage und der vorhandene Feldbefüllungspfad bleiben führend; im Betragskasten werden jetzt mindestens Mitgliedsbeitrag, Pacht und Gesamt korrekt befüllt.
- Der bereits begonnene Saisonpfad wurde nur minimal mitgezogen, damit der aktuelle Stand buildfähig bleibt; es wurde keine weitere Saison-UI-Erweiterung gestartet.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Einen kleinen, buildfähigen UI-/UX-Feinschliff-Block für die Formularverwaltung auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt, ohne neue Vertrags- oder Verwaltungslogik zu starten.
- Die Dokumentlisten in WPF und MAUI stellen Vertragsstatus jetzt klarer dar: Vertragsdokumente verwenden statt des bloßen Statuswerts die lesbaren Bezeichnungen `Unsignierte Vertragsfassung` und `Signierte Vertragsfassung`.
- `DocumentInfo` wurde dafür nur minimal um UI-Hilfseigenschaften ergänzt, sodass WPF und MAUI denselben bestehenden Dokumentpfad weiterverwenden und keine Schattenlogik für Anzeige oder Folgeaktionen aufbauen.
- In WPF bleibt der Pfad bewusst auf Verwaltung, Kontrolle, Dokumentanzeige und Upload signierter Scan-Fassungen beschränkt; ein kleiner Hinweistext stellt nun explizit klar, dass die direkte digitale Signatur ausschließlich in MAUI erfolgt und die unsignierte Fassung erhalten bleibt.
- Die WPF-Folgeaktion für unsignierte Vertragsdokumente wurde sprachlich auf `Signierten Scan hochladen` geschärft; für signierte Enddokumente und Nicht-Vertragsdokumente erscheinen weiterhin keine unpassenden Vertrags-Sonderaktionen.
- In MAUI wurden die Dokumentaktionen ebenfalls geglättet: `Öffnen`, `Signierten Scan ablegen` und `Digital signieren` sind für unsignierte Vertragsdokumente sauber getrennt sichtbar; signierte Enddokumente und Nicht-Vertragsdokumente zeigen keine unpassenden Folgeaktionen.
- Ergänzend wurden Meta-/Kontexthinweise in MAUI so geschärft, dass Folgeaktionen verständlich bleiben und ausdrücklich sichtbar ist, dass die unsignierte Vertragsfassung bei Scan-Upload oder digitaler Signatur erhalten bleibt.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den begonnenen MAUI-Digitalsignatur-Block auf dem echten Branch-Stand `feature/formularverwaltung` ohne neuen Fachblock technisch sauber abgeschlossen.
- Im mobilen Dokumentpfad gibt es für vorhandene unsignierte Vertragsdokumente (`Mitgliedsvertrag`, `Pachtvertrag`) jetzt zusätzlich die Aktion `Digital signieren`; der bestehende Upload-/Statuspfad für signierte Fassungen bleibt dabei führend.
- Dafür wurde ein gemeinsames, MAUI-unabhängiges Signaturmodell `DigitalSignatureCapture` ergänzt, damit die erfassten Striche/Punkte nicht als App-spezifische Sonderlogik im UI hängen bleiben.
- Die dedizierte Seite `VertragsSignaturPage` stellt eine große Signaturfläche sowie `Leeren`, `Übernehmen` und `Abbrechen` bereit; auf Android wird während dieses Flows über `MainActivity` gezielt Querformat aktiviert und beim Verlassen wieder freigegeben.
- Im Shared-/Servicepfad wurde `CreateSignedVertragsdokumentAsync(...)` ergänzt: Die bestehende unsignierte Vertragsfassung wird geladen, um eine zusätzliche Signaturseite ergänzt und anschließend als eigenes `signiert`-Enddokument im vorhandenen Mitglieds-Dokumentpfad gespeichert.
- Das bestehende Dateinamenschema bleibt auch für die digital signierte Fassung erhalten; die unsignierte Ausgangsfassung wird weder überschrieben noch gelöscht.
- Es wurde bewusst keine neue WPF-UI-Erweiterung und keine zusätzliche Fachlogik ergänzt; der Block schließt nur den bereits begonnenen MAUI-Digitalsignaturpfad technisch ab.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den nächsten kleinen, buildfähigen Formularblock auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt und den Signatur-/Status-Folgepfad für Vertragsdokumente geschlossen.
- Der bestehende Dokumenttyp-/Statuspfad bleibt führend: `Mitgliedsvertrag` und `Pachtvertrag` werden weiterhin separat über `unsigniert` und `signiert` unterschieden; die unsignierte Fassung bleibt beim Nachpflegen einer unterschriebenen Fassung unverändert erhalten und wird weder überschrieben noch gelöscht.
- Dafür wurde im gemeinsamen Servicepfad `UploadSignedVertragsdokumentAsync(...)` ergänzt. Der Pfad akzeptiert nur bestehende unsignierte Vertragskontexte (`Mitgliedsvertrag`, `Pachtvertrag`) und nur PDF-Dateien; die signierte Fassung wird als eigenes Enddokument im bestehenden Mitglieds-Dokumentpfad abgelegt.
- Das bestehende Dateinamenschema bleibt erhalten und wird für die signierte Fassung sauber fortgeführt: `<Name_Vorname>-<ID>-<yyyy-MM-dd>-<Dokumenttyp>-signiert.pdf`.
- `DocumentInfo` erkennt jetzt explizit, ob ein Dokument ein Vertragsdokument ist und ob dazu im UI eine signierte Fassung abgelegt werden darf; damit bleiben signierte und unsignierte Fassungen im bestehenden Dokumentkontext getrennt sichtbar und nachvollziehbar.
- In WPF wurde der Folgepfad minimal in die bestehende Mitglieds-Dokumentliste eingebunden: Für vorhandene unsignierte Vertragsdokumente erscheint dort jetzt `Signierte Fassung`, ohne den übrigen Dokumentpfad oder die Listenstruktur umzubauen.
- In MAUI wurde derselbe kleine Folgepfad in `DokumentePage` ergänzt; auch mobil kann damit eine unterschriebene Fassung zu einem vorhandenen unsignierten Vertragsdokument abgelegt werden, ohne neue Parallelansicht oder Signatur-Canvas.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.
- Noch bewusst offen bleibt die spätere echte digitale Signatur-/Canvas-Logik; der aktuelle Block schafft nur den anschlussfähigen Status- und Upload-Unterbau.

- Den nächsten kleinen Formularblock auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt und den Mitgliedsvertrag auf den bestehenden Formular-/Dokumentpfad gezogen.

- 2026-04-18 – Mitgliedsantrag: PDF-Ressource korrekt eingebunden

- Ursache: Laufzeitfehler beim Erzeugen der Mitgliedsantrag-Vorschau wegen falschem EmbeddedResource-Namen in `MitgliedsantragDokumentFactory`.
- Fix: `PdfTemplateResourceName` auf `KGV.Core.Templates.Mitgliedsantrag_Vorlage.pdf` gesetzt und Projekt-Resources geprüft (`KGV.Core.csproj` enthält `Templates\\Mitgliedsantrag_Vorlage.pdf`).
- Validierung:
  - `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich
  - `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich
- Dafür wurde ein gemeinsamer Produktivpfad `Mitgliedsvertrag (unsigniert)` ergänzt: `ISupabaseService` und `SupabaseService` besitzen jetzt `CreateMitgliedsvertragDokumentAsync(...)`, das wie die vorhandenen Formularfälle direkt über den bestehenden Dokument-Upload und die Mitgliedsablage läuft.
- Da für den Mitgliedsvertrag noch keine eigene offizielle PDF-Vorlage im Repo geführt wird, wird der Vertrag zunächst als saubere wiederverwendbare Vereins-PDF über den vorhandenen Branding-/Vorlagenpfad erzeugt; die Kapselung liegt in `MitgliedsvertragDokumentFactory`, sodass später leicht auf eine echte Vorlage gewechselt werden kann.
- Der Mitgliedsvertrag gibt jetzt mindestens Name, Vorname, Geburtsdatum soweit vorhanden, Adresse, Kontaktangaben, Mitgliedskontext, Eintritts-/Vertragsdatum sowie einen Unterschriftsbereich aus und bleibt beim bestehenden Dateinamenschema `<Name_Vorname>-<ID>-<yyyy-MM-dd>-mitgliedsvertrag-unsigniert.pdf`.
- Der gemeinsame Vereinsdokument-Builder wurde nur minimal geöffnet: Statt eines fest verdrahteten Mitgliedsantrags-Intros akzeptiert er jetzt einen formularspezifischen Introtext, sodass Mitgliedsantrag und Mitgliedsvertrag denselben Brandingpfad weiterverwenden können, ohne neue Schattenlogik aufzubauen.
- Im WPF-Mitgliedsdetail wird nach erfolgreicher Mitglied-Neuanlage jetzt direkt gefragt `Mitgliedsvertrag erstellen?`; bei Zustimmung wird der unsignierte Vertrag erzeugt, im bestehenden Dokumentpfad des Mitglieds gespeichert und bei verfügbarer Open-URL direkt geöffnet.
- Der bestehende MAUI-Mitgliedspfad wurde in diesem kleinen Block ebenfalls mitgezogen: Nach erfolgreicher Neuanlage erscheint dieselbe Nachfrage, und der Vertrag wird über denselben gemeinsamen Servicepfad erzeugt; dabei wurde der Busy-Guard so korrigiert, dass die Vertragserzeugung im Neuanlagepfad nicht blockiert wird.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.
- Für den fachlichen Minimaltest existiert im aktuellen Workspace kein automatisierter UI-Testpfad; `get_tests` lieferte weder für `KGV.Wpf` noch für `KGV.Maui` passende Tests, daher wurde der Block build-validiert und der sichtbare Flow über die echten UI-/Servicepfade implementiert.

- Den begonnenen Pachtvertragsblock auf dem echten Branch-Stand `feature/formularverwaltung` sauber technisch geschlossen, ohne einen neuen Fachblock zu starten.
- Die offizielle Vorlage `Formulare/Pachtvertrag_KGV_bereinigt_mit_Feldern.pdf` ist jetzt bewusst im nachverfolgten Projektstand eingebunden und wird zentral aus `KGV.Core` als echte PDF-Formularvorlage verwendet; eine Ersatzvorlage oder ein nachgebautes Layout wird nicht verwendet.
- Dafür wurde der gemeinsame Produktivpfad für `Pachtvertrag (unsigniert)` ergänzt: `SaisonRecord` kennt jetzt `pacht_pro_qm`, `SupabaseService` erzeugt den Vertrag über `CreatePachtvertragDokumentAsync(...)`, und `PachtvertragDokumentFactory` berechnet den Pachtzins zentral als `Parzellenfläche * Saison.Pacht_pro_qm` mit kaufmännischer Rundung auf zwei Nachkommastellen.
- Fehlerfälle laufen nicht mehr still weiter: fehlende Saison, fehlendes `pacht_pro_qm`, ungültige oder fehlende Parzellenfläche sowie fehlende Pflicht-/Signaturfelder in der offiziellen PDF-Vorlage brechen den Produktivpfad jetzt mit klaren Fehlermeldungen ab.
- Die echte PDF-Vorlage wird direkt befüllt; mindestens Pächter 1/2, Geburtsdaten, Anschrift, Telefon, Parzellennummer, Parzellenfläche, Vertragsbeginn, `pacht_pro_qm`, Pachtzins, Betragskasten sowie Ort/Datum laufen über die vorhandenen Formularfelder.
- Im WPF-Mitgliedsdetail ist `NewContractCommand` jetzt produktiv, und nach erfolgreicher Parzellenzuweisung folgt im bestehenden Flow die Nachfrage `Pachtvertrag erstellen?`; bei Zustimmung wird der unsignierte Vertrag im bestehenden Mitglieds-Dokumentpfad gespeichert und bei verfügbarer Open-URL direkt geöffnet.
- Der begonnene Restfehler im WPF-Zuweisungspfad wurde minimal behoben, indem die Parzellen-ID vor dem Reload gesichert wird und der nachfolgende Vertragsdialog damit keinen Nullzugriff mehr auf die zurückgesetzte UI-Auswahl hat.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.
- Für den fachlichen Minimaltest existiert im aktuellen Repo kein automatisierter UI-Testpfad; deshalb wurde der Block build-validiert und die Fehlerpfade wurden direkt im produktiven Codepfad abgesichert.

- Den nächsten kleinen Block der Formularverwaltung auf dem echten Branch-Stand `feature/formularverwaltung` umgesetzt und den vorhandenen Zwischenstand direkt weiterverwendet.
- Der Mitgliedsantrag nutzt jetzt keine einfache technische Text-PDF mehr, sondern eine echte Vereinsvorlage mit sauberem Briefkopf, Vereinsname, Register-/Kontaktzeile und identischem Vereinslogo.
- Dafür wurde ein gemeinsamer Core-Pfad für Vereinsbranding und Vereins-PDF-Layout aufgebaut; spätere Dokumente wie Mitgliedsvertrag oder Pachtvertrag können denselben Briefkopf-/Abschnitts-/Unterschriftsrahmen mitbenutzen.
- Die Logo-Quelle bleibt fachlich konsistent: Das bestehende Vereinslogo aus `KGV.Maui/Resources/Images/kgv_logo.png` wird zentral als eingebettete Core-Ressource verwendet und im PDF gerendert.
- Der Mitgliedsantrag gibt jetzt strukturiert mindestens Antragsteller/in, Geburtsdatum, Adresse, Kontaktangaben, Mitgliedskontext sowie einen Unterschriftsbereich aus und bleibt auf dem bestehenden Mitglied-/Dokumentpfad gespeichert.
- Die Einstiege in WPF und MAUI bleiben bestehen und wurden nur minimal auf `Mitgliedsantrag als PDF` geglättet.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

## 2026-04-08
- Den ersten kleinen Grundblock für die neue Formularverwaltung auf dem Branch `feature/formularverwaltung` umgesetzt.
- Dafür eine gemeinsame Core-Grundlage für Formular-Dokumenttyp, Formular-Dokumentstatus, Dateinamenschema und einfache PDF-Erzeugung eingeführt; vorbereitet für `Mitgliedsantrag`, `Mitgliedsvertrag` und `Pachtvertrag` sowie `unsigniert`/`signiert`.
- Den ersten konkreten Formularfall `Mitgliedsantrag` über den bestehenden Mitglieds-/Dokumentpfad angebunden: Mitglied laden, PDF erzeugen, über den vorhandenen Dokument-Upload speichern und dem Mitglied zuordnen.
- In WPF und MAUI jeweils einen ersten sichtbaren Einstieg im Mitgliedskontext ergänzt; nach Erzeugung wird das abgelegte Dokument direkt geöffnet, sofern der bestehende Dokumentpfad bereits eine Open-URL liefert.
- Dokumentlisten können die neue Grundlage jetzt fachlich mindestens nach Typ und Status unterscheiden, ohne neue Schattenablage neben dem bestehenden Dokumentpfad aufzubauen.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Im WPF-Mitgliedsdetail die E-Mail-Sperrlogik minimal korrigiert: Bei Neuanlage bzw. ohne bestehenden App-User bleibt das Feld editierbar.
- Gesperrt bleibt das Feld jetzt nur noch, wenn das Mitglied bereits einen App-User über `AuthUserId` hat; der OTP-Button wird nur in diesem Fall eingeblendet.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` erfolgreich; `dotnet build KGV.Maui/KGV.Maui.csproj` scheitert in diesem Block extern an `java.exe`/Systemressourcen.

- Den WPF-UpdatePrompt minimal repariert, indem `AppUpdateInfo` wieder die erwartete Methode `GetNotesText()` bereitstellt.
- Dadurch kompiliert `KGV.Wpf/Views/UpdatePromptWindow.xaml.cs` wieder ohne `CS1061` auf `AppUpdateInfo.GetNotesText()`.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Die bestehende Demodaten-Ausblendung auf dem zentralen Operational-/Sichtbarkeitspfad auch für Admin erweitert.
- Dazu `OperationalDataFilter` um zentrale Prüfungen für `Arbeitseinsatz`, `Termin` und `Bekanntmachung` ergänzt; `TerminRecord` und `BekanntmachungRecord` tragen dafür jetzt ebenfalls `is_demo`.
- In `SupabaseService` hängen jetzt sowohl die Verwaltungslisten als auch die Home-/Startseitenpfade für Arbeitseinsätze, Termine und Bekanntmachungen an denselben bestehenden Operational-Filtern; dadurch bleiben diese Demodatensätze nun auch im Adminkontext ausgeblendet.
- `GetMitgliederAsync()` blendet Demomitglieder jetzt ebenfalls zentral aus, sodass der bestehende Mitgliedspfad auch für Admin keinen Demomitglied-Listenstand mehr liefert.
- Die früher eingebauten Demo-Schalterreste im Impressum wurden in WPF und MAUI wieder sauber zurückgebaut; beide UIs filtern die Impressum-Kontakte jetzt nur noch über `OperationalDataFilter.IsOperationalImpressumKontakt(...)`.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den Impressum-Demo-Schalter im Abschlusslauf noch einmal direkt gegen `origin/main` nur in den drei Ziel-Dateien geprüft und anschließend minimal nachgeschärft.
- WPF verwendet für den Demo-Schalter nicht mehr den alten fehleranfälligen Converter-Pfad `Visibility="{Binding IsDemoToggleVisible, Converter={StaticResource BoolToVisibilityConverter}}"`, sondern explizit den bestehenden ViewModel-Pfad `Visibility="{Binding DemoToggleVisibility, Mode=OneWay}"`.
- In `KGV.Wpf/ViewModels/ImpressumViewModel.cs` erzwingt `ShowDemoData` jetzt zusätzlich logisch, dass Demo-Daten nur mit Admin-Rolle aktiv sein können.
- In `KGV.Maui/Pages/ImpressumPage.cs` bleibt der Schalter in der Seitenkonstruktion direkt unter der Beschreibungs-Label-Zeile eingebunden; `UpdateDemoToggleVisibility()` refiltert jetzt explizit, wenn ein Admin-Demostatus beim Rollenwechsel entfällt.
- Beide UIs laufen weiter über denselben bestehenden Demo-/Operativpfad `OperationalDataFilter.IsOperationalImpressumKontakt(...)`.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den Impressum-Demo-Schalter im finalen Abschlusslauf robust auf die bestehende WPF-ViewModel-Sichtbarkeit umgestellt und den vorhandenen MAUI-Adminpfad explizit konsolidiert.
- WPF-Ursache des Absturzes war die `StaticResource`-Abhängigkeit am Demo-Schalter in `KGV.Wpf/Views/ImpressumView.xaml`; beim Öffnen der View konnte dieser Ressourcenpfad eine `XamlParseException` auslösen.
- Reale WPF-Behebung: Converter-Abhängigkeit vollständig entfernt und `Visibility` direkt an die bestehende `DemoToggleVisibility`-Property aus `KGV.Wpf/ViewModels/ImpressumViewModel.cs` gebunden.
- Reale MAUI-Ergänzung/Absicherung: `KGV.Maui/Pages/ImpressumPage.cs` nutzt den vorhandenen Demo-Schalter jetzt mit explizitem Standardzustand `aus` und zentraler Admin-Freigabe über `IsDemoToggleVisible`/`ShowDemoData`; Vorstand sieht ihn nicht.
- Beide UIs bleiben auf demselben bestehenden Demo-/Operativpfad `OperationalDataFilter.IsOperationalImpressumKontakt(...)`.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich.

- Den Impressum-Demo-Schalter auf dem echten Repo-Stand final für WPF und MAUI vereinheitlicht.
- WPF-Ursache des Absturzes war die `StaticResource`-/Converter-Abhängigkeit am Demo-Schalter in `KGV.Wpf/Views/ImpressumView.xaml`; im View-Kontext war dieser Pfad nicht robust genug und konnte in eine `XamlParseException` laufen.
- Behebung in WPF: `BooleanToVisibilityConverter` direkt lokal in der View als Ressource `BoolToVisibilityConverter` bereitgestellt und die Sichtbarkeit des Schalters an die bestehende ViewModel-Property `IsDemoToggleVisible` gebunden; `ShowDemoData` bleibt unverändert im bestehenden Pfad.
- MAUI nutzt auf `KGV.Maui/Pages/ImpressumPage.cs` denselben vorhandenen Schalterpfad weiter, jetzt mit expliziter zentraler Admin-Sichtbarkeit; Vorstand sieht ihn nicht, bei Nicht-Admin bleibt der Switch auf `aus`.
- Beide UIs laufen weiter über denselben bestehenden Demo-/Operativfilterpfad `OperationalDataFilter.IsOperationalImpressumKontakt(...)`.
- Validierung: `dotnet build KGV.Wpf/KGV.Wpf.csproj` und `dotnet build KGV.Maui/KGV.Maui.csproj` erfolgreich; passende automatisierte `Impressum`-Tests waren nicht vorhanden.

- Den noch offenen Impressum-Demo-Schalter-Block sauber abgeschlossen.
- WPF-Ursache des Absturzes war die Converter-/`StaticResource`-Abhängigkeit im Demo-Schalter der `ImpressumView`; dieser Pfad wurde robust entfernt und die Sichtbarkeit stattdessen direkt aus dem bestehenden ViewModel über `DemoToggleVisibility` abgeleitet.
- In MAUI wurde der fehlende Schalter `Demo-Datensätze einblenden` auf der `ImpressumPage` ergänzt, nur für Admin sichtbar und standardmäßig aus.
- Beide UIs filtern die sichtbaren Impressum-Kontakte jetzt auf demselben bestehenden gemeinsamen Demo-/Operativpfad `OperationalDataFilter.IsOperationalImpressumKontakt(...)`.
- Ergebnis: WPF crasht beim Öffnen des Impressums nicht mehr und MAUI besitzt nun denselben Admin-only-Demo-Schalter mit identischem Fachverhalten.

- Den defekten Impressum-Demo-Schalter in WPF gegen den echten Runtime-Stand repariert und den fehlenden MAUI-Schalter ergänzt.
- WPF-Ursache war kein Fehler im ViewModel, sondern eine `StaticResource`-Referenz auf `BoolToVisibilityConverter`, die in `ImpressumView.xaml` lokal nicht auflösbar war und die View beim Öffnen in eine `XamlParseException` laufen ließ.
- Der WPF-Ressourcenfehler wurde minimal behoben, indem der benötigte `BooleanToVisibilityConverter` direkt in der View als Ressource bereitgestellt wurde; die vorhandenen ViewModel-Properties `IsDemoToggleVisible` und `ShowDemoData` bleiben unverändert im Einsatz.
- In MAUI wurde auf `ImpressumPage` derselbe Schalter `Demo-Datensätze einblenden` ergänzt, nur für Admin sichtbar, standardmäßig aus und ohne Sonderarchitektur.
- Beide UIs filtern die sichtbaren Impressum-Kontakte über denselben bestehenden gemeinsamen Demo-/Operativpfad `OperationalDataFilter.IsOperationalImpressumKontakt(...)`.

- Das WPF-Impressum um den fehlenden Demo-Schalter ergänzt.
- Der Schalter erscheint nur auf der Impressum-Seite und nur für Admin; Vorstand sieht ihn nicht.
- Standardzustand bleibt aus, sodass Demo-Datensätze ohne explizites Einblenden weiter verborgen bleiben.
- Der Schalter nutzt keine WPF-Sonderlogik, sondern filtert die Impressum-Kontakte über den bestehenden gemeinsamen Demo-/Operativfilterpfad.
- Ergebnis: WPF kann Demo-Kontakte im Impressum nur gezielt durch Admin sichtbar machen, ohne die bestehende Demo-/Reviewer-Isolierung aufzuweichen.

- In MAUI das Feld `arbeitsstunden_altersregel_typ` im Hauptmitglied-Kontext ergänzt.
- Umsetzung auf der Stammdatenseite sowie im Hauptmitglied-Anlegen/Bearbeiten als Picker mit genau den Werten `mann80` und `frau75`.
- Für Hauptmitglieder ist das Feld beim Anlegen jetzt Pflicht; im Save-Pfad wird nur einer der beiden zulässigen Werte akzeptiert.
- Beim Nebenmitglied bleibt das Feld fachlich irrelevant, wird mobil nicht angezeigt und nicht als Pflichtfeld behandelt.
- Der DTO-/Supabase-Pfad wurde minimal mitgezogen, damit der Wert für Hauptmitglieder produktiv geladen und gespeichert wird.

- Den realen MAUI-Save-/Reload-Pfad der Stammdatenseite geprüft und minimal korrigiert.
- Ursache war kein fehlendes Binding, sondern dass der Reload nach erfolgreichem Speichern noch innerhalb des aktiven Busy-Zustands lief und dadurch am Busy-Guard von `LoadAsync()` sofort abbrach.
- `MeineDatenPage` lädt nach erfolgreichem Speichern jetzt erzwungen frisch nach, ohne manuellen Seitenwechsel und ohne erneutes Öffnen der Seite.
- Ergebnis: Die geänderten Stammdaten sind direkt nach dem Speichern auf der sichtbaren MAUI-Stammdatenseite sichtbar.

- Die sichtbare MAUI-Stammdatenseite im Mitgliedskontext gezielt verschlankt.
- Sichtbar bleiben dort jetzt nur noch `Grunddaten`, `Kontakt`, `Adresse` und `Bemerkung`.
- Aus der sichtbaren Stammdatenseite entfernt wurden die Blöcke `Mitgliedschaft`, `Wartungsverträge / Pflichtstunden`, `Mitgliedskontext`, `Verwaltung` sowie der direkte Dokumente-Button.
- Die entfernten Inhalte bleiben fachlich über die vorhandenen Shell-Untermenüs erreichbar; WPF blieb in diesem Block unverändert.
- Zusätzlich `email_rechnung_einwilligung` und `email_info_einwilligung` im MAUI-Kontaktbereich als Schalter eingebunden und durch den DTO-/Supabase-Speicherpfad gezogen.
- Ergebnis: Die mobile Stammdatenseite ist klarer fokussiert, ohne dass die entfernten Fachbereiche oder die beiden E-Mail-Einwilligungen im Datenpfad verloren gehen.

- WPF-Impressum gegen den gemeldeten Runtime-Bindingfehler geprüft.
- Ursache war kein Fehler in `PropertyPathWorker`, sondern eine schreibgeschützte ViewModel-Eigenschaft `ClubEmail`, die in `ImpressumView.xaml` in einem `Run` ohne explizites `OneWay` gebunden war.
- Minimaler Fix: `ClubEmail` im WPF-Impressum explizit auf `Mode=OneWay` gesetzt.
- Ergebnis: Der Runtime-Fehler `TwoWay- oder OneWayToSource-Bindungen funktionieren nicht mit der schreibgeschützten Eigenschaft "ClubEmail"` ist auf diesem Pfad geschlossen.

- Den Demo-/Play-Store-Adminblock auf dem echten Supabase-/RLS-Stand umgesetzt statt nur über UI-Sichtbarkeit.
- Vorrangregel ergänzt: Demo-/Reviewer-Konten bleiben auch mit Rolle `admin`/`vorstand` auf Demo-Daten begrenzt; nur echte produktive Admins/Vorstand behalten Vollzugriff auf Produktivdaten.
- Dafür relevante Admin-Lese-/Übersichts-/Detailpfade u. a. für `app_user`, `mitglied`, `parzelle`, `dokument`, `termin`, `arbeitseinsatz`, `bekanntmachung`, `arbeitsstunden`, `wartungsverträge`, `zaehler` und zugehörige Zuordnungen/Übersichten auf getrennte Produktiv-/Demo-RLS umgestellt.
- `termin` und `bekanntmachung` erhielten echte `is_demo`-Kennzeichnung; `v_startseite_arbeitseinsatz` und `v_pflichtstunden_uebersicht` wurden auf RLS-wirksamen `security_invoker` zurückgeführt.
- Ergebnis: Der vorhandene Demouser kann fachlich als Admin genutzt werden, ohne echte Vereinsdaten aus Adminlisten, Details oder Übersichten offenzulegen.

- Den gemeldeten WPF-Buildrest um `HomeView.xaml` ehrlich gegen den aktuellen Workspace geprüft.
- Befund: Die ungültige WPF-XAML-Eigenschaft `Spacing` war in `HomeView.xaml` im realen Stand bereits nicht mehr vorhanden.
- Der echte verbleibende Buildblocker lag im laufenden DateOnly-Block in `SupabaseService`:
  - fehlender Helper `NormalizeStartseiteArbeitseinsatzRecord(...)`
  - fehlende nullable Überladung von `NormalizeDateOnly(...)`
- Diese beiden Reste wurden minimal ergänzt; `HomeView.xaml` selbst blieb unverändert.
- Ergebnis: `KGV.Wpf/KGV.Wpf.csproj` baut wieder erfolgreich.

## 2026-04-07
- Die Android-/MAUI-System-Zurück-Taste wurde auf dem bestehenden Shell-Modell nachgezogen:
  - tiefe Unterseiten gehen jetzt schrittweise Ebene für Ebene zurück
  - offene Modals werden zuerst geschlossen
  - wenn kein sinnvoller Stack-Rückweg mehr vorhanden ist, erfolgt der Fallback zur Startseite
  - nur auf `home` bleibt das bestehende App-Beenden mit Rückfrage aktiv
- Root-Switch-/Login-Kontexte bleiben zentral geschützt; während aktivem Root-/Mitgliedswechsel wird kein Rücksprung in alte Shell-Kontexte zugelassen.

## 2026-03-22
- Home auf **Übersicht + Admin-Einstiege** ausgerichtet; echte getrennte Verwaltungsviews statt Home-Platzhaltern vorbereitet.
- **Termine**, **Bekanntmachungen** und **Arbeitseinsätze** produktiv an die realen Fachpfade angeschlossen.
- Für Bekanntmachungen wurde ein kleiner **HTML-Editor** eingebaut.
- Für Arbeitseinsätze wurden die Fachregeln um **optionale Teilnehmergrenze** und **optionalen Stundenwert** korrekt nachgezogen.
- **Arbeitsstunden-Freigabe** mit globalem Review-Lock, Prüftabelle und wiederverwendetem Editor aufgebaut.

## 2026-03-23
- Zentralen **Datums-/Zeitbug** der Verwaltungseditoren behoben.
- Begonnenen **Home-/Detail-Block für Arbeitseinsatz** sauber abgeschlossen.
- Produktive **Insert-Pfade** gegen fehlerhafte feste IDs geprüft.
- Zwei echte Fehler im **Arbeitsstunden-Produktivpfad** behoben:
  - fehlerhafte `id = 0` beim Insert
  - Speichern in der Freigabeansicht reagierte nicht sauber auf Checkboxen
- WPF-Bindingfehler in der Arbeitsstunden-Ansicht bereinigt.

## 2026-03-24
- Große **MAUI-Struktur- und Prüfserie** gestartet, um die mobilen Produktivpfade systematisch an den WPF-Fachstand anzugleichen.
- Themenblöcke dabei u. a.:
  - Shell-/Menüstruktur
  - Stammdaten / Gärten / Parzellen
  - Dokumente / Strom / Wasser
  - Mitglied neu / Wartungsverträge
  - Arbeitsstunden
  - Ablesen / RFID / Zählerwechsel
  - Auth-Sonderwege / Dialogersatz
  - Altlasten- und Testpfad-Bereinigung
- Wirkung: viele kleine MAUI-Rückwege, Sichtbarkeiten, Refresh- und Kontextfehler wurden in produktiv nutzbare Bahnen gezogen.

## 2026-03-25
- **Launcher-Icon** in MAUI auf das echte Vereinslogo korrigiert.
- MAUI-Shell-/Menüordnung im Bereich **Admin-Menü / Mein Profil** geglättet.
- **Termine-Nutzerpfad** mobil weiter geschlossen.
- Direkte **Home-Verwaltungszugänge** in MAUI fertiggezogen.
- Artefakt-/FotoUploadTest-Block sowie lokale Bereinigungs-/Archivthemen sauber eingeordnet.

## 2026-03-26
- Größere Serie von **MAUI-Compilefehlern** in vielen Seiten gezielt bereinigt, u. a.:
  - `HomeManagementPage`
  - `MeineDatenPage`
  - `MyProfilePage`
  - `HomeSectionDetailPage`
  - `ParzellenPage`
  - `NebenmitgliedPage`
  - `UserManagementPage`
  - `RfidEinrichtenPage`
  - `MemberGardensPage`
  - `FaelligeZaehlerPage`
  - `ArbeitsstundenReviewDetailPage`
- Zusätzlich aktive Home-/Detail-/Editorpfade fachlich gehärtet.
- Wirkung: MAUI-Gesamtbuild und mehrere produktive Hauptpfade wurden wieder belastbar.

## 2026-03-27
- Projektweite **Prüfung aller Create-Pfade** gegen das ID-freie Insert-Schema; Aufrufer in WPF und MAUI nachgezogen.
- **Startseite, Stammdaten und Flyout-Texte** in MAUI geglättet.
- **Wartungsverträge** fachlich für Haupt- und Nebenmitglieder korrigiert.
- Begonnener **Parzellen-/RFID-Block** real validiert und Restfehler geschlossen.

## 2026-03-28
- **ReleaseManager** und Android-/WPF-Releasepfad deutlich ausgebaut:
  - reales Inno-Setup-Skript
  - Android-Signing mit Laufzeitpasswörtern
  - robuste Versionslesung aus den SDK-Style-`csproj`
- MAUI-Releasepfad für Android stabilisiert:
  - korrekte Icon-/Signing-Basis
  - Release ohne problematischen AOT-Zwang
  - Dateilogging / Logcat-Diagnose für Release verbessert
- Wirkung: reale Release-Erstellung wurde deutlich robuster und nachvollziehbarer.

## 2026-03-29
- **ReleaseManager** um Preflight-/Systemcheck, Commit/Push, Release-Marker, Delta-Export und Versions-Refresh erweitert.
- **Invite-/AuthService-Fix** für FK-Fehler beim Flow `Nutzer hinzufügen`.
- Android-Release-Buildblocker im MAUI-Service minimal behoben.
- MAUI-Mitgliedsdetail gegen den Git-/Repo-Stand erneut verifiziert.

## 2026-03-30
- **OTP-Erstlogin** auf serverseitige Supabase-Edge-Function umgestellt und live verifiziert.
- Problem mit gesperrten Passwortfeldern nach OTP in MAUI behoben.
- MAUI zeigt **OTP-Fehlercode** jetzt supporttauglich sichtbar an.
- Mitgliedskontext in WPF und MAUI stärker auf das **Admin-Menü** ausgerichtet.
- **Impressum-Block** für WPF und MAUI fachlich fertiggezogen.

## 2026-03-31
- Großer **Foto-Upload-Block** produktiv abgeschlossen:
  - lokale Pending-Grundlage
  - WLAN-only-Schalter
  - persistente Pending-Queue
  - Retry-/Sync-Logik
  - produktive Anbindung an Ablesung und Zählereinbau/Erstablesung
- **Wartungsverträge** in Schreibpfaden vollständig auch für Nebenmitglieder geöffnet.
- **Resume-/Timeout-Block** aufgebaut und abgeschlossen:
  - Hintergrundzeit erfassen
  - Login-/Root-Reset nach 15 Minuten
  - Guardrails für mobile Lifecycle-Situationen

## 2026-04-01
- **Dokumente** in WPF und MAUI stark ausgebaut und auf den bestehenden Google-Drive-Unterbau gezogen:
  - UI geglättet
  - Drive-Vertrag nachgeschärft
  - Löschen inkl. Drive-/DB-Abgleich
  - finaler Drive-Rootvertrag
- **Parzellen-/Mitgliedspfad** in MAUI weiter vervollständigt.
- **RFID-Quittungston** minimal ergänzt.
- **Google-Play-Testfähigkeit / Release-Readiness** der MAUI-App geprüft und an Reststellen geschlossen.

## 2026-04-02
- In MAUI öffnet die **Mitgliedersuche** das ausgewählte Mitglied jetzt direkt in **Stammdaten**.
- **Normale Nutzer** wurden im Dokumentbereich klar auf **View-only** begrenzt.
- WPF-Startseite und Verwaltungslisten für **Bekanntmachungen, Termine und Arbeitseinsätze** fachlich geschlossen.
- Kleiner WPF-Restfehler um `SelectedFileName` beseitigt.
- Zusätzlich Save/Rücknavigation/Home-Refresh in den Managementpfaden geglättet.

## 2026-04-04
- **Rechtebasis V1** und benutzerspezifische Fachrechte weiter fertiggezogen.
- Verbleibende direkte Rollenprüfungen im Meterbereich auf **`PermissionChecks`** gezogen.
- **Nutzer-Ablesung als Einreichung** mit Freigabefundament aufgebaut.
- Globaler **Admin-Schalter** für Nutzer-Ablesungen technisch sauber ergänzt.
- Korrektur und Entfernen eingereichter Ablesungen in den Freigabepfad eingebaut.
- **Zählereinbau + Erstablesung** gegen den aktuellen Stand end-to-end validiert.
- Ergebnis: Admin-Menü und Rechteoberfläche tragen nun deutlich mehr echte Fachlogik statt grober Rollenchecks.

## 2026-04-05
- Großer Rollen-/Rechteumbau abgeschlossen:
  - **`app_user.role`** ist jetzt die führende Rollenquelle
  - **`mitglied.role`** bleibt nur noch Altbestand
- Dazu wurden Client-, DTO-, Login-, Rechte- und SQL-/RPC-Pfade bereinigt.
- Feature-Branch **`feature/app-user-role-source`** wurde merge-reif geprüft und anschließend sauber nach **`main`** übernommen.
- Für benutzerspezifische Fachrechte wurden Diagnose- und Stabilitätsfixes ergänzt:
  - WPF-Dateilogging für den Permission-/App-User-Fehlerpfad
  - Predicate-Fix beim `app_user`-Lookup nach `mitglied_id`
  - Typfix im Save-Pfad (`int8` / `long?`)
  - transparenterer Reload-/Verify-Pfad
- Wirkung: Rollen- und Rechteverhalten ist fachlich konsistenter und technisch belastbarer.

## 2026-04-06
- Sehr viele **ToDo-/Restblöcke** zusammengeführt und abgeschlossen.
- Relevante Fachabschlüsse:
  - **Termin/Bekanntmachung löschen** in den echten Managementpfaden sauber geschlossen
  - **Home** lädt mobil beim Anzeigen wieder frisch
  - **Android-Back** zentral auf `home` geglättet
  - WPF-Bindingwarnung `MemberDTO.Name` beseitigt
  - Rechte-Reload in WPF/MAUI transparenter gemacht
  - `mitglied.role` im Code klar als Legacy markiert; Eigenkontext-/Globalrechte geglättet
  - mobile Arbeitsstunden-Prüfung an den bestehenden globalen **Review-Lock** angeglichen
  - sichtbaren Diagnosepfad aus `Ablesen` entfernt
  - WPF-Sichtbarkeiten in `Ablesen` sauber nachgezogen
  - sinnvolle Unterschiede aus lokalem Vergleichsstand zurück in `main` übernommen
  - Dokumente im Parzellen-/Mitgliedskontext weiter auf **view-only/Eigenkontext** geglättet
  - stale Detailansicht in `MemberParzellenDetailPage` beseitigt
- Mitglieder-/Nebenmitglied-Block stark erweitert:
  - `Mitglied neu anlegen` in WPF und MAUI
  - direkte Nachfrage nach Hauptmitglied-Neuanlage, ob ein **Nebenmitglied** angelegt werden soll
  - `Nebenmitglied`-Seite mit **Neu**-Einstieg
  - Reload/Pull-to-Refresh der Mitgliedersuche
  - `Mitgliedschaft beenden` mit Folgeentscheid für vorhandenes Nebenmitglied
  - Admin-Shell-Mitgliedskontext gehärtet
  - Nebenmitglied-Create fachlich vervollständigt
- Release-/Diagnosepfad weiter ausgebaut:
  - Google-Play-Diagnoseartefakte
  - Android-Upload-Ordner für manuelle Releases
- Wirkung: Viele zuvor verstreute Restpunkte wurden auf produktive Endpfade gezogen.

## 2026-04-07
- **Nebenmitglied-Bearbeiten** gegen `origin/main` geprüft, vorhandenen Rettungsstand fachlich bestätigt und validiert.
- MAUI-Nebenmitglied-Maske um die vorbereiteten Zusatzfelder vervollständigt.
- Android-Startup-/Normalstart-Problem intensiv eingegrenzt und zusammengeführt:
  - frühe Shell-/VisualElement-/Root-Pfade mehrfach entschärft
  - Diagnosepfade verbessert
  - frühe Probezugriffe entfernt
  - Root-/Loaded-Verzögerungen gegen stabile Zustände zurückgeführt
  - Logging für `InnerException` und frühe Android-Pfade gehärtet
  - Log zusätzlich auf extern lesbaren Android-Pfad gespiegelt
- Entscheidendster Befund:
  - der reale Startup-Crash passte zum **R8/ProGuard-Packagingpfad**
  - `AndroidLinkTool=r8` und `AndroidEnableProguard=true` wurden aus dem Releasepfad wieder entfernt
  - die zuvor fehlende Klasse `com.microsoft.maui.PlatformDispatcher` war danach wieder im finalen APK enthalten
- Ehrlicher Reststand nach diesem Tag:
  - der Codepfad wurde auf einen stabileren Zustand zurückgeführt
  - der unmittelbare reale Gerätetest des finalen Startup-Fixes blieb als externer Nachtest offen

## Wichtig für die weitere Nutzung dieses Logs

Diese kompakte Fassung ersetzt **nicht** jedes einzelne Diagnose- oder Prüfdetail der Langfassung.
Sie ist dafür gedacht, den Projektstand schnell zu verstehen, ohne sich durch hunderte kleinteilige Zwischenblöcke zu arbeiten.

Für die tägliche Arbeit reicht diese Fassung in der Regel aus, weil sie die folgenden Informationen beibehält:
- welche großen Blöcke erledigt wurden
- welche Architekturentscheidungen getroffen wurden
- welche Fachpfade produktiv sind
- welche wichtigen Resttests oder Grenzen noch existieren
## 2026-09-23 - MAUI: Vereins-ID als Zugangsvoraussetzung

- Die Anmeldung wurde auf einen vorgeschalteten Vereinskontext vorbereitet: Vereins-ID wird über das zentrale Register per eingeschränkter RPC aufgelöst; erst dann wird der normale Login aufgebaut.
- Der gespeicherte Kontext enthält nur Vereinskennung, Anzeigeinformationen und die für den jeweiligen Supabase-Client erforderliche URL samt Publishable Key. Rollen, Tokens und Mitgliedsdaten bleiben weiterhin ausschließlich im Zielverein.
- WPF bleibt auf ausdrückliche Vorgabe unverändert. Der nächste technische Block umfasst Wechsel/Abmeldung und Biometrie, bevor ein produktiver Rollout erfolgen kann.
## 2026-09-23 - Neutrales App-Logo und Demo-Stempel

- Für die vereinsneutrale App-Identität wurden ein neutrales Garten-/Parzellen-Logo und eine dazugehörige, diagonal gestempelte Demo-Variante erzeugt und als MAUI-Bildressourcen aufgenommen.
- Vor der Vereinsauflösung bleibt die App neutral. Nach erfolgreicher Zuordnung wird für KGV-DEMO sichtbar die DEMO-Variante verwendet; der bekannte Produktionsverein verwendet weiterhin sein vorhandenes Wappen.
## 2026-09-23 - Sicherer Vereinswechsel in MAUI

- Der Wechsel wird nicht innerhalb eines weiterlaufenden Client-Graphen erzwungen. Stattdessen erfolgt Abmeldung, vollständige Bereinigung lokaler Benutzer- und Vereinsdaten und ein kontrolliertes Beenden der App.
- Nach dem Öffnen startet die App ohne Vereinskontext wieder bei der Vereins-ID-Eingabe. Damit werden pro Verein getrennte Auth- und Supabase-Clients garantiert.

## 2026-09-23 - Erstlogin ohne Edge Function

- Der Aufruf `RequestOtpAsync` verwendet wieder den vorhandenen direkten Supabase-Recovery-Pfad `RequestRecoveryOtpAsync`.
- Dadurch greift der bereits etablierte Auth-E-Mail-Template-Ablauf mit `{{ .Token }}` wieder unmittelbar: Code anfordern, Code in der App bestätigen, Passwort vergeben.
- Die App ist für den Erstlogin damit nicht mehr von Deployment, JWT-Konfiguration oder Secrets einer Edge Function abhängig. Die Funktion wird nicht gelöscht, damit ein erfolgreicher Gerätecheck zuerst ohne riskante Datenbankänderung erfolgen kann.

## 2026-10-11 - WEB-F1 Saison-Kontext

- Der durchgehende Saison-Datenfluss wurde im Web geprüft: `SeasonPicker`, `WorkspaceContext` samt Persistierung, Seiteneinstieg und die bestehenden Mitglieds-/Startseitenpfade arbeiten bereits mit der zentralen Auswahl.
- Die tatsächliche Lücke lag in der globalen Arbeitsstunden-Freigabe. Ihre Repository-Abfrage hatte keinen `saison_id`-Filter und reagierte deshalb auch nicht auf einen Saisonwechsel. Der Kontext wird nun als `saisonId` übergeben, der Filter gesetzt und der Ladezustand bei Wechsel zurückgesetzt.
- Exportfilter mit Jahresbezug nutzen für ihre Vorbelegung jetzt die ausgewählte Workspace-Saison statt `new Date().getFullYear()`; beim Wechsel werden Filteroptionen und Ergebnisvorschau erneuert.
- Es wurden keine parallelen Saison-States eingeführt und keine datumsgesteuerten Zähler-/Arbeitseinsatzpfade künstlich saisongefiltert.

## 2026-10-11 - WEB-F1 Start-Saison

- Die persistierte Workspace-Saison hatte beim App-Start bisher Vorrang. Damit konnte eine früher manuell gewählte alte Saison zum Startstandard werden.
- `selectInitialSeasonFromList(...)` wählt jetzt zuerst die Saison des aktuellen Kalenderjahres. Existiert sie nicht, greift der bestehende Fallback bewusst auf die persistierte und danach auf die erste verfügbare Saison zurück.
- `WorkspaceContext` und die bestehende Speicherung bleiben unverändert: Während der Sitzung setzt eine manuelle Auswahl `saisonId` und `saisonJahr`; nach dem nächsten Start überschreibt die Initialisierung beides mit der aktuellen Saison, ohne Mitglieds- oder Parzellenkontext zu verändern.

## 2026-10-11 - WEB-F2 Impressum

- MAUI bleibt der fachliche Referenzpfad: Es lädt die Impressums-Funktionsslots, löst eine belegte `mitglied_id` über die Mitgliederliste auf, trennt nach Slot/Funktion Vorstand und Bauausschuss und blendet den ersten Vorsitzenden aus dem dynamischen Kontaktbereich aus.
- Der Web-Service und die UI setzen denselben Auflösungspfad bereits korrekt um. Die Regression lag in der vorgelagerten gemeinsamen REST-Abfrage: `impressum_funktion_slot` war fälschlich als Tabelle mit direktem `is_demo`-Filter geführt, obwohl die Tabelle diese Spalte nicht besitzt.
- Das Datenbank-RLS bestimmt den Scope eines Slots absichtlich über `is_demo_impressum_slot_scope(mitglied_id)`. Nach Entfernen des falschen Clientfilters kommen Slots wieder zur Service-Auflösung; `mitglied` wird weiterhin über sein echtes `is_demo`-Feld kontextgerecht geladen. Weder feste Vereins-/Verantwortlichendaten noch Datenschutz oder sonstige Web-Fehler wurden verändert.

## 2026-10-11 - WEB G7 Mitglieds-Arbeitsstunden

- Betroffene Seite ist `Mitglied → Arbeitsstunden` (`MemberWorkHours`), nicht die globale `WorkHoursReview`-Freigabe. Der bestehende Pfad reicht das ausgewählte Mitglied und `workspaceContext.saisonId` unverändert über Service und Repository weiter.
- Ursache war der ungültige clientseitige `is_demo`-Filter: Datenbankschema und beide Typdefinitionen weisen für `arbeitsstunde` keine eigene `is_demo`-Spalte aus, die Tabelle war dennoch in `demoScopedTables` geführt. `readSupabase()` ergänzte dadurch einen nicht unterstützten REST-Filter.
- Ausschließlich `arbeitsstunde` wurde aus der clientseitigen Demo-Scope-Liste entfernt. Die Demo-Grenze bleibt in der vorhandenen RLS über Eigenkontext beziehungsweise `is_demo_mitglied_id(mitglied_id)` wirksam. Mitglieds-/Saisonfilter, Pflichtstunden-View und WEB-F1-Reviewlogik blieben unangetastet.
- Ein Regressionstest prüft den tatsächlichen Service-/Repository-Request mit `mitglied_id` und `saison_id`, das Fehlen des clientseitigen `is_demo`-Filters sowie den fortbestehenden `is_demo`-Filter für eine Tabelle mit eigener Spalte.
- Separate technische Auffälligkeit für Folgearbeit festgehalten: G7/G8-Triggerfunktionen referenzieren `NEW.is_demo`/`OLD.is_demo`, obwohl diese Spalte in `arbeitsstunde` fehlt. Ausführung betroffener UPDATE-Triggerpfade kann deshalb scheitern; in diesem Block wurde keine Migration geändert.

## 2026-10-11 - G7/G8 Arbeitsstunden-UPDATE-Trigger

- Die Migrationshistorie weist keine `arbeitsstunde.is_demo`-Spalte aus. Letzte Definition von `public.enforce_own_open_arbeitsstunde_update()` war die G8-Fassung aus `20261009220000_g8_work_assignment_work_hours.sql`; spätere Migrationen korrigierten die Funktion nicht.
- Eine neue Forward-Migration `20261011114000_g7_fix_work_hour_update_trigger.sql` ersetzt nur diese Funktion und entfernt ausschließlich den ungültigen Vergleich `new.is_demo is distinct from old.is_demo`. Eigenmitglied-/Statusprüfungen, unveränderliche Mitglieds-/Saison- und Anmeldungsfelder, G7-Review-Sonderpfad, festes Einsatzdatum, Browser-Lock und bestehende Fehlertexte/SQLSTATEs bleiben erhalten.
- Der bestehende BEFORE UPDATE Trigger verweist bereits auf dieselbe Funktionssignatur; `CREATE OR REPLACE FUNCTION` aktualisiert den Funktionskörper, ohne eine DROP/CREATE-Kette für den Trigger zu benötigen.
- Kein `is_demo`-Feld, keine RLS- oder G8-Fachlogikänderung. Demo-Scope bleibt über Mitglieds-ID und vorhandene RLS-Funktionen. Der Migrationstest vergleicht die Forward-Funktion mit der letzten G8-Definition minus genau dem ungültigen Feldvergleich.

## 2026-10-11 - WEB G10 Mitgliedsdokumente

- Die Mitgliedsdokumentenseite verwendet unverändert den Owner `memberDocumentOwner(selectedMemberId)` und lädt über den G10-Service/das Repository nach `mitglied_id` sowie `archiviert_at is null`.
- Ursache des Ladefehlers war der generische clientseitige `is_demo`-Filter für `dokument`. Schema und Datenbanktypen führen kein `dokument.is_demo`; die RLS bestimmt den erlaubten Scope über Eigentümer (`mitglied_id` beziehungsweise aktive Belegung zur `parzelle_id`) und `is_demo_dokument_scope(...)`.
- Ausschließlich `dokument` wurde aus `demoScopedTables` entfernt. RLS, Archivfilter, Service-/Repositorypfad und getrennte Mitglieds-/Parzellen-Owner bleiben erhalten. Pachtverträge werden fachlich als Parzellendokumente gespeichert und nicht in die Mitgliedsliste gemischt.
- Regressionstest sichert tatsächliche G10-Abfragefilter, leere Ergebnisse ohne Shadow-Logik, Ausschluss archivierter Dokumente, getrennte Parzellenabfragen sowie fortbestehenden Demo-Filter für `mitglied`.

## 2026-10-13 – WEB G15.6b Supabase-Transport-Caller

- Auf dem bestehenden G15-Branch wurde die Caller-Migration von der G15.6a-Übergangsfassade zu den technischen Supabase-Clients abgeschlossen. Repositories, Services und Features reichen `ClubContext` aus dem vorhandenen `AuthProvider` explizit zusammen mit `BrowserSession` bis zu REST-, RPC-, Edge- und Storage-Aufrufen durch; keine globale Clubvariable oder erneute Storage-Auflösung wurde eingeführt.
- `legacyTransportContext`, App-User-RPC und generische Transportfunktionen sind aus `supabase-auth.ts` entfernt. Session-, Club- und Workspace-Persistenz sowie G1-nahe Auth-Helfer bleiben dort. `loadAppUserContext` und App-User-Aktionen liegen im User-Service; Vertragsgenerierung, Fotos und Lock-Orchestrierung in den vorhandenen Fach-/Concurrency-Bereichen. Demo-Filter, RLS-Verhalten und Edit-Lock-Semantik bleiben unverändert.
- G15-Transport-Contract und Dokument-/Arbeitsstunden-Daten-Scope-Tests verwenden den expliziten Kontext. G15.6 ist in README und Web/WPF/MAUI-Vergleich dokumentiert; G15.7 bleibt unbegonnen.
- Fokussierte Transport-/OTP-/Daten-Scope-Tests bestehen mit 8/8. Die gesamte Web-Testauswahl besteht 30/32; die beiden Fehlschläge sind nachweislich bereits auf HEAD vorhanden und erwarten einen alten Export-Rechteausdruck beziehungsweise ein altes `activeId`-Routing.
- Produktionsbuild erfolgreich. TypeScript meldet unverändert sechs HEAD-baseline-Fehler (fünf Reading-Ergebnisfehler in `app/page.tsx`, einer in `services/seasons/season-service.ts`). ESLint auf 97 geänderten TS/TSX-Dateien entspricht exakt der HEAD-Baseline mit 14 Fehlern und 15 Warnungen; keine fehlenden `club`-Hookabhängigkeiten. `git diff --check` sauber.

## 2026-10-13 – G15.7 Abschlussprüfung Browser- und Systemfunktionen

- Der Startstand `9673df34493df5922da1dd262ec89a295f292a59` war auf dem vorhandenen Branch sauber und remote-synchron. PWA, Connectivity, Browser-Storage, Scanner, Web-/MAUI-Pending-Media, Lockvertrag und -Lifecycle, expliziter Supabase-Transport sowie G1/G6/G10/G13-Regressionen wurden geprüft; es erfolgte keine DB-Anwendung.
- G15.3 enthielt einen echten Fallback-Fehler: bei fehlendem oder nicht konstruierbarem `BroadcastChannel` blieben duplizierte `sessionStorage`-Client-IDs ungeprüft. Ein `storage`-Event-Handshake in `KGV.Web/lib/browser/client-instance.ts` übernimmt jetzt Erkennung und Antworten; ein neuer Contract-Test deckt beides ab. Der fehlende MAUI-Namespace für den G15.3-`IDispatcherTimer` wurde ergänzt. Zusätzlich konnte der MAUI-Editor nach einem Save-Versuch Eingaben unabhängig vom Lockstatus reaktivieren und erneuerte den Lock nicht während laufender Saves; Save-Guard, lockgebundene Aktivierung und fortgesetzter Lease-Refresh beheben dies. Ein Lifecycle-Contract deckt den Randfall ab.
- Web-Contracts/Testauswahl: 34/36 bestanden; dieselben zwei bekannten, nicht durch G15 verursachten Fehler wie zuvor (alte Export-Capability-Erwartung sowie altes Parcel-Reading-Routing). TypeScript weiterhin genau sechs Baselinefehler (fünf in `app/page.tsx`, einer im Season-Service). Web-Produktionsbuild erfolgreich. Geänderte G15-Dateien sind ESLint-fehlerfrei; der Voll-Lauf meldet 15 Fehler/15 Warnungen, ausschließlich vorhandene Hooks-/Lint-Baselines.
- `KGV.Core` und `KGV.Infrastructure` bauen ohne Compilerfehler. `KGV.Maui` stoppt nach dem G15-Importfix an sieben Fehlern in unveränderten, fachfremden Seiten (`LineBreakMode`, `Launcher`, `FileSystem`, `Share`, `ShareFileRequest`, `ShareFile`). WPF stoppt an sieben bestehenden, hier nicht geänderten Factory-/Arbeitsstunden-/Mitglied-Aufrufinkonsistenzen. Bekannte ImageSharp-Paketwarnungen bleiben unverändert; keine davon wurde durch Versionsänderungen behandelt.
- Das Architekturvergleichsdokument markiert G15.1–G15.7 als abgeschlossen und dokumentiert Baselines sowie Browser-API-Grenzen. `4b6e097` bleibt in der Historie. Web bleibt ohne versteckte Clubauflösung; kein G15-Technikpfad in `page.tsx`, keine doppelte Browser-API-Implementierung und keine DB-Änderung.

## 2026-10-11 - WEB G1-G11 technischer Regression-Audit

- Vollständige Restliste der clientseitig demo-gefilterten Tabellen gegen beide `database.types.ts`-Dateien, den Schemaexport und Migrationen geprüft. Beibehalten wurden ausschließlich Tabellen mit eigener Spalte: `arbeitseinsatz`, `mitglied`, `parzelle`, `wartungsvertraege`, `termin`, `bekanntmachung`. `termin.is_demo` und `bekanntmachung.is_demo` werden durch die Demo-Migration ergänzt, fehlen aber noch in beiden Typ-Exports.
- Entfernt wurden `app_user`, `mitglied_gesetzlicher_vertreter`, `mitglied_saison`, `saison`, `vereinskonfiguration`, `wartungsvertrag_zuordnungen`, `zaehler` und `zaehler_ablesung`, da keine eigene `is_demo`-Spalte vorliegt. Vorhandene RLS-Relationsprüfungen über Mitglied, Wartungsvertrag, Zähler und Parzelle bleiben unangetastet; Saison und Vereinskonfiguration haben keinen eigenen Demo-Datensatzscope.
- `loadAppUserContext` hatte neben dem generischen Fehler auch `is_demo` explizit selektiert, obwohl `app_user` nur `is_demo_account` besitzt. Auswahl und Ableitung nutzen nun ausschließlich die vorhandene Spalte; die RLS-Funktion `is_demo_app_user_row(...)` bleibt maßgeblich.
- Der neue Schema-Regressionstest prüft den vollständigen Whitelist-Umfang, beide Typ-Exports, die additive Spaltenmigration, konkrete `readSupabase`-Filter sowie den App-User-Auth-Request.
- Navigation/Service/Repository-Pfade für F1-F5 wurden überprüft; keine G12-G15-Dateien oder Fachregeln geändert. Die ToDo-Erledigungsstände widersprechen dem Audit nicht.

## 2026-10-13 – S1.1 Web-Test- und TypeScript-Baseline

- `main` startete sauber und remote-synchron bei `eb8aa33c7fc801b03f439f319869a9638b621c17`; Arbeitsbranch ist `fix/web-baseline-cleanup`.
- Reproduktion auf dem vorgesehenen `.test.mjs`-Umfang: 34/36 Tests. Die Export-Contract-Prüfung verwendete einen alten `searchMembers`-Ausdruck, während Navigation und ExportCenter bereits die fachlich gültige, in `canExportData` definierte Rollenfreigabe verwenden: Administratoren und Vorstand, nicht normale Nutzer. Die Capability- und G14-Contracts sichern bestehende Rollenfreigabe und serverseitige Exportgrenze; der Test prüft nun diesen produktiven Capability-Pfad.
- Der Parzellenhistorien-Contract suchte den Case-Sensitive-Text `activeId`, obwohl die Workspace-Auswahl bereits über `effectiveActiveId` einschließlich erlaubter Navigation und Fallback läuft. Der angepasste Test prüft diesen Pfad, die Übergabe der gewählten `selectedParcelId` sowie, dass die Aktion im Mitglieds-Gartenbereich bleibt.
- Fünf `page.tsx`-Diagnosen betrafen die erfolgreiche Rückgabe nach bestätigter Plausibilitätswarnung (`id`, `reading`, `isSubmission`). Drei Service-Overloadpaare modellieren nun präzise, dass `confirmWarnings: true` einen erfolgreichen Rückgabetyp liefert; die normale Rückgabe behält weiterhin `ReadingConfirmationRequired` als Discriminated-Union-Arm. `page.tsx` blieb unangetastet.
- Der sechste TypeScript-Fehler: `Boolean(season)` verengt `Season | null | undefined` nicht für `seasonYear`. Die explizite `null`-/`undefined`-Rückgabe behebt nur die Typverengung; Regeln für Jahr-ID, Vorjahreswerte und Schreibsperre vergangener Saisons wurden nicht geändert.
- Ergebnis: 36/36 ausgewählte Web-Tests, `npm exec tsc -- --noEmit` ohne Fehler und `npm run build` erfolgreich. Keine ESLint-, MAUI-/WPF-, DB-/Migrations- oder Fachregeländerungen.

## 2026-10-13 – S1.2 Web-ESLint-Baseline

- Ausgangs-HEAD `98ca30c12a034cc8369e0944ce464ca7fd80c3f2` war sauber und identisch zum Remote-Branch `fix/web-baseline-cleanup`; kein neuer Branch wurde angelegt.
- ESLint-Ausgang: 15 Fehler/15 Warnungen. 14 `react-hooks/set-state-in-effect` in `app/page.tsx` (6), `app/parcel-workspaces.tsx` (4), `ClubConfiguration.tsx`, `LeaseContractFlow.tsx`, `ImprintPage.tsx` und `MemberWorkHours.tsx` (je 1); zusätzlich `@next/next/no-assign-module-variable` in `security/capabilities.test.mjs`. Die 15 Warnungen waren zehn `react-hooks/exhaustive-deps`, zwei `@next/next/no-img-element` (Ableseformular und Pending-Foto-Vorschau) und drei `@typescript-eslint/no-unused-vars` (Zeitformatierungsfunktion, Konfigurations-Payload-ID, Impressumsmarker).
- Änderungen: Capability-Test-Variable zu `compiledModule` umbenannt; Navigation aus vorhandenem Zustand und Berechtigungen abgeleitet; synchrones Setzen von Lade-/Derived-State in Effects entfernt. Daten bleiben asynchron geladen und spätere Antworten werden bei Kontextwechseln verworfen. Editoren erhalten beim sachlich relevanten Verein-/Benutzer-/Mitglied-/Saison-/Parzellenwechsel einen Remount-Key, wodurch Drafts nicht in fremde Kontexte übernommen werden. Edit-Lock-Meldung ist abgeleitet; lokale Blob-Vorschauen nutzen `next/image` mit `unoptimized`; ungenutzte Werte werden ohne Regelunterdrückung entfernt. Der MemberWorkHours-Contract sichert den vollständigen Kontext-Key ab.
- Keine Regeln global abgeschaltet, keine Disable-Kommentare, keine Fehlerunterdrückung, keine Fachregel oder neue Architektur. DB/Migrationen, MAUI und WPF blieben unverändert.
- Abschlussgates: `npm run lint` ohne Meldungen (0 Fehler/0 Warnungen), `npm exec tsc -- --noEmit` erfolgreich, `node --test "**/*.test.mjs"` 36/36, `npm run build` erfolgreich. Der Build meldet nur bestehende nicht-blockierende Chunkgrößen- und Routenerkennungshinweise.

## 2026-10-13 – S2 MAUI-Build-Baseline

- Reproduzierte Ausgangslage: `dotnet build KGV.Maui/KGV.Maui.csproj` lieferte sieben Fehler. `ArbeitseinsatzTeilnehmerPage.cs:21` meldete CS0103 für `LineBreakMode`; `HomeSectionDetailPage.cs:653,675,677,677,680` meldete CS0103/CS0246 für `Launcher`, `FileSystem`, `Share`, `ShareFileRequest` und `ShareFile`; `BekanntmachungEditorPage.cs:499` meldete CS0103 für `Launcher`.
- Analyse: Bei MAUI 10 liegen die verwendeten APIs weiterhin in den bestehenden Cross-Platform-APIs, jedoch in getrennten Namespacebereichen. `LineBreakMode` benötigt `Microsoft.Maui`; `Launcher` `Microsoft.Maui.ApplicationModel`; `FileSystem` `Microsoft.Maui.Storage`; Share-Typen `Microsoft.Maui.ApplicationModel.DataTransfer`. Es handelte sich daher um fehlende Imports, nicht um eine Signatur- oder Plattformänderung.
- Es wurden ausschließlich diese Imports in den drei betroffenen MAUI-Dateien ergänzt. Die UI-Darstellung, Linköffnung, Kalenderdateierzeugung und Dateifreigabe sind funktional unverändert; insbesondere keine Plattformsonderimplementierung, keine Hardcodes und keine Deaktivierung.
- Finaler MAUI-Build (`--no-restore`) erfolgreich mit 0 Fehlern. Bekannte Warnungen bleiben außerhalb des S2-Scopes: ImageSharp 2.1.13 (NU1902/NU1903, fünf Advisory-IDs) und bestehende Nullbarkeitswarnungen, darunter CS8625 in `VereinskonfigurationPage.cs`. Keine Web-/WPF-/DB-/Migrationsdateien und keine Paket- oder Frameworkversionen geändert.

## 2026-10-13 – MAUI UserContext RPC-Ergebnismenge

- Ursache des Loginfehlers: PostgreSQL-RPC `current_app_user_context` ist als `returns table(...)` definiert, daher liefert PostgREST ein JSON-Array. `UserContextService` deserialisierte zuvor direkt in ein einzelnes `AppUserRecord`; dies erzeugte die JsonSerializationException beim erfolgreichen Supabase-Login.
- Die RPC wird jetzt als `AppUserRecord[]` verarbeitet. Bei null beziehungsweise 0 Datensätzen wird der Zugriff wie bisher mit `KgvAccessDeniedException` verweigert. Exakt ein Datensatz wird verwendet. Bei mehr als einem Datensatz protokolliert der Service die inkonsistente Ergebnismenge und verweigert ebenfalls den Zugriff; es wird kein beliebiger Kontext ausgewählt.
- Die nachgelagerte Identitäts-, Mitglieds-, Rollen- und Permission-Prüfung bleibt identisch. Keine SQL-Funktion, Migration, Web-/WPF-Datei oder Auth-/Security-Regel wurde geändert.

## 2026-10-15 – Export-PDF-Build und Web-Versionsanzeige

- Prüfung auf `feature/pdf-template-provider`: `GenerateExportPdfAsync(CentralExportRequest request)` enthält bereits die kollisionsfreie lokale Variable `httpRequest`. Der Request-Body bleibt `JsonSerializer.Serialize(request)`; Authorization-, API-Key- und Accept-Header sowie `SendAsync` beziehen sich auf `httpRequest`. Es war keine Änderung am Export-Contract erforderlich.
- Die beiden Web-Verwendungen der Versionsanzeige beziehen ihren Wert nun über `useAppVersion`. Der zentrale Client-Loader fordert `/release-info.json` mit `cache: "no-store"` an, liest explizit das vollständige Feld `displayVersion` und verwendet bei HTTP-/JSON-/Inhaltsfehlern den neutralen Fallback „Version unbekannt“. Der Fehler wird protokolliert; die App bleibt nutzbar.
- `dotnet build KGV.Infrastructure`, `dotnet build KGV.Maui/KGV.Maui.csproj -f net10.0-android` und `npm run build` in `KGV.Web` erfolgreich; `git diff --check` ohne Befund. Gemeldet wurden bestehende NuGet-/Nullbarkeits- und Web-Chunk-Warnungen.

## 2026-10-23 – Arbeitsstunden-PDF-Datenisolation

- Exportstrecke geprüft: MAUI `ExportPage` → `ExportViewModel.BuildCentralExportRequest` → `SupabaseService.GenerateExportPdfAsync` → `kgv-generate-export-pdf` → `rpc_execute_export` → Metadaten-/Spaltenabfrage und PDF-Renderer. Web-PDFs verwenden dieselbe Edge Function. Die fachliche Arbeitsstunden-RPC und alle bestehenden Soll-, Geleistet-, WV-Gutschrift-, Offen- und Statusberechnungen blieben unangetastet.
- Das ausdrücklich angegebene Referenz-PDF (SHA-256 `5bae8ed1...`) wurde lokal gerendert und seitenweise technisch geprüft: drei Seiten mit 25/24/14 Arbeitsstunden-Zusammenfassungen und jeweils denselben 15 Zählerzeilen. Diese Zählerdaten einschließlich „Zähler - Eichfälligkeit“, Header und Stand 14.10.2026 sind identisch mit dem Inhalt von `export-allgemein/v1/template.pdf`; diese Datei ist bytegleich zur Querformat-Eichfälligkeitsvorlage. Ursache ist folglich das Zählerdaten enthaltende Hintergrund-PDF, das die gemeinsame Funktion `buildTemplateTablePdf` bei jedem Seitenstart als Hintergrund dupliziert – nicht eine gemeinsame RPC-Collection oder Zählerzeilen aus `rpc_export_arbeitsstunden_uebersicht`.
- Eine neue saubere `export-allgemein/v2`-PDF-Hintergrundvorlage mit Vereinslogo/-kopf und leerem Inhaltsbereich wurde erstellt; Field-Spec und Manifest bleiben inhaltlich kompatibel, der Katalog erhält eine versionierte neue Veröffentlichung. Die v1 wird nicht überschrieben. Da eine Template-Korrektur allein keine falsche Datenquelle erkennen könnte, bleiben auch die festen Quell-/Schema-Verträge für Arbeitsstunden und Eichfälligkeit sowie die Projektion auf ausgewählte Spalten bestehen. Tabellenköpfe wiederholen sich weiterhin; Zeilen, die nicht komplett in den Tabellenbereich passen, werden nun abgewiesen.
- Vier Regressionstests bestanden (v2-Wechsel/Hash, Quellen-/Feldisolation beider Reports, Projektion allgemeiner Exporte). Ein synthetischer dreiseitiger Renderer-Prüflauf mit 63 Stundenzeilen auf v2 enthält ausschließlich den Arbeitsstundentitel und die 63 Stundenzeilen; keine Zählerfelder oder -werte. Read-only-Abfrage des verknüpften Projekts bestätigt, dass derzeit v1 mit dem bekannten fehlerhaften SHA-256 aktiv ist. Der vorgeschriebene `db push --linked --dry-run` führt zusätzlich sieben andere lokale, noch nicht angewandte Dokument-/Mitglieds-/Wartungsvertrag-Migrationen auf. Weder diese Migrationen noch v2 wurden gepusht oder live geschaltet. Deshalb gab es noch keinen neuen Live-Export.
- Der vorgeschriebene MAUI-Build wurde ausgeführt, scheitert mit 42 vorhandenen Fehlern in blockfremden, uncommitteten MAUI-Dateien. Gemeinsame PDF-Infrastruktur ist betroffen; andere Exporte erhalten jetzt nur ausgewählte Spalten. Ihre fachlich eigenständigen Quellen bleiben unverändert.
- Rolloutprüfung: Die Edge Function kann mit `supabase functions deploy kgv-generate-export-pdf` unabhängig von `db push` veröffentlicht werden, aber der Funktionsdeploy allein beseitigt den Fehler nicht: Der aktive Katalog verweist weiterhin auf die v1-Hintergrundvorlage mit Zählerdaten. Die notwendige `20261023123000_publish_export_allgemein_v2.sql` prüft die zuvor hochgeladenen v2-Storage-Objekte, deaktiviert die aktive `export-allgemein`-Version und trägt v2 mit den geprüften Hashes als aktiv ein.
- Die verknüpfte Migrationstabelle weist vor dieser PDF-Migration sieben ausstehende frühere Migrationen aus: `20261022150000_alt3_dokumenttypen_foundation.sql`, `20261022151000_alt3_dokument_metadaten.sql`, `20261022152000_alt3_register_historisches_dokument.sql`, `20261022153000_alt3_relational_document_proof_statuses.sql`, `20261022154000_alt3_save_dokumenttyp.sql`, `20261022160000_vor3_mitgliedsaufnahme_vorstand.sql` und `20261023110000_wv3_wartungsvertrag_folgeentscheidungen.sql`. Supabase CLI `db push` nimmt diese ebenfalls auf; es gibt keinen offiziellen gezielten Einzelmigrations-Deploy. Um Reihenfolge und Historie nicht zu umgehen, wurden nach Anweisung weder Funktion noch Storage-Asset noch Migration ausgerollt und keine Historie verändert. Sicherer nächster Schritt: die sieben Migrationen separat fachlich prüfen/freigeben und normal anwenden; danach den PDF-Storage-Upload, die einzelne verbleibende PDF-Migration per regulärem `db push` und den Funktionsdeploy ausführen und den Live-Export seitenweise prüfen.

## 2026-10-25 – NB-2A-M5A Mitgliedssuche und einheitliche MAUI-Mitgliedslabels

- Ausgangsstand `b518219442509d689bd2ec977cdd808642e25205` auf `main` vor Änderungen geprüft; nach `fetch origin main` war der Branch sauber und ohne Divergenz.
- `MemberSearchViewModel` verwendet das kanonische `MitgliedRecord.Mitgliedsnummer`: Titel `<Name> · Mgl.-Nr. <nummer>`, ohne Namen `Mgl.-Nr. <nummer>`, E-Mail als ergänzenden Untertitel. Das ausgewählte `MemberDTO` übernimmt die Nummer. `MemberId` bleibt der technische Auswahl-/Navigationsschlüssel.
- Arbeitseinsatz-Teilnehmerverwaltung und die Teilnehmerliste im `HomeSectionDetailPage`, Vereinsfunktions-Auswahl/Besetzungen/Vertretungen sowie die Mitgliedsreferenz in Kommunikationsvorgängen zeigen den Standard. Die Abschlussprüfung fand im Home-Detail-Servicepfad noch eine Name-only-Anzeige mit technischem ID-Fallback; auch dieser Pfad verwendet jetzt die kanonische Nummer und einen neutralen „Mitglied unbekannt“-Fallback. Die digitale Mitgliedsdokument-Versandbestätigung in `MemberDetailPage` und service-seitige Wartungsvertragszuordnungen zeigten ebenfalls noch unvollständige Mitgliedslabels und verwenden jetzt den Standard ohne technischen Fallback. Beide Teilnehmer-Servicepfade verwenden je eine gebündelte Mitgliederliste ohne Einzelabruf je Teilnehmer. Statuswerte, Member-/Besetzungs-/Vorgangs-IDs, FK-/Serviceaufrufe und Signaturlogik bleiben unverändert.
- Zwei direkt passende interne Mitgliedsansichten ergänzt: Zielauswahl und Bestätigung der individuellen Rechnungszuordnung sowie Mitgliedslabel im Dokumentversand-Bestätigungsdialog. Keine Änderung am öffentlichen `ImpressumPage` oder an Funktions-/Vertragsunterzeichnern. Mitgliedsnummern `<= 0` zeigen ausdrücklich „Mitgliedsnummer nicht verfügbar“, nie eine technische ID.
- Audit aktualisiert: verbindliche Name-only-Entscheidung dokumentiert, NB-2A-M5 in M5A (**erledigt**) und M5B (**offen, nicht begonnen**) geteilt. Gezielt verbleibender Resttreffer für M5B: `AblesungenFreigabePage`/`AblesungReviewItem.MitgliedDisplay`; hierfür ist eine gebündelte `SupabaseService`-Projektion über `Mitgliedsnummer` erforderlich. Vertrags-/Funktionsunterzeichner in `ParzellenProtokollePage` bleiben ausdrücklich ausgenommen.
- Tests: 17 fokussierte Contract-Tests bestanden; `git diff --check` fehlerfrei. Infrastructure-Build erfolgreich (0 Fehler; vorhandene NuGet-/Nullbarkeitswarnungen). Android-Build bis zum Compiler, dann 11 blockfremde C#-Fehler in `HistoricalDocumentImportPage.cs`, `PreparedMembershipApplicationPreviewFlow.cs`, `PachtvertragFlowHelper.cs`, `MembershipBoardReviewPage.cs` und `MemberDetailPage.cs`; keine Fehler in M5A-Änderungen. Vollständiger MAUI-Build daher blockiert, diese fremden Fehler sind nicht Teil von M5A.

## 2026-10-26 – NB-2A-D5C Abschlussprüfung

- Den lokalen D5C-2B-Stand ab `f0d94d82b12e5f050445fa11205fad1650465d06` fortgesetzt; vorhandene Änderungen beibehalten. Die Testdatei `contract-action-placement.test.mjs` erwartet nun die tatsächlich in `app/page.tsx` eingebundene `MemberDocumentWorkspace`.
- Im aktiven Pachtvertrags-Signaturseiten-Renderer den sichtbaren `member.id`-Ersatz entfernt; bei fehlendem Namen erscheint `Nicht angegeben`. Der Nebenmitglied-Kontext des lokalen Mitgliedsvertrags bleibt ohne Hauptmitglied-ID.
- Finaler einschlägiger Node-Lauf: 38/38 Tests erfolgreich, einschließlich des bisherigen 30er-Dokumentblocks und `contract-action-placement`. `npm run build` in `KGV.Web`, `dotnet build KGV.Core/KGV.Core.csproj --no-restore` und `dotnet build KGV.Infrastructure/KGV.Infrastructure.csproj --no-restore` erfolgreich. Bekannte ImageSharp-NU1902-/NU1903-Warnungen und Web-Chunkgrößenwarnung sind nicht neu.
- Globaler Kurzcheck aktiver Core-/Edge-PDF-/Dokumentgeneratoren ohne sichtbaren technischen Mitglieds-ID-Fallback. `mitglied.id` und `member.id` verbleiben nur für interne FK-/Lookup-/Repräsentationszwecke.
- Audit um D5C-Ablauf, Datenquellen, Idempotenz, Retry-/Fehlerverhalten und Abschlussverifikation ergänzt. Migrationen nicht ausgeführt; kein Deployment, Storage-Upload oder Template-Aktivierung.
- **NB-2A-D5C = 100 %**; **NB-2A PDF-/Dokumentgeneratoren = 100 %**.
