import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
const root = new URL("../../", import.meta.url);
test("prepared lease contract draft and parking contract remain complete and storage-free", async () => {
  const [model, service, preview, flow, migration, membership] = await Promise.all([
    readFile(new URL("models/communication/prepared-contract-draft.ts", root), "utf8"), readFile(new URL("services/communication/prepared-contract-service.ts", root), "utf8"), readFile(new URL("features/contracts/LeaseContractPreview.tsx", root), "utf8"), readFile(new URL("features/contracts/LeaseContractFlow.tsx", root), "utf8"), readFile(new URL("../supabase/migrations/20261017140000_kommunikationsvorgang_active_contract_uniques.sql", root), "utf8"), readFile(new URL("features/contracts/MembershipApplicationFlow.tsx", root), "utf8"),
  ]);
  for (const field of ["startDate", "parcelId", "hasPreviousContract", "previousContractDate", "includeSecondaryMember", "handoverDate", "fixedUntil", "gardenRulesVersion", "gardenRulesHandedOverAt", "additionalAgreements", "attachment1", "attachment2", "attachment3", "attachment4", "clubSignatory"]) assert.match(model, new RegExp(field));
  assert.doesNotMatch(model, /signature_/i);
  assert.match(service, /dokumenttyp: draft\.document_type[\s\S]*status: "vorbereitet"[\s\S]*mitglied_id: draft\.memberId[\s\S]*parzelle_id: isLease \? draft\.draft\.parcelId/);
  assert.match(service, /findActivePreparedContract[\s\S]*status !== "vorbereitet"[\s\S]*savePreparedContractIfChanged/);
  assert.doesNotMatch(service, /finalize|storage|bucket/i);
  assert.match(preview, /Zur Signatur vorbereiten/); assert.match(preview, /Weiter zu Unterschriften/);
  assert.match(flow, /onPark=\{\(\) => void park\(\)\}/); assert.match(flow, /parkPreparedContract/);
  const parkBody = flow.match(/const park = async \(\) => \{([\s\S]*?)const finalize/ )?.[1] ?? "";
  assert.doesNotMatch(parkBody, /generateContract|finalize|signature_/i);
  assert.match(membership, /parkPreparedContract[\s\S]*document_type: "mitgliedsantrag"/);
  assert.match(migration, /kommunikationsvorgang \(mitglied_id, parzelle_id\)[\s\S]*dokumenttyp = 'pachtvertrag'/);
});

test("prepared lease final review completes only through the lease edge-function client", async () => {
  const [service, completion] = await Promise.all([
    readFile(new URL("services/communication/prepared-contract-service.ts", root), "utf8"),
    readFile(new URL("repositories/contracts/lease-contract-completion-repository.ts", root), "utf8"),
  ]);
  assert.match(service, /dokumenttyp === "pachtvertrag"[\s\S]*completePreparedLeaseContract/);
  assert.match(service, /completePreparedMembershipApplication/);
  assert.match(completion, /kgv-complete-prepared-lease-contract/);
  assert.match(completion, /expected_revision: expectedRevision/);
  assert.match(completion, /Authorization: `Bearer \$\{session\.accessToken\}`/);
  assert.doesNotMatch(completion, /service_role|rest\/v1\/rpc/i);
  assert.match(completion, /bereits_abgeschlossen/);
});
