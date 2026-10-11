import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { filterParcelAssignmentReadings } from "./parcel-reading-history.ts";

const meters = [
  { id: 10, parzelle_id: 5 },
  { id: 11, parzelle_id: 5 },
  { id: 12, parzelle_id: 6 },
];

const readings = [
  { id: 1, zaehler_id: 10, ablesedatum: "2025-04-30T12:00:00", stand: 80 },
  { id: 2, zaehler_id: 10, ablesedatum: "2025-05-01T12:00:00", stand: 100 },
  { id: 3, zaehler_id: 11, ablesedatum: "2025-06-01T12:00:00", stand: 50 },
  { id: 4, zaehler_id: 10, ablesedatum: "2025-08-31T12:00:00", stand: 150 },
  { id: 5, zaehler_id: 10, ablesedatum: "2025-09-01T12:00:00", stand: 160 },
  { id: 6, zaehler_id: 12, ablesedatum: "2025-06-15T12:00:00", stand: 200 },
];

test("active assignment includes readings from von_datum without an upper bound", () => {
  const result = filterParcelAssignmentReadings(
    { parzelle_id: 5, von_datum: "2025-05-01", bis_datum: null },
    meters,
    readings,
  );

  assert.deepEqual(result.map(({ reading }) => reading.id), [5, 4, 3, 2]);
});

test("historical assignment includes both date boundaries and excludes later tenants", () => {
  const result = filterParcelAssignmentReadings(
    { parzelle_id: 5, von_datum: "2025-05-01", bis_datum: "2025-08-31" },
    meters,
    readings,
  );

  assert.deepEqual(result.map(({ reading }) => reading.id), [4, 3, 2]);
});

test("the exact assignment window distinguishes two tenants of the same parcel", () => {
  const predecessor = filterParcelAssignmentReadings(
    { parzelle_id: 5, von_datum: "2025-04-01", bis_datum: "2025-04-30" },
    meters,
    readings,
  );
  const selectedTenant = filterParcelAssignmentReadings(
    { parzelle_id: 5, von_datum: "2025-05-01", bis_datum: "2025-08-31" },
    meters,
    readings,
  );

  assert.deepEqual(predecessor.map(({ reading }) => reading.id), [1]);
  assert.deepEqual(selectedTenant.map(({ reading }) => reading.id), [4, 3, 2]);
});

test("history includes readings from multiple meters of the selected parcel only", () => {
  const result = filterParcelAssignmentReadings(
    { parzelle_id: 5, von_datum: "2025-05-01", bis_datum: null },
    meters,
    readings,
  );

  assert.deepEqual(new Set(result.map(({ meter }) => meter.id)), new Set([10, 11]));
  assert.ok(result.every(({ meter }) => meter.parzelle_id === 5));
});

test("empty meter history stays empty and does not synthesize a lease-start reading", () => {
  const result = filterParcelAssignmentReadings(
    { parzelle_id: 5, von_datum: "2025-05-01", bis_datum: null },
    meters,
    [],
  );

  assert.deepEqual(result, []);
});

test("a reading before the assignment start cannot become its first displayed reading", () => {
  const result = filterParcelAssignmentReadings(
    { parzelle_id: 5, von_datum: "2025-05-01", bis_datum: null },
    meters,
    [readings[0]],
  );

  assert.deepEqual(result, []);
});

test("member-garden reading action keeps the member workspace active", async () => {
  const page = await readFile(new URL("../../app/page.tsx", import.meta.url), "utf8");
  const memberGardenRoute = page.split(/\r?\n/).find((line) => line.includes('activeId === "mitglied-gaerten"'));

  assert.ok(memberGardenRoute, "member-gardens workspace route should exist");
  assert.doesNotMatch(memberGardenRoute, /setActiveId\(["']ablesen["']\)/);
});
