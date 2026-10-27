import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const root = new URL("../../../", import.meta.url);
const read = (path) => readFile(new URL(path, root), "utf8");
const [migration, catalogMigration, continuationMigration, generator, webFlow, materialChange, continuation, maui, memberList, assignmentService, assignmentPanel, contractTemplate, extensionTemplate] = await Promise.all([
  read("supabase/migrations/20261027150000_nb2b_b4_wartungsvertragsnummern.sql"),
  read("supabase/migrations/20261027160000_nb2b_b4_wartungsvertrag_template_v2.sql"),
  read("supabase/migrations/20261023170000_wv6_echte_verlaengerung.sql"),
  read("supabase/functions/kgv-generate-contract/index.ts"),
  read("KGV.Web/features/maintenance/MaintenanceContractSignatureFlow.tsx"),
  read("KGV.Web/features/maintenance/MaintenanceMaterialChangePanel.tsx"),
  read("KGV.Web/features/maintenance/MaintenanceContinuationPanel.tsx"),
  read("KGV.Maui/Pages/WartungsvertragFortfuehrungPage.cs"),
  read("KGV.Maui/Pages/MemberWartungsvertraegePage.cs"),
  read("KGV.Web/services/maintenance/maintenance-assignment-service.ts"),
  read("KGV.Web/features/maintenance/assignments/AssignContractsToMember.tsx"),
  read("KGV.Core/Templates/wartungsvertrag/v2/field-spec.json").then(JSON.parse),
  read("KGV.Core/Templates/wartungsvertrag-verlaengerung/v2/field-spec.json").then(JSON.parse),
]);

test("a maintenance contract number is reserved once from the contract source on signature start", () => {
  assert.match(migration, /new\.status <> 'zur_signatur' or old\.status is not distinct from 'zur_signatur'/);
  assert.match(migration, /v_quelle_art := 'wartungsvertrag'/);
  assert.match(migration, /v_quelle_id := new\.wartungsvertrag_id/);
  assert.match(migration, /perform public\.reserve_fachnummer\(v_art, v_quelle_art, v_quelle_id/);
  assert.match(migration, /when 'mitgliedsantrag' then 'mitgliedsantrag'[\s\S]*when 'pachtvertrag' then 'pachtvertrag'/);
  assert.match(migration, /v_quelle_art := 'kommunikationsvorgang'/);
  assert.match(migration, /quelle_art in \('kommunikationsvorgang', 'historisches_dokument', 'wartungsvertrag'\)/);
  assert.match(generator, /loadMaintenanceContractNumber\(auth, wartungsvertragId\)/);
  assert.match(generator, /action === "finalize" && \(!Number\.isSafeInteger\(fachnummer\)/);
  assert.match(generator, /Vertrags-Nr\. \$\{fachnummer\}/);
  assert.match(generator, /title = `Wartungsvertrag\$\{fachnummer \? ` · Vertrags-Nr\. \$\{fachnummer\}`/);
  assert.match(generator, /type === "wartungsvertrag" \|\| type === "wartungsvertrag_verlaengerung" \? "Wartungsvertrag"/);
  assert.match(generator, /numberPrefix \? `\$\{numberPrefix\}-\$\{fachnummer\}`/);
  assert.equal(contractTemplate.field_spec_version, "wartungsvertrag-field-spec/v2");
  assert.ok(contractTemplate.fields.some((field) => field.name === "vertragsnummer"));
  assert.match(catalogMigration, /'wartungsvertrag-field-spec\/v2'.*'top-left\/v1', 'draft', false, null/s);
  assert.match(catalogMigration, /'wartungsvertrag-verlaengerung-field-spec\/v2'.*'top-left\/v1', 'draft', false, null/s);
  assert.doesNotMatch(catalogMigration, /set aktiv\s*=\s*true|status\s*=\s*'published'/);
});

test("pre-signature preview stays unnumbered and prepared completion remains idempotent", () => {
  assert.match(generator, /values\.vertragsnummer = fachnummer \? `Vertrags-Nr\. \$\{fachnummer\}` : ""/);
  assert.match(generator, /if \(action === "preview"\) \{[\s\S]*drawPdfTemplate\(template\.pdfBytes, template\.fields, values, \{\}, "none"\)/);
  assert.match(migration, /public\.prepared_contract_signaturen_vollstaendig\(/);
  assert.match(migration, /if v_communication\.status = 'erledigt'[\s\S]*return query select v_communication\.wartungsvertrag_zuordnung_id, true/);
  assert.match(migration, /set status = 'erledigt'[\s\S]*where id = v_communication\.id/);
});

test("normal extensions reuse the existing contract number in their notice", () => {
  assert.match(continuationMigration, /v_neu\.wartungsvertrag_id\s*=\s*v_alt\.wartungsvertrag_id/);
  assert.match(generator, /loadMaintenanceContractNumber\(auth, Number\(process\.wartungsvertrag_id\)\)/);
  assert.match(generator, /Wartungsvertrag Nr\. \$\{fachnummer\}/);
  assert.match(generator, /title = `Wartungsvertragsverlängerung · Wartungsvertrag Nr\. \$\{fachnummer\}`/);
  const workflowValidator = migration.slice(migration.indexOf("create or replace function public.validate_wartungsvertrag_dokument_workflow()"));
  assert.match(workflowValidator, /new\.workflow_art = 'erstvertrag' and \(v_communication\.vorgangstyp <> 'vertrag_signatur' or v_communication\.dokumenttyp <> 'wartungsvertrag'\)/);
  assert.match(workflowValidator, /new\.workflow_art = 'verlaengerungsmitteilung' and \(v_communication\.vorgangstyp <> 'dokument_erzeugung' or v_communication\.dokumenttyp <> 'wartungsvertrag_verlaengerung'\)/);
  assert.doesNotMatch(workflowValidator, /if not found\s+or v_communication\.vorgangstyp <> 'vertrag_signatur'/);
  assert.equal(extensionTemplate.field_spec_version, "wartungsvertrag-verlaengerung-field-spec/v2");
  assert.ok(extensionTemplate.fields.some((field) => field.name === "vertragsnummer"));
  assert.match(migration, /v_quelle_art := 'wartungsvertrag'/);
});

test("basic changes require an explicit, retryable action and activate only after full signing", () => {
  assert.match(migration, /create or replace function public\.start_wartungsvertrag_grundlegende_aenderung/);
  assert.match(migration, /where vorgaenger_zuordnung_id = v_old\.id\s+for update/);
  assert.match(migration, /return query select v_communication\.id, v_change\.wartungsvertrag_id, v_communication\.status/);
  assert.match(migration, /insert into public\.wartungsvertraege\([\s\S]*?values \([\s\S]*?false, nullif\(btrim\(p_bemerkung\)/);
  assert.match(migration, /if public\.prepared_contract_signaturen_vollstaendig\(/);
  assert.match(migration, /set aktiv = true\s+where id = v_communication\.wartungsvertrag_id/);
  assert.match(migration, /set gueltig_bis = v_start - 1\s+where id = v_old\.id/);
  assert.match(migration, /'vorgaenger_zuordnung_id', v_old\.id/);
  assert.doesNotMatch(migration, /abs\(.*p_arbeitsstunden|p_titel.*v_old_contract\.titel/);
  assert.match(webFlow, /PreviewMaintenanceContractAsync|preview/);
  assert.match(materialChange, /startMaintenanceMaterialChange/);
  assert.match(materialChange, /grundlegende Änderung/i);
  assert.match(maui, /StarteWartungsvertragGrundlegendeAenderungAsync/);
});

test("Web and MAUI show the reserved number instead of technical contract IDs", () => {
  assert.match(continuation, /Wartungsvertrag Nr\./);
  assert.doesNotMatch(continuation, /Wartungsvertrag #\{/);
  assert.match(memberList, /Vertrags-Nr\./);
  assert.match(maui, /Wartungsvertrag Nr\./);
  assert.match(webFlow, /Vertrags-Nr\./);
  assert.doesNotMatch(assignmentService, /Wartungsvertrag #/);
  assert.doesNotMatch(assignmentPanel, /Wartungsvertrag #/);
});
