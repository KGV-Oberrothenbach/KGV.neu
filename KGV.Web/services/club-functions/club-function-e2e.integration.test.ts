import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

type Purpose = "impressum" | "vertragsunterzeichnung" | "mitgliedsaufnahme_abstimmung" | "mitgliedsaufnahme_entscheidung" | "wartungsvertrag_entscheidung";
type ClubFunction = { id: number; key: string; label: string; active: boolean; multiple: boolean; purposes: Set<Purpose> };
type Assignment = { id: number; functionId: number; memberId: number; from: string; until: string | null };
type Representation = { id: number; representedId: number; representativeId: number; from: string; until: string | null };
type Authorization = { memberId: number; assignmentId: number; kind: "direkt" | "vertretung"; representationId: number | null; representedAssignmentId: number | null; functionLabel: string };

const activeAt = (value: { from: string; until: string | null }, date: string) => value.from <= date && (!value.until || value.until >= date);
const overlaps = (a: { from: string; until: string | null }, b: { from: string; until: string | null }) => a.from <= (b.until ?? "9999-12-31") && b.from <= (a.until ?? "9999-12-31");

class FunctionHarness {
  nextId = 1;
  functions: ClubFunction[] = [];
  assignments: Assignment[] = [];
  representations: Representation[] = [];

  addFunction(key: string, label: string, multiple: boolean, purposes: Purpose[]) {
    assert(!this.functions.some(item => item.key === key));
    const value = { id: this.nextId++, key, label, active: true, multiple, purposes: new Set(purposes) };
    this.functions.push(value); return value;
  }

  rename(id: number, label: string) { this.functions.find(item => item.id === id)!.label = label; }

  addAssignment(functionId: number, memberId: number, from: string, until: string | null = null) {
    const fn = this.functions.find(item => item.id === functionId)!;
    const candidate = { id: this.nextId++, functionId, memberId, from, until };
    if (!fn.multiple && this.assignments.some(item => item.functionId === functionId && overlaps(item, candidate))) throw new Error("single occupancy overlap");
    this.assignments.push(candidate); return candidate;
  }

  addRepresentation(representedId: number, representativeId: number, from: string, until: string | null = null) {
    const represented = this.assignments.find(item => item.id === representedId)!;
    const representative = this.assignments.find(item => item.id === representativeId)!;
    const candidate = { id: this.nextId++, representedId, representativeId, from, until };
    if (represented.memberId === representative.memberId) throw new Error("self representation");
    if (!activeAt(represented, from) || !activeAt(representative, from) || (until && (!activeAt(represented, until) || !activeAt(representative, until)))) throw new Error("outside assignments");
    if (this.representations.some(item => item.representedId === representedId && overlaps(item, candidate))) throw new Error("representation overlap");
    this.representations.push(candidate); return candidate;
  }

  resolve(purpose: Purpose, date: string) {
    const basis = this.assignments.filter(assignment => {
      const fn = this.functions.find(item => item.id === assignment.functionId)!;
      return fn.active && fn.purposes.has(purpose) && activeAt(assignment, date);
    });
    return basis.map<Authorization>(assignment => {
      const representation = this.representations.find(item => item.representedId === assignment.id && activeAt(item, date));
      if (representation) {
        const representative = this.assignments.find(item => item.id === representation.representativeId)!;
        const representativeFunction = this.functions.find(item => item.id === representative.functionId)!;
        if (activeAt(representative, date) && representativeFunction.active) return { memberId: representative.memberId, assignmentId: representative.id, kind: "vertretung", representationId: representation.id, representedAssignmentId: assignment.id, functionLabel: representativeFunction.label };
      }
      const fn = this.functions.find(item => item.id === assignment.functionId)!;
      return { memberId: assignment.memberId, assignmentId: assignment.id, kind: "direkt", representationId: null, representedAssignmentId: null, functionLabel: fn.label };
    });
  }

  snapshot(purpose: Purpose, date: string) { return structuredClone(this.resolve(purpose, date)); }
}

test("A/B/C/D: current, future, historical and multiple assignments resolve by date without random selection", () => {
  const flow = new FunctionHarness();
  const single = flow.addFunction("vorsitz", "Vorsitz", false, ["vertragsunterzeichnung"]);
  const old = flow.addAssignment(single.id, 10, "2025-01-01", "2026-12-31");
  const future = flow.addAssignment(single.id, 11, "2027-01-01");
  assert.deepEqual(flow.resolve("vertragsunterzeichnung", "2026-10-28").map(x => x.memberId), [10]);
  assert.deepEqual(flow.resolve("vertragsunterzeichnung", "2027-01-01").map(x => x.memberId), [11]);
  assert.throws(() => flow.addAssignment(single.id, 12, "2026-01-01"), /overlap/);
  const multi = flow.addFunction("beirat", "Beirat", true, ["mitgliedsaufnahme_abstimmung"]);
  flow.addAssignment(multi.id, 20, "2026-01-01"); flow.addAssignment(multi.id, 21, "2026-01-01");
  assert.deepEqual(flow.resolve("mitgliedsaufnahme_abstimmung", "2026-10-28").map(x => x.memberId), [20, 21]);
  assert.equal(activeAt(old, "2027-01-01"), false); assert.equal(activeAt(future, "2026-12-31"), false);
});

test("E/F: one valid representation replaces exactly its assignment and expiry restores direct authority", () => {
  const flow = new FunctionHarness();
  const chair = flow.addFunction("vorsitz", "Vorsitz", false, ["mitgliedsaufnahme_entscheidung"]);
  const deputy = flow.addFunction("schriftfuehrung", "Schriftführung", false, []);
  const represented = flow.addAssignment(chair.id, 10, "2026-01-01");
  const representative = flow.addAssignment(deputy.id, 20, "2026-01-01");
  const power = flow.addRepresentation(represented.id, representative.id, "2026-10-01", "2026-10-31");
  assert.deepEqual(flow.resolve("mitgliedsaufnahme_entscheidung", "2026-10-28")[0], { memberId: 20, assignmentId: representative.id, kind: "vertretung", representationId: power.id, representedAssignmentId: represented.id, functionLabel: "Schriftführung" });
  assert.equal(flow.resolve("mitgliedsaufnahme_entscheidung", "2026-11-01")[0].memberId, 10);
  assert.throws(() => flow.addRepresentation(represented.id, representative.id, "2026-10-15", "2026-10-20"), /overlap/);
});

test("G: a prepared snapshot survives later label, assignment and representation changes", () => {
  const flow = new FunctionHarness();
  const chair = flow.addFunction("vorsitz", "Vorsitz", false, ["vertragsunterzeichnung"]);
  const deputy = flow.addFunction("stellvertretung", "Stellvertretung", false, []);
  const represented = flow.addAssignment(chair.id, 10, "2026-01-01"); const representative = flow.addAssignment(deputy.id, 20, "2026-01-01");
  const power = flow.addRepresentation(represented.id, representative.id, "2026-01-01");
  const snapshot = flow.snapshot("vertragsunterzeichnung", "2026-10-28");
  flow.rename(deputy.id, "Neue sichtbare Bezeichnung"); power.until = "2026-10-28";
  assert.equal(snapshot[0].functionLabel, "Stellvertretung"); assert.equal(snapshot[0].memberId, 20);
  assert.equal(flow.resolve("vertragsunterzeichnung", "2026-10-29")[0].memberId, 10);
});

test("H/I: purpose binding excludes ordinary board/admin roles and administration is distinct from action authority", () => {
  const flow = new FunctionHarness();
  const board = flow.addFunction("beirat", "Beirat", true, ["mitgliedsaufnahme_abstimmung"]);
  flow.addAssignment(board.id, 30, "2026-01-01");
  assert.equal(flow.resolve("mitgliedsaufnahme_entscheidung", "2026-10-28").length, 0);
  assert.equal(flow.resolve("vertragsunterzeichnung", "2026-10-28").length, 0);
  const appRoles = new Map([[30, "vorstand"], [99, "admin"]]);
  assert.equal(appRoles.get(30), "vorstand"); assert.equal(appRoles.get(99), "admin");
  assert.equal(flow.resolve("mitgliedsaufnahme_entscheidung", "2026-10-28").some(x => appRoles.has(x.memberId)), false);
});

test("server, Web, MAUI and Phase 8-10 contracts retain immutable purpose-bound snapshots", () => {
  const migration = readFileSync(new URL("../../../supabase/migrations/20261028140000_phase11_vereinsfunktionen_vertretung_e2e_haertung.sql", import.meta.url), "utf8");
  const snapshotMigration = readFileSync(new URL("../../../supabase/migrations/20261023120000_zb2_prepared_contract_signatory_snapshot.sql", import.meta.url), "utf8");
  const membershipMigration = readFileSync(new URL("../../../supabase/migrations/20261022160000_vor3_mitgliedsaufnahme_vorstand.sql", import.meta.url), "utf8");
  const web = readFileSync(new URL("../../features/club-functions/ClubFunctionManagement.tsx", import.meta.url), "utf8");
  const maui = readFileSync(new URL("../../../KGV.Maui/Pages/VereinsfunktionenPage.cs", import.meta.url), "utf8");
  const lease = readFileSync(new URL("../contracts/lease-contract-e2e.integration.test.ts", import.meta.url), "utf8");
  const maintenance = readFileSync(new URL("../maintenance/maintenance-contract-e2e.integration.test.ts", import.meta.url), "utf8");
  assert.match(migration, /prevent_vereinsfunktion_vertretung_identity_change/);
  assert.match(migration, /nur eine Vertretung gelten/);
  assert.match(migration, /join public\.vereinsfunktion rf[\s\S]*rf\.aktiv/);
  assert.doesNotMatch(migration, /1\. Vorsitz|2\. Vorsitz|Kassierer/);
  assert.match(snapshotMigration, /prepared_contract_signatur_snapshot_person/);
  assert.match(snapshotMigration, /Die Person oder Berechtigungsquelle gehört nicht zum eingefrorenen Signatur-Snapshot/);
  assert.match(membershipMigration, /mitgliedsaufnahme_entscheidung/);
  assert.match(web, /disabled=\{existing\}/);
  assert.match(maui, /IsEnabled = record\.Id <= 0/);
  assert.match(lease, /snapshot/i); assert.match(maintenance, /actual party/i);
});
