import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const root = new URL("../../", import.meta.url);
const migration = new URL("supabase/migrations/20261022160000_vor3_mitgliedsaufnahme_vorstand.sql", root);
const admissionConfirmationMigration = new URL("supabase/migrations/20261026160000_nb2a_d5c_aufnahmebestaetigung_workflow.sql", root);
const admissionCatalogMigration = new URL("supabase/migrations/20261026150000_nb2a_d5c_publish_aufnahmebestaetigung_v1.sql", root);

test("membership board workflow is snapshot-based, append-only and has no majority rule", async () => {
  const sql = await readFile(migration, "utf8");
  for (const table of ["mitgliedsaufnahme_vorstandsbearbeitung", "mitgliedsaufnahme_abstimmung_snapshot_person", "mitgliedsaufnahme_abstimmung_snapshot_berechtigung", "mitgliedsaufnahme_abstimmung_stimme"]) assert.match(sql, new RegExp(`create table public\\.${table}`));
  assert.match(sql, /aufnahme_status in \('antrag_offen', 'aufgenommen', 'abgelehnt'\)/);
  assert.match(sql, /unique\(vorstandsbearbeitung_id, mitglied_id\)/);
  assert.match(sql, /unique\(snapshot_person_id, version\)/);
  assert.match(sql, /Mehrere gleichzeitig gültige Vertreter/);
  assert.match(sql, /Einstimmiger Weg verlangt vollständige Ja-Abstimmung/);
  assert.doesNotMatch(sql, /mehr Ja|mehr Nein|Mehrheits/);
});

test("membership final review starts board handling instead of directly completing the application", async () => {
  const edge = await readFile(new URL("supabase/functions/kgv-complete-prepared-membership-application/index.ts", root), "utf8");
  assert.match(edge, /start_mitgliedsaufnahme_vorstandsbearbeitung/);
  assert.doesNotMatch(edge, /admin\.rpc\("complete_prepared_mitgliedsantrag"/);
});

test("board document access is limited to the final application and the selected admission date", async () => {
  const sql = await readFile(migration, "utf8");
  assert.match(sql, /can_access_mitgliedsaufnahme_vorstand_document\(p_auth_user_id uuid,p_dokument_id bigint\)/);
  assert.match(sql, /d\.id=p_dokument_id and d\.archiviert_at is null/);
  assert.match(sql, /v\.final_dokument_id/);
  assert.match(sql, /dokument_select_mitgliedsaufnahme_vorstand/);
  assert.match(sql, /start_date:=p_aufnahmedatum/);
  assert.match(sql, /complete_prepared_mitgliedsantrag_aus_entscheidung\(v\.id,v\.revision,caller,p_aufnahmedatum\)/);
  assert.match(sql, /old\.aufnahme_status in \('antrag_offen','abgelehnt'\)[\s\S]*new\.aufnahme_status = 'aufgenommen'[\s\S]*Die Aufnahme darf nur über den Vorstandsbeschluss erfolgen/);
  assert.match(sql, /set_config\('kgv\.mitgliedsaufnahme_abschluss','1',true\)[\s\S]*aufnahme_status='aufgenommen'/);
  assert.doesNotMatch(sql, /start_date:=coalesce\(\(v\.entwurf_daten->>'startDate'\)::date/);
});

test("web and download path use the board-document contract without role-derived UI decisions", async () => {
  const [web, edge] = await Promise.all([
    readFile(new URL("KGV.Web/features/communication/MembershipBoardReviewList.tsx", root), "utf8"),
    readFile(new URL("supabase/functions/kgv-upload-document/index.ts", root), "utf8"),
  ]);
  assert.match(web, /Mitgliedsantrag öffnen/);
  assert.match(web, /canDecideMembershipBoardReview/);
  assert.match(web, /berechtigungsquellen/);
  assert.match(edge, /can_access_mitgliedsaufnahme_vorstand_document/);
  assert.match(edge, /p_auth_user_id: auth\.userId/);
});

test("positive admission creates one retryable confirmation process without changing a rejection", async () => {
  const [sql, edge, web] = await Promise.all([
    readFile(admissionConfirmationMigration, "utf8"),
    readFile(new URL("supabase/functions/kgv-generate-contract/index.ts", root), "utf8"),
    readFile(new URL("KGV.Web/features/communication/MembershipBoardReviewList.tsx", root), "utf8"),
  ]);
  assert.match(sql, /ux_kommunikationsvorgang_aufnahmebestaetigung_quelle/);
  assert.match(sql, /if p_ergebnis='annehmen' then\s+perform public\.ensure_aufnahmebestaetigung_dokumentvorgang\(b\.id\);/);
  assert.match(sql, /'dokument_erzeugung','aufnahmebestaetigung','vorbereitet'/);
  assert.match(sql, /create or replace function public\.complete_aufnahmebestaetigung/);
  assert.match(edge, /auth\.admin\.rpc\("complete_aufnahmebestaetigung"/);
  assert.match(web, /Aufnahmebestätigung erneut erstellen/);
  assert.match(web, /ensureAdmissionConfirmationProcess/);
});

test("admission confirmation is linked to the completed original application and retry-safe", async () => {
  const [sql, edge] = await Promise.all([
    readFile(admissionConfirmationMigration, "utf8"),
    readFile(new URL("supabase/functions/kgv-generate-contract/index.ts", root), "utf8"),
  ]);
  assert.match(sql, /mitgliedsantrag_kommunikationsvorgang_id',v_antrag\.id/);
  assert.match(sql, /perform pg_advisory_xact_lock\(v_antrag\.id\)/);
  assert.match(sql, /and \(entwurf_daten->>'mitgliedsantrag_kommunikationsvorgang_id'\)::bigint=v_antrag\.id/);
  assert.match(sql, /complete_prepared_mitgliedsantrag_aus_entscheidung\(v\.id,v\.revision,caller,p_aufnahmedatum\)[\s\S]*?entscheidung=case[\s\S]*?ensure_aufnahmebestaetigung_dokumentvorgang/);
  assert.match(sql, /aufnahme_status='aufgenommen',aktiv=true,mitglied_seit=p_aufnahmedatum/);
  assert.match(sql, /if p_ergebnis='annehmen' then[\s\S]*?ensure_aufnahmebestaetigung_dokumentvorgang/);
  assert.doesNotMatch(sql.match(/else[\s\S]*?end if;[\s\S]*?update public\.mitgliedsaufnahme_vorstandsbearbeitung/)?.[0] ?? "", /aufnahmebestaetigung/);
  assert.match(edge, /sourceKommunikationsvorgangId, sourceRevision/);
  assert.match(edge, /source_kommunikationsvorgang_id: provenance\.sourceKommunikationsvorgangId, source_revision: provenance\.sourceRevision/);
  assert.match(edge, /if \(existing\) \{[\s\S]*?complete_aufnahmebestaetigung[\s\S]*?Vorhandene Aufnahmebestätigung wurde wiederverwendet/);
  assert.match(sql, /v_vorgang\.status='erledigt' and v_vorgang\.final_dokument_id=v_dokument\.id/);
});

test("admission confirmation renders only canonical application and membership numbers", async () => {
  const edge = await readFile(new URL("supabase/functions/kgv-generate-contract/index.ts", root), "utf8");
  const branch = /else if \(type === "aufnahmebestaetigung"\) \{([\s\S]*?)\n    \} else if \(type === "parzellenprotokoll"\)/.exec(edge)?.[1];
  assert.ok(branch, "admission-confirmation branch is missing");
  assert.match(branch, /membershipNumber\(member, true, "das aufgenommene Mitglied"\)/);
  assert.match(branch, /from\("fachnummer_reservierung"\)[\s\S]*?eq\("quelle_art", "kommunikationsvorgang"\)[\s\S]*?eq\("quelle_id", applicationProcessId\)[\s\S]*?eq\("art", "mitgliedsantrag"\)/);
  assert.match(branch, /Antrags-Nr\.: \$\{Number\(reservation\.nummer\)\}/);
  assert.match(branch, /Mgl\.-Nr\.: \$\{membershipNo\}/);
  assert.match(branch, /mitglied_seit: `Aufgenommen am: \$\{formatDate\(member\.mitglied_seit\)\}`/);
  assert.match(branch, /empfaenger_block:/);
  assert.match(branch, /absender_name: formatLetterheadName\(configurationSnapshot!\.vereinName\)/);
  assert.match(branch, /fusszeile_verein:/);
  assert.match(branch, /fusszeile_bank:/);
  assert.match(branch, /fusszeile_vorstand: boardFooter/);
  assert.match(branch, /loadImpressumBoardFooter\(auth\)/);
  assert.match(edge, /from\("mitglied"\)\.select\("id,vorname,name"\)/);
  assert.doesNotMatch(branch, /\.insert\([^\n]*fachnummer_reservierung|Fachnummer\.Mitglied|member\.id\s*\+\s*1000|1000\s*\+\s*member\.id/);
});

test("admission field spec extends the dynamic briefbogen v2 contract without changing v1", async () => {
  const [admissionSpecText, letterheadV2Text, letterheadV1Text, catalog] = await Promise.all([
    readFile(new URL("KGV.Core/Templates/aufnahmebestaetigung/v1/field-spec.json", root), "utf8"),
    readFile(new URL("KGV.Core/Templates/briefbogen-allgemein/v2/field-spec.json", root), "utf8"),
    readFile(new URL("KGV.Core/Templates/briefbogen-allgemein/v1/field-spec.json", root), "utf8"),
    readFile(admissionCatalogMigration, "utf8"),
  ]);
  const admissionSpec = JSON.parse(admissionSpecText);
  const letterheadV2 = JSON.parse(letterheadV2Text);
  const letterheadV1 = JSON.parse(letterheadV1Text);
  const admissionFields = admissionSpec.fields.map((field) => field.name);
  const letterheadV2Fields = letterheadV2.fields.map((field) => field.name);
  assert.equal(admissionSpec.field_spec_version, "aufnahmebestaetigung-field-spec/v1");
  assert.deepEqual(admissionFields, ["absender_name", "absender_anschrift", "empfaenger_block", "datum", "betreff", "antragsnummer", "mitgliedsnummer", "mitglied_vorname", "mitglied_name", "mitglied_seit", "anrede", "brieftext", "abschluss", "fusszeile_verein", "fusszeile_bank", "fusszeile_vorstand"]);
  assert.deepEqual(admissionFields.filter((field) => !["antragsnummer", "mitgliedsnummer", "mitglied_vorname", "mitglied_name", "mitglied_seit"].includes(field)), letterheadV2Fields);
  assert.equal(letterheadV1.fields.length, 6);
  assert.match(catalog, /'aufnahmebestaetigung', 'v1'/);
  assert.match(catalog, /Source only: this migration must not be executed/);
  assert.match(catalog, /templates\/aufnahmebestaetigung\/v1\/template\.pdf/);
  assert.match(catalog, /aufnahmebestaetigung-field-spec\/v1/);
});

test("admission UI retries only the document generation and skips an already completed document", async () => {
  const web = await readFile(new URL("KGV.Web/features/communication/MembershipBoardReviewList.tsx", root), "utf8");
  const retry = web.match(/const createAdmissionConfirmation[\s\S]*?\n  \};/)?.[0] ?? "";
  assert.match(retry, /ensureAdmissionConfirmationProcess/);
  assert.match(retry, /process\.status === "erledigt"/);
  assert.match(retry, /type: "aufnahmebestaetigung"/);
  assert.doesNotMatch(retry, /decideMembershipBoardReview/);
  assert.match(web, /aufnahmebestaetigung_status !== "erledigt"[\s\S]*?Aufnahmebestätigung erneut erstellen/);
  assert.match(web, /aufnahmebestaetigung_status === "erledigt" \? "Aufnahmebestätigung erstellt\./);
});
