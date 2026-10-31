import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import test from "node:test";

const root = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(resolve(root, "supabase/migrations/20261031170000_jea_rfid_generic_exports.sql"), "utf8");
const exportService = readFileSync(resolve(root, "KGV.Web/services/export/export-service.ts"), "utf8");
const rfidFunction = migration.match(/create or replace function public\.rpc_export_rfid_status[\s\S]*?\$\$;/i)?.[0] ?? "";

test("JEA applies the missing-only filter in the database with the technical art jea", () => {
  assert.match(migration, /rpc_export_jahresablesung_status\([\s\S]*p_nur_fehlend boolean default false/);
  assert.match(migration, /lower\(a\.art::text\) = 'jea'/);
  assert.doesNotMatch(migration, /lower\(a\.art::text\) = 'jae'/);
  assert.match(migration, /where not coalesce\(p_nur_fehlend, false\)[\s\S]*j\.zaehler_id is null[\s\S]*pruefstatus.*abgelehnt/);
  assert.match(migration, /'nur_fehlend', 'Nur fehlende Jahresendablesungen', 'boolean', null, 'p_nur_fehlend', 'boolean', 'false'::jsonb, false, 20/);
  assert.match(migration, /rpc_export_jahresablesung_status\([\s\S]*p_parameters ->> 'p_nur_fehlend'/);
});

test("RFID switches only the existing definition to a generic protected RPC over the scan context", () => {
  assert.match(migration, /rpc_export_rfid_status\(p_parameters jsonb default '\{\}'::jsonb\)/);
  assert.match(rfidFunction, /from public\.v_rfid_scan_context r/);
  assert.doesNotMatch(rfidFunction, /join public\.(parzelle|zaehler)/i);
  assert.match(migration, /revoke all on function public\.rpc_export_rfid_status\(jsonb\) from public, authenticated/);
  assert.match(migration, /quelle_typ = 'rpc',[\s\S]*quelle_name = 'rpc_export_rfid_status',[\s\S]*erlaubt_csv = true,[\s\S]*erlaubt_pdf = true/);
  assert.match(migration, /RFID scan context remains the sole operational source/i);
});

test("web CSV and PDF continue through the one generic RPC export path", () => {
  assert.match(exportService, /definition\.quelle_typ === "rpc"[\s\S]*executeRpcExport\(definition\.export_key, buildExportRpcPayload\(filters, values\)\)/);
  assert.doesNotMatch(exportService, /rfid_status.*readTableOrViewExport/s);
});
