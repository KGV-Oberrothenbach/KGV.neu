Integration Plan: In-App PDF Viewer & Persistent Sign-Flow

Ziel: Nach POC Integration eines ViewerPage, Wechsel von Launcher.OpenAsync zu In-App Viewer, Signatur-Integration und Upload-Hook.

Schritte (klein, buildfähig):
1. POC abgeschlossen und Technologie ausgewählt (PDF.js empfohlen).
2. Erstelle PdfViewerPage (voll funktionsfähig) in KGV.Maui/Pages.
   - WebView + viewer.html assets oder Syncfusion Control.
   - Load local file from LocalDocumentService.GetLocalPath(fileName).
   - Expose Reload() für Neu-Laden nach lokalen Änderungen.
3. MemberDetailPage anpassen (UI):
   - Ersetze "Mitgliedsantrag als PDF" Button-Handler: wenn LocalDocumentService.GetStatus(...).Exists -> öffne PdfViewerPage mit local path; sonst -> erzeugen & persistieren.
   - Zeige Statustext "Lokal vorhanden, noch nicht hochgeladen" und optional "Speichern & Hochladen" Button.
4. Signatur-Flow Integration:
   - In PdfViewerPage oder über separate Signatur-Button: starte Signatur-Sequenz (wie CreateAndApplySignaturesAsync) mit geladenem PDF-Bytes.
   - Nach InsertSignaturesIntoPdf: speichere lokale Datei via LocalDocumentService.SavePersistentCopyAsync(updatedBytes,...)
   - Rufe PdfViewerPage.Reload() auf.
5. Upload-Flow:
   - Implementiere "Speichern & Hochladen" Button; liest lokale Datei und ruft SupabaseService.CreateSignedMitgliedsantragDokumentAsync/Upload API mit FileContent an.
   - Bei Erfolg: setze DocumentInfo.StoragePath auf serverseitige Referenz, markiere LocalDocumentStatus.IsUploaded=true.
6. Tests: manuelle Abläufe und evtl. Integrationstests.
7. Aufräumen: optional Archivierung alter lokalen Kopien nach Upload oder nach Zeit.

Safety & Backout
- Alle Änderungen in feature/persistent-pdf-viewer Branch.
- Schrittweise PRs: POC, Viewer Page, UI Switch, Signatur-Integration, Upload-Integration.
- Unit-Tests und manuelle QA vor Merge.
