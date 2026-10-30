# Web-/MAUI-Fachvergleich und Abschlussstand

## Geltungsbereich und Lesart

Dieses Dokument hält den Repository-Stand nach G9 am Commit `8d5eab0197a94b8dba31eca0ca38b6db1c0aa726` fest. Es beschreibt fachliche Bereiche und belegte Implementierungen, nicht eine technische Gleichheit der Oberflächen. Web und MAUI verwenden unterschiedliche Navigation, UI-Komponenten und Teile der Client-Infrastruktur; gemeinsame Datenbank- und Workflowverträge bilden die fachliche Grenze.

Die folgende Produktstruktur ist rollen- und berechtigungsabhängig. Sie bedeutet nicht, dass jeder Eintrag für jede angemeldete Person sichtbar ist. Die Web-Navigation filtert nach effektiven Rechten. MAUI unterscheidet unter anderem `UserShell` und `AdminShell`; die jeweilige Shell zeigt nur freigegebene Ziele.

## Zielstruktur und Navigation

```text
Startseite
Impressum

Vorgänge
  Signaturen
  Dokumente
  Vorstand
  Mailausgang
  Versendet
  Druckausgabe
  Verlauf

Arbeitsstunden freigeben
Export

Ablesen
  Ablesung erfassen
  JAE erfassen
  Zählerwechsel
  RFID einrichten
  Ablesungen freigeben

Verwaltung
  Parzellenverwaltung
  Wartungsverträge
  Saisonverwaltung
  Jahresabschluss
  Vereinskonfiguration
  Vereinsunterlagen
  Vereinsfunktionen
  Dokumenttypen
  Verträge nacherfassen

Mitgliedssuche
  bestehende kontextbezogene Unterseiten
```

In Web und MAUI sind die Bereiche fachlich zugeordnet, aber nicht als identische Menü- oder Routenkonstruktion umgesetzt. Web verwendet IDs wie `signaturen`, `ablesen-erfassen` und `vertraege-nacherfassen`; MAUI nutzt Shell-Routen und Seiten. In der MAUI-AdminShell fehlt derzeit ein eigener Verwaltungseintrag für **Dokumenttypen**; die Web-Navigation führt diesen Bereich. Das ist in der Matrix als konkreter Paritätsunterschied festgehalten und nicht als vollständig identisches Ziel behauptet.

## Vergleichsmatrix

| Fachbereich | Web | MAUI | Backend/Fachlogik | Status |
|---|---|---|---|---|
| Startseite | `start` / `HomeDashboard` | `home` / `HomePage` | Vereins- und Saisonkontext; plattformspezifische Darstellung | plattformspezifisch umgesetzt |
| Impressum | `impressum` / `ImprintPage` | `impressum` / `ImpressumPage` | Vereinsfunktionen und kontrollierte Impressumsdaten | fachlich gleichwertig |
| Signaturen | `signaturen` / `CommunicationCenter` | feste `CommunicationArea.Signatures` | gemeinsame Vorgangs- und Signaturverträge | plattformspezifisch umgesetzt |
| Dokumente | `dokumente` / `CommunicationCenter` | feste `CommunicationArea.Documents` | gemeinsame Kommunikations-/Dokumentvorgänge; zusätzlich gibt es Mitgliedsdokumente als Kontextansicht | plattformspezifisch umgesetzt |
| Vorstand | `vorstand` / Kommunikationsbereich mit Abstimmungsansicht | direkter Einstieg in `MembershipBoardReviewPage` | gemeinsamer Vorstandsbewertungs- und Abstimmungsvertrag | plattformspezifisch umgesetzt |
| Mailausgang | `mailausgang` / `CommunicationCenter` | feste `CommunicationArea.MailOutbox` | gemeinsame Versandvorgänge | fachlich gleichwertig |
| Versendet | `versendet` / `CommunicationCenter` | feste `CommunicationArea.Sent` | gemeinsamer Status- und Versandbestand | fachlich gleichwertig |
| Druckausgabe | `druckausgabe` / `CommunicationCenter` | feste `CommunicationArea.Print` | gemeinsame Vorgänge und Druckstatus | fachlich gleichwertig |
| Verlauf | `verlauf` / `CommunicationCenter` | feste `CommunicationArea.History` | gemeinsame Vorgangshistorie; 90 Tage Standard, „Alle anzeigen“ verfügbar | fachlich gleichwertig |
| Arbeitsstunden freigeben | `arbeitsstunden-pruefen` / `WorkHoursReview` | `workhours_review` / `ArbeitsstundenReviewPage` | gemeinsame Arbeitsstunden- und Prüfverträge | fachlich gleichwertig |
| Export | `export` / `ExportCenter` | `export` / `ExportPage` | Exportdaten aus dem gemeinsamen Vereinsbestand; Zugriff und Darstellung sind clientseitig umgesetzt | plattformspezifisch umgesetzt |
| Ablesung erfassen | `ablesen-erfassen` / `MeterWorkspace` im Modus `capture` | `reading_capture` / `AblesungErfassenPage` | gemeinsame Meterdaten und Erfassungsregeln | fachlich gleichwertig |
| JAE erfassen | `jae-erfassen` / `MeterWorkspace` im Modus `annual` | `annual_reading_capture` / `AblesungErfassenPage` mit Jahresendmodus | gemeinsame Meterdaten; JAE-Modus wird explizit gesetzt | fachlich gleichwertig |
| Zählerwechsel | `zaehlerwechsel` / `MeterChange` | `meter_change` / `ZaehlerwechselPage` | gemeinsame Zähler- und Parzellenbestände | fachlich gleichwertig |
| RFID einrichten | `rfid-einrichten` / `RfidAssignmentPanel` | `rfid_setup` / `RfidEinrichtenPage` | gemeinsame Zähler-/RFID-Fachverträge | fachlich gleichwertig |
| Ablesungen freigeben | `ablesungen-freigeben` / `MeterWorkspace` im Modus `review` | `reading_review` / `AblesungenFreigabePage` | gemeinsamer Prüfstatus und Meterdaten | fachlich gleichwertig |
| Parzellenverwaltung | `parzellen` / `ParcelWorkspace` | `parzellen` / `ParzellenPage` | gemeinsame Parzellen-, Mitglieds- und Zuordnungsdaten | fachlich gleichwertig |
| Wartungsverträge | `wartung` / `MaintenanceContracts` | `wartungsvertraege` / `WartungsvertraegePage` | gemeinsame Wartungsvertrags- und Zuordnungsdaten | fachlich gleichwertig |
| Saisonverwaltung | `saisons` / `SeasonManagement` | `season_management` / `SaisonverwaltungPage` | gemeinsamer Saisonbestand | fachlich gleichwertig |
| Jahresabschluss | `jahresabschluss` / Jahresabschlussansicht | `annual_closing` / `JahresabschlussPage` | gemeinsamer Jahresabschlussvertrag und Berechtigungsprüfung | fachlich gleichwertig |
| Vereinskonfiguration | `verein` / `ClubConfiguration` | `club_configuration` / `VereinskonfigurationPage` | gemeinsamer Vereinskontext; Rechte begrenzen die Bearbeitung | fachlich gleichwertig |
| Vereinsunterlagen | `vereinsunterlagen` / `ClubDocumentsManagement` | `club_documents` / `VereinsunterlagenPage` | gemeinsamer Vereinsdokumentbestand | fachlich gleichwertig |
| Vereinsfunktionen | `vereinsfunktionen` / `ClubFunctionManagement` | `club_functions` / `VereinsfunktionenPage` | gemeinsame Core-Records, Tabellen und Datenbankregeln | fachlich gleichwertig |
| Dokumenttypen | `dokumenttypen` / `DocumentTypeManagement` | kein eigener Eintrag in der `AdminShell` nachgewiesen | Web-Seite unterscheidet Anzeige und Administration; keine abweichende MAUI-Fachlogik behauptet | offener Paritätsunterschied |
| Verträge nacherfassen | `vertraege-nacherfassen` / `HistoricalContractRegistrationWorkflow` | `historical_contract_registration` / `HistoricalContractRegistrationPage` | gleiche Core-Modelle, Preview-/Registrierungs-RPCs, Capability und Nummerierung | plattformspezifisch umgesetzt |
| Mitgliedssuche | `mitglieder` / `MemberSearch` plus kontextbezogene Mitgliedsseiten | `membersearch` / `MemberSearchPage` plus kontextbezogene Shell-Ziele | gemeinsamer Mitgliedsbestand; Unterseiten unterscheiden sich nach Plattform und Rechten | plattformspezifisch umgesetzt |

**Statuslesart:** „fachlich gleichwertig“ bezeichnet denselben fachlichen Ablauf auf gemeinsamer Datenbasis; „plattformspezifisch umgesetzt“ kennzeichnet unterschiedliche UI-/Navigationswege; „offener Paritätsunterschied“ markiert einen im aktuellen Menüvergleich nicht nachgewiesenen Gegenpart. „Vorbereitet“ wird nicht als fertige Fachoberfläche gewertet. Die Tabelle bezeichnet Ziele nur dort als aktiv, wo Navigation und Render-/Page-Aufruf im aktuellen Code vorhanden sind.

### Vorgänge

Die sieben Bereiche sind direkt über die Navigation erreichbar; eine zusätzliche Sammel-Hubseite ist nicht erforderlich. MAUI verwendet dieselbe `KommunikationPage` mit festem `CommunicationArea` statt sechs Seitenkopien. Web verwendet den gemeinsamen `CommunicationCenter`. Der MAUI-Vorstandseintrag führt direkt zur `MembershipBoardReviewPage`; Web bindet den Vorstandsbereich in die Kommunikationsansicht ein.

„Meine“, „Unzugeordnet“ und „Alle“ sind Zuweisungsfilter innerhalb des jeweiligen Fachbereichs und keine Navigationsziele. Im Verlauf werden standardmäßig die letzten 90 Tage angezeigt; in beiden Clients kann auf „Alle anzeigen“ gewechselt werden.

### Vorstandsvoting zur Mitgliedsaufnahme

- Stimmberechtigte geben Einzelstimmen **Ja** oder **Nein** ab; eine persönliche Nein-Begründung ist optional.
- Abgegebene Stimmen und der Abstimmungsstand sind sichtbar; es wird keine automatische Mehrheitsentscheidung getroffen.
- Bei ausschließlich abgegebenen Ja-Stimmen kann die einstimmige Entscheidungsroute genutzt werden. Die Aufnahme bestätigt anschließend eine dafür berechtigte entscheidende Person, insbesondere der 1. oder 2. Vorsitz.
- Bei Nein-Stimmen oder begründeten Zweifeln erfolgt die Entscheidung manuell, gegebenenfalls auf Grundlage einer Vorstandssitzung.

Die Datenbank prüft die aktuelle Entscheidungsberechtigung und erzwingt die vollständige Ja-Abstimmung für den einstimmigen Pfad. Eine Ablehnung benötigt eine Abschlussbegründung.

### Ablesen

Die fünf direkten Bereiche ersetzen die alte Ablesen-Hubnavigation. `MeterWorkspace` und die gemeinsame Meter-Fachlogik bleiben erhalten. „Fällige Zähler“ ist kein Navigationspunkt; die interne Fälligkeitsfunktion und Route dürfen weiterhin existieren. WLAN-Einstellung und Foto-Synchronisierung wurden in die Erfassungsstrecke integriert. Offene Foto-Uploads bleiben als technische Funktion bestehen, soweit sie genutzt werden; die MAUI-AdminShell zeigt den Upload-Einstieg bei offenen Einträgen.

## Historische Vertragsnacherfassung

### Fachmodell und Vertragsverlauf

Es gibt genau zwei historische Vertragsarten:

1. **MA + PV – Erstvertrag**: ein gemeinsames historisches Dokument als Mitgliedsantrags- und Pachtvertragsnachweis; mehrere Pachtbeziehungen/Parzellen können dem Dokument zugeordnet werden. Die Registrierung legt keine neue Mitgliedschaft an und ersetzt keinen allgemeinen Mitgliedsantragsprozess.
2. **PV – Folgevertrag**: ein späterer Pachtvertragsnachweis für ein bereits vorhandenes Mitglied; keine neue Mitgliedschaft, aber eine eigene historische Vertragsfassung.

Ein separater historischer Mitgliedsantrag ist nicht vorgesehen. Die Dokumenttyp-Codes lauten `historischer_ma_pv_erstvertrag` und `historischer_pv_folgevertrag`.

Je Pachtbeziehung können mehrere Vertragsfassungen bestehen. Die zeitliche Einordnung richtet sich nach Vertragsdatum und nicht nach der Erfassungsreihenfolge; ältere Verträge können nachträglich eingetragen werden. Die Vorschau bestimmt den zeitlich richtigen Vorgänger und Nachfolger. Das Vorgängerende wird standardmäßig auf den Tag vor dem neuen Vertragsbeginn gesetzt und ist editierbar. Ein Nachfolger wird zeitlich korrekt verknüpft und begrenzt. Änderungen betreffen nur ausdrücklich ausgewählte Pachtbeziehungen; frühere Vertragsstände bleiben erhalten.

Parteien werden ebenfalls je Pachtbeziehung historisiert. Zulässige Übergänge umfassen `A+B → A`, `A → A+B` und `A+B → A+B`; verschiedene Parzellen dürfen unterschiedliche Vertragsstände haben. Der Wegfall einer Partei beendet ihre Mitgliedschaft nicht automatisch und ein PV erzeugt keine Mitgliedschaft.

Wenn eine Person aus einer Pachtbeteiligung ausscheidet, kann entweder nur die Pachtbeteiligung enden oder zusätzlich – ausdrücklich bestätigt – die Mitgliedschaft. Das Mitgliedschaftsende wird nicht automatisch gesetzt. Eine letzte wirksame Pachtbeziehung, die endet, kann einen Vorschlag **aktiv → passiv** auslösen; eine neue wirksame Pachtbeziehung eines passiven Mitglieds kann **passiv → aktiv** vorschlagen. Statusänderungen werden nur nach bestätigtem Workflow gespeichert.

Bei einem MA+PV-Erstvertrag kann ein offensichtlich falsches „Mitglied seit“ bewusst korrigiert werden. Die Korrektur wird auditiert, ist Bestandteil der relationalen Registrierungs-RPC und kein allgemeiner Stammdateneditor.

### Berechtigung und Nummerierung

`CanRegisterHistoricalContracts` ist **Bit 24** (`1 << 24`, dezimal `16777216`). Die Capability wird nicht automatisch aus der Rolle `admin` oder `vorstand` abgeleitet; sie kann einem normalen `user` gezielt erteilt werden. Navigation, Registrierungs-RPC und Stempel-Endpunkt prüfen diese Capability. Ohne sie sind Menü/Workflow nicht zugänglich und Serveraufrufe werden abgewiesen. Es gibt keine zusätzliche Freigabe durch Vorstand oder Admin.

Pro historischem Vertragsdokument wird automatisch eine historische Nummer im bestehenden **80xxx**-Bereich (`80000`–`89999`) reserviert. Es gibt keine manuelle Nummerneingabe. Die Reservierung ist quellbezogen und bei erneutem Aufruf derselben Quelle idempotent. Moderne Vertragsnummern verwenden nicht diesen historischen Nummernkreis.

### PDF-Stempel

Bei einem PDF wird die erste Seite oben rechts kompakt gestempelt. Beispiele:

```text
Mgl.-Nr. 1042
Vertrags-Nr. 80037
```

```text
Mgl.-Nr. 1042 / 1048
Vertrags-Nr. 80037
```

Die Nummern der Parteien werden aus den zum jeweiligen Vertragsbeginn wirksamen Pachtbeteiligungen geladen. Web und MAUI rufen denselben `kgv-upload-document`-Endpunkt auf; dieser verwendet dieselbe Reservierungs- und Stempellogik.

### Gemeinsame Web-/MAUI-Verträge und Online-Grenze

Beide Clients verwenden die gemeinsamen Modelle aus `KGV.Core`, `get_historische_pachtvertragsvorschau`, `register_historischen_pachtvertrag`, `CanRegisterHistoricalContracts`, die 80xxx-Reservierung sowie dieselbe PDF-Stempel-Funktion. Die UI-Darstellung und die Client-Services sind plattformspezifisch. Der MAUI-Workflow ist bewusst online-only und wird nicht in die allgemeine Dokument-Queue eingestellt.

### Transaktionsgrenze und verbleibende Fehlerfenster

`register_historischen_pachtvertrag` ist die atomare **relationale Registrierungsgrenze**: Dokumentklassifizierung, Vertragsnummerreservierung, Pachtbeziehungen/-fassungen, Parteienhistorie, `mitglied_seit`-Korrektur und ausdrücklich bestätigte Mitgliedschafts-/Aktivstatusänderungen werden innerhalb der Datenbanktransaktion verarbeitet. Ein Fehler innerhalb dieser RPC hinterlässt keinen Teilzustand dieser relationalen Änderungen; Wiederholungen derselben Registrierung sind auf vorhandene Fassungen und Nummern ausgelegt.

Die gesamte Dateiablage liegt jedoch nicht in dieser SQL-Transaktion: Web und MAUI laden das Dokument vorher hoch; danach folgt die Registrierungs-RPC. Schlägt die Registrierung fehl, kann daher ein bereits gespeichertes, noch nicht als Vertrag registriertes Dokument verbleiben. Der PDF-Stempel wird anschließend über einen separaten Edge-Function-Aufruf erzeugt und sein Status separat gespeichert. Ein Fehler in dieser Nachbearbeitung rollt die erfolgreiche Registrierung nicht zurück. Der Pfad „PDF ersetzt, aber Stempelstatus noch nicht markiert“ muss bei der echten Retry-/End-to-End-Abnahme gezielt geprüft werden; der Repository-Stand belegt keine transaktionsweite Atomarität für Upload und Stempel.

### Allgemeiner historischer Dokumentimport

Der allgemeine historische Dokumentimport bleibt für allgemeine Dokumentnachweise erhalten. Dazu gehören der RPC `register_historisches_dokument`, `RegisterHistorischesDokumentAsync` und die MAUI-`HistoricalDocumentRegistrationQueue`. Diese Funktionen sind ausdrücklich **nicht** der neue Altvertragsworkflow und werden für dessen atomare Registrierung nicht verwendet.

### Maßgebliche Migrationen

| Migration | Zweck |
|---|---|
| `supabase/migrations/20261030110000_g6a_historical_contract_foundation.sql` | Historische Vertragstypen, Vertragsfassungen, Capability-Prüfung, Audittabelle für „Mitglied seit“ und erste atomare Registrierungs-RPC |
| `supabase/migrations/20261030120000_g6b2_historical_contract_state_transitions.sql` | Vertragsparteien je Pachtbeziehung sowie ausdrücklich bestätigte Mitgliedschafts- und Aktivstatusänderungen |
| `supabase/migrations/20261030130000_g7_historical_contract_preview.sql` | Capability-geschützte Vorschau-RPC zur zeitlichen Einordnung von Vorgänger/Nachfolger |
| `supabase/migrations/20261027120000_nb2b_b2_historische_vertragsnummern.sql` | 80xxx-Feld, quellbezogene Nummernreservierung und PDF-Stempelstatus-Metadaten |

Die Tabelle beschreibt den Zweck der Migrationen; sie ist keine SQL-Zeilenreferenz und dokumentiert keine Anwendung auf eine Live-Datenbank.

## Test- und Integrationsstand nach G9

Die folgenden Ergebnisse stammen aus dem G9-Abschlusslauf und wurden für diese reine Dokumentationsänderung nicht erneut ausgeführt:

- **.NET:** 412/418 Tests bestanden. Sechs bekannte Fehler: drei Demo-Marker-Assertions, ein UUID-Dateiname-Test und zwei Mitgliedsantrag-Renderer-Tests wegen der bestehenden v5-Feldschema-Inkompatibilität.
- **Web-Build:** erfolgreich; vorhandene Chunkgrößen- und Routenklassifizierungswarnungen.
- **Web-Lint:** global 7 Fehler / 10 Warnungen. G9-geänderte Web-Dateien wurden gezielt lint-geprüft; bekannte Bestandsfehler wurden nicht im Rahmen von G9 behoben.
- **MAUI-Android-Build:** erfolgreich, 0 Fehler; vorhandene ImageSharp-NU1902-/NU1903-Warnungen.
- **`git diff --check`:** im G9-Abschluss erfolgreich; für die G10-Dokumentationsänderung erneut zu prüfen.

## Bekannte Restpunkte

- Die sechs genannten .NET-Testfehler bestehen fort.
- Der globale Web-Linter meldet weiterhin 7 Fehler und 10 Warnungen; G9 hat keine neuen Lintfehler in den gezielt geprüften geänderten Web-Dateien hinzugefügt.
- Für G6/G7 stand keine testfähige lokale Supabase-Instanz zur Verfügung. SQL-/RPC-Verträge wurden lokal per SQL-/Contracttests geprüft; eine vollständige Migration und RPC-End-to-End-Ausführung gegen eine frische Testdatenbank steht aus.
- Die getrennten Phasen für Datei-Upload, relationale Registrierung und PDF-Stempel sind keine gemeinsame Transaktion. Fehler-/Retry-Verhalten über diese Phasengrenzen muss mit einer Testdatenbank und realem Storage geprüft werden.
- Die vorhandenen ImageSharp-NU1902-/NU1903-Warnungen sowie Web-Buildwarnungen sind nicht durch G9 eingeführt.
- Ein eigener Dokumenttypen-Navigationseintrag ist in der MAUI-AdminShell nicht nachgewiesen, während Web ihn anbietet.

Es wurde keine Produktionsdatenbank für Tests verwendet und keine reale DB-Integration behauptet.

## Manuelle Abnahme-Checkliste

- [ ] Login als normaler `user` ohne Altvertrags-Capability: kein Menü/Workflow; RPC und Stempel-Endpunkt lehnen ab.
- [ ] Login als normaler `user` mit gezielt erteilter Altvertrags-Capability: vollständiger Altvertragsworkflow in Web und MAUI.
- [ ] Web-Navigation mit den jeweils berechtigten Rollen prüfen.
- [ ] MAUI-Navigation in `UserShell` und `AdminShell` mit den jeweils berechtigten Rollen prüfen.
- [ ] Moderner Mitgliedsantrag.
- [ ] Moderner Pachtvertrag.
- [ ] Vorstandsvoting: Ja/Nein, optionale Nein-Begründung, Sichtbarkeit der Stimmen, Einstimmigkeit und manuelle Entscheidung.
- [ ] MA + PV – Erstvertrag.
- [ ] PV – Folgevertrag für ein vorhandenes Mitglied.
- [ ] Vertragsparteien mit zwei Pächtern.
- [ ] Ein Vertrag mit mehreren Parzellen/Pachtbeziehungen.
- [ ] Einen älteren Vertrag nach einem neueren Vertrag nachträglich einfügen.
- [ ] Parteienwechsel `A+B → A`, `A → A+B` und `A+B → A+B` prüfen.
- [ ] Pachtbeteiligung beenden und Mitgliedschaft bestehen lassen.
- [ ] Mitgliedschaftsende ausdrücklich bestätigen und prüfen.
- [ ] Aktiv-/Passiv-Vorschlag und bestätigte Statusänderung prüfen.
- [ ] „Mitglied seit“ beim MA+PV-Erstvertrag bewusst korrigieren; Auditdatensatz prüfen.
- [ ] Automatische 80xxx-Nummer und idempotente Wiederholung prüfen.
- [ ] PDF-Stempel auf erster Seite oben rechts mit allen am Vertragsstand wirksamen Parteien prüfen.
- [ ] Nachweisstatus für Mitgliedsantrag und Pachtvertrag prüfen.
- [ ] Wiederholung nach Timeout/Retry; keine doppelten Fassungen oder Nummern.
- [ ] Fehler innerhalb der Registrierungs-RPC; relationalen Rollback prüfen.
- [ ] Fehler zwischen Datei-Upload und Registrierung sowie Fehler im PDF-Stempel-Fenster gezielt prüfen.
- [ ] Allgemeiner historischer Dokumentimport und `HistoricalDocumentRegistrationQueue` unabhängig vom Altvertragsworkflow prüfen.
- [ ] MAUI beim Altvertragsworkflow offline schalten: Registrierung schlägt fehl und erzeugt keinen Queue-Eintrag.

## Geprüfte Code-Einstiege

- Web-Navigation und Rendering: `KGV.Web/features/navigation/NavigationConfig.ts`, `KGV.Web/services/workspace/navigation-service.ts`, `KGV.Web/app/page.tsx`
- MAUI-Navigation: `KGV.Maui/UserShell.cs`, `KGV.Maui/AdminShell.cs`, `KGV.Maui/ShellNavigationHelper.cs`
- Gemeinsame Berechtigung: `KGV.Core/Security/PermissionFlags.cs`, `KGV.Core/Security/PermissionCatalog.cs`, `KGV.Core/Security/PermissionChecks.cs`
- Historisches Fachmodell und Registrierung: `KGV.Core/Models/HistoricalDocumentModels.cs`, `KGV.Infrastructure/Services/SupabaseService.cs`, `KGV.Web/features/documents/HistoricalContractRegistrationWorkflow.tsx`
- Allgemeiner Dokumentimport und Stempelung: `KGV.Maui/Pages/HistoricalDocumentImportPage.cs`, `KGV.Maui/Services/Documents/HistoricalDocumentRegistrationQueue.cs`, `supabase/functions/kgv-upload-document/index.ts`
