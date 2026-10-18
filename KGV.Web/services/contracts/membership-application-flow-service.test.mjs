import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import ts from "typescript";

async function loadFlowService() {
  const source = await readFile(new URL("./membership-application-flow-service.ts", import.meta.url), "utf8");
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 } });
  return import(`data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`);
}

test("membership application signatures follow the MAUI order and require every signature", async () => {
  const { buildMembershipApplicationSignatureSteps, canFinalizeMembershipApplication, canStartMembershipApplicationSignatures, hasCompletedMembershipApplicationSignatures } = await loadFlowService();
  const alone = buildMembershipApplicationSignatureSteps({ isMinor: false, includeSecondaryMember: false });
  const withSecondary = buildMembershipApplicationSignatureSteps({ isMinor: false, includeSecondaryMember: true });
  const minor = buildMembershipApplicationSignatureSteps({ isMinor: true, includeSecondaryMember: false });

  assert.deepEqual(alone.map((step) => step.key), ["applicationMember", "privacyMember", "board"]);
  assert.deepEqual(withSecondary.map((step) => step.key), ["applicationMember", "privacyMember", "applicationSecondParty", "privacySecondParty", "board"]);
  assert.deepEqual(minor.map((step) => step.key), withSecondary.map((step) => step.key));
  assert.match(minor[2].label, /Gesetzlicher Vertreter/);
  assert.deepEqual(minor.map((step) => step.order), [1, 2, 3, 4, 5]);
  assert.throws(() => buildMembershipApplicationSignatureSteps({ isMinor: true, includeSecondaryMember: true }), /minderjähriges Mitglied/);
  assert.equal(hasCompletedMembershipApplicationSignatures(alone, { applicationMember: "a", board: "b" }), false);
  assert.equal(hasCompletedMembershipApplicationSignatures(alone, { applicationMember: "a", board: "b", privacyMember: "c" }), true);
  assert.equal(canStartMembershipApplicationSignatures("edit"), false);
  assert.equal(canStartMembershipApplicationSignatures("preview"), true);
  assert.equal(canFinalizeMembershipApplication("preview", alone, { applicationMember: "a", board: "b", privacyMember: "c" }), false);
  assert.equal(canFinalizeMembershipApplication("signatures", alone, { applicationMember: "a", board: "b", privacyMember: "c" }), true);
});

test("the current signer receives a fresh signature pad while completed signatures remain in flow state", async () => {
  const flow = await readFile(new URL("../../features/contracts/MembershipApplicationFlow.tsx", import.meta.url), "utf8");
  assert.match(flow, /<SignaturePad key=\{currentSignatureStep\.key\}/);
  assert.match(flow, /setSignatures\(\(current\) => \(\{ \.\.\.current, \[currentSignatureStep\.key\]: value \}\)\)/);
});
