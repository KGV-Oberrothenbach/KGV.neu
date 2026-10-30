import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
import { buildHistoricalMembershipEnds } from "./historical-contract-membership-commands.ts";
const followUp = "historischer_pv_folgevertrag";
test("A: A+B to A keeps B's membership unless explicitly confirmed", () => {
  assert.deepEqual(buildHistoricalMembershipEnds(followUp, [2], {}), []);
});
test("B: A+B to A sends the removed B and preserves an edited end date", () => {
  assert.deepEqual(buildHistoricalMembershipEnds(followUp, [2], {2:"2010-01-15"}), [{mitglied_id:2,mitglied_ende:"2010-01-15"}]);
});
test("stale decisions for a remaining or unrelated party never enter the request", () => {
  assert.deepEqual(buildHistoricalMembershipEnds(followUp, [2], {1:"2010-01-01",2:"2010-01-01",3:"2010-01-01"}), [{mitglied_id:2,mitglied_ende:"2010-01-01"}]);
  assert.deepEqual(buildHistoricalMembershipEnds(followUp, [], {2:"2010-01-01"}), []);
  assert.deepEqual(buildHistoricalMembershipEnds("historischer_ma_pv_erstvertrag", [2], {2:"2010-01-01"}), []);
});
test("workflow offers removed-party decisions with the contract date as editable default", async () => {
  const workflow = await readFile(new URL("../../features/documents/HistoricalContractRegistrationWorkflow.tsx", import.meta.url), "utf8");
  assert.match(workflow, /removedPartyIds.map/);
  assert.match(workflow, /next\[id\] = contractDate/);
  assert.match(workflow, /type="date" value={confirmedMembershipEnds\[id\]}/);
  assert.match(workflow, /membershipEnds: buildHistoricalMembershipEnds/);
  assert.doesNotMatch(workflow, /endSecondaryMembership|secondaryMembershipEnd/);
});
