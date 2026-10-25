import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";

const migration = await readFile(new URL("../../supabase/migrations/20261023110000_wv3_wartungsvertrag_folgeentscheidungen.sql", import.meta.url), "utf8");
const correction = await readFile(new URL("../../supabase/migrations/20261023170000_wv6_echte_verlaengerung.sql", import.meta.url), "utf8");
const generator = await readFile(new URL("../../supabase/functions/kgv-generate-contract/index.ts", import.meta.url), "utf8");
const spec = JSON.parse(await readFile(new URL("../../KGV.Core/Templates/wartungsvertrag-verlaengerung/v1/field-spec.json", import.meta.url), "utf8"));

test("WV continuation has an explicit, single, chronologically constrained predecessor relation", () => {
  assert.match(migration, /vorgaenger_zuordnung_id bigint null references public\.wartungsvertrag_zuordnungen/);
  assert.match(migration, /ck_wvz_vorgaenger_not_self/);
  assert.match(migration, /ux_wvz_vorgaenger_eindeutig/);
  assert.match(migration, /new\.gueltig_ab <> v_vorgaenger\.gueltig_bis \+ 1/);
  assert.match(migration, /new\.hauptmitglied_id is distinct from v_vorgaenger\.hauptmitglied_id/);
});

test("WV decisions distinguish continuation and non-continuation and keep withdrawal audit", () => {
  assert.match(migration, /create table if not exists public\.wartungsvertrag_folgeentscheidung/);
  assert.match(migration, /entscheidung in \('verlaengern','nicht_verlaengern'\)/);
  assert.match(migration, /ux_wvfe_wirksam_je_vorgaenger/);
  assert.match(migration, /zurueckgenommen_am/);
  assert.match(migration, /current_date>=v_alt\.gueltig_bis/);
});

test("WV RPCs use the decision purpose, do not accept a client start date, and are idempotent", () => {
  assert.match(migration, /ist_wartungsvertrag_entscheidung_berechtigt/);
  assert.match(correction, /verlaengere_wartungsvertrag\(\s*p_vorgaenger_zuordnung_id bigint,\s*p_neues_gueltig_bis date/s);
  assert.doesNotMatch(correction, /p_wartungsvertrag_id/);
  assert.doesNotMatch(correction, /p_neues_gueltig_ab/);
  assert.match(correction, /v_alt\.gueltig_bis\+1/);
  assert.match(correction, /if v_dec\.entscheidung='verlaengern'/);
  assert.match(correction, /v_neu\.wartungsvertrag_id=v_alt\.wartungsvertrag_id/);
  assert.match(migration, /entscheide_wartungsvertrag_nichtverlaengerung/);
  assert.match(migration, /nehme_wartungsvertrag_nichtverlaengerung_zurueck/);
});

test("WV continuation uses its workflow art and cannot enter the prepared-signature flow", () => {
  assert.match(migration, /workflow_art text not null default 'erstvertrag'/);
  assert.match(migration, /'verlaengerungsmitteilung'/);
  assert.match(migration, /Eine Verlängerungsmitteilung verwendet keinen Prepared-Signaturvorgang/);
  assert.match(migration, /vorgangstyp='dokument_erzeugung'/);
  assert.match(migration, /dokumenttyp='wartungsvertrag_verlaengerung'/);
});

test("WV extension template and generator intentionally have no signature fields", () => {
  assert.equal(spec.field_spec_version, "wartungsvertrag-verlaengerung-field-spec/v1");
  assert.ok(spec.fields.some((field) => field.name === "verlaengerungsentscheidung_datum"));
  assert.ok(spec.fields.every((field) => field.type !== "signature"));
  assert.match(generator, /maintenanceExtensionTemplate/);
  assert.match(generator, /type === "wartungsvertrag_verlaengerung"/);
  assert.match(generator, /drawPdfTemplate\(template\.pdfBytes, template\.fields, values, \{\}, "none"\)/);
  assert.match(generator, /complete_wartungsvertrag_verlaengerungsmitteilung/);
  assert.match(generator, /ist_wartungsvertrag_entscheidung_berechtigt/);
});
