import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
import ts from "typescript";

const source = await readFile(new URL("./member-club-document-snapshot-service.ts", import.meta.url), "utf8");
const runnable = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext } }).outputText;
const mod = await import(`data:text/javascript,${encodeURIComponent(runnable)}`);
const base = ["kleingartenordnung", "beitragsordnung", "arbeitsordnung"].map((dokumenttyp, index) => ({ dokumenttyp, vereinsdokument_fassung_id: index + 1, fassung: `F${index + 1}`, gueltig_ab: "2026-01-01", gueltig_bis: index === 0 ? null : "2026-12-31", bereitstellungsart: "digital", bereitgestellt_am: "2026-01-02T10:00:00Z", bereitstellung_id: index + 10 }));
test("club document snapshots survive JSON round-trips, key reordering and nullish gueltig_bis", () => {
  const roundTrip = JSON.parse(JSON.stringify(base));
  const reordered = roundTrip.map(item => ({ bereitstellung_id: item.bereitstellung_id, bereitgestellt_am: item.bereitgestellt_am, bereitstellungsart: item.bereitstellungsart, gueltig_bis: item.gueltig_bis, gueltig_ab: item.gueltig_ab, fassung: item.fassung, vereinsdokument_fassung_id: item.vereinsdokument_fassung_id, dokumenttyp: item.dokumenttyp }));
  const undefinedEnd = reordered.map((item, index) => index ? item : { ...item, gueltig_bis: undefined });
  assert.equal(mod.memberClubDocumentSnapshotsEqual(base, roundTrip, "pachtvertrag"), true);
  assert.equal(mod.memberClubDocumentSnapshotsEqual(base, reordered, "pachtvertrag"), true);
  assert.equal(mod.memberClubDocumentSnapshotsEqual(base, undefinedEnd, "pachtvertrag"), true);
});
test("club document snapshots detect materially different evidence", () => {
  assert.equal(mod.memberClubDocumentSnapshotsEqual(base, base.map((x, i) => i ? x : { ...x, vereinsdokument_fassung_id: 999 }), "pachtvertrag"), false);
  assert.equal(mod.memberClubDocumentSnapshotsEqual(base, base.map((x, i) => i ? x : { ...x, bereitstellung_id: 999 }), "pachtvertrag"), false);
  assert.equal(mod.memberClubDocumentSnapshotsEqual(base, base.map((x, i) => i ? x : { ...x, bereitgestellt_am: "2026-01-03T10:00:00Z" }), "pachtvertrag"), false);
});

test("required sets accept exactly the contract-specific digital or paper evidence", () => {
  const ma = [{ dokumenttyp: "satzung", vereinsdokument_fassung_id: 1, fassung: "S1", gueltig_ab: "2026-01-01", bereitstellungsart: "digital", bereitgestellt_am: "2026-01-02", bereitstellung_id: 10 }];
  const pv = ["kleingartenordnung", "beitragsordnung", "arbeitsordnung"].map((dokumenttyp, index) => ({ ...ma[0], dokumenttyp, vereinsdokument_fassung_id: index + 1, bereitstellung_id: index + 10 }));
  assert.equal(mod.isCompleteMemberClubDocumentSnapshot(ma, "mitgliedsantrag"), true);
  assert.equal(mod.isCompleteMemberClubDocumentSnapshot([{ dokumenttyp: "satzung", vereinsdokument_fassung_id: null, fassung: null, gueltig_ab: null, bereitstellungsart: "papier", bereitgestellt_am: "2026-01-02", bereitstellung_id: null }], "mitgliedsantrag"), true);
  assert.equal(mod.isCompleteMemberClubDocumentSnapshot(pv, "pachtvertrag"), true);
  assert.equal(mod.isCompleteMemberClubDocumentSnapshot(pv.map((item, index) => index === 1 ? { ...item, vereinsdokument_fassung_id: null, fassung: null, gueltig_ab: null, bereitstellung_id: null, bereitstellungsart: "papier" } : item), "pachtvertrag"), true);
  assert.equal(mod.isCompleteMemberClubDocumentSnapshot(pv.slice(0, 2), "pachtvertrag"), false);
  assert.equal(mod.isCompleteMemberClubDocumentSnapshot([{ ...ma[0], dokumenttyp: "kleingartenordnung" }], "mitgliedsantrag"), false);
});
