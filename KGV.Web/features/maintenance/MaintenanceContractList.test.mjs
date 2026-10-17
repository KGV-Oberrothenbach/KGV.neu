import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("maintenance contract overview keeps only the compact columns and selection handler", async () => {
  const component = await readFile(new URL("./MaintenanceContractList.tsx", import.meta.url), "utf8");

  assert.match(component, /<th>Titel<\/th><th>Belegung<\/th><th>Arbeitsstundenwirkung<\/th>/);
  assert.match(component, /\{item\.occupied\} von \{item\.maxActiveAssignments\}/);
  assert.match(component, /onClick=\{\(\) => onSelect\(item\.id\)\}/);
  assert.doesNotMatch(component, /<th>Bereich<\/th>|<th>Status<\/th>|item\.description|item\.area|item\.available/);
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
