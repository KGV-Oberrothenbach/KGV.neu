import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const pdfService = readFileSync(new URL("./export-pdf-service.ts", import.meta.url), "utf8");
const exportCenter = readFileSync(new URL("../../features/export/ExportCenter.tsx", import.meta.url), "utf8");

test("members-list PDF uses the session-authenticated central Edge Function", () => {
  assert.match(pdfService, /functions\/v1\/kgv-generate-export-pdf/);
  assert.match(pdfService, /Authorization: `Bearer \$\{session\.accessToken\}`/);
  assert.match(pdfService, /apikey: publishableKey/);
  assert.match(pdfService, /export_key: "mitgliederliste"/);
  assert.match(pdfService, /aktiv_filter/);
  assert.match(pdfService, /sortierung/);
  assert.match(pdfService, /application\/pdf/);
  assert.match(pdfService, /content-disposition/);
  assert.match(pdfService, /%PDF-/);
});

test("only members-list PDF bypasses the legacy browser renderer", () => {
  assert.match(exportCenter, /usesCentralMembersPdf/);
  assert.match(exportCenter, /downloadCentralMembersListPdf/);
  assert.match(exportCenter, /membersListPdfParameters\(values\)/);
  assert.match(exportCenter, /downloadExportPdf\(club, session, exportFileName/);
});
