Testplan: Persistente PDF-Erzeugung, In-App Viewer, Signatur und Upload

Szenarien (manuell):
1. Erzeuge PDF offline (Zuhause)
   - Erwartung: Datei wird in DocumentStorage persistiert. MemberDetailPage zeigt "Dokument öffnen".
2. Vor Ort: Öffne Dokument in App
   - Erwartung: PdfViewerPage zeigt die Datei, Rendering korrekt.
3. Signatur: Erfasse Unterschrift(en)
   - Erwartung: Signatur-Sequenz funktioniert, InsertSignaturesIntoPdf zeichnet Unterschriften ein.
4. Speichern lokal
   - Erwartung: Lokale Datei wird überschrieben oder neue Version erzeugt; Viewer kann neu laden.
5. Upload
   - Erwartung: Upload API akzeptiert FileContent; DocumentInfo.StoragePath wird gesetzt; UI zeigt "Hochgeladen" und optional lokale Datei archiviert.
6. Edge Cases
   - Fehlendes CurrentMitgliedId beim Auto-Freigeben/Signieren: App zeigt Fehlerhinweis.
   - Datei bereits auf Server vorhanden: Konfliktdialog.

Automatisierte Tests (optional):
- Unit: LocalDocumentService.GetStatus, SavePersistentCopyAsync
- Integration: SignedVertragsdokumentPdfBuilder.InsertSignaturesIntoPdf (on generated sample PDF)

Rollout
- Merge feature branch nach erfolgreichem POC + Tests
- Release notes in DEV_LOG.md mit kurzer Anleitung
