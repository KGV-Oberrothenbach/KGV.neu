import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import ts from "typescript";

async function loadService() {
  const source = await readFile(new URL("./lease-contract-flow-service.ts", import.meta.url), "utf8");
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 } });
  return import(`data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`);
}

test("lease contract signatures follow the MAUI order and require every signer", async () => {
  const { buildLeaseContractSignatureSteps, hasCompletedLeaseContractSignatures } = await loadService();
  const alone = buildLeaseContractSignatureSteps({ isMinor: false, includeSecondaryMember: false });
  const withSecondary = buildLeaseContractSignatureSteps({ isMinor: false, includeSecondaryMember: true });
  const minor = buildLeaseContractSignatureSteps({ isMinor: true, includeSecondaryMember: false });

  assert.deepEqual(alone.map((step) => step.key), ["member", "board"]);
  assert.deepEqual(withSecondary.map((step) => step.key), ["member", "secondary", "board"]);
  assert.deepEqual(minor.map((step) => step.key), withSecondary.map((step) => step.key));
  assert.match(minor[1].label, /gesetzliche\/r Vertreter\/in/);
  assert.equal(hasCompletedLeaseContractSignatures(withSecondary, { member: "a", board: "b" }), false);
  assert.equal(hasCompletedLeaseContractSignatures(withSecondary, { member: "a", secondary: "b", board: "c" }), true);
});

test("lease flow stays bound to its parcel, previews inline and mounts one fresh pad per signer", async () => {
  const flow = await readFile(new URL("../../features/contracts/LeaseContractFlow.tsx", import.meta.url), "utf8");
  const action = await readFile(new URL("../../features/contracts/LeaseContractAction.tsx", import.meta.url), "utf8");
  const generator = await readFile(new URL("../../../supabase/functions/kgv-generate-contract/index.ts", import.meta.url), "utf8");
  assert.match(flow, /parcelId:\s*number;\s*startDate:\s*string/);
  assert.doesNotMatch(flow, /listEligibleLeaseParcels|fixedParcel/);
  assert.match(flow, /<LeaseContractPreview previewUrl=\{previewUrl\}/);
  assert.match(flow, /onBack=\{\(\) => setStep\("edit"\)\}/);
  assert.match(flow, /<SignaturePad key=\{currentSignatureStep\.key\}/);
  assert.match(flow, /clearSignatures\(\);\s*setStep\("signatures"\)/);
  assert.match(flow, /step\s*!==\s*"signatures"\s*\|\|\s*!hasCompletedLeaseContractSignatures/);
  assert.match(action, /await load\(\);\s*setCreating\(false\)/);
  assert.match(generator, /documentStatus\(document, "mitgliedsantrag"\) === "signiert"/);
  assert.match(generator, /from\("parzellen_belegung"\)[\s\S]*eq\("mitglied_id", memberId\)[\s\S]*eq\("parzelle_id", parcelId\)/);
});
