import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const root = new URL("../../", import.meta.url);
const migration = new URL("supabase/migrations/20261022160000_vor3_mitgliedsaufnahme_vorstand.sql", root);

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
