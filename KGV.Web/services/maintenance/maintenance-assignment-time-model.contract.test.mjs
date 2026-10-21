import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { classifyMaintenanceAssignments } from "./maintenance-assignment-time-model.js";

const assignment = (id, contractId, validFrom, validUntil = null) => ({
  id,
  maintenanceContractId: contractId,
  principalMemberId: 1,
  validFrom,
  validUntil,
  note: null,
});

test("member assignments use inclusive shared date boundaries and all three statuses", async () => {
  const [coreModel, service] = await Promise.all([
    readFile(new URL("../../../KGV.Core/Models/WartungsvertragZeitstatus.cs", import.meta.url), "utf8"),
    readFile(new URL("./maintenance-assignment-service.ts", import.meta.url), "utf8"),
  ]);
  const classified = classifyMaintenanceAssignments([
    assignment(1, 1, "2026-10-21", "2026-10-21"),
    assignment(2, 2, "2026-10-22"),
    assignment(3, 3, "2026-10-20", "2026-10-20"),
  ], "2026-10-21", [
    { id: 1, title: "Aktuell", active: true },
    { id: 2, title: "Zukünftig", active: true },
    { id: 3, title: "Historisch", active: true },
  ]);

  assert.deepEqual(classified.active.map((item) => item.id), [1]);
  assert.deepEqual(classified.future.map((item) => item.id), [2]);
  assert.deepEqual(classified.history.map((item) => item.id), [3]);
  assert.match(coreModel, /gueltigAb\.Date > today/);
  assert.match(coreModel, /gueltigBis\.Value\.Date < today/);
  assert.match(service, /classifyMaintenanceAssignments\(assignments, maintenanceReferenceDate\(\), contracts\)/);
});

test("member assignment lists sort current by title, future by start, history by end", () => {
  const contracts = [
    { id: 1, title: "Zulu" },
    { id: 2, title: "Alpha" },
    { id: 3, title: "Beta" },
  ];
  const classified = classifyMaintenanceAssignments([
    assignment(1, 1, "2026-10-21", null),
    assignment(2, 2, "2026-10-21", null),
    assignment(3, 1, "2026-10-25"),
    assignment(4, 2, "2026-10-23"),
    assignment(5, 1, "2026-10-01", "2026-10-10"),
    assignment(6, 3, "2026-10-01", "2026-10-19"),
  ], "2026-10-21", contracts);

  assert.deepEqual(classified.active.map((item) => item.maintenanceContractId), [2, 1]);
  assert.deepEqual(classified.future.map((item) => item.id), [4, 3]);
  assert.deepEqual(classified.history.map((item) => item.id), [6, 5]);
});

test("member view delegates classification and contract work-hour labels to services", async () => {
  const [component, assignmentForm, service, core, repository] = await Promise.all([
    readFile(new URL("../../features/maintenance/MemberMaintenanceContracts.tsx", import.meta.url), "utf8"),
    readFile(new URL("../../features/maintenance/assignments/AssignContractsToMember.tsx", import.meta.url), "utf8"),
    readFile(new URL("./maintenance-assignment-service.ts", import.meta.url), "utf8"),
    readFile(new URL("../../../KGV.Infrastructure/Services/SupabaseService.cs", import.meta.url), "utf8"),
    readFile(new URL("../../repositories/maintenance/maintenance-assignment-repository.ts", import.meta.url), "utf8"),
  ]);
  assert.match(component, /renderSection\("Aktuell", "Aktuell", assignments\.active\)/);
  assert.match(component, /renderSection\("Zukünftig", "Zukünftig", assignments\.future\)/);
  assert.match(component, /renderSection\("Historisch", "Historisch", assignments\.history\)/);
  assert.match(component, /maintenanceWorkHoursEffectLabel\(contract\)/);
  assert.doesNotMatch(component, /classifyMaintenanceAssignments|exemptsFromDutyHours\s*\?|workHoursCredit\s*>/);
  assert.match(service, /resolveMaintenancePrincipalMember\(club, session, selectedMemberId\)/);
  assert.match(service, /loadMaintenanceAssignments\(club, session, principalMemberId\)/);
  assert.match(service, /function principalMemberId\(member:[\s\S]*?member\.hauptmitglied_id \?\? member\.id/);
  assert.match(service, /new Set\(principalMemberIds\)\.size !== principalMemberIds\.length/);
  assert.match(assignmentForm, /if \(result\.created\.length\) await onChanged\(\)/);
  assert.match(repository, /resolveMaintenanceAssignmentPrincipalMember/);
  assert.match(core, /ResolveHomeMitgliedIdAsync\(mitgliedId\)/);
});
