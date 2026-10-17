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
