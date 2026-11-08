import assert from "node:assert/strict";
import { readdir, readFile } from "node:fs/promises";
import { test } from "node:test";

const root = new URL("../../", import.meta.url);
const read = (path) => readFile(new URL(path, root), "utf8");

test("AP-01 fixture catalog covers every required P11 scenario and its AP-00 rules", async () => {
  const [seed, matrix, config] = await Promise.all([
    read("supabase/seed.sql"),
    read("Documentation/P11_FACHREGELMATRIX_TESTKATALOG.md"),
    read("supabase/config.toml"),
  ]);

  assert.match(config, /sql_paths\s*=\s*\["\.\/seed\.sql"\]/);
  for (const fixtureId of [
    "haupt-nebenmitglied",
    "passive-mitgliedschaft",
    "pachtwechsel",
    "nachsorge",
    "zaehlerwechsel",
    "leerstand",
    "vereinsgarten",
    "individuelle-rechte",
  ]) assert.match(seed, new RegExp(`'${fixtureId}'`));

  for (const ruleId of ["P11-R01", "P11-R02", "P11-R06", "P11-R07", "P11-R08", "P11-R09", "P11-R10", "P11-R14", "P11-R15"]) {
    assert.match(seed, new RegExp(ruleId));
    assert.match(matrix, new RegExp(ruleId));
  }
});

test("AP-01 fixture catalog is local-only and does not introduce a production migration", async () => {
  const [seed, migrations] = await Promise.all([
    read("supabase/seed.sql"),
    readdir(new URL("supabase/migrations/", root)),
  ]);
  assert.match(seed, /local-only fixture catalog/i);
  assert.ok(migrations.every((file) => !/p11.*ap01/i.test(file)));
});
