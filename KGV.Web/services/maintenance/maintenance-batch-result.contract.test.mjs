import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("batch saves retain successful operations and report every failed target", async () => {
  const [service, core, web, maui] = await Promise.all([
    readFile(new URL("./maintenance-assignment-service.ts", import.meta.url), "utf8"),
    readFile(new URL("../../../KGV.Core/Models/WartungsvertragAssignmentSaveResult.cs", import.meta.url), "utf8"),
    readFile(new URL("../../features/maintenance/assignments/AssignMembersToContract.tsx", import.meta.url), "utf8"),
    readFile(new URL("../../../KGV.Maui/Pages/WartungsvertragAssignMembersPage.cs", import.meta.url), "utf8"),
  ]);
  assert.match(core, /MitgliedName.*WartungsvertragTitel.*Success.*Message/s);
  assert.match(core, /FailedCount/);
  assert.match(service, /const created: MaintenanceAssignment\[\] = \[\]; const failures/);
  assert.match(service, /failures\.push/);
  assert.match(web, /result\.created\.length} von/);
  assert.match(maui, /result\.FailedCount > 0/);
});
