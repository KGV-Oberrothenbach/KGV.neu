# NB-2A-E5A – Audit fachlich sichtbarer Dateinamen

Stand: 27.10.2026  
Prüfstatus: NB-2A-E5B1, E5B2 und E5B3 umgesetzt; alle aktiven sichtbaren Pfade sind abgeschlossen.

## Prüfmaßstab

Geprüft wurden aktive Pfade für Downloads, Vorschauen, Freigaben, persistierte
Dokumente, Versandanhänge, Exportdateien, Vertrags- und Antragsdokumente,
Parzellenprotokolle, historische Dokumente, Vereinsunterlagen, Fotos und
Kalenderdateien. Archivordner sowie Build-Ausgaben sind nicht Bestandteil des
Audits.

Bewertung:

- **OK**: Der sichtbar übergebene Name enthält nur fachlich verständliche Werte.
- **FEHLER**: Eine interne Datenbank-ID wird in einem sichtbaren Dateinamen
  verwendet.
- **NICHT SICHTBAR**: Der Wert ist ausschließlich Storage-, Cache- oder
  temporärer Pfad und wird nicht als Dateiname an Nutzende übergeben.
- **NICHT AKTIV**: Nur historische Kompatibilitätslogik, kein Erzeugungspfad.
- **ENTSCHEIDUNG ERFORDERLICH**: Sichtbarer, aber nutzerbestimmter bzw. nicht
  fachlich normalisierter Name; keine automatisch ableitbare Umbenennung.

Die Spalte „fachliche Alternative vorhanden?“ benennt eine bereits verfügbare
fachliche Kennung. Sie ist keine Aufforderung, eine Nummer zu erfinden.
`mitgliedsnummer` ist bei Mitgliedsdokumenten die maßgebliche Kennung;
`garten_nr` ist eine fachliche Parzellennummer. Interne IDs in `storage_path`,
Drive-/Blob-IDs und Cache-Pfade wurden nur dann bewertet, wenn sie als sichtbarer
Dateiname weitergegeben werden.

| Bereich | Pfad/Funktion | sichtbarer Dateiname | technische ID sichtbar? | fachliche Alternative vorhanden? | Bewertung | Änderung nötig |
|---|---|---|---|---|---|---|
| Web-CSV-Exporte | `KGV.Web/services/export/export-output-service.ts` – `exportFileName` | `<Datum>_<Exporttitel>.csv` | Nein | Exporttitel | OK | Nein |
| Web-PDF-Exporte | `supabase/functions/kgv-generate-export-pdf/index.ts` und `export-pdf-service.ts` | `<Exporttitel>_<Datum>.pdf` | Nein | Exporttitel | OK | Nein |
| JAE-Export | `KGV.Web/services/export/jae-xlsx-service.ts` | `<Jahr>_JAE.xlsx` | Nein | Saisonjahr | OK | Nein |
| MAUI-CSV-Export/Teilen | `KGV.Maui/ViewModels/ExportViewModel.cs`, `Pages/ExportPage.cs` | `<Zeitstempel>_<export_key>.csv` | Nein | Exportkennung | OK | Nein |
| MAUI-zentraler PDF-Export | `KGV.Maui/ViewModels/ExportViewModel.cs` | Servername; Fallback `<Zeitstempel>_<export_key>.pdf` | Nein | Exporttitel/-kennung | OK | Nein |
| Persistierte allgemeine Dokumente und Download | `supabase/functions/kgv-upload-document/index.ts` – `buildFileName`, Download-Header | `<Titel>_<Zeitstempel>.<Endung>` | Nein | Titel | OK | Nein |
| Dokument-Storage | `kgv-upload-document` – `buildStorageSegments` / `buildStoragePath` | Ordner enthalten `owner_id` | Nur intern | Titel/Dateiname, aber nicht im Storage nötig | NICHT SICHTBAR | Nein |
| Lokale Formularfabriken | `KGV.Core/Utilities/FormularDokumentDateiname.cs` – `BuildMitgliedDateiname`; Aufrufer für Antrag, Mitglieds- und Pachtvertrag | `<Name>_<Vorname>-<Fachnummer oder Mgl-Nr.>-<Datum>-<Typ>-<Status>.pdf`, ohne Kennung neutral | Nein | Mitgliedsnummer; beim Antrag vorhandene Antragsnummer | OK | Nein (E5B1) |
| Edge-Vertragsvorschau | `supabase/functions/kgv-generate-contract/index.ts` – finale `fileName`/`Content-Disposition` | `<Dokumenttyp>_<Fachnummer oder Mgl-Nr.>_<Datum>_<Status>.pdf`, ohne Kennung neutral | Nein | Vorhandene Antrags-/Pacht-/Wartungsnummer, sonst Mitgliedsnummer | OK | Nein (E5B1) |
| MAUI-Pachtvertragsvorschau | `KGV.Maui/Pages/PachtvertragFlowHelper.cs` und `PachtvertragPreviewPage.cs` | `Pachtvertrag_Vorschau[_Mgl-<mitgliedsnummer>][_Garten-<garten_nr>].pdf` | Nein | Mitgliedsnummer, `garten_nr`; sonst neutraler Vorschau-Name | OK | Nein (E5B1) |
| MAUI-Mitgliedsantragsvorschau (vorbereiteter Ablauf) | `KGV.Maui/Pages/PreparedMembershipApplicationPreviewFlow.cs` | `Mitgliedsantrag_Vorschau.pdf` | Nein | Bezeichnung | OK | Nein |
| Parzellenprotokoll | `KGV.Maui/Pages/ParzellenProtokollePage.cs` | `<Protokolltyp>_<garten_nr>_<Datum>.pdf` | Nein | `garten_nr` | OK | Nein |
| Ablesungs-/Zählerfotos | `supabase/functions/kgv-upload-photo/index.ts` – `buildFileName` | `<Datum>_<Art>_<Medium>_garten-<garten_nr>[_zaehler-<Nr>].<Endung>` | Nein | Garten- und Zählernummer | OK | Nein |
| Lokale vorgemerkte Fotos | `KGV.Maui/Services/PendingPhotos/*` | `<Datum>_<Fotoart>_<garten_nr>_<Medium>.jpg` | Nein; UUID bleibt nur im lokalen Cache-Pfad | `garten_nr`, Fotoart und Medium | OK | Nein (E5B3); sichtbarer Queue-/Uploadname ist fachlich |
| PDF-Viewer teilen | `KGV.Maui/Pages/PdfViewerPage.cs` | ursprünglicher Dokumentname, sonst Dokumenttitel, sonst `KGV_Dokument.pdf` | Nein; UUID bleibt nur im Cache-Verzeichnis | Ursprungsname bzw. Titel | OK | Nein (E5B3); Share-Datei wird separat fachlich angelegt |
| Historische Dokumente / manuelle Uploads | `KGV.Maui/Pages/HistoricalDocumentImportPage.cs`, `Pages/DokumentePage.xaml.cs`, `KGV.Web/features/documents/HistoricalDocumentUploadDialog.tsx` | vom Nutzer gewählter Originalname; persistiert danach `<Titel>_<Zeitstempel>` | Nicht systemseitig | Nutzername bzw. fachlicher Titel | OK | Keine automatische Umbenennung, keine erzwungene Fachnummer und keine Migration |
| Vereinsunterlagen und digitaler Versand | `KGV.Web/features/club-documents/*`, `KGV.Maui/Pages/VereinsunterlagenPage.cs`, `kgv-send-club-documents` | vom Vorstand gewählter PDF-Name, als Versandanhang wiederverwendet | Nicht systemseitig | Dokumenttyp/Fassung | OK | Keine automatische Umbenennung, kein technischer ID-Zusatz und keine Rückwirkung |
| Vertragsversand | `supabase/functions/kgv-send-contract-documents/index.ts` | `dateiname_snapshot` als Mailanhang | Nein für künftig erzeugte Dokumente; alte Snapshots bleiben unverändert | Siehe Vertrags-/Dokumentpfade | OK | Nein (E5B1); keine Rückwirkung |
| Signierter Pachtvertrag (bestehendes Dokument) | `kgv-generate-contract` – `signExisting` | Übergabename enthält `source_document_id`, wird beim Upload jedoch in `<Titel>_<Zeitstempel>` normalisiert | Nein im persistierten/versendeten Ergebnis | Titel | OK | Nein |
| Kalenderexport Web und MAUI | `KGV.Web/features/home/HomeDetail.tsx`, `services/calendar/ics-calendar-service.ts`, `KGV.Maui/Pages/HomeSectionDetailPage.cs` | `<Terminbezeichnung>_<YYYY-MM-DD>.ics`, bei leerem Titel `Termin_<YYYY-MM-DD>.ics` | Nein | Terminbezeichnung und Datum | OK | Nein (E5B2); ICS-UID bleibt technisch intern |
| Alte Status-Erkennung für Formularnamen | `FormularDokumentDateiname.TryParse`, `KGV.Web/services/documents/document-metadata-service.ts` | akzeptiert alte Namen mit ID | Keine neue Ausgabe | Titelstatus; neue fachliche Konvention | NICHT AKTIV | In E5B nur Kompatibilität erhalten, nicht neu erzeugen |

## Befundzusammenfassung

| Bewertung | Anzahl |
|---|---:|
| OK | 19 |
| FEHLER | 0 |
| NICHT SICHTBAR | 1 |
| NICHT AKTIV | 1 |
| ENTSCHEIDUNG ERFORDERLICH | 0 |

Die 21 Auditzeilen sind damit vollständig klassifiziert: 19 `OK`, eine rein
interne Storage-Struktur und eine nicht aktive Kompatibilitätslogik.

Die vier Formular-, Vertrags- und Pachtvorschau-Befunde wurden mit E5B1
geschlossen. E5B2 schließt den Kalenderbefund: Web und MAUI verwenden für
sichtbare ICS-Dateien ausschließlich die sanitizierte Terminbezeichnung und das
Datum. Die technische Termin-ID bleibt ausschließlich im ICS-UID erhalten.
E5B3 trennt die UUID-Caches von sichtbaren PendingPhoto- und PDF-Share-Namen.
Die beiden verbliebenen fachlichen Entscheidungen bestätigen ausdrücklich, dass
historische Uploads und Vereinsunterlagen unverändert nutzerbestimmt bleiben.

Der generische Dokumentupload ist hiervon abgegrenzt: Er speichert und liefert
den fachlichen Titel mit Zeitstempel aus. Seine Storage-Struktur enthält zwar
Owner-IDs, übergibt diese aber nicht als sichtbaren Dateinamen. Auch die
Exportpfade aus NB-2A-E1 bleiben ohne sichtbare `mitglied_id` oder
`mitglied.Id`; dieser Befund wird hier bestätigt.

## E5B – fachliche Umstellung

**NB-2A-E5A = 100 %. NB-2A-E5B1 = 100 %. NB-2A-E5B2 = 100 %.
NB-2A-E5B3 = 100 %. NB-2A-E5 Dateinamen = 100 %.** Bestehende alte
Dokumentnamen und Versand-Snapshots werden nicht rückwirkend umbenannt. Die
Parser akzeptieren weiterhin den alten ID-Stil, während neue Generatoren ihn
nicht mehr ausgeben. Technische IDs in Storage-Pfaden, lokalen Cache-Pfaden und
Queue-Schlüsseln bleiben intern zulässig, werden aber nicht als sichtbarer
Dateiname weitergegeben.

Für E5B wurde folgende fachliche Namensmatrix angewendet:

| Dokumentfamilie | vorgeschlagener sichtbarer Kern | Voraussetzung |
|---|---|---|
| Mitgliedsdokument | `Mgl-<mitgliedsnummer>` | gültige Mitgliedsnummer |
| Mitgliedsantrag | `Antrag-<antragsnummer>`; sonst neutraler Antragstitel | Antragsnummer nur verwenden, wenn fachlich vorhanden |
| Pachtvertrag | `Pachtvertrag-<vertragsnummer>` | Vertragsnummer nur verwenden, wenn fachlich vorhanden |
| Wartungsvertrag | `Wartungsvertrag-<nummer>` | 90000–99999 nur als bestehende fachliche Nummer, nicht aus ID ableiten |
| Historisches kombiniertes Dokument | bestehende historische Kennung | 80000–89999 nur bei vorhandener fachlicher Kennung |
| Aufnahmebestätigung | `Aufnahmebestaetigung-<Antrags-oder-Mitgliedsnummer>` | vorhandene Kennung |
| Pachtvorschau ohne Vertragsnummer | `Pachtvertrag-Vorschau-Mgl-<mitgliedsnummer>-Garten-<garten_nr>` oder neutraler Vorschau-Name | gültige Mitglieds-/Parzellendaten |
| Kalenderdatei | `<Terminbezeichnung>_<Datum>.ics` | Titel sicher sanitizen; bei Gleichheit fachlichen Datumszusatz verwenden |

## Gesamtabschluss NB-2A

Die bestehenden Audits für Web, MAUI, PDF-/Dokumentgeneratoren und Exporte sind
abgeschlossen; E5B3 schließt den verbleibenden Dateinamenblock. Damit gilt:

**NB-2A Exporte = 100 %.**

**NB-2A = 100 %.**

Die vorhandenen Parser/Status-Erkennungen bleiben zu den alten ID-Namen
kompatibel, geben aber ausschließlich die neue Konvention aus. Die technischen
Storage- und Protokollschlüssel bleiben dabei unverändert.

## Nicht Bestandteil dieses Audits

- Änderung von Produktcode, Datenbank, SQL-Migrationen, Storage-Daten oder
  Deployments.
- Umbenennung bereits gespeicherter Nutzerdateien oder existierender
  Versand-Snapshots.
- Bewertung rein lokaler Cache-Pfade, sofern sie nicht über das Share-Sheet
  sichtbar werden.
