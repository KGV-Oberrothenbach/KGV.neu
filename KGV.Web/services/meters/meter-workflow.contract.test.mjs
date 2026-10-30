import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const read = (path) => readFile(new URL(path, import.meta.url), "utf8");

test("meter workflows use server-side permissions, leases and one atomic change RPC", async () => {
  const [migration, readings, maui, scanPage] = await Promise.all([
    read("../../../supabase/migrations/20261030150000_meter_workflow_unification.sql"),
    read("../readings/reading-service.ts"),
    read("../../../KGV.Maui/Pages/AblesungErfassenPage.cs"),
    read("../../../KGV.Maui/Pages/RfidScanWorkflowPage.cs"),
  ]);
  assert.match(migration, /current_user_has_active_lease_for_parcel[\s\S]*pachtverhaeltnis_beteiligung/);
  assert.match(migration, /submit_own_meter_reading[\s\S]*'eingereicht'/);
  assert.match(migration, /capture_meter_reading[\s\S]*has_effective_permission\(32\)/);
  assert.match(migration, /execute_meter_change[\s\S]*for update[\s\S]*insert into public\.zaehler_ablesung[\s\S]*update public\.zaehler[\s\S]*insert into public\.zaehler/);
  assert.match(migration, /zaehlerwechsel_antrag/);
  assert.match(migration, /review_meter_change_request[\s\S]*execute_meter_change/);
  assert.match(readings, /submitOwnMeterReading/);
  assert.match(readings, /captureMeterReading/);
  assert.match(maui, /IsDirectApprovalMode => PermissionChecks\.CanReadMeters/);
  assert.match(scanPage, /CreateFallbackSection\(\),[\s\S]*CreateManualEmergencySection\(\)/);
});
