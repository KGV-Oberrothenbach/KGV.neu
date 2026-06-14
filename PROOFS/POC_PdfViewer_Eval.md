Ziel: Proof-of-Concept für In-App PDF Viewer (PDF.js vs Syncfusion)

POC A: PDF.js in WebView
- Assets: pdf.js (bundled), viewer.html, pdf.worker.js
- Seite: KGV.Maui/Pages/PdfViewerPage.cs lädt viewer.html via local file URL in WebView.
- Kommunikation: JS -> C# mittels EvaluateJavaScriptAsync und WebView JS messages für Events (pageLoaded, error).
- Vorteile: keine Lizenz, volle Kontrolle, gezielte Erweiterung für Signatur-Overlays.
- Nachteile: Asset-Größe, Performance, JS↔C# Bridge Aufwand.

POC B: Syncfusion.Maui.PdfViewer
- NuGet: Syncfusion.Maui.PdfViewer
- Page: PdfViewerPage nutzt SfPdfViewer (Syncfusion.Forms.Maui.PdfViewer?)
- Vorteile: Out-of-the-box Viewer, native Kontrolle, gute Performance.
- Nachteile: Lizenz, NuGet Installation.

POC Abläufe
1. Implement minimal Page, die eine lokale PDF-Datei aus DocumentStorage öffnet.
2. Test: Rendering, Scroll, Zoom, Reload nach Datei-Änderung.
3. Test: Über Signatur-Flow lokale Datei überschreiben und Viewer neu laden.

Empfehlung: Beginne mit PDF.js POC, da lizenzfrei und vollständige Kontrolle. Wenn Lizenz für Syncfusion verfügbar ist, kann später schnell umgestellt werden.
