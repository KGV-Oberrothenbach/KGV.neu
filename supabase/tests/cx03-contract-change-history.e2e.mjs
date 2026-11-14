// CX-03 local database E2E. CX-01 supplies real users/signatures and CX-02
// supplies the rendered PDF. Only final-document metadata is a local fixture;
// no Google Drive upload is claimed or simulated.
import assert from "node:assert/strict";
import { access, mkdir, readFile, writeFile } from "node:fs/promises";
import { dirname } from "node:path";
import { spawnSync } from "node:child_process";

const apiUrl = required("P11_LOCAL_API_URL").replace(/\/$/, "");
const anonKey = required("P11_LOCAL_ANON_KEY");
const dbUrl = required("P11_LOCAL_DB_URL");
const cx01Path = required("CX01_RESULT_PATH");
const pdfPath = required("CX02_PDF_PATH");
const resultPath = process.env.CX03_RESULT_PATH;
const managerEmail = "cx01.manager@example.test";
const managerPassword = "CX01-local-test-password-2026!";
const cases = [];

function required(name) {
  const value = process.env[name];
  if (!value) throw new Error(`${name} is required`);
  return value;
}

function psql(sql, tuplesOnly = false) {
  const args = process.platform === "win32"
    ? ["exec", "-i", "supabase_db_03_Arbeitsstand", "psql", "-U", "postgres", "-d", "postgres", "-X", "-v", "ON_ERROR_STOP=1"]
    : [dbUrl, "-X", "-v", "ON_ERROR_STOP=1"];
  if (tuplesOnly) args.push("-q", "-t", "-A");
  const result = spawnSync(process.platform === "win32" ? "docker" : "psql", args, { input: sql, encoding: "utf8" });
  if (result.status !== 0) throw new Error(`psql failed:\n${result.stdout}\n${result.stderr}`);
  return result.stdout.trim();
}

async function http(path, { method = "GET", token, body } = {}) {
  const response = await fetch(`${apiUrl}${path}`, {
    method,
    headers: { apikey: anonKey, ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(body === undefined ? {} : { "Content-Type": "application/json" }) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const data = await response.json().catch(() => null);
  return { response, data };
}

async function check(name, kind, action) {
  try {
    await action();
    cases.push({ name, kind, status: "passed" });
    console.log(`ok - ${name}`);
  } catch (error) {
    cases.push({ name, kind, status: "failed", message: error.message });
    console.error(`not ok - ${name}: ${error.message}`);
    throw error;
  }
}

const cx01 = JSON.parse(await readFile(cx01Path, "utf8"));
assert.equal(cx01.complete, true);
await access(pdfPath);
const processId = Number(cx01.kommunikationsvorgang_id);
const revision = Number(cx01.signatur_revision);
const parcelId = Number(cx01.parcel_id);

let token;
let managerId;
let ids;
let report;
try {
  await check("manager obtains a real local Auth session", "positive", async () => {
    const login = await http("/auth/v1/token?grant_type=password", { method: "POST", body: { email: managerEmail, password: managerPassword } });
    assert.equal(login.response.status, 200, JSON.stringify(login.data));
    token = login.data.access_token;
    managerId = login.data.user.id;
  });

  await check("CX-02 PDF is represented only by a local provenance fixture", "positive", async () => {
    const json = psql(`
      begin;
      insert into public.dokument(parzelle_id,bucket,storage_path,titel,dateiname,mime_type,drive_file_id,source_kommunikationsvorgang_id,source_revision)
      values (${parcelId},'dokumente','cx03/local-fixture/cx02-signed-lease.pdf','CX-03 lokale Finaldokument-Fixture (kein Google-Drive-Upload)','cx02-signed-lease.pdf','application/pdf','cx03-local-fixture-no-google-drive',${processId},${revision})
      returning id \\gset
      update public.kommunikationsvorgang set status='letzte_pruefung',final_dokument_id=:id where id=${processId};
      commit;
      select json_build_object(
        'document_id',:id,
        'change_id',(select id from public.pacht_vertragsaenderung where kommunikationsvorgang_id=${processId}),
        'lease_id',(select pachtverhaeltnis_id from public.pacht_vertragsaenderung where kommunikationsvorgang_id=${processId}),
        'predecessor_id',(select vorgaenger_fassung_id from public.pacht_vertragsaenderung where kommunikationsvorgang_id=${processId})
      );`, true).split(/\r?\n/).findLast(line => line.startsWith("{"));
    ids = JSON.parse(json);
    assert.ok(ids.document_id && ids.change_id && ids.lease_id && ids.predecessor_id);
  });

  await check("application without a new contract version is rejected atomically", "negative", async () => {
    const result = await http("/rest/v1/rpc/wende_signierte_parzellen_vertragsaenderung_an", { method: "POST", token, body: { p_pacht_vertragsaenderung_id: ids.change_id } });
    assert.equal(result.response.status, 400, JSON.stringify(result.data));
  });

  await check("premature version binding still cannot authorize application", "negative", async () => {
    psql(`
      begin;
      set local role service_role;
      select set_config('request.jwt.claim.role','service_role',true);
      do $test$
      declare v_version bigint;
      begin
        select pachtvertragsfassung_id into v_version from public.register_pachtvertrags_neufassung(
          ${ids.lease_id},${ids.predecessor_id},${ids.document_id},current_date,current_date,'${managerId}'::uuid);
        perform set_config('request.jwt.claim.sub','${managerId}',true);
        begin
          perform public.wende_signierte_parzellen_vertragsaenderung_an(${ids.change_id});
          raise exception 'premature application unexpectedly succeeded';
        exception when sqlstate '23514' then null;
        end;
      end $test$;
      rollback;`);
  });

  await check("wrong document revision blocks completion without residue", "negative", async () => {
    psql(`
      begin;
      update public.dokument set source_revision=${revision + 1} where id=${ids.document_id};
      set local role service_role;
      select set_config('request.jwt.claim.role','service_role',true);
      do $test$ begin
        begin perform public.complete_prepared_pachtvertrag(${processId},${revision},'${managerId}'::uuid);
        raise exception 'wrong revision unexpectedly completed'; exception when sqlstate '23514' then null; end;
      end $test$;
      rollback;`);
  });

  await check("incomplete mandatory signatures block completion", "negative", async () => {
    psql(`
      begin;
      set local session_replication_role=replica;
      delete from public.prepared_contract_signatur where kommunikationsvorgang_id=${processId} and signatur_schluessel='vorstand_2';
      set local session_replication_role=origin;
      set local role service_role;
      select set_config('request.jwt.claim.role','service_role',true);
      do $test$ begin
        begin perform public.complete_prepared_pachtvertrag(${processId},${revision},'${managerId}'::uuid);
        raise exception 'incomplete signatures unexpectedly completed'; exception when sqlstate '23514' then null; end;
      end $test$;
      rollback;`);
  });

  await check("wrong predecessor and changed frozen snapshot are rejected", "negative", async () => {
    psql(`
      begin;
      set local session_replication_role=replica;
      update public.pacht_vertragsaenderung set vorgaenger_fassung_id=${ids.predecessor_id + 999999} where id=${ids.change_id};
      set local session_replication_role=origin;
      set local role service_role;
      select set_config('request.jwt.claim.role','service_role',true);
      do $test$ begin
        begin perform public.register_pachtvertrags_neufassung(${ids.lease_id},${ids.predecessor_id},${ids.document_id},current_date,current_date,'${managerId}'::uuid);
        raise exception 'wrong predecessor unexpectedly bound'; exception when sqlstate '23514' then null; end;
      end $test$;
      rollback;
      begin;
      set local role service_role;
      select set_config('request.jwt.claim.role','service_role',true);
      do $test$
      begin
        update public.pacht_vertragsaenderung set aenderungsstand='{"flaeche_qm":121,"hat_strom":true,"hat_wasser":false}'::jsonb where id=${ids.change_id};
        begin perform public.register_pachtvertrags_neufassung(${ids.lease_id},${ids.predecessor_id},${ids.document_id},current_date,current_date,'${managerId}'::uuid);
        raise exception 'changed snapshot unexpectedly bound'; exception when sqlstate '23514' then null; end;
      end $test$;
      rollback;`);
  });

  await check("direct parcel mutation and forged session setting are rejected", "negative", async () => {
    psql(`
      begin;
      set local role authenticated;
      select set_config('request.jwt.claim.sub','${managerId}',true);
      select set_config('kgv.pacht_aenderung_freigabe','${ids.change_id}',true);
      do $test$ begin
        begin update public.parzelle set flaeche_qm=999 where id=${parcelId};
        raise exception 'direct mutation unexpectedly succeeded'; exception when sqlstate '42501' then null; end;
      end $test$;
      rollback;`);
  });

  let completed;
  await check("existing server Edge Function completes and registers the exact version", "positive", async () => {
    completed = await http("/functions/v1/kgv-complete-prepared-lease-contract", { method: "POST", token, body: { kommunikationsvorgang_id: processId, expected_revision: revision } });
    assert.equal(completed.response.status, 200, JSON.stringify(completed.data));
    assert.equal(completed.data.success, true);
    assert.equal(Number(completed.data.pachtverhaeltnis_id), Number(ids.lease_id));
  });

  await check("completed process, version, predecessor and provenance are exact", "positive", async () => {
    const state = JSON.parse(psql(`select json_build_object(
      'status',k.status,'version_id',f.id,'predecessor_id',f.vorgaenger_fassung_id,
      'document_id',f.dokument_id,'source_process',d.source_kommunikationsvorgang_id,
      'source_revision',d.source_revision,'old_obsolete',old.nicht_mehr_massgeblich_am is not null)
      from public.kommunikationsvorgang k join public.pachtvertragsfassung f on f.dokument_id=k.final_dokument_id
      join public.dokument d on d.id=f.dokument_id join public.pachtvertragsfassung old on old.id=f.vorgaenger_fassung_id
      where k.id=${processId};`, true));
    assert.deepEqual(state, { status: "erledigt", version_id: Number(completed.data.pachtvertragsfassung_id), predecessor_id: Number(ids.predecessor_id), document_id: Number(ids.document_id), source_process: processId, source_revision: revision, old_obsolete: true });
  });

  await check("ended lease and revoked effective permission prevent application", "negative", async () => {
    psql(`begin; update public.pachtverhaeltnis set beendet_am=current_date where id=${ids.lease_id}; set local role authenticated; select set_config('request.jwt.claim.sub','${managerId}',true); do $test$ begin begin perform public.wende_signierte_parzellen_vertragsaenderung_an(${ids.change_id}); raise exception 'ended lease applied'; exception when sqlstate '23514' then null; end; end $test$; rollback;`);
    psql(`update public.app_user set permission_revocations=permission_revocations|524288 where user_id='${managerId}'::uuid;`);
    try {
      const denied = await http("/rest/v1/rpc/wende_signierte_parzellen_vertragsaenderung_an", { method: "POST", token, body: { p_pacht_vertragsaenderung_id: ids.change_id } });
      assert.equal(denied.response.status, 403, JSON.stringify(denied.data));
    } finally {
      psql(`update public.app_user set permission_revocations=permission_revocations & (~524288::bigint) where user_id='${managerId}'::uuid;`);
    }
  });

  await check("two concurrent applications serialize successfully", "positive", async () => {
    const body = { p_pacht_vertragsaenderung_id: ids.change_id };
    const [a, b] = await Promise.all([
      http("/rest/v1/rpc/wende_signierte_parzellen_vertragsaenderung_an", { method: "POST", token, body }),
      http("/rest/v1/rpc/wende_signierte_parzellen_vertragsaenderung_an", { method: "POST", token, body }),
    ]);
    assert.equal(a.response.status, 200, JSON.stringify(a.data));
    assert.equal(b.response.status, 200, JSON.stringify(b.data));
    assert.equal(a.data, true);
    assert.equal(b.data, true);
  });

  await check("identical replay is idempotent and adds no audit row", "positive", async () => {
    const replay = await http("/rest/v1/rpc/wende_signierte_parzellen_vertragsaenderung_an", { method: "POST", token, body: { p_pacht_vertragsaenderung_id: ids.change_id } });
    assert.equal(replay.response.status, 200, JSON.stringify(replay.data));
    assert.equal(Number(psql(`select count(*) from public.pacht_daten_audit where pachtverhaeltnis_id=${ids.lease_id} and art='vertragsaenderung';`, true)), 1);
  });

  await check("final parcel, lease identity, change and full audit payload are correct", "positive", async () => {
    const state = JSON.parse(psql(`select json_build_object(
      'area',pa.flaeche_qm,'power',pa.hat_strom,'water',pa.hat_wasser,
      'lease_id',p.id,'start',p.urspruenglicher_pachtbeginn,
      'applied',c.angewendet_am is not null,'actor',c.angewendet_von,
      'audit_count',(select count(*) from public.pacht_daten_audit a where a.pachtverhaeltnis_id=p.id and a.art='vertragsaenderung'),
      'audit_ok',(select a.vorher='{"flaeche_qm":100,"hat_strom":false,"hat_wasser":false}'::jsonb and a.nachher='{"flaeche_qm":120,"hat_strom":true,"hat_wasser":false}'::jsonb and a.begruendung='Anpassung der Gartenfläche und Aufnahme des Stromanschlusses' and a.pachtvertragsfassung_id=${Number(completed.data.pachtvertragsfassung_id)} and a.akteur_user_id='${managerId}'::uuid and a.erfasst_am is not null from public.pacht_daten_audit a where a.pachtverhaeltnis_id=p.id and a.art='vertragsaenderung'))
      from public.pachtverhaeltnis p join public.parzelle pa on pa.id=p.parzelle_id join public.pacht_vertragsaenderung c on c.pachtverhaeltnis_id=p.id where p.id=${ids.lease_id};`, true));
    assert.equal(Number(state.area), 120);
    assert.equal(state.power, true);
    assert.equal(state.water, false);
    assert.equal(Number(state.lease_id), Number(ids.lease_id));
    assert.equal(state.start, "2026-01-01");
    assert.equal(state.applied, true);
    assert.equal(state.actor, managerId);
    assert.equal(Number(state.audit_count), 1);
    assert.equal(state.audit_ok, true);
  });

  await check("contract history and audit rows reject update and delete", "negative", async () => {
    psql(`do $test$ begin
      begin update public.pachtvertragsfassung set fassungstyp='erstvertrag' where id=${Number(completed.data.pachtvertragsfassung_id)}; raise exception 'version update succeeded'; exception when sqlstate '55000' then null; end;
      begin delete from public.pachtvertragsfassung where id=${ids.predecessor_id}; raise exception 'version delete succeeded'; exception when sqlstate '55000' then null; end;
      begin update public.pacht_daten_audit set begruendung='tampered' where pachtverhaeltnis_id=${ids.lease_id}; raise exception 'audit update succeeded'; exception when sqlstate '55000' then null; end;
      begin delete from public.pacht_daten_audit where pachtverhaeltnis_id=${ids.lease_id}; raise exception 'audit delete succeeded'; exception when sqlstate '55000' then null; end;
    end $test$;`);
  });

  const passed = cases.filter(test => test.status === "passed").length;
  report = { suite: "CX-03 contract change, history and audit", passed, failed: 0, process_id: processId, change_id: ids.change_id, lease_id: ids.lease_id, previous: { flaeche_qm: 100, hat_strom: false, hat_wasser: false }, current: { flaeche_qm: 120, hat_strom: true, hat_wasser: false }, audit_rows: 1, concurrent_calls: 2, local_document_fixture_only: true, google_drive_e2e: false, complete: true, cases };
  console.log(`CX03_RESULT ${JSON.stringify(report)}`);
} catch (error) {
  report = { suite: "CX-03 contract change, history and audit", passed: cases.filter(test => test.status === "passed").length, failed: cases.filter(test => test.status === "failed").length || 1, complete: false, error: error.message, cases };
  console.error(`CX03_RESULT ${JSON.stringify(report)}`);
  process.exitCode = 1;
} finally {
  if (resultPath) {
    await mkdir(dirname(resultPath), { recursive: true });
    await writeFile(resultPath, `${JSON.stringify(report, null, 2)}\n`, "utf8");
  }
}
