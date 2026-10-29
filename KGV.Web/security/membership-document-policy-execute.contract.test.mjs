import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const root = new URL("../../", import.meta.url);
const documentPolicyMigration = readFileSync(new URL("supabase/migrations/20261029113000_grant_board_document_policy_function_execute.sql", root), "utf8");
const workHoursMigration = readFileSync(new URL("supabase/migrations/20261029114000_fix_arbeitsstunden_export_column_ambiguity.sql", root), "utf8");
const service = readFileSync(new URL("KGV.Infrastructure/Services/SupabaseService.cs", root), "utf8");
const headers = readFileSync(new URL("KGV.Infrastructure/Supabase/SupabaseHeaderValue.cs", root), "utf8");

test("signed legacy membership applications are queried without a false fallback", () => {
  assert.match(documentPolicyMigration, /grant execute on function public\.can_access_mitgliedsaufnahme_vorstand_document\(uuid,bigint\) to authenticated/i);
  const method = service.match(/public async Task<bool> HasSignedMitgliedsantragAsync[\s\S]*?\n        }/)?.[0] ?? "";
  assert.match(method, /HasSignedFormularDokument/);
  assert.doesNotMatch(method, /ExecuteAsync/);
});

test("Supabase header values are trimmed and reject embedded control characters", () => {
  assert.match(headers, /value\?\.Trim\(\)/);
  assert.match(headers, /IndexOfAny\(\['\\r', '\\n', '\\0'\]\)/);
});

test("work-hours export qualifies CTE columns that collide with RETURNS TABLE names", () => {
  assert.match(workHoursMigration, /select s\.\* from summary s/);
  assert.match(workHoursMigration, /s\.offene_stunden > 0/);
  assert.match(workHoursMigration, /s\.pflichtstunden_soll > 0 and s\.offene_stunden <= 0/);
  assert.match(workHoursMigration, /s\.hat_wartungsvertrag/);
  assert.match(workHoursMigration, /p_jahr integer default null/);
  assert.match(workHoursMigration, /p_stunden_offen boolean default false/);
  assert.match(workHoursMigration, /p_stunden_fertig boolean default false/);
  assert.match(workHoursMigration, /p_wartungsvertraege boolean default false/);
  assert.match(workHoursMigration, /p_ansicht text default 'zusammenfassung'/);
});
