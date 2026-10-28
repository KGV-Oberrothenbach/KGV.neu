import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { maintenanceContractPersistentSlots } from "../contracts/maintenance-contract-flow-service.ts";

type Contract = { id: number; number: number | null; active: boolean };
type Assignment = { id: number; contractId: number; partyId: number; from: string; until: string | null; predecessorId: number | null; documentComplete: boolean };
type Process = { id: number; contractId: number; partyId: number; assignmentId: number | null; status: "vorbereitet" | "zur_signatur" | "erledigt" | "abgebrochen"; documentId: number | null; signatures: Set<string> };

class MaintenanceHarness {
  nextId = 100;
  nextNumber = 90012;
  contracts: Contract[] = [];
  assignments: Assignment[] = [];
  processes: Process[] = [];
  notices = new Map<number, number>();

  prepareNew(partyId: number, existingAssignmentId: number | null = null) {
    if (existingAssignmentId !== null) {
      const existing = this.processes.find(item => item.assignmentId === existingAssignmentId);
      if (existing) return existing;
    } else {
      const existing = this.processes.find(item => item.assignmentId === null && item.partyId === partyId && ["vorbereitet", "zur_signatur"].includes(item.status));
      if (existing) return existing;
    }
    const contract: Contract = { id: this.nextId++, number: null, active: existingAssignmentId !== null };
    this.contracts.push(contract);
    const process: Process = { id: this.nextId++, contractId: contract.id, partyId, assignmentId: existingAssignmentId, status: "vorbereitet", documentId: null, signatures: new Set() };
    this.processes.push(process);
    return process;
  }

  start(process: Process) {
    if (process.status === "vorbereitet") process.status = "zur_signatur";
    const contract = this.contracts.find(item => item.id === process.contractId)!;
    contract.number ??= this.nextNumber++;
    return contract.number;
  }

  sign(process: Process, keys: string[]) { keys.forEach(key => process.signatures.add(key)); }

  finalize(process: Process, validFrom = "2027-01-01") {
    const required = maintenanceContractPersistentSlots(process.partyId).map(slot => slot.key);
    if (!required.every(key => process.signatures.has(key))) throw new Error("missing signature");
    process.documentId ??= this.nextId++;
    if (process.status === "erledigt") return process.assignmentId;
    if (process.assignmentId === null) {
      const previous = this.assignments.find(item => item.until === null && item.partyId === process.partyId);
      process.assignmentId = this.nextId++;
      this.assignments.push({ id: process.assignmentId, contractId: process.contractId, partyId: process.partyId, from: validFrom, until: null, predecessorId: previous?.id ?? null, documentComplete: true });
      if (previous) previous.until = "2026-12-31";
    } else {
      const assignment = this.assignments.find(item => item.id === process.assignmentId);
      if (assignment) assignment.documentComplete = true;
    }
    this.contracts.find(item => item.id === process.contractId)!.active = true;
    process.status = "erledigt";
    return process.assignmentId;
  }

  extend(assignmentId: number, until: string) {
    const old = this.assignments.find(item => item.id === assignmentId)!;
    const existing = this.assignments.find(item => item.predecessorId === assignmentId);
    if (existing) return existing;
    const next: Assignment = { id: this.nextId++, contractId: old.contractId, partyId: old.partyId, from: "2027-01-01", until, predecessorId: old.id, documentComplete: false };
    this.assignments.push(next);
    this.notices.set(next.id, this.nextId++);
    return next;
  }

  completeNotice(assignmentId: number) {
    const assignment = this.assignments.find(item => item.id === assignmentId)!;
    assignment.documentComplete = true;
    return this.notices.get(assignmentId)!;
  }
}

test("A/B/C: new contract uses the actual party, one 90xxx, required signatures and idempotent completion", () => {
  const flow = new MaintenanceHarness();
  const anchor: Assignment = { id: 50, contractId: 0, partyId: 1082, from: "2026-01-01", until: null, predecessorId: null, documentComplete: false };
  flow.assignments.push(anchor);
  const process = flow.prepareNew(1082, anchor.id);
  assert.equal(process.status, "vorbereitet"); assert.equal(flow.contracts[0].number, null);
  assert.equal(flow.prepareNew(1082, anchor.id), process);
  const number = flow.start(process); assert.equal(number, 90012); assert.equal(flow.start(process), number);
  const slots = maintenanceContractPersistentSlots(1082);
  assert.deepEqual(slots.map(slot => slot.memberId).filter(Boolean), [1082]);
  assert.throws(() => flow.finalize(process), /missing signature/);
  flow.sign(process, slots.map(slot => slot.key));
  const assignmentId = flow.finalize(process); const documentId = process.documentId;
  assert.equal(flow.finalize(process), assignmentId); assert.equal(process.documentId, documentId);
  assert.equal(flow.assignments.length, 1); assert.equal(anchor.documentComplete, true);
});

test("D/E: normal extension keeps contract and 90xxx and reuses its notice on retry", () => {
  const flow = new MaintenanceHarness();
  flow.contracts.push({ id: 10, number: 90017, active: true });
  flow.assignments.push({ id: 20, contractId: 10, partyId: 1082, from: "2026-01-01", until: "2026-12-31", predecessorId: null, documentComplete: true });
  const next = flow.extend(20, "2027-12-31"); const notice = flow.completeNotice(next.id);
  assert.equal(next.contractId, 10); assert.equal(flow.contracts[0].number, 90017);
  assert.equal(flow.extend(20, "2027-12-31"), next);
  assert.equal(flow.completeNotice(next.id), notice); assert.equal(flow.contracts.length, 1);
  assert.equal(maintenanceContractPersistentSlots(next.partyId)[0].memberId, 1082);
});

test("F: non-extension ends at the existing date without a contract or number", () => {
  const flow = new MaintenanceHarness();
  flow.contracts.push({ id: 10, number: 90017, active: true });
  flow.assignments.push({ id: 20, contractId: 10, partyId: 1081, from: "2026-01-01", until: "2026-12-31", predecessorId: null, documentComplete: true });
  assert.equal(flow.assignments.filter(item => item.predecessorId === 20).length, 0);
  assert.equal(flow.contracts.length, 1); assert.equal(flow.contracts[0].number, 90017);
});

test("G/H/I: explicit material change gets a new 90xxx only at signing and leaves old contract intact until completion", () => {
  const flow = new MaintenanceHarness();
  flow.contracts.push({ id: 10, number: 90012, active: true });
  flow.nextNumber = 90013;
  flow.assignments.push({ id: 20, contractId: 10, partyId: 1081, from: "2026-01-01", until: null, predecessorId: null, documentComplete: true });
  const process = flow.prepareNew(1081);
  assert.equal(flow.contracts.at(-1)!.number, null); assert.equal(flow.assignments[0].until, null);
  assert.equal(flow.prepareNew(1081), process, "retry must reuse the prepared material change");
  const abandonedNumber = flow.start(process);
  assert.equal(abandonedNumber, 90013); assert.equal(flow.assignments[0].until, null);
  process.status = "abgebrochen";
  const abandonedContractId = process.contractId;
  assert.equal(flow.contracts.at(-1)!.number, abandonedNumber); assert.equal(flow.assignments[0].until, null); assert.equal(flow.assignments.length, 1);

  const successful = flow.prepareNew(1081);
  assert.notEqual(successful, process); assert.notEqual(successful.contractId, abandonedContractId);
  assert.equal(flow.contracts.at(-1)!.number, null);
  assert.equal(flow.start(successful), 90014);
  assert.equal(flow.contracts.find(item => item.id === abandonedContractId)?.number, abandonedNumber);
  flow.sign(successful, maintenanceContractPersistentSlots(1081).map(slot => slot.key));
  flow.finalize(successful, "2027-02-01");
  assert.equal(flow.assignments.length, 2); assert.equal(flow.assignments[0].until, "2026-12-31");
  assert.equal(flow.contracts.at(-1)!.active, true);
});

test("server and generator contracts preserve the actual party and completed-notice retry", () => {
  const migration = readFileSync(new URL("../../../supabase/migrations/20261028130000_phase10_wartungsvertrag_e2e_haertung.sql", import.meta.url), "utf8");
  const generator = readFileSync(new URL("../../../supabase/functions/kgv-generate-contract/index.ts", import.meta.url), "utf8");
  const service = readFileSync(new URL("./maintenance-continuation-service.ts", import.meta.url), "utf8");
  const maui = readFileSync(new URL("../../../KGV.Maui/Pages/WartungsvertragFortfuehrungPage.cs", import.meta.url), "utf8");
  assert.match(migration, /z\.vertragspartei_mitglied_id/);
  assert.match(migration, /if v_vorgang\.status='erledigt'/);
  assert.match(migration, /final_dokument_id is distinct from p_dokument_id/);
  assert.match(migration, /set status='abgebrochen'/);
  assert.match(generator, /\["vorbereitet", "erledigt"\]\.includes\(process\.status\)/);
  assert.match(service, /member_id: detail\.vertragspartei_mitglied_id/);
  assert.doesNotMatch(service, /member_id: detail\.hauptmitglied_id/);
  assert.match(maui, /_detail\.VertragsparteiMitgliedId/);
  assert.doesNotMatch(maui, /WiederholenAsync\(_detail\.HauptmitgliedId/);
});
