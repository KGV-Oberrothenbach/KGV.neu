import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const migration = readFileSync(new URL("../../../supabase/migrations/20261015210000_generic_export_gateway.sql", import.meta.url), "utf8");

test("CX10 resolves active RPC exports from metadata without fixed export cases", () => {
  assert.match(migration, /rpc_execute_export\(p_export_key text, p_parameters jsonb/);
  assert.match(migration, /from public\.app_export_definition/);
  assert.match(migration, /aktiv = true and quelle_typ = 'rpc'/);
  assert.match(migration, /format\('select \* from public\.%I\(\$1\)', source_name\)/);
  assert.doesNotMatch(migration, /case p_source_name/);
  assert.match(migration, /assert_export_access/);
});

test("CX10 supplies JSON wrappers and a disabled metadata-only architecture test", () => {
  assert.match(migration, /rpc_export_mitgliederliste_json\(p_parameters jsonb/);
  assert.match(migration, /rpc_export_cx10_test\(p_parameters jsonb/);
  assert.match(migration, /'cx10_test_export'/);
  assert.match(migration, /aktiv = false/);
});
