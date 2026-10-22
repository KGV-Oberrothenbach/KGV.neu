import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import ts from "typescript";

async function loadFlowService() {
  const source = await readFile(new URL("./membership-application-flow-service.ts", import.meta.url), "utf8");
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 } });
  return import(`data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`);
}

test("membership application builds idempotent persistent positions with mutually exclusive second-party alternatives", async () => {
  const { buildMembershipApplicationPersistentSlots } = await loadFlowService();
  const alone = buildMembershipApplicationPersistentSlots({ isMinor: false, includeSecondaryMember: false, memberId: 1 });
  const withSecondary = buildMembershipApplicationPersistentSlots({ isMinor: false, includeSecondaryMember: true, memberId: 1, secondaryMemberId: 2 });
  const minor = buildMembershipApplicationPersistentSlots({ isMinor: true, includeSecondaryMember: false, memberId: 1, representativeMemberId: 3 });
  assert.deepEqual(alone.map(slot => slot.key), ["member.application", "member.privacy", "vorstand_1", "vorstand_2"]);
  assert.deepEqual(withSecondary.map(slot => slot.key), ["member.application", "member.privacy", "nebenmitglied.antrag", "nebenmitglied.datenschutz", "vorstand_1", "vorstand_2"]);
  assert.deepEqual(minor.map(slot => slot.key), ["member.application", "member.privacy", "gesetzliche_vertretung.antrag", "gesetzliche_vertretung.datenschutz", "vorstand_1", "vorstand_2"]);
  assert.throws(() => buildMembershipApplicationPersistentSlots({ isMinor: true, includeSecondaryMember: true, memberId: 1 }), /minderjähriges Mitglied/);
});

test("web flow uses persisted single positions, structured board selection and no client signature payload", async () => {
  const flow = await readFile(new URL("../../features/contracts/MembershipApplicationFlow.tsx", import.meta.url), "utf8");
  assert.match(flow, /ensurePreparedContractSignaturePosition/);
  assert.match(flow, /listPreparedContractSignatures/);
  assert.match(flow, /uploadPreparedContractSignature/);
  assert.match(flow, /savePreparedContractSignature/);
  assert.match(flow, /PreparedContractSignatorySelector/);
  assert.match(flow, /excludeMemberId=\{boardSignerAlreadyUsed\}/);
  assert.match(flow, /kommunikationsvorgang_id: resumeContext\.id/);
  assert.doesNotMatch(flow, /signatureIndex|advanceSignature|setSignatures\(|signature_application_member/);
  assert.match(flow, /Speichern und Weiter/);
  assert.match(flow, />Abbrechen</);
});
