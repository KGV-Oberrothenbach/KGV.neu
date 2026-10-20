import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const root = new URL("../../", import.meta.url);

test("prepared contracts validate resume identity and use a stable draft comparison", async () => {
  const service = await readFile(new URL("services/communication/prepared-contract-service.ts", root), "utf8");
  assert.match(service, /loadPreparedContractForEditing/);
  assert.match(service, /parsePreparedContractDraft/);
  const model = await readFile(new URL("models/communication/prepared-contract-draft.ts", root), "utf8");
  assert.match(model, /containsSignatureData/);
  assert.match(service, /item\.dokumenttyp !== documentType/);
  assert.match(service, /draft\.memberId !== item\.mitglied_id/);
  assert.match(service, /\.draft\.parcelId !== item\.parzelle_id/);
  assert.match(service, /Object\.entries\(value as Record<string, unknown>\)\.sort/);
  assert.match(service, /preparedContractDraftsEqual\(context\.entwurf_daten, nextDraft\)/);
});

test("prepared contract updates are revision-safe and cannot create a second contract", async () => {
  const [repository, service, migration] = await Promise.all([
    readFile(new URL("repositories/communication/communication-repository.ts", root), "utf8"),
    readFile(new URL("services/communication/prepared-contract-service.ts", root), "utf8"),
    readFile(new URL("../supabase/migrations/20261017143000_prepared_contract_edit_guard.sql", root), "utf8"),
  ]);
  assert.match(repository, /revision: `eq\.\$\{revision\}`/);
  assert.match(repository, /status: "eq\.vorbereitet"/);
  assert.match(repository, /signatur_gestartet_am: "is\.null"/);
  assert.match(repository, /signatur_revision: "is\.null"/);
  assert.match(repository, /zugewiesen_an: `eq\.\$\{userId\}`/);
  assert.match(repository, /revision: revision \+ 1/);
  assert.match(repository, /inzwischen geändert oder ist nicht mehr zur Bearbeitung freigegeben/);
  assert.match(service, /if \(preparedContractDraftsEqual[\s\S]*?\) return context/);
  assert.doesNotMatch(service.match(/savePreparedContractIfChanged[\s\S]*?\n}\nexport async function parkPreparedContract/)?.[0] ?? "", /createPreparedContract/);
  assert.match(migration, /old\.zugewiesen_an is distinct from auth\.uid\(\)/);
  assert.match(migration, /old\.signatur_gestartet_am is not null/);
});

test("communication opens only own or successfully claimed prepared contracts and reuses both editors", async () => {
  const [center, membership, lease] = await Promise.all([
    readFile(new URL("features/communication/CommunicationCenter.tsx", root), "utf8"),
    readFile(new URL("features/contracts/MembershipApplicationFlow.tsx", root), "utf8"),
    readFile(new URL("features/contracts/LeaseContractFlow.tsx", root), "utf8"),
  ]);
  assert.match(center, /item\.zugewiesen_an === null\) await claimCommunicationItem/);
  assert.match(center, /item\.zugewiesen_an && item\.zugewiesen_an !== userId/);
  assert.match(center, />Öffnen</);
  assert.match(center, /<MembershipApplicationFlow[\s\S]*preparedContractId/);
  assert.match(center, /<LeaseContractFlow[\s\S]*preparedContractId/);
  for (const flow of [membership, lease]) {
    assert.match(flow, /loadPreparedContractForEditing/);
    assert.match(flow, /savePreparedContractIfChanged/);
    assert.match(flow, /Änderungen speichern/);
    assert.match(flow, /startPreparedContractSigning/);
  }
  assert.match(membership, /generateContract\(club, session, request\("preview", snapshot\)\)/);
  assert.match(lease, /generateContract\(club, session, request\("preview", snapshot\)\)/);
  assert.doesNotMatch(membership.match(/const savePrepared[\s\S]*?const preview/)?.[0] ?? "", /signature_/i);
  assert.doesNotMatch(lease.match(/const savePrepared[\s\S]*?const preview/)?.[0] ?? "", /signature_/i);
});
