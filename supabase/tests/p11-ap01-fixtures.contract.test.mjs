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

test("export metadata foundation precedes its first historical consumer", async () => {
  const foundation = await read("supabase/migrations/20260923210000_export_metadata_foundation.sql");
  for (const table of ["app_export_definition", "app_export_filter_definition", "app_export_column_definition"]) {
    assert.match(foundation, new RegExp(`create table if not exists public\\.${table}`));
    assert.match(foundation, new RegExp(`alter table public\\.${table} enable row level security`));
  }
  assert.match(foundation, /trg_app_export_definition_updated_at/);
  assert.match(foundation, /references public\.app_export_definition\(export_key\) on delete cascade/g);
});

test("season finance foundation precedes the historical-season copy", async () => {
  const foundation = await read("supabase/migrations/20260924220000_saison_finance_foundation.sql");
  for (const column of ["pacht_pro_qm", "mitgliedsbeitrag", "mitgliedsbeitrag_nebenmitglied", "aufnahmegebuehr", "gebuehr_bauantrag", "is_demo"]) {
    assert.match(foundation, new RegExp(`add column if not exists ${column}`));
  }
  assert.match(foundation, /alter column pacht_pro_qm set not null/);
});

test("local template replay fixture covers every historically guarded storage object", async () => {
  const fixture = await read("supabase/migrations/20261017124000_local_template_storage_replay_fixture.sql");
  assert.match(fixture, /insert into storage\.objects/);
  assert.match(fixture, /where not exists \(/);
  for (const path of [
    "templates/export-zaehler-eichfaelligkeit-hoch/v1/template.pdf",
    "templates/briefbogen-allgemein/v1/template.pdf",
    "templates/pachtvertrag/v9/field-spec.json",
    "templates/export-allgemein/v3/field-spec.json",
  ]) assert.match(fixture, new RegExp(path.replaceAll(".", "\\.")));
});
