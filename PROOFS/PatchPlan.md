PatchPlan: Konkrete, kleine Änderungen für feature/persistent-pdf-viewer

1) LocalDocumentService (fertig) + DocumentStorage (fertig)
2) PdfViewerPage (POC stub vorhanden) -> erweitern zu PDF.js WebView oder Syncfusion
   - Files: KGV.Maui/Pages/PdfViewerPage.cs, wwwroot/pdfjs/* oder NuGet Syncfusion
3) MemberDetailPage: Button-Handler anpassen
   - Datei: KGV.Maui/Pages/MemberDetailPage.cs
   - Änderungen: Prüfe LocalDocumentService.GetStatus(document); wenn Exists -> Navigation.PushModalAsync(new PdfViewerPage(localPath)); else -> erzeugen & LocalDocumentService.SavePersistentCopyAsync
4) PreviewPages: statt FileSystem.CacheDirectory Öffnen, weiterhin Cache für externes Open behalten, aber persistent copy schreiben (bereits umgesetzt)
5) Signatur-Flow: CreateAndApplySignaturesAsync ggf. anpassen, sodass sie optional lokal speichert und Viewer neu lädt
   - Datei: KGV.Maui/Pages/MemberDetailPage.cs (bestehende CreateAndApplySignaturesAsync verwenden/erweitern)
6) Upload-Button in Viewer: implementieren und Upload-Handler auf SupabaseService aufrufen
   - Datei: KGV.Maui/Pages/PdfViewerPage.cs
7) Tests & DEV_LOG.md Update

Reihenfolge der Commits (jeweils klein):
- commit 1: add LocalDocumentService + PdfViewerPage stub + PROOFS docs
- commit 2: implement PdfViewerPage POC (PDF.js assets or Syncfusion control)
- commit 3: MemberDetailPage UI switch + use of LocalDocumentService
- commit 4: Signatur-Flow integration (save local, reload viewer)
- commit 5: Upload button and server integration
- commit 6: Tests, logging, cleanup logic

Jeder Commit bauen und lokal testen.
