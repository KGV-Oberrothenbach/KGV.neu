import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const migration = readFileSync(new URL("../../supabase/migrations/20261028150000_phase12_workflow_idempotenz_haertung.sql", import.meta.url), "utf8");
const signatureUpload = readFileSync(new URL("../../supabase/functions/upload-prepared-contract-signature/index.ts", import.meta.url), "utf8");
const dispatch = readFileSync(new URL("../../supabase/functions/kgv-send-contract-documents/index.ts", import.meta.url), "utf8");
const generator = readFileSync(new URL("../../supabase/functions/kgv-generate-contract/index.ts", import.meta.url), "utf8");
const documentUpload = readFileSync(new URL("../../supabase/functions/kgv-upload-document/index.ts", import.meta.url), "utf8");
const provenance = readFileSync(new URL("../../supabase/migrations/20261019100000_prepared_contract_final_document_provenance.sql", import.meta.url), "utf8");
const numbers = readFileSync(new URL("../../supabase/migrations/20261025130000_nb1_fachnummern_fundament.sql", import.meta.url), "utf8");
const maintenanceNumbers = readFileSync(new URL("../../supabase/migrations/20261027150000_nb2b_b4_wartungsvertragsnummern.sql", import.meta.url), "utf8");
const phase10 = readFileSync(new URL("../../supabase/migrations/20261028130000_phase10_wartungsvertrag_e2e_haertung.sql", import.meta.url), "utf8");
const phase11 = readFileSync(new URL("../../supabase/migrations/20261028140000_phase11_vereinsfunktionen_vertretung_e2e_haertung.sql", import.meta.url), "utf8");

test("A/B/D/H: number and signature-start retries reuse one revision and frozen snapshot", () => {
  assert.match(numbers, /unique\(is_demo, quelle_art, quelle_id\)/);
  assert.match(numbers, /reserve_fachnummer/);
  assert.match(numbers, /when 'mitgliedsantrag' then 'mitgliedsantrag'/);
  assert.match(numbers, /when 'pachtvertrag' then 'pachtvertrag'/);
  assert.match(maintenanceNumbers, /quelle_art = 'wartungsvertrag'/);
  assert.match(migration, /v\.status in \('zur_signatur','letzte_pruefung','erledigt'\)/);
  assert.match(migration, /v\.signatur_revision=p_expected_revision/);
  assert.match(migration, /prepared_contract_signatur_snapshot_person/);
  assert.match(migration, /prepared_contract_signatur p[\s\S]*p\.signatur_revision=p_expected_revision/);
  assert.match(phase11, /prevent_vereinsfunktion_vertretung_identity_change/);
});

test("signature image retry is content-addressed while a different binding is rejected", () => {
  assert.match(signatureUpload, /stableObjectId\(hash\)/);
  assert.match(signatureUpload, /upsert: true/);
  assert.match(signatureUpload, /already_stored: true/);
  assert.match(signatureUpload, /bereits abweichend unterschrieben/);
  assert.match(migration, /v_position\.storage_path=p_storage_path/);
  assert.match(migration, /v_position\.signatur_sha256=p_signatur_sha256/);
  assert.match(migration, /Berechtigungsquelle gehört nicht zum eingefrorenen Signatur-Snapshot/);
});

test("C/F: final document and completion paths keep their stable source keys", () => {
  assert.match(provenance, /create unique index if not exists ux_dokument_source_kommunikationsvorgang/);
  assert.match(provenance, /source_kommunikationsvorgang_id, source_revision/);
  assert.match(generator, /form\.set\("source_kommunikationsvorgang_id"/);
  assert.doesNotMatch(generator, /from\("dokument"\)\.update\(\{ source_kommunikationsvorgang_id/);
  assert.match(generator, /if \(existing\) return existing\.id/);
  assert.match(documentUpload, /source_kommunikationsvorgang_id: sourceProcessId/);
  assert.match(documentUpload, /Dokument-Provenienz passt nicht zum Owner oder Vertragsstand/);
  assert.match(phase10, /if v_vorgang\.status='erledigt'/);
  assert.match(phase10, /final_dokument_id is distinct from p_dokument_id/);
  assert.match(migration, /b\.entscheidung<>'offen'[\s\S]*then return/);
  assert.match(migration, /Bearbeitung wurde bereits abweichend entschieden/);
});

test("E/G: replacement, initial dispatch and interrupted claims are serialized and resumed", () => {
  assert.match(migration, /from public\.pachtvertragsfassung where id=p_vorgaenger_fassung_id for update/);
  assert.match(migration, /status not in \('erledigt','abgebrochen'\) for update/);
  assert.match(migration, /erneuter_versand_von_id is null/);
  assert.match(migration, /erneuter_versand_von_id=p_erneuter_versand_von_id and z\.status<>'versendet'/);
  assert.match(migration, /status='wird_versendet' and beansprucht_am<now\(\)-interval '15 minutes'/);
  assert.match(dispatch, /stableMessageId/);
  assert.match(dispatch, /eq\("status", "wird_versendet"\)/);
});

test("authorization is rechecked before retry actions and snapshots are not recalculated", () => {
  assert.match(migration, /not public\.is_admin_or_vorstand\(\)/);
  assert.match(migration, /Keine aktuelle Entscheidungsberechtigung/);
  assert.match(migration, /Nur Admin oder Vorstand dürfen einen Ersatzvertrag vorbereiten/);
  assert.match(migration, /not public\.kann_vertragsversand_ausfuehren\(\)/);
  assert.match(migration, /if v\.status in \('zur_signatur','letzte_pruefung','erledigt'\)[\s\S]*return v/);
});
