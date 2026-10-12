import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import test from "node:test";

const root = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(resolve(root, "supabase/migrations/20261012210000_g14_export_metadata_contract.sql"), "utf8");
const coreModel = readFileSync(resolve(root, "KGV.Core/Models/AppExportModels.cs"), "utf8");
const repository = readFileSync(resolve(root, "KGV.Web/repositories/export/export-definition-repository.ts"), "utf8");

test("G14.3 keeps database, Core and Web export metadata names aligned", () => {
  assert.match(migration, /add column if not exists parameter_name text/i);
  assert.match(migration, /add column if not exists standardwert_json jsonb/i);
  assert.match(coreModel, /\[Column\("parameter_name"\)\][\s\S]*ParameterName/);
  assert.match(coreModel, /\[Column\("standardwert_json"\)\][\s\S]*StandardwertJson/);
  assert.match(repository, /parameter_name,standardwert_json/);
  assert.match(repository, /ist_sortierspalte/);
});
