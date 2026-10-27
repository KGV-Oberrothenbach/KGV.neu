import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const migration = readFileSync(new URL("../../../supabase/migrations/20261027110000_nb2a_e3_personenexporte_mitgliedsnummer.sql", import.meta.url), "utf8");
const exportCenter = readFileSync(new URL("../../features/export/ExportCenter.tsx", import.meta.url), "utf8");
const mauiViewModel = readFileSync(new URL("../../../KGV.Maui/ViewModels/ExportViewModel.cs", import.meta.url), "utf8");

test("NB-2A-E3 adds the stored canonical member number to the work-hours export without exposing its ID", () => {
  assert.match(migration, /v\.hauptmitglied_id as mitglied_id, m\.mitgliedsnummer/);
  assert.match(migration, /returns table\([\s\S]*mitglied_id bigint, mitgliedsnummer bigint, garten_nr text/);
  assert.match(migration, /\('arbeitsstunden_uebersicht', 'mitgliedsnummer', 'Mitgliedsnummer'/);
  assert.doesNotMatch(migration, /\('arbeitsstunden_uebersicht', 'mitglied_id',/);
  assert.match(migration, /mitgliedsnummer is null or m\.mitgliedsnummer <= 0/);
});

test("NB-2A-E3 adds the stored canonical member number to the privacy export without an ID fallback", () => {
  assert.match(migration, /select m\.id, m\.hauptmitglied_id, m\.mitgliedsnummer/);
  assert.match(migration, /select b\.garten_nr, b\.mitgliedsnummer, b\.name/);
  assert.match(migration, /\('datenschutz_aktive_mitglieder', 'mitgliedsnummer', 'Mitgliedsnummer'/);
  assert.doesNotMatch(migration, /\('datenschutz_aktive_mitglieder', 'mitglied_id',/);
  assert.match(migration, /MEMBER_NUMBER_MISSING/);
});

test("NB-2A-E3 contains no derived or technical visible member-number substitute", () => {
  assert.doesNotMatch(migration, /select r\.mitglied_id,\s*r\.mitglied_id|select b\.garten_nr,\s*b\.id/);
  assert.doesNotMatch(migration, /\+\s*1000|1000\s*\+|Fachnummer\.Mitglied/);
  assert.match(migration, /rpc_export_arbeitsstunden_uebersicht_json/);
  assert.match(migration, /rpc_export_datenschutz_aktive_mitglieder_json/);
});

test("web and MAUI retain the shared metadata-driven row source for CSV and PDF", () => {
  assert.match(exportCenter, /executeExport\(club, session/);
  assert.match(exportCenter, /downloadExportCsv/);
  assert.match(exportCenter, /generateExportPdf\(club, session/);
  assert.match(mauiViewModel, /RunProtectedExportRpcAsync/);
  assert.match(mauiViewModel, /GenerateExportPdfAsync\(BuildCentralExportRequest\(\)\)/);
});
