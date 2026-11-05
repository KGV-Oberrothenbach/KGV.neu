# CX – G1: JEA und Hauptzähler – Arbeitsplan

Stand der Analyse: 04.11.2026. Dieses Dokument beschreibt ausschließlich den
Bestand und die empfohlenen nächsten Schritte. Es wurden keine produktiven
Fachabläufe, Tabellen oder Daten verändert.

## Kurzfazit

Die gewünschte JEA-Auswahl ist im aktuellen Stand bereits weitgehend als
gemeinsamer Auswahlvertrag umgesetzt:

- Normal: **Parzelle → aktive Zähler dieser Parzelle**; die Anzeige lautet
  `Strom – Nummer` oder `Wasser – Nummer`.
- Hauptzähler: Checkbox **Hauptzähler**, keine Parzellenauswahl, Auswahl aus
  den vier logischen Kombinationen von Medium und Bereich.
- Ausgebaute Zähler werden im Auswahlvertrag anhand von Status, Ausbaudatum
  und Einbaudatum ausgeschlossen.

Die kritische Lücke liegt nicht in der Erfassung, sondern in der
Auswertung: `rpc_jahresverbrauch_segmente` und der JAE-XLSX-Export setzen
heute zwingend eine Parzelle voraus. Deshalb können Hauptzähler nicht einfach
in den bestehenden Segment-RPC aufgenommen werden. Es braucht einen
separaten bzw. erweiternden Auswertungsweg für die vier Messstellen, der die
Unterzähler über `parzelle."Anlage"` Nord/Süd aggregiert.

## 1. Ist-Zustand: Web

### JEA-Erfassung und Auswahl

`KGV.Web/app/page.tsx` rendert für den Menümodus `jae-erfassen` die Komponente
`MeterReadingEntry` mit `initialKind = "jea"`.

- Im JEA-Modus blendet die Komponente das frühere Medium-Picker-Feld aus.
- Bei normaler Auswahl wird zuerst eine aktive Parzelle geladen; anschließend
  liefert `listSelectableActiveMeters(..., { parcelId, mainMeterMode: false,
  onDate })` nur aktive Parzellenzähler dieser Parzelle.
- Bei aktivierter Hauptzähler-Checkbox wird `parcelId` zurückgesetzt und
  ausgeblendet; derselbe Vertrag wird mit `mainMeterMode: true` aufgerufen.
- Die Darstellung kommt aus
  `KGV.Web/repositories/meters/selectable-meter-repository.ts`: normale
  Zähler `Strom – Nummer`/`Wasser – Nummer`, Hauptzähler anschließend in der
  UI als `Hauptzähler Strom Nord` usw.
- Das Datum ist Teil der Auswahl. Damit erscheinen nur Zähler, die an diesem
  Datum eingebaut, aktiv und nicht ausgebaut sind.

Die Speicherung erfolgt über
`KGV.Web/services/readings/reading-service.ts::saveMeterReading`. Sie erlaubt
Hauptzähler nur für eine direkte, berechtigte JEA-Erfassung; die sonst für
Parzellen geltenden Aktiv-, Anschluss- und Belegungsprüfungen werden nur für
normale Zähler ausgeführt. Doppelte Ablesungen gleicher Art am selben Tag und
Plausibilitätsgrenzen werden weiterhin geprüft.

### RFID

Der Web-Standardweg `KGV.Web/services/rfid/rfid-service.ts` löst einen Tag
über `v_rfid_scan_context` auf und ist weiterhin parzellenorientiert.
Hauptzähler werden ergänzend über
`hauptzaehler-messstelle-repository.ts` gesucht: Die Messstelle enthält
`medium`, `anlagenbereich` und den dauerhaften Tag, anschließend wird der
aktive Hauptzähler dieser Kombination gesucht. Das ist in der Web-
Zählerwechselmaske bereits integriert. Für die JEA-Auswahl besteht der
manuelle Hauptzählerpfad; der allgemeine Web-RFID-Kontext muss für eine
vollständig einheitliche JEA noch auf den Messstellenweg erweitert werden.

### Zählerwechsel

`MeterChange` in `KGV.Web/app/page.tsx`,
`services/meters/meter-change-service.ts` und
`repositories/meters/meter-change-repository.ts` verwenden für Hauptzähler
den separaten RPC `execute_hauptzaehler_meter_change`. Der Wechsel ist
atomar: Ausbauablesung, Ausbau des alten Zählers, Einbau des neuen Zählers
und Einbauablesung. Für Parzellenzähler bleibt der bisherige RPC unverändert.

## 2. Ist-Zustand: MAUI

`KGV.Maui/AdminShell.cs` und `KGV.Maui/UserShell.cs` öffnen für **JAE
erfassen** dieselbe `AblesungErfassenPage` mit `art = jea`.

- `KGV.Maui/Pages/AblesungErfassenPage.cs` enthält den NFC/RFID-Weg sowie
  einen manuellen Ersatzweg.
- Der Ersatzweg besteht bereits aus Parzellen-Picker, Hauptzähler-Checkbox
  und einem einzigen Zähler-Picker. `ReloadFallbackMetersAsync` ruft
  `ISupabaseService.GetSelectableActiveMetersAsync` auf.
- Im Hauptzählermodus wird keine Parzelle übergeben; aus dem gewählten Zähler
  entsteht ein Kontext mit `ParzelleId = null`, `IstHauptzaehler = true` und
  `Anlagenbereich`.
- Für einen unbekannten JEA-Tag fragt die Seite
  `GetHauptzaehlerMessstelleByRfidAsync` und anschließend
  `GetActiveHauptzaehlerForMessstelleAsync` ab. Es wird kein Ersatzgarten
  erzeugt und keine Tag-Zuordnung geändert.

`RfidScanContextViewModel` bleibt für Parzellen-RFID der zentrale
ViewModel-Weg. Die Hauptmessstelle ist derzeit eine Ergänzung in der Page,
nicht Teil eines vollständig gemeinsamen Scanresultats. Das ist funktional
brauchbar, aber die wesentliche Harmonisierungsschuld zwischen Web und MAUI.

Der Hauptzählerwechsel ist in `ZaehlerwechselPage`,
`ZaehlerwechselAusbauPage` und `SupabaseService.ExecuteMeterChangeAsync`
angebunden. Die Ausbau-Seite übergibt den nullbaren Parzellenbezug und die
Messstellenmerkmale an den Hauptzähler-RPC. Der Einbau ohne vorher aktiven
Hauptzähler ist absichtlich kein umgedeuteter Parzellen-Einbauflow.

## 3. Datenmodell, Constraints und Abfragen

Der Bestand enthält bereits
`supabase/migrations/20261102224500_hauptzaehler_basis.sql`. Sie entspricht
der vorgesehenen Spaltenstruktur:

- `ist_hauptzaehler boolean not null default false`,
- `anlagenbereich text null`,
- `zaehler.parzelle_id` nullable,
- CHECK: normaler Zähler hat Parzelle und keinen Bereich; Hauptzähler hat
  keine Parzelle und `nord` oder `sued`.

`hauptzaehler_messstelle` enthält genau die vier logischen Kombinationen und
bindet die RFID an die Messstelle statt an den physischen Zähler. Die spätere
Implementierung darf **keine weitere Spaltenmigration** für diese Struktur
anlegen. `20261103090000_hauptzaehler_meter_change.sql` ergänzt bereits den
Partial-Unique-Index, der höchstens einen aktiven Hauptzähler je
`medium + anlagenbereich` zulässt.

Wiederverwendbar sind:

- `KGV.Core/Models/SelectableActiveMeter.cs` und
  `SelectableActiveMeterQuery` als UI-neutraler Auswahlvertrag,
- `KGV.Core/Models/ZaehlerRecord.cs` mit nullable `ParzelleId`, Hauptzähler-
  und Bereichsfeldern,
- `KGV.Infrastructure/Services/SupabaseService.cs::GetSelectableActiveMetersAsync`,
- Web-Repository `selectable-meter-repository.ts` und
  Hauptmessstellen-Repository,
- die aktiven-Prüfungen aus Status, Einbau- und Ausbaudatum.

### Noch parzellengebundene Stellen

Folgende bestehende Datenpfade dürfen Hauptzähler nicht ungeprüft verwenden:

- `rpc_jahresverbrauch_segmente` bildet in den CTEs Mieterwechsel und
  Segmentgrenzen über `parzelle_id`; am Ende ist ein `join public.parzelle`
  zwingend. Für Hauptzähler mit NULL ist dieser Weg leer.
- `rpc_export_jahresablesung_status` joint die Stichtagszähler zwingend mit
  `parzelle`, Pächterbezug und Pachtwechsel. Hauptzähler erscheinen dadurch
  nicht im JEA-Status-Export.
- Die Web-JEA-Foto-Metadaten verwenden `meter + parcel`; bei Hauptzählern
  wird das Foto zwar nicht blockiert, aber die parzellenbezogene
  Detailbeschreibung entfällt. Für Nachvollziehbarkeit sollte später eine
  Messstellenbezeichnung als Foto-Kontext vorgesehen werden.
- `GetStromAblesungenAsync(int parzelleId)` und
  `GetWasserAblesungenAsync(int parzelleId)` sowie die Prüfungen des
  `JahresabschlussService` sind ausschließlich Parzellenpfade.

## 4. Jahresverbrauch und historische Zähler

Die vorhandene SQL-Funktion `rpc_jahresverbrauch_segmente(p_jahr)` ist eine
gute fachliche Grundlage für **Unterzähler**:

- Sie akzeptiert freigegebene Ablesungen.
- Als Startwert verwendet sie die JEA des Vorjahres oder – bei Einbau im
  Auswertungsjahr – die Einbauablesung.
- Als Endwert verwendet sie die aktuelle JEA oder – bei Ausbau im Jahr – die
  Ausbauablesung.
- Daher werden Zählerwechsel innerhalb eines Abrechnungsjahres bereits als
  mehrere Segmente mit Grund `zaehlerwechsel` verarbeitet.
- Pächterwechsel, fehlende oder nicht freigegebene Übergangsablesungen und
  negative Verbräuche werden als berechenbar/nicht berechenbar ausgewiesen.

Für Hauptzähler kann dieselbe **Zählergrenzenlogik** (Vorjahres-JEA bzw.
Einbauablesung bis JEA bzw. Ausbauablesung) wiederverwendet werden. Die
pächter- und parzellenbezogenen CTEs dürfen jedoch nicht mitkopiert werden.
Es empfiehlt sich daher ein klarer, messstellenbezogener Ergebnisvertrag mit
`medium`, `anlagenbereich`, Zähler, Start-/Endablesung, Verbrauch,
Berechenbarkeit und Fehlergrund; erst darüber wird die Differenz gebildet.

## G2-Ergänzung: Hauptzähler-Datenmodell und Migrationskompatibilität

### Ergebnis der Migrationsprüfung

Die vorbereitete Migration
`supabase/migrations/20261104230000_zaehler_hauptzaehler_spalten.sql` ist
**nicht kompatibel und darf im aktuellen Migrationsstrang nicht unverändert
ausgeführt werden**.

Der Konflikt ist konkret:

| Bestand | vorbereitete Migration | Wirkung bei Ausführung nach dem Bestand |
| --- | --- | --- |
| `20261102224500_hauptzaehler_basis.sql` erlaubt für Hauptzähler nur `nord`/`sued`. | `20261104230000_zaehler_hauptzaehler_spalten.sql` erlaubt nur `Nord`/`Süd`. | Es entstehen zwei benannte CHECK-Constraints mit gegensätzlichen Wertemengen. Kein Hauptzähler erfüllt beide Constraints. |
| `hauptzaehler_messstelle` enthält/seedet `nord`/`sued`. | Kommentare und Constraint verlangen `Nord`/`Süd`. | Die logische Messstelle kann nicht mehr mit dem Zählerbereich gleich verglichen werden. |
| `execute_hauptzaehler_meter_change` und Web-Typen verwenden `nord`/`sued`. | Der neue Constraint erwartet `Nord`/`Süd`. | Der atomare Wechsel scheitert beim Einfügen des neuen Hauptzählers. |
| Der aktive-Hauptzähler-Index existiert bereits als `ux_zaehler_aktiver_hauptzaehler_messstelle`. | Ein inhaltlich gleicher zweiter Index wird unter anderem Namen angelegt. | Kein fachlicher Gewinn, aber redundanter Index und vermeidbare Wartungslast. |

Die `if not exists`-Spaltenanweisungen sind einzeln harmlos. Der neue
Constraint prüft jedoch nur auf seinen eigenen Namen
`zaehler_hauptzaehler_zuordnung_chk`; er erkennt den vorhandenen
`zaehler_hauptzaehler_parzelle_bereich_chk` nicht. Deshalb ist der Konflikt
nicht idempotent abgefangen.

### G2.1 – korrigierte Vorwärtsmigration und Remote-Stand

Die Supabase-Migrationshistorie wurde ausschließlich lesend mit `npx
supabase migration list` geprüft. Dabei ist `20261102224500_hauptzaehler_basis`
bereits angewendet; `20261103090000_hauptzaehler_meter_change` und
`20261104230000_zaehler_hauptzaehler_spalten` sind noch nicht angewendet.
Die bereits angewendete Basisdatei wird deshalb nicht verändert.

Auch `20261103090000_hauptzaehler_meter_change.sql` war noch nicht angewendet
und verwendet jetzt in seiner Bereichsprüfung ebenfalls `Nord`/`Süd`.
`20261104230000_zaehler_hauptzaehler_spalten.sql` ist nun eine sichere
Vorwärtsmigration. Sie erzeugt weder Spalten noch Tabellen und normalisiert
die vorhandenen Werte `nord`/`sued` zu `Nord`/`Süd`. Anschließend ersetzt sie
die zwei potenziell widersprüchlichen CHECK-Constraints durch genau je einen
kanonischen Constraint für `zaehler` und `hauptzaehler_messstelle`.

Die Migration bewahrt dabei Messstellen-IDs, RFID-UIDs, Zähler-IDs,
Ablesungen und Historie. Sie schreibt den Hauptzähler-Wechsel-RPC mit
**demselben bestehenden Vertrag** (inklusive
`zaehler_ablesung`, Einbau-/Ausbauablesung, Fotos, Eichdatum und atomarer
Sperre), aber mit `Nord`/`Süd`. Sie entfernt außerdem den eventuell aus G4
stammenden äquivalenten Index und legt exakt einen Partial-Unique-Index für
aktive Hauptzähler je `(medium, anlagenbereich)` an.

Für `hauptzaehler_messstelle` aktiviert die Migration RLS und erlaubt den
direkten Lesezugriff nur Rollen mit `CanReadMeters` (Berechtigungsbit `32`).
Schreibrechte bleiben nicht über die Tabelle offen; die RFID-Zuordnung wird
nicht verändert. Die Service-Rolle bleibt von RLS unberührt.

Die ergänzende, ausschließlich lesende Prüfung liegt unter
`supabase/tests/20261104230000_hauptzaehler_rollout_checks.sql`. Sie prüft
Spalten, Constraints, die vier Messstellen, den aktiven Index, nicht
zuordenbare Hauptzähler, Historie/Ablesungen, RPC und RLS-Policy.

**Verbleibender G3-Auftrag:** Die Web-Zählerwechsel-Parameter sind noch als
`"nord" | "sued"` typisiert und `app/page.tsx` übergibt Kleinbuchstaben.
Nach der Migration muss G3 sie auf `"Nord" | "Süd"` und eine unveränderte
Weitergabe des Messstellenwerts umstellen. MAUI vergleicht die Werte bereits
fallunabhängig; sein bestehender Hauptzähler-Kontext bleibt daher technisch
kompatibel. Ein Web-Hauptzählerwechsel darf erst nach dieser kleinen
Client-Korrektur getestet werden.

**Rollout-Fazit:** Die Migration ist für einen reinen DemoDB-Migrationstest
bereit, sofern die ausstehende Migration `20261103090000` in derselben
Reihenfolge mit ausgerollt wird. Sie wurde in diesem Arbeitsschritt nicht
ausgeführt. Für den End-to-End-Webwechsel ist zusätzlich der genannte
G3-Client-Fix erforderlich.

### `hauptzaehler_messstelle`: Beziehung und RFID

Die Tabelle ist die richtige dauerhafte Identität der vier Messstellen:

- Schlüssel: `medium` + `anlagenbereich` (unique),
- RFID: `rfid_tag_uid` (unique, nullable),
- Seed: Strom/Wasser × Nord/Süd,
- keine physische Zähler-ID und keine `parzelle_id`.

Es gibt bewusst **keine FK von `zaehler` auf die Messstelle**. Der Bezug wird
über die fachliche natürliche Kombination `(medium, anlagenbereich)` gebildet.
Damit kann beim Zählerwechsel der alte Zähler historisch bleiben und der neue
Zähler dieselbe Messstelle ohne RFID-Umschreibung übernehmen. Diese
Entkopplung ist korrekt und schafft keine parallele Zählerdatenhaltung.

Abhängige Pfade sind:

- `HauptzaehlerMessstelleRecord` und `ISupabaseService` im Core,
- `SupabaseService.GetHauptzaehlerMessstellenAsync`,
  `GetHauptzaehlerMessstelleByRfidAsync` und
  `GetActiveHauptzaehlerForMessstelleAsync`,
- Web-Repository `hauptzaehler-messstelle-repository.ts`,
- MAUI `AblesungErfassenPage` und `ZaehlerwechselPage`,
- Web-Zählerwechsel in `app/page.tsx`.

Die Standard-RFID-View `v_rfid_scan_context` enthält dagegen ausschließlich
Parzellen-Tags und parzellengebundene aktive Zähler. Sie ist daher kein
vollständiger Hauptzähler-RFID-Resolver. MAUI und der Web-Zählerwechsel
behandeln dies heute mit einer zusätzlichen Messstellenabfrage. Für G3 ist
ein gemeinsamer Resolververtrag erforderlich, der zuerst den unveränderten
Parzellenweg und danach die Messstelle prüft, ohne die RFID am physischen
Zähler zu speichern.

### Constraints, Trigger, Indizes und RLS

- `ux_zaehler_one_active_per_parcel_medium` bleibt für normale Zähler korrekt;
  bei `parzelle_id = NULL` greift der PostgreSQL-Unique-Index nicht
  konfliktverhindernd. Dafür existiert der separate Partial-Unique-Index für
  Hauptzähler.
- Der Status-Trigger setzt bei gesetztem `ausgebaut_am` den Status auf
  `ausgebaut`; damit bleiben historische Hauptzähler zulässig und aktive
  Wechsel eindeutig.
- Die Eichfrist-/Bauart-Trigger arbeiten ausschließlich auf Zählerfeldern und
  sind mit NULL-`parzelle_id` kompatibel.
- Der normale RPC `execute_meter_change` bleibt zwingend parzellengebunden.
  Das ist korrekt; Hauptzähler verwenden ausschließlich
  `execute_hauptzaehler_meter_change`.
- Der Hauptzähler-RPC sperrt den alten Zähler (`for update`), schreibt
  Ausbauablesung und Historie, setzt ihn ausgebaut, legt den Nachfolger mit
  gleicher Kombination `(medium, anlagenbereich)` und NULL-`parzelle_id` an
  und schreibt die Einbauablesung. Die RFID-Zuordnung wird dabei nicht
  berührt und bleibt an der Messstelle erhalten.
- Die vorliegenden Migrationen enthalten für
  `hauptzaehler_messstelle` keine eigene RLS-Aktivierung oder Policy. Vor
  Produktivrollout muss im Katalog geprüft werden, ob RLS/Grants durch eine
  globale Basiskonfiguration greifen. Andernfalls können direkte REST-Reads
  die RFID-UIDs breiter sichtbar machen als beabsichtigt.
- Die bestehende `zaehler`-RLS erlaubt produktiven Admin-/Vorstandsrollen den
  vollständigen Zugriff. Mitgliederzugriff ist über
  `parzellen_belegung.parzelle_id = zaehler.parzelle_id` formuliert und sieht
  daher keinen Hauptzähler; das entspricht der Web-Validierung, wonach nur
  direkte berechtigte Rollen Hauptzähler-JEA erfassen dürfen. Für
  eingeschränkte Demo-Rollen ist der bisherige Parzellen-Demo-Scope ebenfalls
  nicht auf Hauptzähler ausgelegt.

### Notwendige Arbeiten für G3–G8

1. **G3 – kanonische Werte und Rolloutplan:** `Nord`/`Süd` in Messstelle,
   Zähler-Constraint, RPC, Modellen und Clients vereinheitlichen; vorhandene
   DB-Migrationshistorie vor dem Rollout prüfen. Keine zweite Spaltenmigration.
2. **G4 – RFID-Vertrag und Zugriff:** Gemeinsame Haupt-/Parzellen-Auflösung
   für Web und MAUI definieren; RLS, Grants und Tag-Verwaltung der
   Messstellentabelle gezielt absichern.
3. **G5 – Erfassung und Historie testen:** Vier Kombinationen, Tag-Auflösung,
   Einbau, Ausbau, Wechsel, Einbau-/Ausbauablesung und historische Auswahl
   mit den kanonischen Bereichswerten als Integrationsfälle absichern.
4. **G6 – Verbrauch:** Hauptzähler-Verbrauch über dieselben Ablesegrenzen
   wie Unterzähler berechnen und Unterzähler über
   `parzelle."Anlage" = anlagenbereich` aggregieren.
5. **G7 – Differenz und Export:** Die vier nachvollziehbaren Differenzen
   DB-seitig bereitstellen und erst danach den JEA-/Status-Export erweitern.
6. **G8 – Umlage nach Fachfreigabe:** Nur eine bestätigte Differenz in die
   vorhandenen Jahresabschluss-Rechnungszuordnungen überführen; keine
   automatische Buchung.

## 5. Nord/Süd-Zuordnung der Unterzähler

Die Feststellung aus G1 wird hiermit korrigiert: Die Zuordnung der normalen
Parzellenzähler ist fachlich bereits vorhanden. Sie wird aus
`public.parzelle."Anlage"` abgeleitet; der Bestand enthält dort die Werte
`Nord` und `Süd`. Ein Unterzähler erhält seinen Bereich folglich über seine
`parzelle_id` und deren Anlage. Eigene Bereichsspalten oder eine parallele
Hauptzähler-Datenhaltung sind dafür nicht nötig.

`zaehler.anlagenbereich` gehört nur zum Hauptzähler und muss exakt denselben
kanonischen Wert wie `parzelle."Anlage"` tragen: `Nord` oder `Süd`. Die
spätere Auswertung verbindet Unterzähler über
`zaehler.parzelle_id → parzelle."Anlage"`; Hauptzähler werden über
`zaehler.anlagenbereich` zugeordnet.

Technisch gibt es bislang keine Datenbank-Constraint, die die Schreibweise
von `parzelle."Anlage"` auf diese beiden Werte beschränkt. Für die
Auswertung genügt dies nur, solange der Stammdatenbestand tatsächlich exakt
`Nord`/`Süd` enthält. Eine robuste Umsetzung muss deshalb die Gleichheit
normalisieren bzw. validieren, ohne eine zweite Bereichsdatenhaltung zu
schaffen.

## 6. JEA-Export und Differenzdarstellung

Es existieren zwei relevante Exportpfade:

1. Der metadatengesteuerte Status-Export
   `rpc_export_jahresablesung_status` aus
   `20261031170000_jea_rfid_generic_exports.sql`. Er zeigt die am 31.12.
   aktiven Parzellenzähler und ihren JEA-Status.
2. Der Web-XLSX-Export `KGV.Web/services/export/jae-xlsx-service.ts`. Er
   bezieht Segmentdaten aus
   `repositories/readings/annual-consumption-repository.ts` und schreibt die
   Vorlage `public/templates/exports/JAE_Vorlage.xlsx` sowie das Blatt
   `JAE erweitert`. Derzeit werden nur Wasser- und Stromsummen aller
   umlagefähigen Unterzähler ausgegeben.

Für die vier Vergleiche sollten später keine Werte im Browser nachgerechnet
werden. Empfohlen ist ein DB-seitiger, nur lesender Auswertungs-/Exportvertrag
mit je einer Zeile für Strom Nord, Strom Süd, Wasser Nord und Wasser Süd:

`Hauptzählerverbrauch`, `Unterzählerverbrauch`, `Differenz`,
`Berechenbarkeit`, `Fehlergrund` sowie die zugrundeliegenden Zähler- und
Ablesungs-IDs. Das Ergebnis kann dann sowohl den XLSX-Export als auch den
metadatengesteuerten Export speisen. Die hierfür nötige Exportmigration ist
ausdrücklich erst in der CX-Umsetzungsphase anzulegen.

## 7. Umlage und Nebenkosten

Die bestehende Umlage ist nicht an Zählerverbräuche gekoppelt:

- Modelle: `KGV.Core/Models/JahresabschlussModels.cs`.
- Persistenz: `jahresabschluss`, `jahresabschluss_rechnung`,
  `jahresabschluss_rechnung_zuordnung`, `jahresabschluss_position`,
  `kostenart`, `umlageart`.
- Service: `KGV.Infrastructure/Services/JahresabschlussService.cs`.
- Umlagearten: `KEINE`, `EINZEL`, `U-S` (Stromanschluss), `U-W`
  (Wasseranschluss) und `U2` (flächen-/zeitgewichtet). Die Verteilung entsteht
  aus Rechnungszuordnungen, aktiven Parzellen und Belegungszeiträumen.

Eine Differenz kann fachlich als zusätzliche Rechnung bzw. Rechnungszuordnung
in den bestehenden Jahresabschluss eingegeben und über eine vorhandene
Umlageart verteilt werden. Es gibt jedoch keine vorhandene, automatische
Brücke von Verbrauchsdifferenz zu Rechnung, Kostenart, Preis oder
`jahresabschluss_position`. Daher darf CX keine automatische Umlagebuchung
ableiten, solange Preisgrundlage, Umlageart, Rundung und Freigabe nicht
festgelegt sind.

## 8. Betroffene Dateien

| Bereich | Vorhanden / voraussichtlich anzupassen |
| --- | --- |
| Web-JEA | `KGV.Web/app/page.tsx`, `services/readings/reading-service.ts`, `repositories/meters/selectable-meter-repository.ts` |
| Web-RFID | `services/rfid/rfid-service.ts`, `repositories/rfid/rfid-repository.ts`, `repositories/meters/hauptzaehler-messstelle-repository.ts` |
| Web-Export | `repositories/readings/annual-consumption-repository.ts`, `models/export/jae-export.ts`, `services/export/jae-xlsx-service.ts`, zugehörige Tests und `JAE_Vorlage.xlsx` nur bei fachlich bestätigtem Layoutbedarf |
| MAUI-JEA/RFID | `KGV.Maui/Pages/AblesungErfassenPage.cs`, `ViewModels/RfidScanContextViewModel.cs`, `State/ZaehlerwechselWorkflowState.cs`, `AdminShell.cs`, `UserShell.cs` |
| MAUI-Wechsel | `ZaehlerwechselPage.cs`, `ZaehlerwechselAusbauPage.cs`, `ZaehlerwechselEinbauPage.cs` |
| Core/Backend | `KGV.Core/Models/{ZaehlerRecord,SelectableActiveMeter,RfidScanContextRecord,MeterChangeExecutionRequest}.cs`, `KGV.Core/Interfaces/ISupabaseService.cs`, `KGV.Infrastructure/Services/SupabaseService.cs` |
| Jahresabschluss | `KGV.Infrastructure/Services/JahresabschlussService.cs`, `KGV.Core/Models/JahresabschlussModels.cs`, die MAUI-Jahresabschlussseiten |
| Supabase | bestehende Hauptzähler-/Wechselmigrationen, `rpc_jahresverbrauch_segmente`, `rpc_export_jahresablesung_status`; später gezielt neue Auswertungs- und Exportmigration(en) |

## 9. Offene fachliche Entscheidungen

1. Soll die bestehende Stammdatenvalidierung für `parzelle."Anlage"` die
   kanonischen Werte `Nord`/`Süd` technisch erzwingen oder wird die
   Bestandsbereinigung/Validierung außerhalb der Datenbank verantwortet?
2. Was bedeutet eine negative Differenz fachlich (Messfehler, Verlust,
   Rückspeisung) und wann darf sie abgerechnet werden?
3. Wie werden nicht berechenbare Einzel- oder Hauptzähler in der Differenz
   dargestellt: blockierend, als Warnung oder mit manuellem Ersatzwert?
4. Nach welchem Schlüssel und Preis werden positive Differenzen später
   verteilt? Bestehende Umlagearten reichen technisch als Verteilungsweg,
   definieren aber keinen Verbrauchspreis.
5. Soll ein Hauptzähler ohne aktiven Vorgänger administrativ erstinstalliert
   werden können? Dafür ist ein separater Workflow nötig, nicht der
   parzellenbezogene Einbauflow.
6. Soll die RFID-Einrichtung der vier Messstellen in Web und MAUI eine
   sichtbare Verwaltungsoberfläche erhalten oder ausschließlich administrativ
   erfolgen?

## 10. Empfohlene Umsetzungsschritte ab G3

Die verbindliche, auf G2 aufbauende Reihenfolge steht im Abschnitt
„G2-Ergänzung: Hauptzähler-Datenmodell und Migrationskompatibilität“:

1. **G3:** Kanonische Bereichswerte und den sicheren Rollout der einen
   vorhandenen Spaltenmigration konsolidieren.
2. **G4:** RFID-Resolver und Zugriffsschutz auf Messstellen harmonisieren.
3. **G5:** Ablese- und Wechselhistorie der vier Hauptzähler integriert testen.
4. **G6:** Verbrauch für Hauptzähler und Unterzähler je Anlage berechnen.
5. **G7:** Differenzwerte und JEA-Export erweitern.
6. **G8:** Erst nach Fachfreigabe eine manuelle Übernahme in die bestehende
   Umlage vorsehen.

## G3 – Nord/Süd vereinheitlicht

G3 übergibt die Hauptzählerbereiche in Web, MAUI und Backend ausschließlich
als `Nord` oder `Süd`. Es wurden keine Datenbankmigrationen ausgeführt oder
verändert.

- Der Web-Zählerwechsel typisiert `anlagenbereich` auf `"Nord" | "Süd"` und
  übergibt den vom aktiven Hauptzähler gelesenen Wert unverändert an den RPC.
- Die Web-RFID-Auflösung gleicht Messstelle und auswählbaren Hauptzähler
  direkt über `medium` und `anlagenbereich` ab; es gibt keine Umwandlung der
  Bereichsschreibweise mehr.
- `SupabaseService.ExecuteMeterChangeAsync` sendet den Bereich unverändert
  und lehnt für Hauptzähler jeden anderen Wert vor dem RPC-Aufruf ab.
- Der MAUI-RFID-Fallback übergibt den Messstellenbereich unverändert an die
  aktive Hauptzählerauswahl. Die Auswahl vergleicht kanonisch; Ablesung und
  Wechsel verwenden denselben Kontext.

Prüfstand G3: Web-Build und Infrastruktur-Build waren erfolgreich;
`npm run test:maui-valid-access` war erfolgreich. Der vollständige
MAUI-Android-Build erreicht die geänderte Infrastruktur, stoppt jedoch an
zwei bestehenden, unabhängigen Fehlern in
`KGV.Maui/Pages/MeterChangeRequestPages.cs` (`Keyboard` nicht auflösbar,
Zeilen 162 und 166). Zusätzlich bestehen die bekannten ImageSharp-
Sicherheitswarnungen. `npm run lint` bleibt mit 24 bestehenden ESLint-Fehlern
außerhalb dieser Änderung rot (unter anderem synchrone `setState`-Aufrufe und
`any` in Serienbrief-Tests); an den G3-Zeilen wurde kein Lintfehler gemeldet.
Vor dem DemoDB-End-to-End-Test sind die MAUI-Buildfehler separat zu beheben.

### G3.1 – MAUI-Android-Build wiederhergestellt

Die beiden G3 genannten Kompilierfehler in
`KGV.Maui/Pages/MeterChangeRequestPages.cs` entstanden ausschließlich durch
einen fehlenden Namespace: `Keyboard.Numeric` ist die gültige .NET-10-MAUI-
API aus `Microsoft.Maui`, die Datei importierte bisher nur
`Microsoft.Maui.Controls`. G3.1 ergänzt `using Microsoft.Maui;`; weder
Zählerwechsel-, RFID- noch JEA-Fachlogik wurden geändert.

`dotnet build KGV.Maui/KGV.Maui.csproj --no-restore` und
`npm run test:maui-valid-access` sind danach erfolgreich. Verbleibend sind
nur bereits bekannte Warnungen: die ImageSharp-Sicherheitswarnungen und die
veraltete `DisplayActionSheet`-Überladung in derselben Seite.

### G3.2 – fehlgeschlagene Bereichsmigration abgesichert

Der fehlgeschlagene DemoDB-Lauf von `20261104230000` wurde durch den aus der
Basismigration stammenden CHECK-Constraint
`hauptzaehler_messstelle_bereich_chk` verursacht: Er erlaubte noch
`nord`/`sued`, wurde aber unter seinem tatsächlichen Namen nicht entfernt,
bevor die Migration `Nord`/`Süd` schrieb. Da die Migration mit `begin` läuft,
bleiben die Messstellen, RFID-UIDs, Zähler, Ablesungen und Historie nach dem
Fehler unverändert.

Die korrigierte, weiterhin noch offene Migration entfernt vor der
Normalisierung sowohl diesen tatsächlichen Namen als auch die zwei bereits
berücksichtigten Varianten. Sie prüft anschließend vor jedem Update, ob die
Normalisierung doppelte Messstellen oder doppelte aktive Hauptzähler je
Medium/Bereich erzeugen würde. Im Kollisionsfall bricht sie kontrolliert ab;
sie führt keine RFID- oder Zählerdaten zusammen. Danach werden die
kanonischen CHECK-Constraints und der einzelne bestehende Partial-Unique-
Index wiederhergestellt. Die Rollout-Checks geben nun alle CHECK-Constraints
beider Tabellen sowie Normalisierungskollisionen aus.

Die Datei `20261104230000_zaehler_hauptzaehler_spalten.sql` wurde mit einem
strikten UTF-8-Decoder geprüft: Sie ist gültiges UTF-8 ohne BOM und enthält
das Zeichen `ü` korrekt. Eine Anzeige als `S├╝d` ist deshalb ein
Konsolen-Dekodierungsproblem, kein Fehler der SQL-Datei.

## Abgrenzung

### G3.4 – Trigger-Validierung für Hauptzähler

Der SQL-Funktionstest nach dem Rollout hat einen echten Bestandskonflikt
sichtbar gemacht: Die Triggerfunktionen
`trg_zaehler_validate_medium_allowed()` und
`trg_zaehler_validate_parzelle_rfid()` haben bisher jeden Zähler als
Parzellenzähler behandelt. Bei einem Hauptzähler ist `parzelle_id` jedoch
verbindlich `NULL`; die Medium-Prüfung las dadurch keinen Parzellendatensatz
und meldete folgerichtig „Parzelle &lt;NULL&gt; ist nicht als stromführend markiert.“.
Das blockierte auch den Insert des Nachfolgers in
`execute_hauptzaehler_meter_change`.

Die neue, noch nicht ausgeführte Vorwärtsmigration
`20261105110000_hauptzaehler_trigger_validierung.sql` ersetzt ausschließlich
die beiden Funktionskörper. Die bestehenden Trigger bleiben installiert und
werden weder deaktiviert noch entfernt.

- Der ausdrücklich markierte Hauptzählerpfad verlangt weiterhin NULL als
  `parzelle_id`, das Enum-Medium `strom` oder `wasser`, den kanonischen
  Anlagenbereich `Nord` oder `Süd` und eine vorhandene logische Messstelle
  gleicher Kombination. Die bereits vorhandenen CHECK-Constraints bleiben
  unverändert zusätzlich wirksam.
- Die Parzellen-RFID-Prüfung wird nur für diesen Messstellenpfad nicht
  angewendet. RFID wird weiterhin ausschließlich an
  `hauptzaehler_messstelle` gehalten; sie wird nicht auf den physischen
  Hauptzähler kopiert.
- Die beiden Zweige für normale Parzellenzähler sind zeichen- und
  verhaltensgleich aus den bisherigen Triggern übernommen: Prüfung von
  `hat_wasser`/`hat_strom` sowie `rfid_wasser`/`rfid_strom` bleiben bestehen.
  Es gibt insbesondere keinen allgemeinen NULL-Bypass.

`supabase/tests/20261105110000_hauptzaehler_trigger_funktionstest.sql` ist
ein Test für Demo-/Testdatenbanken mit äußerem `BEGIN`/`ROLLBACK`. Er nutzt
die tatsächlichen Enumwerte und die bestehenden RPC-Signaturen, verlangt die
echten Berechtigungen `CanReadMeters` (32) und `CanManageMeters` (64), legt
alle vier Kombinationen an, prüft den Partial-Unique-Index, speichert eine
JEA-Ablesung und vollzieht einen Strom-Nord-Wechsel mit Ausbau-/Einbauablesung
und unveränderter Messstellen-/RFID-Identität. Er bricht vorab ab, wenn
bereits aktive Hauptzähler vorhanden sind, statt Bestandsdaten zu verändern.

Die Migration und der Test wurden in diesem Arbeitsschritt bewusst nicht
gegen DemoDB oder VereinsDB ausgeführt. Vor dem nächsten Rollout ist der
Test unter einer berechtigten Admin-/Vorstandsidentität auf einer Test- oder
DemoDB ohne aktive Hauptzähler auszuführen.

G1 hat keine Migration erzeugt, keine UI geändert, keine RFID-Zuordnung
geschrieben, keine Verbrauchswerte berechnet und keine Umlage gebucht.

## Ist-Stand-Prüfung – 05.11.2026

Diese Ergänzung dokumentiert nur lokal überprüfbare Erkenntnisse. Es wurden
weder Migrationen ausgeführt noch Daten in DemoDB oder VereinsDB geändert.

### Verifizierte Ergebnisse

- `dotnet build KGV.Infrastructure/KGV.Infrastructure.csproj --no-restore`:
  **erfolgreich** (0 Fehler; bestehende ImageSharp-Sicherheitswarnungen).
- `dotnet build KGV.Maui/KGV.Maui.csproj --no-restore`:
  **erfolgreich** (0 Fehler; 25 bestehende ImageSharp-Sicherheitswarnungen).
- `npm run build` in `KGV.Web`: **erfolgreich**. Verbleibend sind nur die
  bekannten Hinweise zu großen Chunks und dynamisch nicht klassifizierbaren
  Routen.
- `npm run test:maui-valid-access`: **erfolgreich**.
- `git diff --check`: **erfolgreich**; die ausgegebenen CRLF-Hinweise sind
  keine Whitespace-Fehler.
- `npx supabase migration list --linked`: Die fünf lokalen Migrationen
  `20261101130000`, `20261102224500`, `20261103090000`,
  `20261104230000` und `20261105110000` sind sowohl in DemoDB
  (`ymxhszxfutjnljiuciyj`) als auch in der VereinsDB
  (`itjcabiibuodkxayhvjq`) als angewendet verzeichnet. Dies bestätigt nur
  die Migrationshistorie, nicht die fachliche Funktion.
- `npx supabase db push --linked --yes` gegen DemoDB: Die zusätzliche
  Vorwärtsmigration `20261105200000_hauptzaehler_meter_change_medium_cast`
  wurde erfolgreich angewendet. Sie korrigiert ausschließlich den
  Enum-/Text-Vergleich in `execute_hauptzaehler_meter_change`.
- Nach ausdrücklicher Freigabe wurde dieselbe Migration per verifiziertem
  Dry-Run und `npx supabase db push --linked --yes` auch auf die VereinsDB
  angewendet. Es wurden keine Produktions-Funktionstests und keine
  Zählerdatenänderungen durchgeführt.

### G3.5 – aktueller Befund

**Erfolgreich gegen DemoDB getestet.** Der Test wurde in zwei getrennte
Dateien aufgeteilt:

- `20261105110000_hauptzaehler_trigger_strukturtest.sql` prüft ohne
  Benutzerkontext die vier Messstellen, kanonische Werte, Index, RPC,
  Trigger und RLS-Policy.
- `20261105110000_hauptzaehler_trigger_funktionstest.sql` erzeugt innerhalb
  einer äußeren `BEGIN`/`ROLLBACK`-Transaktion eine temporäre Auth-,
  Mitglied- und App-Benutzeridentität mit `CanReadMeters` (32) und
  `CanManageMeters` (64), setzt deren JWT-Claims lokal und testet die
  produktiven RPCs.

Beide Tests liefen am 05.11.2026 über `npx supabase db query --linked --file
...` gegen DemoDB fehlerfrei durch. Der RPC-Test deckt Hauptzähler-Einbau,
JEA-Ablesung, Duplikatschutz, Zählerwechsel, Ausbau-/Einbauablesungen und die
unveränderte Messstellen-/RFID-Identität ab. Sämtliche Testidentitäten und
Zählerdaten wurden zurückgerollt.

Während des Tests wurde ein echter RPC-Fehler entdeckt: Der Vergleich zwischen
`hauptzaehler_messstelle.medium` (`text`) und `zaehler.medium`
(`zaehler_medium`) war nicht typkompatibel. Die neue, ausschließlich auf
DemoDB angewendete Vorwärtsmigration
`20261105200000_hauptzaehler_meter_change_medium_cast.sql` korrigiert ihn
durch den expliziten Cast `v_old.medium::text`.

### Konsequenz für die Gruppen

- G1 und G2: **implementiert/dokumentiert**; die zugehörigen Migrationen
  sind in DemoDB und VereinsDB angewendet, die Struktur selbst ist jedoch
  noch nicht erneut katalogbasiert geprüft.
- G3/G3.1: Client- und Build-Stand **erfolgreich gebaut**.
- G3.2 und G3.4: Migrationen und Testdateien **vorhanden**; die
  Migrationshistorie ist bestätigt.
- G3.5: **erfolgreich auf DemoDB getestet**. Die nach dem Test angelegte
  RPC-Korrektur ist in DemoDB und nach ausdrücklicher Freigabe auch in der
  VereinsDB angewendet; ein Produktions-Funktionstest steht nicht an.
- G4, G5, G6, G7 und G8: **offen**.

Der nächste kleine Arbeitsschritt ist G4: die bereits getrennten RFID-Pfade
für Parzellen und Hauptzähler zu einem gemeinsamen Resolververtrag
zusammenführen, ohne die bestehende Parzellen-RFID-Logik zu duplizieren.

### G4.1 – gemeinsamer RFID-Resolver

Die Vorwärtsmigration `20261105210000_rfid_scan_context_unified.sql` ergänzt
`resolve_rfid_scan_context(text)`. Die nur lesende, mit `CanReadMeters` (32)
geschützte RPC liefert für Parzellen und Hauptzähler denselben Kontextvertrag.
Sie wurde nach Dry-Run ausschließlich auf DemoDB angewendet. Web und MAUI
werden im nächsten G4-Schritt von ihren separaten Nachabfragen auf diese RPC
umgestellt; die VereinsDB bleibt bis zur gesonderten Freigabe unverändert.

## G4-Abschluss und G5-Integrationstest – 05.11.2026

### G4 abgeschlossen

- Web und MAUI verwenden gemeinsam `resolve_rfid_scan_context(text)`; die
  zusätzlichen Hauptzähler-Nachabfragen der Clients sind entfernt.
- Die Migrationshistorien von DemoDB (`ymxhszxfutjnljiuciyj`) und VereinsDB
  (`itjcabiibuodkxayhvjq`) wurden verglichen. Vor dem Rollout fehlte in der
  VereinsDB ausschließlich `20261105210000_rfid_scan_context_unified.sql`.
- Der VereinsDB-Dry-Run enthielt ausschließlich diese erwartete Migration.
  Sie wurde danach mit ausdrücklicher Freigabe ausgerollt. Es gab keine
  Zähler-, RFID- oder Ablesungsänderung in der VereinsDB.
- Nach dem Rollout besitzen beide Datenbanken dieselbe Funktion
  `resolve_rfid_scan_context(text)` (Definitionshash
  `148746474d5944aacb672eca2610fa85`), `SECURITY DEFINER`, EXECUTE für
  `authenticated` und kein EXECUTE für `public`. Die Funktionsdefinition
  prüft serverseitig `has_effective_permission(32)` (`CanReadMeters`), sodass
  ein vorhandener EXECUTE-Grant ohne Leseberechtigung keinen Kontext liefert.

### G5 – erfolgreich auf DemoDB automatisiert getestet

`supabase/tests/20261105210000_rfid_scan_context_integrationstest.sql` läuft
vollständig in `BEGIN`/`ROLLBACK`. Die temporären Auth-, Mitglied-, App-User-,
RFID-, Zähler- und Ablesedaten bleiben damit nicht in der DemoDB zurück.

- Bestehende Parzellen-RFID wird mit normalisiertem Tag unverändert über den
  gemeinsamen Resolver aufgelöst.
- Für Strom Nord, Strom Süd, Wasser Nord und Wasser Süd werden jeweils
  Messstelle, Medium, Bereich und aktiver Hauptzähler geprüft; für jede der
  vier Messstellen wird eine JEA-Ablesung über den produktiven RPC erfasst.
- Unbekannte und leere Tags liefern keinen Kontext. Eine Messstelle ohne
  aktiven Zähler bleibt auflösbar, enthält aber keine aktive Zähler-ID.
- Ein Benutzer mit `CanReadMeters` erhält Kontext; ein Benutzer ohne diese
  Berechtigung erhält den erwarteten Berechtigungsfehler.
- Der vorhandene G3.5-Regressionslauf wurde wiederholt: Hauptzähler-Einbau,
  aktiver-Zähler-Eindeutigkeit, Strom-Nord-Wechsel, Ausbau-/Einbauablesungen,
  Historisierung und die unveränderte RFID-Zuordnung an der Messstelle sind
  erfolgreich. Der Test ist jetzt unabhängig von einer hinter der Demo-Tabelle
  liegenden Mitglieds-ID-Sequenz und verändert diese Sequenz nicht.

### Offen / bewusst nicht als erfolgreich gemeldet

- Es wurden keine echten Web-Browser-, MAUI-Geräte- oder Kamera/Scanner-Tests
  ausgeführt. Der gemeinsame Service-/RPC-Vertrag und die Client-Builds sind
  geprüft; die Bedienabläufe RFID-Scan, manuelle Auswahl und Anzeige auf
  Android bzw. im Browser benötigen noch einen manuellen Abnahmelauf.
- G6 wird nicht begonnen. Als nächster CX-Schritt ist ein manueller
  Abnahmelauf auf beiden Clients mit je einer normalen Parzellen- und einer
  Hauptzähler-RFID pro Medium sinnvoll; dabei insbesondere Nord/Süd,
  manuelle Auswahl und ein Hauptzählerwechsel im UI protokollieren.
