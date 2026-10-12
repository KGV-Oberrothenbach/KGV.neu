import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import test from "node:test";

const root = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(resolve(root, "supabase/migrations/20261012210000_g14_export_metadata_contract.sql"), "utf8");
const executionMigration = readFileSync(resolve(root, "supabase/migrations/20261012220000_g14_export_execution_security.sql"), "utf8");
const coreModel = readFileSync(resolve(root, "KGV.Core/Models/AppExportModels.cs"), "utf8");
const repository = readFileSync(resolve(root, "KGV.Web/repositories/export/export-definition-repository.ts"), "utf8");

test("G14.3 keeps database, Core and Web export metadata names aligned", () => {
  assert.match(migration, /add column if not exists parameter_name text/i);
  assert.match(migration, /add column if not exists standardwert_json jsonb/i);
  assert.match(coreModel, /\[Column\("parameter_name"\)\][\s\S]*ParameterName/);
  assert.match(coreModel, /\[Column\("standardwert_json"\)\][\s\S]*StandardwertJson/);
  assert.match(repository, /parameter_name,wert_typ,standardwert_json/);
  assert.match(repository, /ist_sortierspalte/);
});

test("G14.4 protects export sources behind one role-checked server gateway", () => {
  assert.match(executionMigration, /assert_export_access/);
  assert.match(executionMigration, /c\.role in \('admin', 'vorstand'\)/);
  assert.match(executionMigration, /rpc_execute_export/);
  assert.match(executionMigration, /rpc_execute_table_or_view_export/);
  assert.match(executionMigration, /rpc_export_mitgliederliste/);
  assert.match(executionMigration, /v_export_rfid_status/);
  assert.match(executionMigration, /revoke execute on function public\.rpc_export_arbeitsstunden_uebersicht/);
});
