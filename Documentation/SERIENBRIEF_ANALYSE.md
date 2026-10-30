# Serienbrief – Bestandsanalyse und Anschlussstellen (G1)

Stand: 2026-10-31  
Geltungsbereich: Analyse des vorhandenen KGV-Bestands; **keine Serienbrieffunktion implementiert**.

## Ergebnis in Kürze

Der Bestand enthält die wesentlichen Bausteine, die für Serienbriefe weiterzuverwenden sind:

- metadatengesteuerte Exporte mit einem sicheren, JSON-basierten Gateway,
- den veröffentlichten, privaten PDF-Vorlagenkatalog,
- eine fachlich leere Briefbogen-Vorlage,
- das Dokumentarchiv und Mitglieds-/Parzellenbezüge,
- die gemeinsame Kommunikationszentrale mit Mailausgang und Druckausgabe,
- den bestehenden Standard-Mailtext einschließlich Datenschutzhinweis.

Nicht vorhanden ist noch der verbindende Serienbrief-Workflow: eine persistente Serienbriefvorlage, ein typisierter Datenvertrag je Exportzeile, eine Batch-/Einzelbrief-Provenance sowie die Zuordnung erzeugter Briefe zu Mail- oder Druckvorgängen. Diese Lücke soll durch Anschluss an die vorhandenen Bausteine geschlossen werden, nicht durch eine zweite Export-, PDF-, Dokument- oder Versandarchitektur.

## 1. Vorhandene Komponenten, die wiederverwendet werden sollen

| Fachbereich | Bestand | Wiederverwendung für Serienbrief |
| --- | --- | --- |
| Exportdefinition und Filter | `app_export_definition`, `app_export_filter_definition`, `app_export_column_definition`; Web: `KGV.Web/services/export/export-metadata-service.ts` | Vorlage speichert Feldschlüssel gegen die Exportmetadaten. Beim Start werden Definition, Filterwerte und komplette Ergebnisdaten erneut über den bestehenden Weg geladen. |
| Vollständige Exportdaten | `rpc_execute_export(p_export_key, p_parameters)` und JSON-Wrapper-RPCs; Web: `executeExport` | Eine Zeile wird nur verarbeitet, wenn `mitglied_id` eindeutig vorhanden ist. Die sichtbare Spaltenauswahl darf die verfügbaren Vorlagenfelder nicht reduzieren. |
| Exportoberflächen | Web `KGV.Web/features/export/ExportCenter.tsx`, `ExportResults.tsx`; MAUI `KGV.Maui/Pages/ExportPage.cs`, `KGV.Maui/ViewModels/ExportViewModel.cs` | Einstieg **Export → Filter → Serienbrief** an das jeweils vorhandene Ergebnis anbinden. |
| Briefbogen | `KGV.Core/Templates/briefbogen-allgemein/v2/template.pdf` mit `field-spec.json` | Als verbindliches Grundlayout verwenden. Bereits vorgesehen: `empfaenger_block`, `datum`, `betreff`, `anrede`, `brieftext`, `abschluss` und die Absender-/Fußzeilenfelder. |
| PDF-Vorlagenkatalog | `pdf_template_catalog`, privater Bucket `pdf-templates`; .NET: `IPdfTemplateProvider`, `SupabasePdfTemplateProvider` | Veröffentlichten Briefbogen über den Katalog laden, inklusive Version, Feldspec und Prüfsummenprüfung. Keine lokale Produktivkopie und kein stiller Fallback. |
| PDF-Rendering | `KGV.Core/Utilities/PdfTemplateRenderer.cs`, `PdfSharpCore`; bestehende Dokumentpipelines | Neuen Serienbrief-Renderer nur als fachlichen Aufrufer des vorhandenen Renderers/Providers ausprägen. HTML-/Rich-Text muss vor dem PDF-Rendering in einen begrenzten, deterministischen Text-/Layoutvertrag überführt werden. |
| Dokumentablage | Tabelle `dokument`, Google-Drive-Anbindung, `kgv-upload-document`; Web: `document-upload-service.ts`, `document-service.ts` | Jedes finale PDF als reguläres Dokument erzeugen und exakt dieses Dokument an Folgeprozesse übergeben. |
| Mitglied/Parzelle | `dokument.mitglied_id`, `dokument.parzelle_id`; `parzellen_belegung`/Pachtbezug | Mitglied ist Pflichtbezug. Ein Parzellenbezug wird nur bei eindeutigem Ergebniswert geschrieben. Bestehende Altregel verlangt derzeit genau einen Eigentümerbezug; vor G10/G12 ist die endgültige Dokument-Relation zu verifizieren bzw. gezielt zu erweitern. |
| Kommunikation | `kommunikationsvorgang`; Web: `CommunicationCenter`, `communication-service.ts`, `communication-repository.ts`; MAUI: `CommunicationArea` | Bestehende Bereiche Mailausgang und Druckausgabe verwenden; keine neue Versandwarteschlange neben der Kommunikationszentrale. |
| Mailausgang | `versandinstanz`, Empfänger-/Dokumentsnapshots und Versuche; `prepare_contract_dispatch` | Die vorhandene Snapshot- und Retry-Idee wiederverwenden. Da `versandinstanz` derzeit vertragsartspezifisch eingeschränkt ist, braucht Serienbrief eine gezielte Erweiterung oder einen bestehenden allgemeinen Kommunikationspfad – keinen Parallelversand. |
| Druckausgabe | `kommunikationsvorgang` mit `vorgangstyp='druck'`, Status `druck_bereit`; Abschluss über `completePrintCommunicationItem` | Bei fehlender gültiger Mailfreigabe einen Druckvorgang mit dem bereits archivierten PDF anlegen. |
| Rechte | Rollen Admin/Vorstand, `PermissionFlags`, `PermissionChecks`, DB-Helfer `is_admin_or_vorstand()` | Kein neues Sonderrecht. UI und serverseitige Erzeugungs-/Bearbeitungs-RPCs prüfen Admin oder Vorstand. |

## 2. Heutige Exporte, Filter und Ergebnisstruktur

### Export-Gateway

Der reguläre Weg ist metadatengesteuert:

```text
app_export_definition
  → quelle_name (aktive RPC-/Tabellenquelle)
  → rpc_execute_export(export_key, parameters)
  → setof jsonb
  → Web/MAUI-Ergebniszeilen
```

`rpc_execute_export` prüft mit `assert_export_access()` den Zugriff und ruft ausschließlich in aktiven Metadaten hinterlegte Quellen auf. Parameter werden als `jsonb` gebunden. Die vorhandenen JSON-Wrapper decken mindestens Mitgliederliste, Arbeitsstunden, Jahresablesung, Datenschutz und Zähler-Eichfälligkeit ab. `parzellenliste` ist als metadatengesteuerter Referenzexport dokumentiert.

### Vollständige Ergebnismenge und Anzeigegrenze

Im Web lädt `executeExport` sämtliche Zeilen in `ExportResult.rows`. `ExportResults.tsx` begrenzt ausschließlich die Desktop-Vorschau auf `rows.slice(0, 500)` und weist darauf hin, dass CSV/PDF alle Daten enthalten. Für Serienbriefe darf daher **nie** die sichtbare Vorschauzeilenmenge verwendet werden, sondern immer die vollständige Ergebnisliste beziehungsweise eine serverseitige, gleichwertige Wiederholung derselben Abfrage.

MAUI hält die Ergebnismenge ebenfalls vollständig, materialisiert sie in `ProcessedResults`, wandelt Werte aber zu Zeichenketten und nutzt technische Aliasregeln. Diese Darstellung ist für die Anzeige nützlich, aber keine belastbare Serienbriefdatenquelle für Datum, Zahl, NULL und eindeutige Identifikatoren.

### `mitglied_id`, `parzelle_id` und Sonderfälle

- Die öffentliche Exportanzeige blendet fachlich technische IDs üblicherweise aus. Die Exportrückgabe kann sie enthalten, es gibt jedoch derzeit keinen verbindlichen, exportübergreifenden `mitglied_id`-/`parzelle_id`-Vertrag.
- Die Mitgliederliste löst aktuelle Parzellenbelegung sowie Mitgliedsstammdaten zusammen auf; andere Exporte sind fachlich nicht zwingend mit einer Mitgliedszeile verknüpft (z. B. Auswertungen, Zähler- oder Arbeitsstundenlisten).
- Die Mitgliederliste ist im PDF-Export ein ausdrücklicher Sonderrenderer. JAE-XLSX besitzt einen eigenen Downloadpfad. Solche Sonderfälle dürfen Serienbriefe nicht per UI-/Spaltenheuristik behandeln.

**Folgerung:** G2/G3 braucht eine explizite, serverseitig validierte Serienbrief-Fähigkeit je Exportdefinition und einen Datenvertrag mit mindestens `mitglied_id`, optional `parzelle_id` und den freigegebenen, typisierten Feldwerten. Nur ein solcher Export darf den Serienbrief-Einstieg anbieten.

## 3. Bestehende Tabellen, RPCs und Services

### Betroffene vorhandene Datenbankverträge

| Bereich | Tabellen/RPCs/Funktionen |
| --- | --- |
| Export | `app_export_definition`, `app_export_filter_definition`, `app_export_column_definition`, `rpc_execute_export`, `rpc_execute_table_or_view_export`, export-spezifische JSON-RPCs |
| Mitglieder/Parzellen | `mitglied`, `parzelle`, `parzellen_belegung` sowie die jeweiligen Exportquellen |
| Vorlagen | `pdf_template_catalog`, Storage-Bucket `pdf-templates` |
| Dokumente | `dokument`, `dokumenttyp`, `dokumenttyp_wirkung`, optional `dokument_wartungsvertrag_zuordnung`; Dokumentupload über `kgv-upload-document` |
| Archivierung | `dokument.archiviert_at`, `archiviert_by`, `archiviert_begruendung`; diese Felder bedeuten fachliche Archivierung/Stilllegung, nicht die Serienbrief-Ablage selbst |
| Kommunikation | `kommunikationsvorgang` mit `vorgangstyp`, `dokumenttyp`, `status`, `mitglied_id`, `parzelle_id`, `final_dokument_id`, `empfaenger_email`, `betreff`, `mailtext`, `versendet_am`, `gedruckt_am`, `archiviert_am`, `fehlertext` |
| Vertragsversand | `versandinstanz`, `versandinstanz_empfaenger_snapshot`, `versandinstanz_dokument_snapshot`, `versandinstanz_versuch` sowie `prepare_contract_dispatch` |
| Berechtigung | `app_user.role`, `permission_grants`, `permission_revocations`; `permission_role_base`, `current_effective_permissions`, `has_effective_permission`, `is_admin_or_vorstand` |

### Vorhandene Services und Komponenten

- Web-Export: `export-metadata-service.ts`, `export-service.ts`, `export-data-repository.ts`, `ExportCenter.tsx`, `ExportResults.tsx`.
- MAUI-Export: `ExportPage.cs`, `ExportViewModel.cs`, `ISupabaseService`-Exportmethoden.
- PDF: `IPdfTemplateProvider`, `SupabasePdfTemplateProvider`, `PdfTemplateRenderer`, `DocumentGenerationPipeline`.
- Dokumente: Web-Repositories/-Services unter `KGV.Web/repositories/documents` und `KGV.Web/services/documents`; MAUI `DocumentStorage` und Dokumentseiten; Edge Function `kgv-upload-document`.
- Kommunikation: Web `CommunicationCenter.tsx`, `communication-service.ts`, `communication-repository.ts`; MAUI `KommunikationPage`/`CommunicationArea`.

## 4. Briefkopf, PDF und Standard-Mailtext

### Briefkopf

`briefbogen-allgemein/v2` ist der vorhandene leere KGV-Briefkopf. Seine Feldspec enthält genau die für den Serienbrief benötigten Einsetzpunkte. Empfängername und -adresse sollen dabei nicht aus den ausgewählten Exportspalten, sondern anhand von `mitglied_id` aus dem aktuellen Mitgliedsstamm ermittelt werden. Die Briefanrede wird als fachliches Serienbrieffeld implementiert; ihre Ermittlung folgt der Vorgabe Frau/Herr/Fallback und wird im erzeugten Brief als Snapshot abgelegt.

### PDF

Der Katalogprovider erlaubt ausschließlich aktive, veröffentlichte, versionsgebundene und prüfsummengeprüfte PDF-Vorlagen. Die PDF-Erzeugung kann den vorhandenen Renderer nutzen, erfordert aber für Fließtext eine neue, klar begrenzte Rich-Text-zu-PDF-Aufbereitung. Der Briefbogen hat auf Seite 1 für `brieftext` aktuell eine begrenzte Fläche; mehrseitige Briefe und Überlauf sind deshalb ein fachlich/technisch zu entscheidendes Risiko, nicht implizit durch den bisherigen Feldrenderer gelöst.

### Standard-Mailtext inklusive Datenschutzhinweis

Die verbindliche vorhandene Quelle ist:

`supabase/migrations/20261023140000_vd3_contract_dispatch.sql`, Funktion `public.vertragsversand_mailtext_snapshot()`.

Die Funktion erzeugt den Mailtext inklusive Vertraulichkeits-/Datenschutzhinweis und ergänzt Vereinsname, Anschrift, vertretungsberechtigte Personen, Registerangabe und Standard-E-Mail aus `vereinskonfiguration` und `impressum_kontakte()`. Sie wird beim bestehenden Vertragsversand in `prepare_contract_dispatch` als Snapshot gespeichert. Serienbrief darf keinen eigenen Mailtext erfinden; G13 muss diese Funktion wiederverwenden oder sie fachlich neutral erweitern, ohne den bestehenden Vertragstext zu verändern.

## 5. Bestehende Status-, Mail- und Drucklogik

Die Kommunikationszentrale zeigt heute mindestens folgende relevante Zustände:

```text
dokument_erzeugung: vorbereitet | fehler
mail:               mail_bereit | wird_versendet | fehler | erledigt
druck:              druck_bereit | fehler | erledigt
```

`CommunicationCenter` und die MAUI-`CommunicationArea` nutzen diese Bereiche gemeinschaftlich. Im Druckpfad markiert `completePrintCommunicationItem` einen zugewiesenen, druckbereiten Vorgang mit `erledigt` und `gedruckt_am`.

Der bestehende Vertragsversand snapshottet Empfänger-E-Mail, Einwilligungen und die Versandfähigkeit. Der Versand ist nur zulässig bei verwertbarer Mailadresse und passender Einwilligung (`email_info_einwilligung` oder `email_rechnung_einwilligung` im aktuellen Vertragsversand). Für Serienbriefe ist die fachlich passende, vorhandene Mailfreigabe je Brief zu bestimmen und als Snapshot zu speichern; eine E-Mail-Adresse allein genügt nicht.

## 6. Voraussichtlich neue Tabellen und Felder

Die folgenden Namen sind ein Vorschlag für G2, kein bereits implementierter Datenbankvertrag.

| Objekt | Mindestinhalt und Zweck |
| --- | --- |
| `serienbrief_vorlage` | Name, Betreff-Template, HTML-Fließtext, Briefanrede-Konfiguration, `grundlayout_template_key`/-Version, Aktivstatus, Auditfelder. Export-unabhängig. |
| `serienbrief_vorlage_exportfeld` | Vorlagenfeldschlüssel, Label-/Typ-Snapshot und Pflichtkennzeichen; erlaubt die Kompatibilitätsprüfung gegen einen gewählten Export. |
| `serienbrief_vorlage_freifeld` | Vorlage, Schlüssel/Name, Typ (`text`, `date`, `number`), Sortierung, Pflichtkennzeichen. |
| `serienbrief_lauf` | Vorlage, Exportdefinition, Filter- und Freifeldwert-Snapshots, Start-/Abschlusszeit, Ersteller, aggregierte Anzahl je Ergebnisstatus. |
| `serienbrief_empfaenger` | Lauf, Reihenfolge, `mitglied_id`, optionale `parzelle_id`, Exportdaten-Snapshot, generierter Betreff/Dateiname, PDF-/`dokument_id`, Kommunikationsvorgang, Status, Fehlertext und Zeitstempel. |

Die Einzelbrief-Tabelle ist wesentlich: Sie ermöglicht Teilfehler ohne Abbruch des Laufs, Nachbearbeitung, genaue Statusanzeige sowie idempotente Wiederholung ohne doppelte PDFs/Vorgänge.

Für `kommunikationsvorgang` sollte G2 zunächst prüfen, ob die bereits vorhandenen Felder für Serienbrief-Referenz und PDF-Provenance genügen. Falls nicht, ist eine schmale optionale Referenz `serienbrief_empfaenger_id` plus Eindeutigkeitsregel sinnvoll. Neue fachliche Dokumenttypen oder breit wirkende Änderungen an bestehenden Vertrags-Constraints dürfen erst nach dieser Prüfung erfolgen.

## 7. Voraussichtlich neue Services und Views

- **Serienbrief-Fähigkeitsprüfung:** exportbezogene Prüfung auf eindeutiges `mitglied_id`, optionales `parzelle_id`, Feldkatalog und Berechtigung.
- **Vorlagenservice/-repository:** CRUD, HTML-Sanitizing, freie Felder, Feldabhängigkeiten und Kompatibilitätsprüfung.
- **Serienbrief-Datenservice:** vollständige, typisierte Ergebnismenge und deterministic Snapshot pro Lauf; nicht aus Tabellenansicht oder MAUI-Stringanzeige gewinnen.
- **Renderer/Preview-Service:** Platzhalterauflösung, Briefanrede, Briefkopf und begrenzte Rich-Text-Umsetzung über den zentralen PDF-Vorlagenprovider/-renderer.
- **Batch-Generator:** pro Empfänger idempotente Schritte PDF → Dokument → Kommunikationsvorgang; Fehler isolieren und erfassen.
- **Kommunikationsadapter:** mailfreigabeabhängig Mail- oder Druckvorgang anlegen und vorhandenen Mailtext als Snapshot übernehmen.
- **Web-Views:** Serienbriefstart von Exportergebnis, Empfängerauswahl, Vorlageneditor, Freifeldschritt, Vorschau und Abschlussübersicht.
- **MAUI-Views:** derselbe fachliche Ablauf auf denselben Tabellen/RPCs; keine lokale zweite Erzeugungslogik.

## 8. Bestehende Dateien, die später wahrscheinlich geändert werden

| Bereich | Wahrscheinliche Änderungen |
| --- | --- |
| Web Export | `KGV.Web/features/export/ExportCenter.tsx`, `ExportResults.tsx`, Exportmodelle/-services für Serienbriefeinstieg und vollständigen Datenvertrag |
| Web Navigation/Routing | Navigation und `app/page.tsx`, sofern Serienbrief als Unterprozess der Exportansicht eine Route benötigt |
| Web Kommunikation | `communication-service.ts`, `communication-repository.ts`, `CommunicationCenter.tsx` für Bezeichnung, Filter und Abschluss von Serienbrief-Mail-/Druckvorgängen |
| Web Dokumente | Dokumentservice/-repository für Serienbriefprovenance und Archivdarstellung |
| MAUI Export | `ExportPage.cs`, `ExportViewModel.cs`, ggf. `ISupabaseService`/`SupabaseService` für denselben Ablauf |
| MAUI Kommunikation | `KommunikationPage` und zugehörige ViewModels/Models für Serienbriefvorgänge |
| Core/Infrastructure | PDF-Render-/Datenmodelle, Provider-Requests und DI-Registrierung; bestehender Katalogprovider bleibt zentral |
| Supabase | neue additive Migration(en), gegebenenfalls Erweiterung der Kommunikations- und Versand-RPCs sowie Edge Function nur für den bereits etablierten Upload-/PDF-Ablauf |

## 9. Sinnvolle neue Dateien

Die konkreten Namen können dem bestehenden Namensstil angepasst werden. Sinnvoll sind unter anderem:

- `KGV.Core/Models/Serienbrief*.cs`
- `KGV.Core/Services/SerienbriefTemplateRenderer.cs` und Batch-/Statusmodelle
- `KGV.Web/models/serienbrief/*`, `repositories/serienbrief/*`, `services/serienbrief/*`
- `KGV.Web/features/serienbrief/SerienbriefStart.tsx`, `SerienbriefTemplateEditor.tsx`, `SerienbriefPreview.tsx`, `SerienbriefRecipients.tsx`
- `KGV.Maui/Pages/Serienbrief*.cs` und passende ViewModels, falls MAUI in der jeweiligen Gruppe mitliefert
- eine zeitgestempelte Migration wie `supabase/migrations/<timestamp>_serienbrief_foundation.sql`
- gezielte Unit-, Vertrags- und Integrations-/E2E-Tests für Vorlagenkompatibilität, Anrede, Einzelbrief-Fehlerisolation, Dateinamen, Freigabeentscheidung und Idempotenz.

## 10. Technische Risiken und Leitplanken

1. **Kein stabiler Exportzeilenvertrag für Serienbriefe:** `mitglied_id`/`parzelle_id` sind nicht für jeden Export garantiert oder sichtbar. Die Serienbrieffähigkeit muss metadata-/serverseitig geprüft werden.
2. **Datenvollständigkeit:** Die 500-Zeilen-Grenze ist nur Web-Darstellung, aber eine spätere Implementierung darf nicht versehentlich `preview` statt `result.rows` verwenden.
3. **Web-/MAUI-Typunterschied:** Web hält Rohwerte, MAUI materialisiert Zeichenketten und Aliaswerte. Der Lauf muss auf einem gemeinsamen typisierten Server-Snapshot basieren.
4. **Rich-Text und Mehrseitigkeit:** Der vorhandene PDF-Feldrenderer ist kein HTML-Layoutsystem. Zulässige Tags, Sanitizing, Umbrüche, Listen, Überlauf und weitere Seiten müssen ausdrücklich gestaltet und getestet werden.
5. **Dokumentbezug:** Der historische `dokument_owner_chk` verlangt zumindest in älteren Migrationen genau einen Mitglieds- oder Parzellenbezug; die gewünschte zusätzliche Parzellenverknüpfung ist vor Umsetzung gegen den aktuellen DB-Stand abzusichern.
6. **Versandvertrag:** `versandinstanz` ist derzeit auf Vertragsarten beschränkt. Erweiterung nur als additive Generalisierung oder über den bestehenden allgemeinen Kommunikationsweg; kein zweites Mail-/Drucksystem.
7. **Mailfreigabe:** Die aktuell verwendeten Einwilligungsfelder sind vertragsbezogen dokumentiert. G2 muss festlegen, welche bestehende Freigabe für Serienbriefe zulässig ist und sie als Snapshot sichern.
8. **Idempotenz und Teilfehler:** PDF-/Storage-/Kommunikationsschritte liegen nicht alle in einer SQL-Transaktion. Der Einzelbriefstatus, eindeutige Quellenreferenzen und Retry-Regeln sind Pflicht.
9. **PDF-Dateiname:** `[Datum]_[Vorlagenname]_[Parzelle]_[Nachname].pdf` braucht sichere Bereinigung, einen definierten Platzhalter bei fehlender Parzelle und eine Kollisionsregel.
10. **Rechte:** Die Rollenbasis erlaubt Admin und Vorstand bereits relevante Export-, Dokument- und Stammdatenrechte. Keine neue Capability anlegen; serverseitig trotzdem explizit prüfen.

## 11. Abweichungen zwischen Web und MAUI

| Thema | Web | MAUI | Konsequenz |
| --- | --- | --- | --- |
| Exportergebnis | `ExportResult.rows` enthält Rohobjekte; Desktopvorschau maximal 500 Zeilen | `ProcessedResults` enthält string-materialisierte Werte mit Alias-/Normalisierungslogik | Serienbriefdaten nicht aus einer Clientdarstellung ableiten. |
| Export-PDF | nutzt gemeinsame Edge Function `kgv-generate-export-pdf` | gemeinsamer zentraler PDF-Request dokumentiert, MAUI besitzt zusätzlich ältere Exportmaterialisierung | Serienbrief-PDF als eigener gemeinsamer Server-/Core-Vertrag, nicht als Tabellen-PDF. |
| Kommunikation | `CommunicationCenter` mit Datenzugriff und Vertragsversandservices | feste `CommunicationArea`/Seiten; Workflow laut Fachvergleich fachlich gleichwertig | Dieselben persistierten Läufe/Einzelbriefe und Kommunikationsvorgänge verwenden, Oberflächen separat bauen. |
| PDF-Katalog | Edge Functions laden privaten Katalog direkt | `SupabasePdfTemplateProvider` ist zentraler .NET-Ladeweg | Katalogschlüssel/Feldspec-Versionen identisch halten; keine Plattformkopien der Vorlage. |
| Navigation | Export- und Kommunikationsansichten als React-Features | `AdminShell`/Seitenrouten | Serienbriefe in beiden Clients als Export-Unterworkflow einhängen; keine neue fachliche Navigationsebene erforderlich. |

## 12. Empfehlung für G2 – Datenmodell Serienbriefvorlagen/freie Felder

G2 soll ausschließlich die persistente Grundlage herstellen, in dieser Reihenfolge:

1. Additive Migration für `serienbrief_vorlage`, Feldabhängigkeiten und freie Felder erstellen; Audit, Aktivstatus, eindeutige Schlüssel und RLS auf Admin/Vorstand absichern.
2. Exportvorlagen **nicht** an einen einzelnen `export_key` binden. Stattdessen nur die benötigten fachlichen Feldschlüssel und Typinformationen speichern.
3. Das vorhandene `briefbogen-allgemein/v2` als Grundlayout referenzieren; Schlüssel/Version als bewusstes Layout-Provenance-Feld abspeichern.
4. Einen kleinen Core-/Web-Vertrag für die Validierung der benötigten Exportfelder schaffen; fehlende Felder müssen vor dem Lauf eindeutig benannt werden.
5. Freie Felder mit den Typen Text, Datum und Zahl modellieren. Laufzeitwerte gehören ausdrücklich nicht in die Vorlage, sondern erst in den späteren Serienbrieflauf.
6. Noch keine Massen-PDF-Erzeugung, Mail-/Druckanbindung oder neue Exportoberfläche implementieren. Diese folgen erst auf die geprüfte G2-Grundlage.

## Geprüfte Quellen

- `Documentation/CENTRAL_EXPORT_PDF.md`
- `Documentation/KOMMUNIKATIONS_WORKFLOW.md`
- `Documentation/PDF_TEMPLATE_CATALOG.md`
- `Documentation/WEB_MAUI_FACHVERGLEICH.md`
- `KGV.Web/features/export/ExportCenter.tsx`, `ExportResults.tsx`
- `KGV.Web/services/export/export-service.ts`, `repositories/export/export-data-repository.ts`
- `KGV.Maui/ViewModels/ExportViewModel.cs`
- `KGV.Core/Templates/briefbogen-allgemein/v2/README.md`, `field-spec.json`
- `KGV.Core/Interfaces/IPdfTemplateProvider.cs`, `KGV.Infrastructure/Services/SupabasePdfTemplateProvider.cs`
- `KGV.Web/services/communication/communication-service.ts`, `repositories/communication/communication-repository.ts`
- relevante Migrationen für Exportgateway, Dokumentarchiv, Kommunikation, Vertragsversand und Rechte, insbesondere `20261015210000_generic_export_gateway.sql`, `20260925210000_document_archive.sql`, `20261023140000_vd3_contract_dispatch.sql` und `20261011130000_g13_permission_contract.sql`.
