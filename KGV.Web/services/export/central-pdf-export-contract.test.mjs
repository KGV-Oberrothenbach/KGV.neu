import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const functionSource = readFileSync(new URL("../../../supabase/functions/kgv-generate-export-pdf/index.ts", import.meta.url), "utf8");
const migration = readFileSync(new URL("../../../supabase/migrations/20261015100000_cx1_mitgliederliste_pdf_export.sql", import.meta.url), "utf8");
const config = readFileSync(new URL("../../../supabase/config.toml", import.meta.url), "utf8");

test("CX1 registers a protected central members-list PDF endpoint", () => {
  assert.match(config, /\[functions\.kgv-generate-export-pdf\]/);
  assert.match(functionSource, /function normalizeRequest/);
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
  assert.match(functionSource, /format\?: string/);
  assert.match(functionSource, /parameters\?: Record<string, unknown>/);
  assert.match(functionSource, /columns\?: unknown/);
  assert.match(functionSource, /orientation\?: string/);
});

test("central export request rejects unsupported generic contract values", () => {
  assert.match(functionSource, /format !== "pdf"/);
  assert.match(functionSource, /orientation muss auto, portrait oder landscape/);
  assert.match(functionSource, /Keine PDF-Spalten konfiguriert/);
  assert.match(functionSource, /new Set\(body\.columns/);
  assert.match(functionSource, /unbekannten Spaltenschlüssel/);
  assert.match(functionSource, /isJsonValue/);
  assert.match(functionSource, /parameters: body\.parameters/);
});

test("members-list PDF distributes the active columns across the full content width", () => {
  assert.match(functionSource, /function selectedMemberColumns/);
  assert.match(functionSource, /function memberColumnWidths/);
  assert.match(functionSource, /contentWidth - widths\.reduce/);
  assert.match(functionSource, /columns\.unshift\(columns\.splice/);
  assert.match(functionSource, /width: contentWidth/);
});
