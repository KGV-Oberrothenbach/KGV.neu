import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("maintenance contract overview shows the current W2 state and selection handler", async () => {
  const component = await readFile(new URL("./MaintenanceContractList.tsx", import.meta.url), "utf8");

  assert.match(component, /<th>Titel<\/th><th>Status<\/th><th>Belegung heute<\/th><th>Arbeitsstundenwirkung<\/th>/);
  assert.match(component, /\{item\.occupied\} von \{item\.maxActiveAssignments\}/);
  assert.match(component, /item\.active \? "Aktiv" : "Inaktiv"/);
  assert.match(component, /onClick=\{\(\) => onSelect\(item\.id\)\}/);
  assert.doesNotMatch(component, /<th>Bereich<\/th>|item\.description|item\.area|item\.available/);
});

test("selected maintenance contracts open the existing read-only detail view with all contract fields", async () => {
  const [overview, detail] = await Promise.all([
    readFile(new URL("./MaintenanceContractOverview.tsx", import.meta.url), "utf8"),
    readFile(new URL("./MaintenanceContractDetail.tsx", import.meta.url), "utf8"),
  ]);

  assert.match(overview, /const selected = items\.find\(\(item\) => item\.id === selectedId\)/);
  assert.match(overview, /<MaintenanceContractDetail contract=\{selected\}/);
  assert.match(detail, /<h2>\{contract\.title\}<\/h2>/);
  for (const label of ["Bereich", "Beschreibung", "Max. Belegung", "Aktuelle Belegung", "Arbeitsstundenwirkung", "Status", "Bemerkung"]) {
    assert.match(detail, new RegExp(`>${label}<`));
  }
  assert.match(detail, /\{canManage && <button onClick=\{onEdit\}>Bearbeiten<\/button>\}/);
});

test("member assignment stays in the selected contract detail and reuses the existing assignment flow", async () => {
  const [overview, detail, assignmentForm, assignmentService, page] = await Promise.all([
    readFile(new URL("./MaintenanceContractOverview.tsx", import.meta.url), "utf8"),
    readFile(new URL("./MaintenanceContractDetail.tsx", import.meta.url), "utf8"),
    readFile(new URL("./assignments/AssignMembersToContract.tsx", import.meta.url), "utf8"),
    readFile(new URL("../../services/maintenance/maintenance-assignment-service.ts", import.meta.url), "utf8"),
    readFile(new URL("../../app/page.tsx", import.meta.url), "utf8"),
  ]);

  assert.match(overview, /memberAssignmentPanel=\{assignmentPanel\}/);
  assert.match(detail, /<h3>Mitgliederzuordnung<\/h3>/);
  assert.match(detail, /title="Aktuelle Zuordnungen"/);
  assert.match(detail, /title="Zukünftige Zuordnungen"/);
  assert.match(detail, /title="Historische Zuordnungen"/);
  assert.match(detail, /memberAssignmentPanel/);
  assert.match(assignmentForm, /assignMembersToMaintenanceContract/);
  assert.match(assignmentService, /resolveMaintenanceAssignmentMembers/);
  assert.match(assignmentService, /calculatePeakMaintenanceOccupancy[\s\S]*?maxActiveAssignments/);
  assert.match(assignmentService, /endMaintenanceAssignmentForContract/);
  assert.match(page, /assignmentPanel=\{canManage && selected\?\.active/);
  assert.match(page, /onSaved=\{async \(count\) => \{ setAssigningMembers\(false\); await load\(\);/);
  assert.match(page, /await endMaintenanceAssignmentForContract\(club, session, item, endDate\); await load\(\);/);
});
