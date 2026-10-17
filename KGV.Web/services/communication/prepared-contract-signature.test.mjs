import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const root = new URL("../../", import.meta.url);

test("signature start is server-filtered and validates the frozen revision", async () => {
  const [repository, service] = await Promise.all([
    readFile(new URL("repositories/communication/communication-repository.ts", root), "utf8"),
    readFile(new URL("services/communication/prepared-contract-service.ts", root), "utf8"),
  ]);
  assert.match(repository, /status: "eq\.vorbereitet"[\s\S]*signatur_gestartet_am: "is\.null"[\s\S]*signatur_revision: "is\.null"/);
  assert.match(repository, /signatur_revision: `eq\.\$\{revision\}`[\s\S]*zugewiesen_an: `eq\.\$\{userId\}`/);
  assert.match(service, /startPreparedContractSigning/);
  assert.match(service, /item\.signatur_revision !== item\.revision/);
  assert.match(service, /loadPreparedContractForSignature/);
});

test("MA and PV start pads only after parking and server-side signature start", async () => {
  const [membership, lease, center] = await Promise.all([
    readFile(new URL("features/contracts/MembershipApplicationFlow.tsx", root), "utf8"),
    readFile(new URL("features/contracts/LeaseContractFlow.tsx", root), "utf8"),
    readFile(new URL("features/communication/CommunicationCenter.tsx", root), "utf8"),
  ]);
  for (const flow of [membership, lease]) {
    assert.match(flow, /parkPreparedContract[\s\S]*loadPreparedContractForEditing[\s\S]*startPreparedContractSigning[\s\S]*setStep\("signatures"\)/);
    assert.match(flow, /completePreparedContractSigning/);
    assert.match(flow, /Vorgang erneut abschließen/);
    assert.doesNotMatch(flow, /localStorage|sessionStorage/);
  }
  assert.match(center, /Signatur fortsetzen/);
  assert.match(center, /resumeMode="signatures"|resumeMode=\{opened\.resumeMode\}/);
});

test("final document is created before the workflow is completed", async () => {
  const [membership, lease] = await Promise.all([
    readFile(new URL("features/contracts/MembershipApplicationFlow.tsx", root), "utf8"),
    readFile(new URL("features/contracts/LeaseContractFlow.tsx", root), "utf8"),
  ]);
  for (const flow of [membership, lease]) {
    assert.match(flow, /generateContract\(club, session, request\("finalize"\)\)[\s\S]*completeWorkflow/);
    assert.match(flow, /finalDocumentCreated/);
  }
});
