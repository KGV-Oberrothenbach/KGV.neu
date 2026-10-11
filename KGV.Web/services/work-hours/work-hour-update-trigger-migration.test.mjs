import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const functionPattern = /CREATE OR REPLACE FUNCTION public\.enforce_own_open_arbeitsstunde_update\(\)[\s\S]*?AS \$\$([\s\S]*?)\$\$;/i;

async function readMigration(path) {
  return readFile(new URL(path, import.meta.url), "utf8");
}

function normalizeSql(value) {
  return value.toLowerCase().trim().replace(/\s+/g, " ").replace(/\s*([()])\s*/g, "$1");
}

test("forward migration only removes the nonexistent is_demo comparison from the latest G8 trigger", async () => {
  const latestG8 = await readMigration("../../../supabase/migrations/20261009220000_g8_work_assignment_work_hours.sql");
  const forward = await readMigration("../../../supabase/migrations/20261011114000_g7_fix_work_hour_update_trigger.sql");
  const latestG8Body = latestG8.match(functionPattern)?.[1];
  const forwardBody = forward.match(functionPattern)?.[1];

  assert.ok(latestG8Body, "latest G8 function definition should exist");
  assert.ok(forwardBody, "forward function definition should exist");
  assert.equal(forward.match(/CREATE OR REPLACE FUNCTION/gi)?.length, 1);
  assert.match(forward, /^\s*CREATE OR REPLACE FUNCTION public\.enforce_own_open_arbeitsstunde_update\(\)/i);
  assert.equal(
    normalizeSql(forwardBody),
    normalizeSql(latestG8Body.replace(" or new.is_demo is distinct from old.is_demo", "")),
    "the forward function should preserve all G8 behavior except the invalid is_demo field comparison",
  );
  assert.doesNotMatch(forwardBody, /\b(?:new|old)\.is_demo\b/i);

  for (const field of ["mitglied_id", "saison_id", "arbeitseinsatz_anmeldung_id"]) {
    assert.match(forwardBody, new RegExp(`new\\.${field} IS DISTINCT FROM old\\.${field}`, "i"));
  }
  assert.match(forwardBody, /current_setting\('app\.g7_work_hour_review', true\).*has_effective_permission\(256\)/i);
  assert.match(forwardBody, /status <> 'offen'.*freigegeben <> false/is);
  assert.match(forwardBody, /new\.datum <> v_assignment_date/i);
  assert.match(forwardBody, /public\.browser_edit_lock/);
  assert.match(forwardBody, /new\.lockedbyuserid = auth\.uid\(\)/i);
  assert.match(forwardBody, /old\.lockedbyuserid = auth\.uid\(\)/i);
  assert.match(forwardBody, /Die Arbeitseinsatzzuordnung ist unveränderlich\./);
  assert.match(forwardBody, /Nur eigene offene Arbeitsstunden dürfen bearbeitet werden\..*42501/s);
  assert.match(forwardBody, /Arbeitsstunden-Sperren dürfen nur über den Browser-Lock geändert werden\..*42501/s);

  assert.doesNotMatch(forward, /DROP TRIGGER|CREATE TRIGGER/i);
  const triggerMigration = await readMigration("../../../supabase/migrations/20261009120000_g7_own_work_hour_update.sql");
  assert.match(triggerMigration, /CREATE TRIGGER trg_enforce_own_open_arbeitsstunde_update[\s\S]*?EXECUTE FUNCTION public\.enforce_own_open_arbeitsstunde_update\(\)/i);

  const reviewMigration = await readMigration("../../../supabase/migrations/20261009140000_g7_atomic_work_hour_review.sql");
  assert.match(reviewMigration, /set_config\('app\.g7_work_hour_review', 'true', true\)/i);
  assert.match(reviewMigration, /UPDATE public\.arbeitsstunde/i);
  const workAssignmentMigration = await readMigration("../../../supabase/migrations/20261009220000_g8_work_assignment_work_hours.sql");
  assert.match(workAssignmentMigration, /public\.submit_arbeitseinsatz_arbeitsstunde/);
  assert.match(workAssignmentMigration, /arbeitseinsatz_anmeldung_id IS DISTINCT FROM old\.arbeitseinsatz_anmeldung_id/i);
});
