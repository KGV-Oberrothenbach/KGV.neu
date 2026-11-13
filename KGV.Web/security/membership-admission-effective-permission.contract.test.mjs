import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";

test("membership admission decisions use the same effective CanCreateMitglied contract as board start", async () => {
  const migration = await readFile(new URL("../../supabase/migrations/20261113110000_p11_ap03c_membership_admission_effective_decision.sql", import.meta.url), "utf8");
  assert.match(migration, /has_effective_permission\(524288\)/);
  assert.match(migration, /kann_mitgliedsaufnahme_entscheiden\(\)/);
  assert.match(migration, /Keine aktuelle Entscheidungsberechtigung/);
});
