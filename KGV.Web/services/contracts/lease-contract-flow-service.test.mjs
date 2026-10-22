import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import ts from "typescript";

async function loadService() {
  const source = await readFile(new URL("./lease-contract-flow-service.ts", import.meta.url), "utf8");
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 } });
  return import(`data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`);
}

test("lease contract creates persistent slots with optional second party and two board positions", async () => {
  const { buildLeaseContractPersistentSlots, preparedLeaseSignatureProgress } = await loadService();
  const alone = buildLeaseContractPersistentSlots({ isMinor: false, includeSecondaryMember: false, memberId: 7 });
  const withSecondary = buildLeaseContractPersistentSlots({ isMinor: false, includeSecondaryMember: true, memberId: 7, secondaryMemberId: 8 });
  const minor = buildLeaseContractPersistentSlots({ isMinor: true, includeSecondaryMember: false, memberId: 7 });
  assert.deepEqual(alone.map((slot) => slot.key), ["lease.member", "vorstand_1", "vorstand_2"]);
  assert.deepEqual(withSecondary.map((slot) => slot.key), ["lease.member", "lease.second_party", "vorstand_1", "vorstand_2"]);
  assert.deepEqual(minor.map((slot) => slot.key), withSecondary.map((slot) => slot.key));
  assert.equal(withSecondary[1].memberId, 8);
  assert.deepEqual(preparedLeaseSignatureProgress([{ pflicht: true, status: "unterschrieben" }, { pflicht: true, status: "offen" }]), { required: 2, signed: 1, complete: false });
});

test("lease flow stays bound to its parcel and persists individual positions", async () => {
  const flow = await readFile(new URL("../../features/contracts/LeaseContractFlow.tsx", import.meta.url), "utf8");
  const action = await readFile(new URL("../../features/contracts/LeaseContractAction.tsx", import.meta.url), "utf8");
  const generator = await readFile(new URL("../../../supabase/functions/kgv-generate-contract/index.ts", import.meta.url), "utf8");
  assert.match(flow, /parcelId:\s*number;\s*startDate:\s*string/);
  assert.doesNotMatch(flow, /listEligibleLeaseParcels|fixedParcel/);
  assert.match(flow, /<LeaseContractPreview previewUrl=\{previewUrl\}/);
  assert.match(flow, /onBack=\{\(\) => setStep\("edit"\)\}/);
  assert.match(flow, /ensurePreparedContractSignaturePosition/);
  assert.match(flow, /savePreparedContractSignature/);
  assert.match(flow, /PreparedContractSignatorySelector/);
  assert.doesNotMatch(flow, /signatureIndex|setSignatures\(|signature_member:\s*signatures/);
  assert.match(action, /await load\(\);\s*setCreating\(false\)/);
  assert.match(generator, /documentStatus\(document, "mitgliedsantrag"\) === "signiert"/);
  assert.match(generator, /from\("parzellen_belegung"\)[\s\S]*eq\("mitglied_id", memberId\)[\s\S]*eq\("parzelle_id", parcelId\)/);
});
