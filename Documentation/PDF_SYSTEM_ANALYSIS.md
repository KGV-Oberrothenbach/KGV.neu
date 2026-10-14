# PDF System Analyse

Stand: 14.10.2026. Diese Analyse beschreibt ausschließlich den bestehenden Codepfad. Es wurden keine PDF- oder Storage-Funktionen geändert.

## 1. Aktueller Aufbau

Es gibt derzeit keinen zentralen Template-Service. Die Implementierung teilt sich in zwei Muster:

- Mitgliedsantrag und Pachtvertrag laden eingebettete PDF-Dateien aus `KGV.Core/Templates` und beschreiben sie mit festen Feldkoordinaten.
- Protokolle und Exporte werden direkt mit `PdfSharpCore` gezeichnet. Der Kopf des Exports `zaehler_eichfaelligkeit` ist ebenfalls im Code aufgebaut.

Die gemeinsame Persistenz erzeugter Fach-Dokumente liegt hinter `ISupabaseService` bzw. `SupabaseService`. Der Dienst übergibt den erzeugten Byte-Stream an die Edge Function `kgv-upload-document`; diese liefert eine Google-Drive-Datei-ID und einen Storage-Pfad zurück. Die Tabelle `dokument` speichert die Referenzen. Für das Öffnen lädt der Service bevorzugt über den Drive-Proxy.

## 2. Übersicht je Dokumenttyp

### Mitgliedsantrag

- **Relevante Dateien:** `KGV.Core/Utilities/MitgliedsantragDokumentFactory.cs`, `KGV.Core/Templates/Mitgliedsantrag_Vorlage.pdf`, `KGV.Core/Templates/pdf-form-spec Mitgliedsantrag.json`, `KGV.Infrastructure/Services/SupabaseService.cs` und die MAUI-Vorschau `KGV.Maui/Pages/MitgliedsantragPreviewPage.cs`.
- **Erzeugung:** `MitgliedsantragDokumentFactory.CreateUploadRequest` lädt die eingebettete PDF-Ressource, zeichnet die Stammdaten anschließend mit `PdfSharpCore` auf die erste Seite und entfernt danach die AcroForm-Annotationen. Das Ergebnis ist eine abgeflachte PDF.
- **Vorlage:** Die PDF liegt im Repository und ist in `KGV.Core.csproj` als `EmbeddedResource` eingebunden. Die JSON-Datei dokumentiert Feldnamen bzw. Spezifikationen, wird vom aktuellen Factory-Code aber nicht als Laufzeitquelle gelesen.
- **Koordinaten/Felder:** Feste `PdfFormFieldSpec`-Koordinaten für Text, mehrzeilige Textfelder, Checkboxen und Signaturflächen. Signaturen werden später über `SignedVertragsdokumentPdfBuilder` auf Basis von Platzhaltern eingesetzt.
- **Bibliothek:** `PdfSharpCore` (`PdfReader`, `PdfDocument`, `XGraphics`, Annotationen).
- **Vorschau/Ablage:** Die Vorschau erhält ein `DokumentUploadRequest` mit `FileContent` und öffnet `PdfViewerPage`. Die finale Erstellung löst in `SupabaseService` die Stammdaten auf und ruft danach `CreateDokumentAsync` auf.
- **Abhängigkeiten:** aktive Vereinskonfiguration inklusive Bank- und Dokumentdaten; Beitragsberechnung; gesetzliche Vertreter; Signaturfluss; feste Feldgeometrie und eingebettete Ressource.

### Pachtvertrag

- **Relevante Dateien:** `KGV.Core/Utilities/PachtvertragDokumentFactory.cs`, `KGV.Core/Utilities/PachtvertragTemplateFactory.cs`, `KGV.Core/Templates/Pachtvertrag_KGV_Oberrothenbach_neu.pdf`, `KGV.Core/Templates/Pachtvertrag_Vorlage.pdf`, `KGV.Core/Templates/pdf-form-spec Pachtvertrag.json`, `KGV.Infrastructure/Services/SupabaseService.cs`, `KGV.Maui/Pages/PachtvertragPreviewPage.cs` und `KGV.Maui/Pages/PachtvertragFlowHelper.cs`.
- **Erzeugung:** Die Factory erzeugt zunächst fachliche `PachtvertragTemplateData` aus Mitglied, zweitem Pächter, Parzelle, Saison und Vereinskonfiguration. Anschließend wird die eingebettete PDF geöffnet, mit festen Koordinaten auf mehreren Seiten beschrieben und abgeflacht.
- **Vorlage:** Der produktive Ressourcename zeigt auf `Pachtvertrag_KGV_Oberrothenbach_neu.pdf`; in `KGV.Core.csproj` sind mehrere Pachtvertrags-PDFs als Ressourcen eingebunden. HTML- und Word-Dateien im Templates-Ordner sind Bestandteile/Artefakte, aber nicht der aktuell verwendete Laufzeitpfad.
- **Koordinaten/Felder:** Feste `PdfFormFieldSpec`-Positionen pro Seite; Signaturplatzhalter sind Koordinatenobjekte und keine fortbestehenden PDF-Formularfelder.
- **Bibliothek:** `PdfSharpCore`; Datenaufbereitung durch die eigene `PachtvertragTemplateFactory`.
- **Vorschau/Ablage:** Die MAUI-Vorschau öffnet denselben Byte-Stream in `PdfViewerPage`; nach den erforderlichen digitalen Signaturen erzeugt der Service die finale Fassung und lädt sie über den gemeinsamen Dokumentpfad hoch.
- **Abhängigkeiten:** zeitlich gültige Parzellenbelegung, Saison/Pachtpreis, Flächendaten, zweiter Pächter bzw. Vertreter, Vereinskonfiguration und Mehrfachsignaturen.

### Parzellenprotokoll

- **Relevante Dateien:** `KGV.Core/Utilities/ParzellenProtokollPdfBuilder.cs`, `KGV.Infrastructure/Services/SupabaseService.cs`, `KGV.Maui/Pages/ParzellenProtokollePage.cs`.
- **Erzeugung:** `ParzellenProtokollPdfBuilder` zeichnet die PDF programmatisch mit `PdfSharpCore`. Es gibt keine PDF-Hintergrundvorlage und keine AcroForm-Felder.
- **Vorlage:** Kein externes Template; Layout, Ränder, Schriften und Tabellenlogik sind im Builder fest kodiert.
- **Koordinaten/Felder:** Feste Layoutkoordinaten mit manueller Seitenverwaltung und Seitenumbrüchen.
- **Bibliothek:** `PdfSharpCore`.
- **Vorschau/Ablage:** Die MAUI-Seite erstellt einen `DokumentUploadRequest` und übergibt ihn dem gemeinsamen Dokumentdienst. Das Protokoll selbst wird fachlich zuvor über `CreateParzellenProtokollAsync` gespeichert.
- **Abhängigkeiten:** Parzelle, Ablesungs-Snapshots/Protokolldaten sowie gemeinsamer Dokument-Upload.

### Export und PDF Kopf, insbesondere Zähler Eichfälligkeit

- **Relevante Dateien:** `KGV.Core/Utilities/ExportPdfBuilder.cs`, `KGV.Core/Utilities/ExportPdfDocumentDefinition.cs`, `KGV.Core/Utilities/VereinsdokumentBranding.cs`, `KGV.Core/Utilities/PdfSharpFontResolverInitializer.cs`, `KGV.Maui/ViewModels/ExportViewModel.cs`.
- **Erzeugung:** `ExportPdfBuilder.BuildExportPdf` zeichnet Tabellen und Seiten direkt. `ExportViewModel.ExportToPdfAsync` bereitet die sichtbaren Exportspalten auf, ruft den Builder auf und schreibt die PDF in das MAUI-Cache-Verzeichnis.
- **Vorlage:** Keine externe Vorlagendatei. Für `zaehler_eichfaelligkeit` baut `DrawVereinskopf` Logo, Vereinsname, Registerangabe, E-Mail, grüne Linie, Titel und Stichtag direkt aus Code auf.
- **Koordinaten/Felder:** Direkte Koordinaten, Spaltengewichte und manuelle Wiederholung des Kopfes bei Seitenwechseln; keine PDF-Formulare.
- **Bibliothek:** `PdfSharpCore`; das Logo stammt als eingebettete PNG-Ressource aus `KGV.Maui/Resources/Images/kgv_logo.png` über `VereinsdokumentBranding`.
- **Vorschau/Ablage:** Der Export bleibt zunächst eine lokale Cache-Datei. Das Öffnen erfolgt aus der jeweiligen Exportoberfläche; der Exportpfad nutzt nicht automatisch den Dokument-/Google-Drive-Upload.
- **Abhängigkeiten:** export-spezifische Spalten-/RPC-Daten, Layoutregeln in `ExportPdfBuilder`, Branding-Helfer und Schriftresolver.

### PDF View

- **Relevante Datei:** `KGV.Maui/Pages/PdfViewerPage.cs`.
- **Verhalten:** Der Viewer erhält Titel und PDF-Bytes, rendert Seiten lokal und bietet Navigation. Die Vorschau-Seiten für MA/PV reichen ihm direkt die noch nicht hochgeladene PDF weiter.
- **Aktueller Umfang:** Im untersuchten Viewer sind keine allgemeinen Aktionen zum Speichern oder Teilen des angezeigten PDFs vorhanden.
- **Abhängigkeiten:** byte-basierte Übergabe; Plattform-Rendering im MAUI-Client. Eine spätere Share-/Save-Funktion muss sowohl Cache-/temporäre Vorschauen als auch bereits persistente Drive-Dokumente unterscheiden.

### Google Drive und Dokumentablage

- **Relevante Dateien:** `KGV.Infrastructure/Services/SupabaseService.cs`, `KGV.Core/Models/DokumentUploadRequest.cs`, `KGV.Core/Models/DocumentInfo.cs`, `supabase/functions/kgv-upload-document`.
- **Ablauf:** `CreateDokumentAsync` validiert die Zuordnung, ruft `UploadDokumentToDriveAsync` auf und schreibt anschließend die Dokumentreferenz. Der Upload verwendet die Edge Function `kgv-upload-document`; die Antwort enthält mindestens `drive_file_id`, Storage-Pfad und Dateimetadaten.
- **Lesen:** `DownloadDokumentContentAsync` verwendet bei vorhandener Drive-ID den Drive-Proxy. Es existieren zudem Builder für Google-Drive-View-/Download-URLs und ein Löschpfad über die gleiche Infrastruktur.
- **Wichtige Trennung:** Der bestehende `dokument`-Pfad ist für fertige Fach-Dokumente gedacht. Er ist nicht als versionierter Template-Katalog modelliert.

## 3. Gemeinsame Komponenten

- `PdfSharpCore` ist die durchgängige PDF-Bibliothek für aktuelle Vorlagen, Formulardaten, programmatische Dokumente und Exporte.
- `PdfSharpFontResolverInitializer` muss vor der PDF-Erzeugung initialisiert werden.
- `VereinsdokumentBranding` liefert Vereinsname, Register, E-Mail und eingebettetes Logo für programmatische PDF-Köpfe.
- `VereinskonfigurationSnapshot` kapselt dokumentrelevante Vereins-/Bankdaten zur Erzeugungszeit.
- `DokumentUploadRequest` vereinheitlicht die Übergabe erzeugter Bytes an den Dokument-Upload.
- MAUI verwendet denselben `PdfViewerPage` für temporäre Vorschauen und geladene Dokumentinhalte.

## 4. Risiken und Hindernisse einer späteren Verlagerung nach Supabase

- Die beiden Formular-PDFs sind keine generischen Feldfüllungen: der Code zeichnet anhand eigener, fester Feldkoordinaten und flacht Formulare ab. Eine Storage-Vorlage muss daher zusammen mit einer unveränderlichen Feldspezifikation versioniert werden.
- Mehrere Dateiversionen liegen bereits im Projekt, während der produktiv verwendete Ressourcename im Code festgelegt ist. Die Zuordnung Template-Schlüssel -> Version -> Metadaten muss explizit werden.
- Protokoll und Export besitzen bisher gar keine Vorlagendatei. Eine Migration betrifft nicht nur das Laden eines PDFs, sondern müsste Layoutparameter bzw. ein geeignetes Renderkonzept versionieren.
- MAUI und Web können erst dieselben Vorlagen verwenden, wenn Feldspezifikation, Renderer-Verhalten, Schriftarten, Logo und Asset-Auflösung plattformneutral festgelegt werden. Aktuell liegt die PDF-Erzeugung überwiegend im .NET-Client.
- Eine Vorlage darf nach Veröffentlichung nicht überschrieben werden. Deshalb sollten Version, Prüfsumme, Inhaltstyp, Freigabestatus und kompatible Renderer-/Feldspec-Version gemeinsam verwaltet werden.
- Fertige Dokumente und Vorlagen dürfen nicht denselben Lifecycle oder dieselben Berechtigungen erhalten: fertige PDFs bleiben Drive-Dokumente; Templates benötigen private, versionsgeschützte Storage-Pfade.
- Digitale Signaturen hängen an konkreten Seitenkoordinaten. Jede Template-Version muss zu den jeweils zulässigen Signaturplatzhaltern passen.

## 5. Sinnvoller nächster Umbau Schritt

Zuerst einen rein lesenden, zentralen Template-Katalog definieren und an einem einzigen bestehenden Formular erproben: Template-Schlüssel, unveränderliche Version, privater Storage-Pfad, Prüfsumme und die dazugehörige Feldspezifikation. Der erste Migrationskandidat sollte der Mitgliedsantrag sein, weil seine PDF-Quelle und Feldliste bereits klar abgegrenzt sind. Der bestehende Factory-/Signatur-/Google-Drive-Pfad bleibt dabei unverändert; lediglich `LoadTemplatePdfBytes` würde nach erfolgreicher, separat getesteter Einführung aus dem Katalog statt aus einer eingebetteten Ressource lesen.
