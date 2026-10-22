import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const source = await readFile(new URL("./index.ts", import.meta.url), "utf8");

test("wartungsvertrag finalization uses only the persistent three-signature path", () => {
  const branch = /else if \(type === "wartungsvertrag"\) \{([\s\S]*?)\n    \} else \{ pdf = await buildSimpleContract/.exec(source);
  assert.ok(branch, "wartungsvertrag branch is missing");
  assert.match(source, /type === "wartungsvertrag" \? await loadPersistentMaintenanceSignatures/);
  assert.match(branch[1], /maintenancePersistent\.signatures/);
  assert.doesNotMatch(branch[1], /body\.signature_/);
  for (const key of ["mitglied", "vorstand_1", "vorstand_2"]) assert.match(source, new RegExp(`"${key}"`));
});

test("wartungsvertrag maps signatures and board snapshots to the published field ids", () => {
  for (const [slot, field] of [["mitglied", "unterschrift_mitglied"], ["vorstand_1", "unterschrift_vorstand_1"], ["vorstand_2", "unterschrift_vorstand_2"]]) assert.match(source, new RegExp(`${slot}: "${field}"`));
  assert.match(source, /vereinsunterzeichner_1: maintenancePersistent\.board1Snapshot/);
  assert.match(source, /vereinsunterzeichner_2: maintenancePersistent\.board2Snapshot/);
});

test("wartungsvertrag finalization checks storage integrity and persists assignment provenance", () => {
  assert.match(source, /prepared_contract_signaturen_vollstaendig/);
  assert.match(source, /auth\.admin\.storage\.from\(p\.storage_bucket\)\.download/);
  assert.match(source, /await sha256\(bytes\) !== p\.signatur_sha256/);
  assert.match(source, /source_wartungsvertrag_zuordnung_id/);
  assert.match(source, /final_dokument_id: documentId, status: "erledigt"/);
});
