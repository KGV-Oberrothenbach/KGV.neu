import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import test from "node:test";

const root = resolve(import.meta.dirname, "../../..");
const migration = readFileSync(resolve(root, "supabase/migrations/20261031100000_serienbrief_foundation.sql"), "utf8");
const coreExports = readFileSync(resolve(root, "KGV.Core/Models/AppExportModels.cs"), "utf8");
const coreTemplates = readFileSync(resolve(root, "KGV.Core/Models/SerienbriefVorlageModels.cs"), "utf8");
const definitionModel = readFileSync(resolve(root, "KGV.Web/models/export/export-definition.ts"), "utf8");
const columnModel = readFileSync(resolve(root, "KGV.Web/models/export/export-column.ts"), "utf8");
const repository = readFileSync(resolve(root, "KGV.Web/repositories/export/export-definition-repository.ts"), "utf8");
const restClient = readFileSync(resolve(root, "KGV.Web/lib/supabase/rest-client.ts"), "utf8");
const meterMigration = readFileSync(resolve(root, "supabase/migrations/20261031110000_serienbrief_zaehler_eichfaelligkeit.sql"), "utf8");

test("G2 defines an opt-in serial-letter export contract with stable technical IDs", () => {
  assert.match(migration, /serienbrief_erlaubt boolean not null default false/i);
  assert.match(migration, /serienbrief_mitglied_id_feld text/i);
  assert.match(migration, /serienbrief_parzelle_id_feld text/i);
  assert.match(migration, /not serienbrief_erlaubt[\s\S]*serienbrief_mitglied_id_feld/i);
  assert.doesNotMatch(migration, /update public\.app_export_definition\s+set serienbrief_erlaubt\s*=\s*true/i);
  assert.match(coreExports, /\[Column\("serienbrief_mitglied_id_feld"\)\][\s\S]*SerienbriefMitgliedIdFeld/);
  assert.match(definitionModel, /serienbrief_mitglied_id_feld: string \| null/);
  assert.match(repository, /definitionCoreSelect/);
  assert.match(repository, /definitionSerialLetterSelect/);
  assert.match(repository, /withOptionalDefinitionSerialLetterMetadata/);
  assert.match(repository, /optionalMetadata\(\(\) => readSupabaseRest<DefinitionSerialLetterMetadata>/);
});

test("G2 exposes only explicitly approved non-technical fields to future editors", () => {
  assert.match(migration, /serienbrief_freigegeben boolean not null default false/i);
  assert.match(migration, /serienbrief_gruppe text/i);
  assert.match(migration, /column_key not in \('mitglied_id', 'parzelle_id'\)/i);
  assert.match(coreExports, /\[Column\("serienbrief_freigegeben"\)\][\s\S]*SerienbriefFreigegeben/);
  assert.match(columnModel, /serienbrief_freigegeben: boolean/);
  assert.match(repository, /columnCoreSelect/);
  assert.match(repository, /columnSerialLetterSelect/);
  assert.match(repository, /withOptionalColumnSerialLetterMetadata/);
  assert.match(repository, /optionalMetadata\(\(\) => readSupabaseRest<ColumnSerialLetterMetadata>/);
});

test("keeps read errors user-friendly while retaining PostgREST diagnostics", () => {
  assert.match(restClient, /class SupabaseRestError/);
  assert.match(restClient, /technicalMessage/);
  assert.match(restClient, /await readError\(response\)/);
  assert.match(restClient, /Die Vereinsdaten konnten nicht geladen werden/);
});

test("G2 stores export-independent templates and typed free-field definitions only", () => {
  for (const table of ["serienbrief_vorlage", "serienbrief_vorlage_exportfeld", "serienbrief_vorlage_freifeld"]) assert.match(migration, new RegExp(`create table if not exists public\\.${table}`, "i"));
  assert.match(migration, /typ text not null check \(typ in \('text', 'date', 'number'\)\)/i);
  assert.match(migration, /anrede_modus text not null default 'briefanrede'/i);
  assert.match(migration, /create policy serienbrief_vorlage_admin_vorstand_all/i);
  assert.match(coreTemplates, /class SerienbriefVorlageRecord/);
  assert.match(coreTemplates, /class SerienbriefVorlageFreifeldRecord/);
  assert.doesNotMatch(migration, /create table[^;]*serienbrief_(lauf|empfaenger|zustellung)/i);
});

test("G3 activates only the calibration export with explicit recipient priority", () => {
  assert.match(meterMigration, /serienbrief_erlaubt=true/);
  assert.match(meterMigration, /serienbrief_mitglied_id_feld='mitglied_id'/);
  assert.match(meterMigration, /hauptpaechter/);
  assert.match(meterMigration, /legacy_eindeutig/);
  assert.match(meterMigration, /mehrdeutig/);
  assert.match(meterMigration, /not coalesce\(current_lease\.hat_aktive_beteiligung,false\).*legacy\.anzahl=1/s);
});
