# PDF: verbleibende, bewusst erhaltene Bestandteile

Stand: 17.10.2026 – D12 nach erfolgreicher DDB-Abnahme.

Die produktive Erzeugung von Mitgliedsantrag und Pachtvertrag lädt ausschließlich die aktive, veröffentlichte Version aus `pdf_template_catalog` und dem privaten Storage. Weder die .NET-Factories noch `kgv-generate-contract` enthalten einen Embedded- oder lokalen Laufzeitfallback.

Erhalten bleiben ausschließlich diese begründeten Quellen bzw. Pfade:

- `KGV.Core/Templates/mitgliedsantrag/v1/`: kanonische, unveränderte Publish- und Regressionstestquelle der aktiven MA-Version.
- `KGV.Core/Templates/pachtvertrag/v2/`: kanonische Publish- und Regressionstestquelle der aktiven PV-Version.
- `KGV.Core/Templates/pachtvertrag/v1/`: unveränderliche historische Quelle der veröffentlichten, im Katalog inaktiven PV-v1. Sie bleibt für Nachweis, Regression und gegebenenfalls erneute Veröffentlichung erhalten; sie darf erst nach einer expliziten Archivierungsentscheidung entfernt werden.
- `PachtvertragTemplateFactory` und `PachtvertragTemplateData`: fachliche Aufbereitung der PV-Daten, weiterhin vom zentralen v2-Renderer verwendet.
- `SignedVertragsdokumentPdfBuilder`: Signaturpfad für bereits vorhandene unsignierte Vertragsdokumente; er wird von `SupabaseService.CreateSignedVertragsdokumentAsync` und der MAUI-Dokumentansicht verwendet und ist kein Template-Fallback.
- `VereinsdokumentPdfBuilder`: produktiver Erzeuger für den eigenständigen Mitgliedsvertrag; er gehört nicht zum MA-/PV-Templatepfad.

`PachtvertragTemplate.html`, eingebettete PDFs, alte PDF-/Word-Duplikate, die Edge-Assets und Einmal-Generatoren wurden entfernt. Eine fachliche PV-Endfassung erfolgt erst nach Abschluss von Statut und KGO/Gartenordnung.
