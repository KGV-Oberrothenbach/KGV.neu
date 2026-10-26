import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const source = await readFile(new URL("./index.ts", import.meta.url), "utf8");
const spec = JSON.parse(await readFile(new URL("../../../KGV.Core/Templates/pachtvertrag/v8/field-spec.json", import.meta.url), "utf8"));
const migration = await readFile(new URL("../../migrations/20261026130000_nb2a_d4_publish_pachtvertrag_v8.sql", import.meta.url), "utf8");

test("lease contract uses stored membership numbers for both contract parties", () => {
  const branch = /else if \(type === "pachtvertrag"\) \{([\s\S]*?)\n    \} else if \(type === "wartungsvertrag_verlaengerung"\)/.exec(source)?.[1];
  assert.ok(branch, "lease branch is missing");
  assert.match(branch, /membershipNumber\(member, true, "Pächter\/in 1"\)/);
  assert.match(branch, /representative \? "" : membershipNumber\(party2, Boolean\(party2\)/);
  assert.match(branch, /mitgliedsnummer: primaryMembershipNumber/);
  assert.match(branch, /zweite_partei_mitgliedsnummer: secondPartyMembershipNumber/);
  assert.doesNotMatch(branch, /mitgliedsnummer: String\(member\.id\)|zweite_partei_mitgliedsnummer: party2\?\.id/);
  assert.doesNotMatch(branch, /\+ 1000|1000 \+|Fachnummer\.Mitglied/);
});

test("v8 places membership numbers beside both party names without overwriting v7", () => {
  assert.equal(spec.field_spec_version, "pachtvertrag-field-spec/v8");
  assert.deepEqual(spec.fields.filter((field) => ["mitgliedsnummer", "zweite_partei_mitgliedsnummer"].includes(field.name)).map((field) => field.name), ["mitgliedsnummer", "zweite_partei_mitgliedsnummer"]);
  assert.match(source, /leaseFieldSpecV8/);
  assert.match(source, /leaseRequiredFieldsV8/);
  assert.match(migration, /'pachtvertrag', 'v8'/);
  assert.match(migration, /templates\/pachtvertrag\/v8\/template\.pdf/);
});

test("a non-member representative has no number while a linked member must have one", () => {
  assert.match(source, /if \(!person\) return ""/);
  assert.match(source, /if \(required\) throw new Error/);
  assert.doesNotMatch(source, /String\(person\.id\)/);
});
