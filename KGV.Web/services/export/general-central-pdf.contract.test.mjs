import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const functionSource = readFileSync(new URL("../../../supabase/functions/kgv-generate-export-pdf/index.ts", import.meta.url), "utf8");
const exportCenter = readFileSync(new URL("../../features/export/ExportCenter.tsx", import.meta.url), "utf8");
const mauiViewModel = readFileSync(new URL("../../../KGV.Maui/ViewModels/ExportViewModel.cs", import.meta.url), "utf8");
const oldMigration = readFileSync(new URL("../../../supabase/migrations/20261017124500_publish_export_zaehler_eichfaelligkeit_v1.sql", import.meta.url), "utf8");
const membersMigration = readFileSync(new URL("../../../supabase/migrations/20261021110000_fix_mitgliederliste_grouping_and_layout.sql", import.meta.url), "utf8");

test("every permitted PDF export uses export-allgemein and the template orientation", () => {
  assert.match(functionSource, /exportTemplateKey = "export-allgemein"/);
  assert.match(functionSource, /rpc_execute_export/);
  assert.match(functionSource, /width > height \? "landscape" : "portrait"/);
  assert.match(functionSource, /sha256\(pdfBytes\)/);
  assert.match(functionSource, /pdf_darstellung/);
  assert.match(functionSource, /detailColumns/);
  assert.match(functionSource, /tableColumns/);
  assert.doesNotMatch(functionSource, /templateKeyForExport|export-zaehler-eichfaelligkeit-hoch|export-zaehler-eichfaelligkeit-quer|buildMembersPdf/);
});

test("member grouping and compact layout remain server-side metadata", () => {
  assert.match(membersMigration, /coalesce\(m\.hauptmitglied_id, m\.id\) principal_member_id/);
  assert.match(membersMigration, /case when g\.hauptmitglied_id is null then 0 else 1 end/);
  assert.match(membersMigration, /adresse_block/);
  assert.match(membersMigration, /kontakt_block/);
  assert.match(membersMigration, /pdf_darstellung='detail'/);
});

test("web and MAUI always use the protected central renderer for PDFs", () => {
  assert.match(exportCenter, /generateExportPdf\(club, session/);
  assert.doesNotMatch(exportCenter, /downloadExportPdf|mitgliederliste|zaehler_eichfaelligkeit/);
  assert.match(mauiViewModel, /GenerateExportPdfAsync\(BuildCentralExportRequest\(\)\)/);
  assert.doesNotMatch(mauiViewModel, /ExportPdfBuilder/);
});

test("MAUI members list uses the metadata-driven protected export RPC", () => {
  assert.match(mauiViewModel, /RunProtectedExportRpcAsync/);
  assert.doesNotMatch(mauiViewModel, /Special-case: Mitgliedersliste|mitgliederliste loaded|memberFieldAliases/);
  assert.doesNotMatch(mauiViewModel, /GetMitgliederAsync\(\)|GetAllParzellenAsync\(\)|GetAllParzellenBelegungenAsync\(\)/);
  assert.match(mauiViewModel, /foreach \(var kv in mapped\)/);
});

test("the historical meter-template migration remains present", () => {
  assert.match(oldMigration, /export-zaehler-eichfaelligkeit-hoch/);
  assert.match(oldMigration, /export-zaehler-eichfaelligkeit-quer/);
});
