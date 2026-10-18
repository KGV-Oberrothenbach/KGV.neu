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

test("membership application creation parks a prepared contract without starting signatures", async () => {
  const [membership, lease, center] = await Promise.all([
    readFile(new URL("features/contracts/MembershipApplicationFlow.tsx", root), "utf8"),
    readFile(new URL("features/contracts/LeaseContractFlow.tsx", root), "utf8"),
    readFile(new URL("features/communication/CommunicationCenter.tsx", root), "utf8"),
  ]);
  const creationPreview = membership.match(/\{step === "preview"\}[\s\S]*?<ContractPreview[\s\S]*?\/>/)?.[0] ?? "";
  assert.match(membership, /parkPreparedContract/);
  assert.match(membership, /Die Signatur kann nur aus der Kommunikationszentrale gestartet werden/);
  assert.doesNotMatch(creationPreview, /onContinue/);
  assert.match(lease, /startPreparedContractSigning/);
  assert.doesNotMatch(membership, /localStorage|sessionStorage/);
  assert.match(center, /Signatur fortsetzen/);
  assert.match(center, /resumeMode="signatures"|resumeMode=\{opened\.resumeMode\}/);
});

test("final document enters final review before it can be completed", async () => {
  const [membership, lease, repository, center] = await Promise.all([
    readFile(new URL("features/contracts/MembershipApplicationFlow.tsx", root), "utf8"),
    readFile(new URL("features/contracts/LeaseContractFlow.tsx", root), "utf8"),
    readFile(new URL("repositories/communication/communication-repository.ts", root), "utf8"),
    readFile(new URL("features/communication/CommunicationCenter.tsx", root), "utf8"),
  ]);
  for (const flow of [membership, lease]) {
    assert.match(flow, /generateContract\(club, session, request\("finalize"\)\)[\s\S]*markFinalDocumentForReview/);
    assert.match(flow, /Finales Dokument zur Prüfung vormerken/);
    assert.match(flow, /PreparedFinalReview/);
  }
  assert.match(repository, /status: "letzte_pruefung", final_dokument_id: finalDocumentId/);
  assert.match(repository, /status: "eq\.letzte_pruefung"[\s\S]*final_dokument_id: `eq\.\$\{finalDocumentId\}`/);
  assert.match(center, /Letzte Prüfung/);
  assert.doesNotMatch(center, /letzte_pruefung[\s\S]*Signatur fortsetzen/);
  assert.match(membership, /bereits erstellte finale Dokument wurde zur letzten Prüfung übernommen/);
  assert.match(membership, /recoverPreparedContractFinalDocument/);
  assert.doesNotMatch(membership, /documentState\.status === "signed"/);
  assert.match(membership, /onCompleted=\{async \(\) => \{ await onSaved\(\); onCancel\?\.\(\); \}\}/);
  assert.match(center, /<MembershipApplicationFlow[\s\S]*onCancel=\{\(\) => \{ setOpened\(null\); void load\(\); \}\}/);
});
