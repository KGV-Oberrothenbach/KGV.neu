import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const source = await readFile(new URL("./index.ts", import.meta.url), "utf8");
const migration = await readFile(new URL("../../migrations/20261026090000_nb2a_d3_wartungsvertrag_mitgliedsnummer.sql", import.meta.url), "utf8");

test("wartungsvertrag renders the stored membership number and keeps technical ids internal", () => {
  const branch = /else if \(type === "wartungsvertrag"\) \{([\s\S]*?)\n    \} else \{ pdf = await buildSimpleContract/.exec(source)?.[1];
  assert.ok(branch, "wartungsvertrag branch is missing");
  assert.match(branch, /Number\(member\.mitgliedsnummer\)/);
  assert.match(branch, /mitgliedsnummer: String\(membershipNumber\)/);
  assert.doesNotMatch(branch, /mitgliedsnummer: String\(member\.id\)/);
  assert.match(branch, /assignment\.wartungsvertrag_id/);
  assert.doesNotMatch(branch, /\+ 1000|1000 \+/);
});

test("extension snapshots store the canonical number and retain continuation semantics", () => {
  assert.match(migration, /'mitgliedsnummer',v_member\.mitgliedsnummer/);
  assert.doesNotMatch(migration, /'mitgliedsnummer',v_member\.id/);
  assert.match(migration, /v_member\.mitgliedsnummer is null or v_member\.mitgliedsnummer <= 0/);
  assert.match(migration, /values\(v_alt\.wartungsvertrag_id,/);
  assert.doesNotMatch(migration, /\+ 1000|1000 \+/);
});

test("extension rendering safely corrects legacy draft values from the loaded member record", () => {
  const branch = /else if \(type === "wartungsvertrag_verlaengerung"\) \{([\s\S]*?)\n    \} else if \(type === "wartungsvertrag"\)/.exec(source)?.[1];
  assert.ok(branch, "wartungsvertrag extension branch is missing");
  assert.match(branch, /Number\(member\.mitgliedsnummer\)/);
  assert.match(branch, /mitgliedsnummer: String\(membershipNumber\)/);
  assert.doesNotMatch(branch, /mitgliedsnummer: safe\(draft\.mitgliedsnummer\)/);
});
