import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const pdfService = readFileSync(new URL("./export-pdf-service.ts", import.meta.url), "utf8");
const exportCenter = readFileSync(new URL("../../features/export/ExportCenter.tsx", import.meta.url), "utf8");

test("members-list PDF uses the session-authenticated central Edge Function", () => {
  assert.match(pdfService, /functions\/v1\/kgv-generate-export-pdf/);
  assert.match(pdfService, /Authorization: `Bearer \$\{session\.accessToken\}`/);
  assert.match(pdfService, /apikey: publishableKey/);
  assert.match(pdfService, /export_key: string/);
  assert.match(pdfService, /parameters: Record<string, unknown>/);
  assert.match(pdfService, /columns: string\[\]/);
  assert.match(pdfService, /orientation: "auto" \| "portrait" \| "landscape"/);
  assert.match(pdfService, /application\/pdf/);
  assert.match(pdfService, /content-disposition/);
  assert.match(pdfService, /%PDF-/);
});

test("members-list PDF uses the generic central export request", () => {
  assert.match(exportCenter, /usesCentralMembersPdf/);
  assert.match(exportCenter, /generateExportPdf/);
  assert.match(exportCenter, /parameters: Object\.fromEntries/);
  assert.match(exportCenter, /columns: visibleColumns\.map/);
  assert.match(exportCenter, /orientation: "auto"/);
  assert.match(exportCenter, /downloadExportPdf\(club, session, exportFileName/);
});
