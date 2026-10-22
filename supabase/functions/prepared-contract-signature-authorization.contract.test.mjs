import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const migration = await readFile(new URL("../migrations/20261021160000_prepared_contract_signatures.sql", import.meta.url), "utf8");

test("contract signers are resolved from the configured purpose and valid representations", () => {
  assert.match(migration, /v\.verwendungszweck = 'vertragsunterzeichnung'/);
  assert.match(migration, /vertretung\.gueltig_ab <= p_stichtag/);
  assert.match(migration, /vertretung\.gueltig_bis is null or vertretung\.gueltig_bis >= p_stichtag/);
  assert.doesNotMatch(migration, /schluessel\s*=\s*'vorstandsvorsitzender'|schluessel\s*=\s*'kassenwart'|schluessel\s*=\s*'schriftfuehrer'/);
});

test("two board signatures cannot reuse a natural person or represented position", () => {
  assert.match(migration, /Vorstand 1 und Vorstand 2 müssen unterschiedliche natürliche Personen sein/);
  assert.match(migration, /Dieselbe vertretene Zeichnungsposition darf nicht beide Vorstandssignaturen abdecken/);
});
