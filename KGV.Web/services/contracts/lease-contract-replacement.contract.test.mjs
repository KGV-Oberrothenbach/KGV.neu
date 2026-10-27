import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const root = new URL("../../../", import.meta.url);
const read = (path) => readFile(new URL(path, root), "utf8");

test("20xxx is read from the communication reservation and rendered consistently", async () => {
  const [generator, spec, catalog] = await Promise.all([
    read("supabase/functions/kgv-generate-contract/index.ts"),
    read("KGV.Core/Templates/pachtvertrag/v9/field-spec.json"),
    read("supabase/migrations/20261027131000_nb2b_b3_publish_pachtvertrag_v9.sql"),
  ]);
  assert.match(spec, /"field_spec_version": "pachtvertrag-field-spec\/v9"/);
  assert.match(spec, /"name": "vertragsnummer"/);
  assert.match(generator, /loadExistingFachnummer\(auth, sourceKommunikationsvorgangId, type\)/);
  assert.match(generator, /values\.vertragsnummer = fachnummer \? `Vertrags-Nr\. \$\{fachnummer\}` : ""/);
  assert.match(generator, /Pachtvertrag\$\{fachnummer \? ` · Vertrags-Nr\. \$\{fachnummer\}` : ""\}/);
  assert.match(generator, /Vorhandenes finales Dokument wurde wiederverwendet/);
  assert.doesNotMatch(generator, /fachnummer\s*=\s*sourceKommunikationsvorgangId|fachnummer\s*=\s*documentId/);
  assert.match(catalog, /pachtvertrag-field-spec\/v9/);
  assert.match(catalog, /do not execute or upload\/activate without separate approval/);
});

test("replacement workflow binds the immediate predecessor and switches authority atomically", async () => {
  const [migration, action, repository, maui] = await Promise.all([
    read("supabase/migrations/20261027130000_nb2b_b3_pachtvertrag_ersatzketten.sql"),
    read("KGV.Web/features/contracts/LeaseContractAction.tsx"),
    read("KGV.Web/repositories/contracts/lease-relationship-repository.ts"),
    read("KGV.Maui/Pages/MemberParzellenDetailPage.cs"),
  ]);
  assert.match(migration, /ersetzt_pachtvertragsfassung_id bigint/);
  assert.match(migration, /v_fassung\.nicht_mehr_massgeblich_am is not null/);
  assert.match(migration, /'vertrag_signatur', 'pachtvertrag', 'vorbereitet'/);
  assert.match(migration, /v_quelle\.entwurf_daten/);
  assert.match(migration, /complete_prepared_pachtvertrag_vor_b3/);
  assert.match(migration, /f\.vorgaenger_fassung_id = v_vorgang\.ersetzt_pachtvertragsfassung_id/);
  assert.match(migration, /set nicht_mehr_massgeblich_am = coalesce/);
  assert.match(migration, /v_pachtvertragsfassung_uebersicht/);
  assert.match(migration, /fr\.quelle_art = 'kommunikationsvorgang'/);
  assert.match(action, /Korrektur-\/Ersatzvertrag erstellen/);
  assert.match(action, /preparedContractId=\{preparedContractId\}/);
  assert.match(action, /Nicht mehr maßgeblich/);
  assert.match(repository, /prepare_pachtvertrag_ersatz/);
  assert.match(maui, /PreparePachtvertragErsatzAsync/);
  assert.doesNotMatch(action, /signExisting|sign-existing/);
});
