import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const functionSource = readFileSync(new URL("../../../supabase/functions/kgv-generate-export-pdf/index.ts", import.meta.url), "utf8");
const exportCenter = readFileSync(new URL("../../features/export/ExportCenter.tsx", import.meta.url), "utf8");
const mauiViewModel = readFileSync(new URL("../../../KGV.Maui/ViewModels/ExportViewModel.cs", import.meta.url), "utf8");
const mauiViewer = readFileSync(new URL("../../../KGV.Maui/Pages/PdfViewerPage.cs", import.meta.url), "utf8");

test("meter calibration PDF resolves the published template after orientation", () => {
  assert.match(functionSource, /function templateKeyForExport/);
  assert.match(functionSource, /zaehler_eichfaelligkeit/);
  assert.match(functionSource, /export-zaehler-eichfaelligkeit-hoch/);
  assert.match(functionSource, /export-zaehler-eichfaelligkeit-quer/);
  assert.match(functionSource, /requestedOrientation === "auto"/);
  assert.match(functionSource, /eq\("status", "published"\)\.eq\("aktiv", true\)/);
  assert.match(functionSource, /sha256\(pdfBytes\)/);
  assert.match(functionSource, /field_spec_checksum/);
  assert.match(functionSource, /page\.drawPage\(background/);
});

test("web forwards meter calibration PDF requests to the central renderer", () => {
  assert.match(exportCenter, /usesCentralTemplatePdf/);
  assert.match(exportCenter, /selected\?\.export_key === "zaehler_eichfaelligkeit"/);
  assert.match(exportCenter, /generateExportPdf\(club, session/);
  assert.match(exportCenter, /columns: visibleColumns\.map/);
});

test("MAUI uses the JWT-protected central PDF and native PDF sharing", () => {
  assert.match(mauiViewModel, /zaehler_eichfaelligkeit/);
  assert.match(mauiViewModel, /GenerateExportPdfAsync\(BuildCentralExportRequest\(\)\)/);
  assert.doesNotMatch(mauiViewModel.match(/if \(string\.Equals\(exportKey, "zaehler_eichfaelligkeit"[\s\S]*?\n            }/)?.[0] ?? "", /ExportPdfBuilder/);
  assert.match(mauiViewer, /Share\.Default\.RequestAsync/);
  assert.match(mauiViewer, /new ShareFile\(_cachePath, "application\/pdf"\)/);
});
