import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { buildLeaseContractPersistentSlots } from "./lease-contract-flow-service.ts";
import { calculateLeaseRent, canUseSecondLeaseTenant, isEligibleLeaseTenant } from "./lease-contract-lifecycle.ts";
import { isCompleteMemberClubDocumentSnapshot, requiredClubDocumentsFor, type MemberClubDocumentSnapshotItem } from "./member-club-document-snapshot-service.ts";
import { isEligibleLeaseTenantRecord } from "../../../supabase/functions/kgv-generate-contract/lease-contract-policy.ts";

type Member = { id: number; aktiv: boolean; aufnahme_status: "aufgenommen" | "antrag_offen" | "abgelehnt"; hauptmitglied_id: number | null };
type Version = { id: number; number: number; predecessorId: number | null; authoritative: boolean };
type Process = { id: number; number: number | null; status: "vorbereitet" | "zur_signatur" | "letzte_pruefung" | "erledigt" | "abgebrochen"; predecessorId: number | null; signatures: Set<string>; documentId: number | null; versionId: number | null; dispatchId: number | null };

class LeaseWorkflowHarness {
  members = new Map<number, Member>([
    [1081, { id: 1081, aktiv: true, aufnahme_status: "aufgenommen", hauptmitglied_id: null }],
    [1082, { id: 1082, aktiv: true, aufnahme_status: "aufgenommen", hauptmitglied_id: 1081 }],
    [1090, { id: 1090, aktiv: true, aufnahme_status: "aufgenommen", hauptmitglied_id: null }],
    [1099, { id: 1099, aktiv: false, aufnahme_status: "antrag_offen", hauptmitglied_id: null }],
  ]);
  processes: Process[] = [];
  versions: Version[] = [];
  relationshipId: number | null = null;
  participants = new Set<number>();
  nextId = 500;
  nextNumber = 20015;

  prepare(primaryId: number, secondId: number | null = null, predecessorId: number | null = null) {
    const primary = this.members.get(primaryId);
    if (!isEligibleLeaseTenant(primary)) throw new Error("primary is not a member");
    if (secondId !== null && !canUseSecondLeaseTenant(primaryId, this.members.get(secondId))) throw new Error("second is not a member");
    if (predecessorId !== null) {
      const existing = this.processes.find(item => item.predecessorId === predecessorId && !["erledigt", "abgebrochen"].includes(item.status));
      if (existing) return existing;
      const predecessor = this.versions.find(item => item.id === predecessorId && item.authoritative);
      if (!predecessor) throw new Error("predecessor is not authoritative");
    }
    const process: Process = { id: this.nextId++, number: null, status: "vorbereitet", predecessorId, signatures: new Set(), documentId: null, versionId: null, dispatchId: null };
    this.processes.push(process);
    return process;
  }

  startSigning(process: Process) {
    if (process.status === "vorbereitet") process.status = "zur_signatur";
    process.number ??= this.nextNumber++;
    return process;
  }

  sign(process: Process, keys: string[]) { for (const key of keys) process.signatures.add(key); }

  finalize(process: Process, requiredKeys: string[], primaryId: number, secondId: number | null = null) {
    if (!requiredKeys.every(key => process.signatures.has(key))) throw new Error("missing signature");
    process.documentId ??= this.nextId++;
    process.status = "letzte_pruefung";
    if (process.versionId !== null) return process.versionId;
    if (this.relationshipId === null) {
      this.relationshipId = this.nextId++;
      this.participants.add(primaryId);
      if (secondId !== null) this.participants.add(secondId);
    }
    const predecessor = process.predecessorId === null ? null : this.versions.find(item => item.id === process.predecessorId)!;
    const version: Version = { id: this.nextId++, number: process.number!, predecessorId: predecessor?.id ?? null, authoritative: true };
    if (predecessor) predecessor.authoritative = false;
    this.versions.push(version);
    process.versionId = version.id;
    process.status = "erledigt";
    return version.id;
  }

  abort(process: Process) { process.status = "abgebrochen"; }
  dispatch(process: Process) { process.dispatchId ??= this.nextId++; return process.dispatchId; }
}

const snapshot = (kind: "digital" | "papier" | "missing"): MemberClubDocumentSnapshotItem[] => requiredClubDocumentsFor("pachtvertrag").flatMap((dokumenttyp, index) => kind === "missing" && index === 2 ? [] : [{ dokumenttyp, vereinsdokument_fassung_id: 100 + index, fassung: "2026", gueltig_ab: "2026-01-01", bereitstellungsart: kind === "papier" && index === 1 ? "papier" : "digital", bereitgestellt_am: "2026-10-28T10:00:00Z", bereitstellung_id: 200 + index }]);

test("A: a new single-tenant lease completes once with 20xxx, relationship, evidence and dispatch", () => {
  const flow = new LeaseWorkflowHarness();
  const process = flow.prepare(1081);
  assert.equal(process.number, null);
  flow.startSigning(process); const number = process.number; flow.startSigning(process);
  assert.equal(number, 20015); assert.equal(process.number, number);
  const keys = buildLeaseContractPersistentSlots({ isMinor: false, includeSecondaryMember: false, memberId: 1081 }).map(slot => slot.key);
  assert.throws(() => flow.finalize(process, keys, 1081), /missing signature/);
  flow.sign(process, keys);
  const version = flow.finalize(process, keys, 1081);
  assert.equal(flow.finalize(process, keys, 1081), version);
  assert.equal(flow.versions.length, 1); assert.equal(flow.participants.size, 1);
  assert.equal(flow.dispatch(process), flow.dispatch(process));
  assert.equal(isCompleteMemberClubDocumentSnapshot(snapshot("digital"), "pachtvertrag"), true);
  assert.deepEqual(calculateLeaseRent(400, 0.5, "2026-07-01"), { annualRent: 200, runningYearRent: 100 });
});

test("B/C/D/E: second tenant may be a secondary or full member, but never a non-member", () => {
  const flow = new LeaseWorkflowHarness();
  for (const secondId of [1082, 1090]) {
    const process = flow.prepare(1081, secondId);
    const slots = buildLeaseContractPersistentSlots({ isMinor: false, includeSecondaryMember: true, memberId: 1081, secondaryMemberId: secondId });
    const keys = slots.map(slot => slot.key);
    assert.ok(keys.includes("lease.second_party"));
    assert.equal(slots.find(slot => slot.key === "lease.second_party")?.signerType, "mitglied");
    assert.equal(canUseSecondLeaseTenant(1081, flow.members.get(secondId)), true);
    flow.abort(process);
  }
  assert.throws(() => flow.prepare(1081, 1099), /second is not a member/);
  assert.equal(isEligibleLeaseTenantRecord(flow.members.get(1090)), true);
  assert.equal(isEligibleLeaseTenantRecord(flow.members.get(1099)), false);
});

test("F/G: A to B to C keeps one relationship and an immediate authoritative chain", () => {
  const flow = new LeaseWorkflowHarness();
  const keys = buildLeaseContractPersistentSlots({ isMinor: false, includeSecondaryMember: false, memberId: 1081 }).map(slot => slot.key);
  const complete = (process: Process) => { flow.startSigning(process); flow.sign(process, keys); flow.finalize(process, keys, 1081); return flow.versions.at(-1)!; };
  const a = complete(flow.prepare(1081));
  const bProcess = flow.prepare(1081, null, a.id);
  assert.equal(flow.prepare(1081, null, a.id), bProcess, "replacement retry must reuse B");
  assert.equal(a.authoritative, true);
  const b = complete(bProcess);
  assert.equal(b.predecessorId, a.id); assert.equal(a.authoritative, false); assert.equal(b.authoritative, true);
  const c = complete(flow.prepare(1081, null, b.id));
  assert.equal(c.predecessorId, b.id); assert.notEqual(c.predecessorId, a.id);
  assert.equal(b.authoritative, false); assert.equal(c.authoritative, true);
  assert.equal(flow.relationshipId !== null, true); assert.equal(flow.participants.size, 1);
  assert.deepEqual(flow.versions.map(item => item.number), [20015, 20016, 20017]);
});

test("H/I: an aborted replacement consumes its number, keeps A authoritative and allows a fresh retry", () => {
  const flow = new LeaseWorkflowHarness();
  const keys = buildLeaseContractPersistentSlots({ isMinor: false, includeSecondaryMember: false, memberId: 1081 }).map(slot => slot.key);
  const aProcess = flow.startSigning(flow.prepare(1081)); flow.sign(aProcess, keys); flow.finalize(aProcess, keys, 1081); const a = flow.versions[0];
  const b = flow.startSigning(flow.prepare(1081, null, a.id)); const consumed = b.number; flow.abort(b);
  assert.equal(a.authoritative, true); assert.equal(consumed, 20016);
  const retry = flow.startSigning(flow.prepare(1081, null, a.id));
  assert.notEqual(retry.id, b.id); assert.equal(retry.number, 20017); assert.equal(a.authoritative, true);
});

test("mandatory-document evidence accepts digital or paper and rejects a missing rulebook", () => {
  assert.deepEqual(requiredClubDocumentsFor("pachtvertrag"), ["kleingartenordnung", "beitragsordnung", "arbeitsordnung"]);
  assert.equal(isCompleteMemberClubDocumentSnapshot(snapshot("digital"), "pachtvertrag"), true);
  assert.equal(isCompleteMemberClubDocumentSnapshot(snapshot("papier"), "pachtvertrag"), true);
  assert.equal(isCompleteMemberClubDocumentSnapshot(snapshot("missing"), "pachtvertrag"), false);
});

test("server migration enforces member eligibility and assigned replacement retry", () => {
  const migration = readFileSync(new URL("../../../supabase/migrations/20261028120000_phase9_pachtvertrag_e2e_haertung.sql", import.meta.url), "utf8");
  assert.match(migration, /aufnahme_status='aufgenommen'[\s\S]*aktiv=true/);
  assert.match(migration, /v_mitpaechter_id=v_vorgang\.mitglied_id/);
  assert.match(migration, /ersetzt_pachtvertragsfassung_id=p_vorgaenger_fassung_id[\s\S]*zugewiesen_an=p_assigned_to[\s\S]*status not in \('erledigt','abgebrochen'\)/);
  assert.match(migration, /complete_prepared_pachtvertrag_vor_phase9/);
});
