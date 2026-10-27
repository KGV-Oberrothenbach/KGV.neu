# NB-2A-E1 – Audit Mitgliedsnummern in aktiven Exporten

Stand: 27.10.2026 · Audit auf Commit `868e93f6322bc8899dd9c4788112082c6bd98ce6`;
E4-Code-Abgleich auf Commit `1267c878fa9d47305105952628107d67aee208c8`.

## Auftrag und Abgrenzung

Dieses Dokument ist ein reines Bestandsaudit. Es ändert weder Produktcode noch
SQL, Exportmetadaten, Vorlagen oder Daten. Als fachliche Mitgliedskennung gilt
ausschließlich die gespeicherte Datenbankspalte `mitglied.mitgliedsnummer`.
`mitglied.id`, `mitglied_id`, `member_id`, berechnete Werte wie `id + 1000`
und die Legacy-Hilfe `Fachnummer.Mitglied` sind keine zulässigen sichtbaren
Ersatzwerte.

Gezählt werden sieben derzeit aktive Listen-/Downloadpfade: sechs aktivierte
Definitionen im metadatengesteuerten Exportzentrum und der separat gestartete
JAE-XLSX-Download. CSV und PDF derselben Definition sind nicht doppelt gezählt:
Sie verwenden dieselbe Zeilenquelle und dieselben auswählbaren Metadaten.

PDF-Verträge und sonstige erzeugte Mitgliedsdokumente sind keine
Listenexporte. Sie wurden dennoch gegen den realen Code abgeglichen und sind
als separate Dokumentpfade unten nachgewiesen; ihr Abschluss ist bereits in
`NB2A_MITGLIEDSNUMMER_PDF_AUDIT.md` dokumentiert. Hochgeladene historische
Dokumente, Archivmaterial unter `_Archiv`, Recovery-Dateien und Buildartefakte
sind nicht aktiv und nicht mitgezählt.

Für die fachliche Bewertung gilt verbindlich: Mitgliedsnummern werden bei
mitglieds- bzw. personenbezogenen Exportzeilen verwendet. Bei parzellen-,
zähler- oder verbrauchsbezogenen Zeilen wird keine Mitgliedsnummer ergänzt,
wenn die Person nur als Kontext angezeigt wird.

## Prüfmethode und Architektur

- Web: `ExportCenter` lädt nur aktive `app_export_definition`-Einträge,
  wählt Spalten aus `app_export_column_definition`, lädt Zeilen über
  `rpc_execute_export` und erzeugt CSV; PDF geht über
  `kgv-generate-export-pdf`. Zusätzlich erzeugt `downloadJaeXlsx` den
  einzigen separaten XLSX-Pfad.
- MAUI: `ExportViewModel` verwendet dieselben Exportdefinitionen und
  Zeilenquellen. CSV wird lokal aus den sichtbaren Metadatenspalten erzeugt;
  PDF verwendet ebenfalls die zentrale Edge Function und wird nur angezeigt/
  geteilt.
- Supabase: `rpc_execute_export` lässt nur aktive RPC-Definitionen zu und
  ruft deren in `app_export_definition.quelle_name` hinterlegte JSON-Wrapper
  auf. Spalten dürfen nur über die zugehörige Metadatentabelle sichtbar
  angeboten werden. Damit ist die Metadatenliste für CSV, Web-/MAUI-Vorschau
  und zentral gerendertes PDF maßgeblich.
- Core/Infrastructure: `MitgliederCsvExportBuilder` und `ExportPdfBuilder`
  haben keine aktive Aufrufstelle; sie sind daher **NICHT AKTIV**. Die aktuelle
  MAUI-PDF-Route verwendet `GenerateExportPdfAsync`, nicht `ExportPdfBuilder`.

Die Migrationen sind Quelldefinitionen des aktiven Schemas; neuere Definitionen
oder `create or replace function`-Stände wurden gegenüber älteren Treffern
vorrangig bewertet.

## Aktive Listen- und Downloadexporte

| Bereich | Export | Plattform | Ausgabeformat | Datenquelle | sichtbare Personenreferenz | aktueller Nummernwert | Soll | Bewertung | Änderung nötig |
|---|---|---|---|---|---|---|---|---|---|
| Mitglieder | Mitgliederliste | Web, MAUI | CSV, PDF | `rpc_export_mitgliederliste` über Gateway | eigene Zeile je Mitglied; Spalte `Mitglieds-Nr.` | `mitgliedsnr` und `mitgliedsnummer` stammen aus `g.mitgliedsnummer`; kein ID-Fallback | gespeicherte `g.mitgliedsnummer`; danach numerisch sortieren | **OK** | Nein; korrigiert durch die versionierte E2-Migration. |
| Arbeitsstunden | Arbeitsstundenübersicht | Web, MAUI | CSV, PDF | `rpc_export_arbeitsstunden_uebersicht` über JSON-Wrapper | eine Zeile je Hauptmitglied und Saison; Vor-/Nachname sichtbar | `mitgliedsnummer` aus `m.mitgliedsnummer`; `mitglied_id` bleibt intern und ist keine Metadatenspalte | sichtbare Spalte `Mitgliedsnummer` aus `m.mitgliedsnummer` | **OK** | Nein; korrigiert durch die versionierte E3-Migration. |
| Datenschutz | Datenschutz – aktive Mitglieder | Web, MAUI | CSV, PDF | `rpc_export_datenschutz_aktive_mitglieder` über JSON-Wrapper | eine Zeile je aktivem Mitglied; Name sichtbar | `mitgliedsnummer` aus `m.mitgliedsnummer`; keine sichtbare technische ID | sichtbare Spalte `Mitgliedsnummer` aus `m.mitgliedsnummer` | **OK** | Nein; korrigiert durch die versionierte E3-Migration. |
| Zähler/JAE | Jahresablesung – Status | Web, MAUI | CSV, PDF | `rpc_export_jahresablesung_status` über JSON-Wrapper | Zähler-/Parzellenzeile; `Pächter am Jahresende` nur als Name | keine Mitgliedsnummer; keine sichtbare `mitglied_id`; `zaehler_id` ist als technische, standardmäßig unsichtbare Spalte markiert | keine Mitgliedsnummer | **NICHT ERFORDERLICH** | Nein. Die Exportzeile repräsentiert fachlich Zähler bzw. Parzelle; der Pächtername dient nur als Kontext. |
| Zähler | Zähler – Eichfälligkeit | Web, MAUI | CSV, PDF | `rpc_export_zaehler_eichfaelligkeit` über JSON-Wrapper | keine Person, nur Parzelle/Zähler/RFID/Status | keine Mitgliedsnummer; `zaehler_id` ist als technische, standardmäßig unsichtbare Spalte markiert | keine Mitgliedsnummer | **NICHT ERFORDERLICH** | Nein. Die technische Zähler-ID betrifft keine Mitgliedskennung. |
| Parzellen | Parzellenliste | Web, MAUI | CSV, PDF | `rpc_export_parzellenliste` über Gateway | Parzellenzeile; aktuelle Pächter als Namensliste | keine Nummer sichtbar; keine `mitglied_id` in der Ausgabe | keine Mitgliedsnummer | **NICHT ERFORDERLICH** | Nein. Die Exportzeile repräsentiert die Parzelle; aktuelle Pächter werden nur als Kontext angezeigt. |
| Jahresabschluss/JAE | JAE-XLSX | Web | XLSX | `rpc_jahresverbrauch_segmente` und Parzellenübersicht; ExcelJS | erweiterte Tabelle zeigt `Pächter` als Namen, Legacy-Blatt zeigt keinen Pächter | keine Mitgliedsnummer sichtbar; `mitglied_id` bleibt nur im Segmentmodell und wird nicht in Zellen geschrieben | keine Mitgliedsnummer | **NICHT ERFORDERLICH** | Nein. Die Ausgabe ist parzellen-/verbrauchsbezogen; der Pächtername dient nur der Orientierung. Die interne `mitglied_id` darf nicht sichtbar als Mitgliedsnummer verwendet werden. |

### Relevante aktive Dateien

- Web-Exportzentrum und Formate: `KGV.Web/features/export/ExportCenter.tsx`,
  `KGV.Web/services/export/export-service.ts`,
  `KGV.Web/services/export/export-csv-service.ts`,
  `KGV.Web/services/export/export-pdf-service.ts`,
  `KGV.Web/services/export/jae-xlsx-service.ts`.
- MAUI-Exportzentrum: `KGV.Maui/ViewModels/ExportViewModel.cs` und
  `KGV.Maui/Pages/ExportPage.cs`.
- Metadaten/Gateway: `KGV.Web/repositories/export/export-definition-repository.ts`,
  `KGV.Web/repositories/export/export-data-repository.ts`,
  `supabase/migrations/20261015210000_generic_export_gateway.sql`.
- Exportprojektionen und Definitionen: die Migrationen
  `20260923223000_export_arbeitsstunden_uebersicht.sql`,
  `20260923234500_export_jahresablesung_status.sql`,
  `20261003100000_export_datenschutz_aktive_mitglieder.sql`,
  `20261004120000_export_zaehler_eichfaelligkeit.sql`,
  `20261015193000_add_mitgliederliste_export_metadata.sql`,
  `20261015220000_add_parzellenliste_export.sql` und
  `20261021110000_fix_mitgliederliste_grouping_and_layout.sql`.

## Generisches Exportgateway: fachliche Bewertung

Die Definitionstabellen enthalten für die Mitgliederliste eine auswählbare,
fachlich eindeutig beschriftete Spalte `mitgliedsnr` / `Mitgliedsnummer`.
Das ist konzeptionell richtig, ist aber aktuell fehlerhaft befüllt: die jüngste
`rpc_export_mitgliederliste` setzt beide JSON-Werte auf `g.id::text`. Dies ist
der konkrete sichtbare technische-ID-Fallback dieses Audits.

Für die Arbeitsstundenübersicht bleibt `mitglied_id` eine interne
Rückgabespalte. Weil die Metadaten keine Spalte mit diesem Schlüssel definieren,
kann sie im normalen Web-/MAUI-CSV, -PDF und in der Vorschau nicht gewählt
werden. Das ist korrekt; es fehlt stattdessen die separate fachliche Spalte.
Für JAE/XLSX verbleibt `mitglied_id` ebenfalls nur im internen Segmentmodell.

`zaehler_id` ist in zwei zählerbezogenen Definitionen technisch beschriftet
und standardmäßig unsichtbar. Sie identifiziert keinen Menschen und ist daher
kein NB-2A-Mitgliedsnummernfehler. Admins können im generischen Zentrum nur
die hinterlegten Spalten wählen; es gibt keine UI zum freien Anlegen oder
Umbenennen von Exportspalten. Eine spätere Metadatenänderung muss die Trennung
zwischen `mitglied_id` (intern) und `mitgliedsnummer` (fachlich sichtbar)
beibehalten.

## Jahresabschluss und Abrechnung

Der Code enthält zwei aktive, getrennt zu bewertende JAE-Ausgaben:

1. `jahresablesung_status` als generischer CSV/PDF-Export: Die Zeile
   repräsentiert einen Zähler. Der Pächtername ist Kontext, die technische
   Mitglieds-ID wird nicht ausgegeben. Nach der verbindlichen Fachentscheidung
   wird keine Mitgliedsnummer ergänzt.
2. `JAE-XLSX`: Das Legacy-Blatt enthält nur Parzellen- und Verbrauchswerte;
   die erweiterte Tabelle führt den Pächter als Namen. Der Segment-RPC enthält
   intern `mitglied_id`, doch die Excel-Erzeugung schreibt stattdessen nur
   `pachtbezug`. Die Ausgabe ist parzellen-/verbrauchsbezogen; deshalb wird
   keine Mitgliedsnummer ergänzt. Die interne `mitglied_id` bleibt als rein
   technische Zuordnung zulässig und darf nicht sichtbar als Mitgliedsnummer
   verwendet werden.

Rechnungs- oder personenbezogene Abrechnungsexporte wurden im aktiven
Web-, MAUI-, Core-, Infrastructure- und Supabase-Code nicht gefunden. Der
Begriff „JAE“ ist hier eine Jahresverbrauchs-/Ablesungsauswertung, keine
mitgliederbezogene Rechnungsliste.

## Abgleich mit aktiven PDF- und Dokumentpfaden

Die folgenden aktiven, personenbezogenen Dokumenterzeuger sind keine
Listenexporte und daher nicht in der Zahl sieben enthalten. Ihr tatsächlicher
Code wurde gegen `Documentation/NB2A_MITGLIEDSNUMMER_PDF_AUDIT.md` abgeglichen:

| Bereich | Pfad | Bewertung | Hinweis |
|---|---|---|---|
| Mitgliedsantrag | lokaler und Edge-Erzeuger | NICHT ERFORDERLICH | Antragsteller erhält fachlich die getrennte Antragsnummer, nicht die Mitgliedsnummer. |
| Mitgliedsvertrag | lokal und Edge | OK | D2 hat die sichtbare ID durch die gespeicherte Mitgliedsnummer ersetzt. |
| Pachtvertrag | lokaler und Edge | OK | D4 verwendet die gespeicherte Mitgliedsnummer für die fachlich vorgesehenen Felder. |
| Wartungsvertrag und Verlängerung | Edge/Prepared-Contract | OK | D3 korrigierte Snapshots und Anzeige auf die kanonische Nummer. |
| Parzellenprotokoll | lokal und Edge | OK | D5B zeigt für Pächter/in 1 `Mgl.-Nr.` aus dem gespeicherten Wert; Funktionen/Begleitung bleiben nummernlos. |
| Aufnahmebestätigung | Edge/Workflow | OK | D5C zeigt Antrags- und Mitgliedsnummer getrennt aus kanonischen Quellen. |
| Signaturergänzung | lokaler Dokumentpfad | OK | kein sichtbarer technischer Mitglieds-ID-Fallback mehr. |
| Vereinsunterlagen/historische Dokumente | Storage/Archiv | NICHT ERFORDERLICH | keine neu generierte Mitgliederreferenz; technische Zuordnung bleibt intern. |

## Dateinamen: nur Vormerkung

Die aktiven Listenexporte verwenden nur Exporttitel/-schlüssel und Datum
(`exportFileName`, Web; Zeitstempel plus `ExportKey`, MAUI; Edge: Titel plus
Datum). Es wurde keine aktive Datei mit einer sichtbaren `mitglied.id` als
Mitgliedsnummer gefunden. Dokument- und Vorschaupfade können technische IDs
in technischen Storage-/Dateinamen führen; das ist
**→ späterer NB-2A-Dateinamenblock** und wird hier nicht verändert.

## Nicht aktive oder ausgeschlossene Treffer

- `KGV.Core/Utilities/MitgliederCsvExportBuilder.cs` erzeugt zwar eine
  sichtbare Spalte `Id`, hat aber keine aktive Aufrufstelle. Bewertung:
  **NICHT AKTIV**; nicht als Produktbefund zählen.
- `KGV.Core/Utilities/ExportPdfBuilder.cs` ist ebenfalls nicht an die aktive
  MAUI- oder Web-PDF-Schaltfläche angeschlossen. Bewertung: **NICHT AKTIV**.
- `_Archiv`, Recovery-Dateien, Tests, historische Dokumentation und
  Buildausgaben wurden nicht als aktive Produktpfade gewertet.

## NB-2A-E2 – umgesetzt am 27.10.2026

Die versionierte, nicht ausgeführte Migration
`20261027100000_nb2a_e2_mitgliederliste_mitgliedsnummer.sql` ersetzt die
aktuelle `rpc_export_mitgliederliste`-Definition. Die sichtbaren JSON-Felder
`mitgliedsnr` und `mitgliedsnummer` lesen ausschließlich
`g.mitgliedsnummer::text`; es gibt weder eine Berechnung noch einen Fallback
auf `g.id`. Eine fehlende oder ungültige Nummer erzeugt den fachlichen Fehler
`MEMBER_NUMBER_MISSING`. Die Sortieroption `mitgliedsnr` verwendet den
numerischen Datenbankwert `g.mitgliedsnummer`.

Die Exportmetadaten wurden nicht geändert. Der gezielte Contract-Test
`member-list-membership-number.contract.test.mjs` sichert die beiden
Projektionen, die Fehlerschranke, die Sortierung sowie die weiterhin gemeinsame
Web-/MAUI-Zeilenquelle für CSV und PDF ab. Die Migration wurde nicht ausgeführt;
es gab keinen Datenbank-Push und keine Produktivänderung.

**NB-2A-E2 = 100 %**

## NB-2A-E3 – umgesetzt am 27.10.2026

Die versionierte, nicht ausgeführte Migration
`20261027110000_nb2a_e3_personenexporte_mitgliedsnummer.sql` ergänzt die
beiden personenbezogenen Standardexporte um `mitgliedsnummer` aus der
gespeicherten Spalte `mitglied.mitgliedsnummer`. In der Arbeitsstundenübersicht
bleibt `mitglied_id` ein internes Rückgabefeld und ist nicht als auswählbare
Exportmetadatenspalte vorhanden. Beide RPCs verweigern eine exportierte Zeile
mit fehlender oder ungültiger Nummer mit `MEMBER_NUMBER_MISSING`; es gibt keinen
ID-Fallback und keine Berechnung.

Die neue Metadatenspalte trägt in beiden Exporten die Bezeichnung
`Mitgliedsnummer` und steht im vorderen Personenbereich. Web und MAUI lesen
weiter dieselbe RPC-/Metadatenquelle für Vorschau, CSV und zentral erzeugtes
PDF. Die Migration wurde nicht ausgeführt; es gab keinen Datenbank-Push und
keine Produktivänderung.

**NB-2A-E3 = 100 %**

## NB-2A-E4 – Fachentscheidungen abgeschlossen am 27.10.2026

Für **Jahresablesung – Status**, **Parzellenliste** und **JAE-XLSX** ist die
Bewertung verbindlich **NICHT ERFORDERLICH**. Die Zeilen sind jeweils zähler-,
parzellen- oder verbrauchsbezogen; sichtbare Pächterangaben geben lediglich
Kontext beziehungsweise Orientierung und machen die Zeile nicht zu einer
mitglieds- oder personenbezogenen Exportzeile.

Der Code-Abgleich auf Commit
`1267c878fa9d47305105952628107d67aee208c8` bestätigt den E1-Stand:

- Jahresablesung – Status zeigt keine technische Mitglieds-ID.
- Die Parzellenliste zeigt keine technische Mitglieds-ID.
- JAE-XLSX schreibt `mitglied_id` nicht in sichtbare Excel-Zellen. Ein kleiner
  bestehender Exporttest sichert dies zusätzlich ab.

Damit ist keine Produktänderung erforderlich. Insbesondere ist keine sichtbare
technische Mitglieds-ID vorhanden. Eine interne `mitglied_id` bleibt für die
technische Zuordnung zulässig. Es sind keine Migration, kein Deployment und
keine Datenbankänderung erforderlich.

**NB-2A-E4 = 100 %**

## Abschluss und offener Folgeblock

1. Aktive Listen-/Downloadexportpfade: **7**.
2. **OK: 3** innerhalb dieser sieben Listenpfade – Mitgliederliste,
   Arbeitsstundenübersicht und Datenschutzexport.
3. **FEHLER: 0**.
4. **FEHLT: 0**.
5. **NICHT ERFORDERLICH: 4** – Jahresablesung – Status,
   Zähler – Eichfälligkeit, Parzellenliste und JAE-XLSX.
6. **ENTSCHEIDUNG ERFORDERLICH: 0**.
7. Der frühere technische-ID-Fallback der Mitgliederliste ist durch E2
   beseitigt; `mitglied_id` bleibt nur in internen Zuordnungen.
8. Die kanonische Nummer ist in allen drei personenbezogenen Standardlisten
   aus der gespeicherten Spalte verfügbar.
9. Bereits korrekt sind die separat geprüften aktiven
   PDF-/Dokumentgeneratoren (sieben personenbezogene Erzeugergruppen oben).
10. Keine aktive Listenexport-Datei mit Mitglieds-ID-Fund; technische
    Dokumentdateinamen sind für den späteren Dateinamenblock vorgemerkt.

Der Export-Fachentscheidungsblock E4 ist abgeschlossen. **E5 Dateinamen bleibt
offen** und erfordert einen separaten Audit der technischen IDs in Dokument-/
Vorschaudateinamen.

**NB-2A-E1 Export-Audit = 100 %**

**NB-2A Exporte noch NICHT = 100 %**
