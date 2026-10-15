import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const functionSource = readFileSync(new URL("../../../supabase/functions/kgv-generate-export-pdf/index.ts", import.meta.url), "utf8");
const migration = readFileSync(new URL("../../../supabase/migrations/20261015100000_cx1_mitgliederliste_pdf_export.sql", import.meta.url), "utf8");
const config = readFileSync(new URL("../../../supabase/config.toml", import.meta.url), "utf8");

test("CX1 registers a protected central members-list PDF endpoint", () => {
  assert.match(config, /\[functions\.kgv-generate-export-pdf\]/);
  assert.match(functionSource, /body\.export_key !== "mitgliederliste"/);
  assert.match(functionSource, /rpc_execute_export/);
  assert.match(functionSource, /application\/pdf/);
  assert.match(functionSource, /Content-Disposition/);
});

test("CX1 keeps members-list filtering, natural gardens and PDF page mechanics server-side", () => {
  assert.match(migration, /coalesce\(m\.is_demo, false\) = false/);
  assert.match(migration, /m\.mitglied_ende is null or m\.mitglied_ende >= current_date/);
  assert.match(migration, /b\.von_datum <= current_date/);
  assert.match(migration, /p_aktiv_filter text/);
  assert.match(migration, /p_sortierung text/);
  assert.match(functionSource, /pageWidth = 841\.89/);
  assert.match(functionSource, /pageHeight = 595\.28/);
  assert.match(functionSource, /startPage\(\)/);
  assert.match(functionSource, /Bemerkung:/);
  assert.match(functionSource, /export_mitgliederliste/);
});
