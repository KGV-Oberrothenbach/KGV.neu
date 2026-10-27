import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const migration = readFileSync(new URL("../../../supabase/migrations/20261027100000_nb2a_e2_mitgliederliste_mitgliedsnummer.sql", import.meta.url), "utf8");
const exportCenter = readFileSync(new URL("../../features/export/ExportCenter.tsx", import.meta.url), "utf8");
const mauiViewModel = readFileSync(new URL("../../../KGV.Maui/ViewModels/ExportViewModel.cs", import.meta.url), "utf8");

test("NB-2A-E2 projects both visible member number keys from the stored canonical column", () => {
  assert.match(migration, /'mitgliedsnr',\s*g\.mitgliedsnummer::text/);
  assert.match(migration, /'mitgliedsnummer',\s*g\.mitgliedsnummer::text/);
  assert.doesNotMatch(migration, /'mitgliedsnr',\s*g\.id::text/);
  assert.doesNotMatch(migration, /'mitgliedsnummer',\s*g\.id::text/);
  assert.doesNotMatch(migration, /\bid\s*\+\s*1000|1000\s*\+\s*id|Fachnummer\.Mitglied/);
});

test("NB-2A-E2 rejects invalid canonical member numbers and sorts numerically by them", () => {
  assert.match(migration, /mitgliedsnummer is null or m\.mitgliedsnummer <= 0/);
  assert.match(migration, /MEMBER_NUMBER_MISSING/);
  assert.match(migration, /case when s\.sortierung='mitgliedsnr' then g\.mitgliedsnummer end/);
  assert.doesNotMatch(migration, /case when s\.sortierung='mitgliedsnr' then g\.id end/);
});

test("web and MAUI retain the one metadata-driven row source for CSV and central PDF", () => {
  assert.match(exportCenter, /executeExport\(club, session/);
  assert.match(exportCenter, /downloadExportCsv/);
  assert.match(exportCenter, /generateExportPdf\(club, session/);
  assert.match(mauiViewModel, /RunProtectedExportRpcAsync/);
  assert.match(mauiViewModel, /GenerateExportPdfAsync\(BuildCentralExportRequest\(\)\)/);
});
