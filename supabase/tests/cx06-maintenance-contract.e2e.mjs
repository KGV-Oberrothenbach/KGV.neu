// Local synthetic CI only. No external API or database targets.
{ const fr2Db=new URL(globalThis.process.env.P11_LOCAL_DB_URL);
  if(fr2Db.protocol!=="postgresql:"||fr2Db.hostname!=="127.0.0.1"||fr2Db.pathname!=="/postgres"||fr2Db.search||fr2Db.hash)throw Error("NONLOCAL_DB");
  if(globalThis.process.platform==="win32"&&!/^supabase_db_kgvfr2ci[a-f0-9]{12}$/.test(globalThis.process.env.FR2_LOCAL_DB_CONTAINER??""))throw Error("LOCAL_CONTAINER_REQUIRED");
  const api=new URL(globalThis.process.env.P11_LOCAL_API_URL);
  if(api.protocol!=="http:"||api.hostname!=="127.0.0.1"||api.username||api.password||api.pathname!=="/"||api.search||api.hash)throw Error("NONLOCAL_API");
  const fr2Fetch=globalThis.fetch; globalThis.fetch=(url,options={})=>{if(new URL(url).origin!==api.origin)throw Error("NONLOCAL_HTTP_TARGET");return fr2Fetch(url,{...options,redirect:"error"});};
}
import { readFileSync as fr2ReadFileSync } from "node:fs";
// Test-only rollback probe; never disables triggers or deletes committed documents.
function fr2RejectInvalidMetadata(processRow, member, revision, label) {
  const numeric=[processRow.id,member,revision].map(Number);
  assert.ok(numeric.every(x=>Number.isSafeInteger(x)&&x>0));
  assert.ok(["wrong-member","wrong-revision"].includes(label));
  const [pid,mid,rev]=numeric;
  const conn=new URL(dbUrl);
  assert.equal(conn.protocol,"postgresql:"); assert.equal(conn.hostname,"127.0.0.1");
  const sql="begin; set local role service_role; select set_config('request.jwt.claim.role','service_role',true);\n"
    +"do $fr2$ declare doc_id bigint; begin\n"
    +"insert into public.dokument(mitglied_id,bucket,storage_path,titel,dateiname,mime_type,source_kommunikationsvorgang_id,source_revision) values ("+mid+",'dokumente','cx06-local-only/"+pid+"/"+label+".pdf','CX06 LOCAL-ONLY rollback fixture','"+label+".pdf','application/pdf',"+pid+","+rev+") returning id into doc_id;\n"
    +"begin perform public.complete_prepared_wartungsvertrag("+pid+",doc_id); raise exception 'FR2_INVALID_PROVENANCE_ACCEPTED'; exception when sqlstate '23514' then if sqlerrm <> 'Das finale Wartungsvertragsdokument passt nicht zum Signaturvorgang.' then raise; end if; end;\n"
    +"if not exists(select 1 from public.dokument where id=doc_id) then raise exception 'FR2_PROBE_DOCUMENT_MISSING'; end if; end $fr2$;\nrollback;\nselect 'FR2_CX06_PROVENANCE_ROLLBACK_PASS';";
  const localContainer=globalThis.process.env.FR2_LOCAL_DB_CONTAINER;
  if(globalThis.process.platform==="win32") assert.match(localContainer??"",/^supabase_db_kgvfr2ci[a-f0-9]{12}$/);
  const cmd=globalThis.process.platform==="win32"?"docker":"psql";
  const args=globalThis.process.platform==="win32"?["exec","-i",localContainer,"psql","-X","-U","postgres","-d","postgres"]:[dbUrl,"-X"];
  const r=spawnSync(cmd,[...args,"-v","ON_ERROR_STOP=1","-v","VERBOSITY=verbose","-qAt","-f","-"],{input:sql,encoding:"utf8",timeout:30000});
  if(r.status!==0){const code=(r.stderr??"").match(/ERROR:\s+([0-9A-Z]{5}):/)?.[1]??"UNKNOWN"; console.log("KGV_FR2_CX06_PROBE_SQLSTATE "+code); throw Error("FR2_CX06_PROVENANCE_SQL_FAILED");}
  assert.ok(r.stdout.split(/\r?\n/).includes("FR2_CX06_PROVENANCE_ROLLBACK_PASS"));
  console.log("KGV_FR2_CX06_ROLLBACK_PASS "+label);
}
async function fr2VerifyActors(users){
  const expected={"main":{"allowed":[],"denied":[]},"secondary":{"allowed":[],"denied":[]},"manager":{"allowed":["mitglied.lesen","mitgliedschaft.verwalten","vertrag.vorbereiten","vorgang.bearbeiten"],"denied":["mitglied.anlegen"]},"outsider":{"allowed":[],"denied":[]},"revoked":{"allowed":[],"denied":["mitgliedschaft.verwalten"]},"board1":{"allowed":[],"denied":[]},"board2":{"allowed":[],"denied":[]}};
  if(Object.keys(users).sort().join(',')!==Object.keys(expected).sort().join(','))throw Error('FR2_ACTOR_SET_MISMATCH');
  for(const [label,user] of Object.entries(users)){
    const rpc=async(name)=>{
      const response=await fetch(process.env.P11_LOCAL_API_URL+'/rest/v1/rpc/'+name,{method:'POST',headers:{apikey:process.env.P11_LOCAL_ANON_KEY,Authorization:'Bearer '+user.token,'Content-Type':'application/json'},body:'{}'});
      if(response.status!==200)throw Error('FR2_ACTOR_RIGHTS_HTTP_FAILED');
      return response.json();
    };
    if(await rpc('fr212_actor_session_valid')!==true)throw Error('FR2_ACTOR_SESSION_INVALID');
    const rights=await rpc('ap02_preview_effective_rights');
    if(!Array.isArray(rights)||rights.length!==55||JSON.stringify(rights.filter(r=>r.erlaubt).map(r=>r.code).sort())!==JSON.stringify(expected[label].allowed))throw Error('FR2_ACTOR_RIGHTS_MISMATCH');
    if(expected[label].denied.some(code=>!rights.some(r=>r.code===code&&r.individuell_entzogen===true&&r.erlaubt===false)))throw Error('FR2_ACTOR_DENY_MISMATCH');
  }
  console.log('KGV_FR2_ACTOR_RIGHTS_PASS '+Object.keys(expected).length);
}
// CX-06 real local E2E: Auth/PostgREST/Edge/Storage, persistent maintenance
// signatures, the productive PDF renderer, and service-only DB completion.
import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { deflateSync } from "node:zlib";
import { PDFDict, PDFDocument, PDFName, PDFStream, StandardFonts, rgb } from "pdf-lib";
import { drawPdfTemplate, sha256Hex } from "../functions/kgv-generate-contract/pdf-rendering-core.mjs";

const apiUrl = required("P11_LOCAL_API_URL").replace(/\/$/, "");
const anonKey = required("P11_LOCAL_ANON_KEY");
const serviceKey = required("P11_LOCAL_SERVICE_ROLE_KEY");
const serviceApiKey = process.env.P11_LOCAL_SECRET_KEY || serviceKey;
const serviceBearer = serviceApiKey.startsWith("sb_secret_") ? undefined : serviceKey;
const dbUrl = required("P11_LOCAL_DB_URL");
const pdfPath = required("CX06_PDF_PATH");
const resultPath = process.env.CX06_RESULT_PATH;
const fixturePath = fileURLToPath(new URL("./cx06-maintenance-contract.fixture.sql", import.meta.url));
const templatePath = resolve("KGV.Core/Templates/wartungsvertrag/v2/template.pdf");
const fieldSpecPath = resolve("KGV.Core/Templates/wartungsvertrag/v2/field-spec.json");
const bucket = "prepared-contract-signatures";
const counters = { passed: 0, failed: 0, positive_passed: 0, negative_passed: 0 };
const uploads = { requests: 0, paths: new Set(), reads: 0 };
const cases = [];

function required(name) { const value = process.env[name]; if (!value) throw new Error(`${name} is required`); return value; }
async function checked(name, kind, action) {
  try { await action(); counters.passed++; counters[`${kind}_passed`]++; cases.push({ name, kind, status: "passed" }); console.log(`ok - ${name}`); }
  catch (error) { counters.failed++; cases.push({ name, kind, status: "failed", message: error.message }); console.error(`not ok - ${name}: ${error.message}`); throw error; }
}

async function http(path, { method = "GET", token, key = anonKey, body, headers = {}, raw = false } = {}) {
  const response = await fetch(`${apiUrl}${path}`, { method, headers: { apikey: key, ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(body === undefined ? {} : { "Content-Type": "application/json" }), ...headers }, body: body === undefined ? undefined : body instanceof Uint8Array ? body : JSON.stringify(body) });
  const type = response.headers.get("content-type") ?? "";
  const data = raw ? new Uint8Array(await response.arrayBuffer()) : type.includes("application/json") ? await response.json() : await response.text();
  return { response, data };
}
async function rpc(name, token, body, key = anonKey) { return http(`/rest/v1/rpc/${name}`, { method: "POST", token, key, body }); }
async function service(path, options = {}) { return http(path, { ...options, token: serviceBearer, key: serviceApiKey }); }
async function serviceRpc(name, body) { return service(`/rest/v1/rpc/${name}`, { method: "POST", body }); }
async function signup(label) {
  const result = await http("/auth/v1/signup", { method: "POST", body: { email: `cx06.${label}@example.test`, password: "CX06-local-test-password-2026!" } });
  assert.equal(result.response.status, 200, JSON.stringify(result.data));
  return { id: result.data.user.id, token: result.data.access_token };
}
function runFixture(users) {
  const command = process.platform === "win32" ? "docker" : "psql";
  const args = process.platform === "win32"
    ? ["exec", "-i", process.env.FR2_LOCAL_DB_CONTAINER, "psql", "-U", "postgres", "-d", "postgres", "-X", "-v", "ON_ERROR_STOP=1"]
    : [dbUrl, "-X", "-v", "ON_ERROR_STOP=1"];
  for (const [name, user] of Object.entries(users)) args.push("-v", `${name}_user_id=${user.id}`);
  args.push("-f", process.platform === "win32" ? "/dev/stdin" : fixturePath);
  const options = { encoding: "utf8" };
  if (process.platform === "win32") options.input = fr2ReadFileSync(fixturePath, "utf8");
  const result = spawnSync(command, args, options);
  if (result.status !== 0) throw new Error(`fixture failed: ${result.stdout}\n${result.stderr}`);
  const jsonLine = result.stdout.split(/\r?\n/).map(line => line.trim()).filter(line => line.startsWith("{")).at(-1);
  if (!jsonLine) throw new Error(`fixture result missing: ${result.stdout}`);
  return JSON.parse(jsonLine);
}
function requireFixture() { return spawnSync("powershell", ["-NoProfile", "-Command", `[IO.File]::ReadAllText('${fixturePath.replaceAll("'", "''")}')`], { encoding: "utf8" }).stdout; }
function png(r, g, b) {
  const width = 80, height = 28, scan = Buffer.alloc((width * 4 + 1) * height);
  for (let y = 0; y < height; y++) { const offset = y * (width * 4 + 1); scan[offset] = 0; for (let x = 0; x < width; x++) { const p = offset + 1 + x * 4; const ink = Math.abs(y - (8 + Math.sin(x / 7) * 7)) < 2; scan[p] = ink ? r : 255; scan[p + 1] = ink ? g : 255; scan[p + 2] = ink ? b : 255; scan[p + 3] = ink ? 255 : 0; } }
  const chunk = (type, data) => { const name = Buffer.from(type); const crcInput = Buffer.concat([name, data]); let crc = 0xffffffff; for (const byte of crcInput) { crc ^= byte; for (let i = 0; i < 8; i++) crc = (crc >>> 1) ^ (0xedb88320 & -(crc & 1)); } const out = Buffer.alloc(data.length + 12); out.writeUInt32BE(data.length, 0); name.copy(out, 4); data.copy(out, 8); out.writeUInt32BE((crc ^ 0xffffffff) >>> 0, data.length + 8); return out; };
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(width, 0); ihdr.writeUInt32BE(height, 4); ihdr[8] = 8; ihdr[9] = 6;
  return new Uint8Array(Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]), chunk("IHDR", ihdr), chunk("IDAT", deflateSync(scan)), chunk("IEND", Buffer.alloc(0))]));
}
const pngs = { member: png(25, 55, 140), board1: png(125, 30, 40), board2: png(20, 105, 55) };
async function upload(token, processId, revision, key, bytes) {
  uploads.requests++;
  const result = await http("/functions/v1/upload-prepared-contract-signature", { method: "POST", token, body: { kommunikationsvorgang_id: processId, signatur_revision: revision, signatur_schluessel: key, png_base64: Buffer.from(bytes).toString("base64") } });
  if (result.response.ok) uploads.paths.add(result.data.storage_path);
  return result;
}
async function save(token, processId, revision, key, memberId, source, uploaded, hash = uploaded.data.sha256) {
  return rpc("speichere_prepared_contract_signatur", token, { p_kommunikationsvorgang_id: processId, p_signatur_revision: revision, p_signatur_schluessel: key, p_mitglied_id: memberId, p_unterzeichner_name_snapshot: null, p_vereinsfunktion_besetzung_id: source?.vereinsfunktion_besetzung_id ?? null, p_vertretung_id: source?.vertretung_id ?? null, p_vertretene_besetzung_id: source?.vertretene_besetzung_id ?? null, p_storage_path: uploaded.data.storage_path, p_signatur_sha256: hash });
}
async function rows(table, query) { const result = await service(`/rest/v1/${table}?${query}`); assert.equal(result.response.status, 200, JSON.stringify(result.data)); return result.data; }
async function one(table, query) { const data = await rows(table, query); assert.equal(data.length, 1, `${table}: expected one row, got ${data.length}`); return data[0]; }
async function ensurePositions(manager, process) {
  const slots = [
    { key: "mitglied", order: 10, type: "mitglied", member: process.mitglied_id },
    { key: "vorstand_1", order: 90, type: "vereinsunterzeichner", member: null },
    { key: "vorstand_2", order: 100, type: "vereinsunterzeichner", member: null },
  ];
  for (const slot of slots) {
    const result = await rpc("ensure_prepared_contract_signatur_position", manager.token, { p_kommunikationsvorgang_id: process.id, p_signatur_revision: process.signatur_revision, p_signatur_schluessel: slot.key, p_sort_order: slot.order, p_pflicht: true, p_unterzeichner_typ: slot.type, p_mitglied_id: slot.member, p_unterzeichner_name_snapshot: null });
    assert.equal(result.response.status, 200, JSON.stringify(result.data));
  }
}
async function signCase(process, partyUser, fixture, users, negativeChecks = false) {
  const sources = await rows("prepared_contract_signatur_snapshot_quelle", `select=*,prepared_contract_signatur_snapshot_person!inner(mitglied_id)&snapshot_person_id=in.(${(await rows("prepared_contract_signatur_snapshot_person", `select=id&kommunikationsvorgang_id=eq.${process.id}&signatur_revision=eq.${process.signatur_revision}`)).map(x => x.id).join(",")})`);
  const board1Source = sources.find(x => Number(x.vereinsfunktion_besetzung_id) === Number(fixture.board1_assignment_id));
  const board2Source = sources.find(x => Number(x.vereinsfunktion_besetzung_id) === Number(fixture.board2_assignment_id));
  assert.ok(board1Source && board2Source, "frozen board sources missing");
  const memberUpload = await upload(partyUser.token, process.id, process.signatur_revision, "mitglied", pngs.member);
  assert.equal(memberUpload.response.status, 200, JSON.stringify(memberUpload.data));
  if (negativeChecks) {
    await checked("wrong member cannot bind the secondary party signature", "negative", async () => { const result = await save(users.main.token, process.id, process.signatur_revision, "mitglied", fixture.secondary_member_id, null, memberUpload); assert.notEqual(result.response.status, 200); });
    await checked("mismatching SHA-256 cannot be persisted", "negative", async () => { const result = await save(partyUser.token, process.id, process.signatur_revision, "mitglied", process.mitglied_id, null, memberUpload, "0".repeat(64)); assert.notEqual(result.response.status, 200); });
  }
  assert.equal((await save(partyUser.token, process.id, process.signatur_revision, "mitglied", process.mitglied_id, null, memberUpload)).response.status, 200);
  const b1 = await upload(users.board1.token, process.id, process.signatur_revision, "vorstand_1", pngs.board1); assert.equal(b1.response.status, 200, JSON.stringify(b1.data));
  assert.equal((await save(users.board1.token, process.id, process.signatur_revision, "vorstand_1", fixture.board1_member_id, board1Source, b1)).response.status, 200);
  const b2 = await upload(users.board2.token, process.id, process.signatur_revision, "vorstand_2", pngs.board2); assert.equal(b2.response.status, 200, JSON.stringify(b2.data));
  if (negativeChecks) {
    await checked("same natural person cannot occupy both board signatures", "negative", async () => { const result = await save(users.board1.token, process.id, process.signatur_revision, "vorstand_2", fixture.board1_member_id, board1Source, b2); assert.notEqual(result.response.status, 200); });
    await checked("non-snapshot signer cannot bind a board signature", "negative", async () => { const result = await save(users.outsider.token, process.id, process.signatur_revision, "vorstand_2", fixture.outsider_member_id, null, b2); assert.notEqual(result.response.status, 200); });
  }
  assert.equal((await save(users.board2.token, process.id, process.signatur_revision, "vorstand_2", fixture.board2_member_id, board2Source, b2)).response.status, 200);
  for (const uploadResult of [memberUpload, b1, b2]) { const downloaded = await service(`/storage/v1/object/authenticated/${bucket}/${uploadResult.data.storage_path}`, { raw: true }); assert.equal(downloaded.response.status, 200); assert.equal(await sha256Hex(downloaded.data), uploadResult.data.sha256); uploads.reads++; }
}
function imageCount(pdf) { let count = 0; for (const page of pdf.getPages()) { const x = page.node.Resources()?.lookupMaybe(PDFName.of("XObject"), PDFDict); if (!x) continue; for (const [, value] of x.entries()) { const object = pdf.context.lookup(value); if (object instanceof PDFStream && object.dict.get(PDFName.of("Subtype"))?.toString() === "/Image") count++; } } return count; }
async function makeLocalFinalDocument(process, assignmentId, memberId, suffix) {
  const result = await service("/rest/v1/dokument", { method: "POST", headers: { Prefer: "return=representation" }, body: { mitglied_id: memberId, bucket: "dokumente", storage_path: `cx06-local-only/${process.id}/${suffix}.pdf`, drive_file_id: `cx06-local-only-no-google:${process.id}:${suffix}`, titel: `CX06 LOCAL-ONLY Finaldokument ${suffix}`, dateiname: `cx06-${suffix}.pdf`, mime_type: "application/pdf", source_kommunikationsvorgang_id: process.id, source_revision: process.signatur_revision, source_wartungsvertrag_zuordnung_id: assignmentId } });
  assert.equal(result.response.status, 201, JSON.stringify(result.data)); return result.data[0];
}

let report;
try {
  const users = Object.fromEntries(await Promise.all(["main","secondary","manager","outsider","revoked","board1","board2"].map(async name => [name, await signup(name)])));
  const fixture = runFixture(users); await fr2VerifyActors(users);
  let processA, processB, numberA, numberB, numberAbort, prematureDocument;

  await checked("FR2 manager without mitglied.anlegen prepares both party cases idempotently", "positive", async () => {
    for (const [right,expected] of [['mitgliedschaft.verwalten',true],['vertrag.vorbereiten',true],['mitglied.anlegen',false]]) { const permission=await rpc('ap02_has_effective_right',users.manager.token,{p_fachrecht:right}); assert.equal(permission.response.status,200); assert.equal(permission.data,expected); }
    const a1 = await rpc("ensure_wartungsvertrag_prepared_contract", users.manager.token, { p_zuordnung_id: fixture.assignment_a_id });
    const a2 = await rpc("ensure_wartungsvertrag_prepared_contract", users.manager.token, { p_zuordnung_id: fixture.assignment_a_id });
    const b1 = await rpc("ensure_wartungsvertrag_prepared_contract", users.manager.token, { p_zuordnung_id: fixture.assignment_b_id });
    assert.equal(a1.response.status, 200, JSON.stringify(a1.data)); assert.equal(a1.data.id, a2.data.id); assert.equal(a1.data.mitglied_id, fixture.main_member_id); assert.equal(b1.data.mitglied_id, fixture.secondary_member_id);
    processA = a1.data; processB = b1.data;
    assert.equal(processA.entwurf_daten.wartungsvertrag_titel, "CX06 Anlagenpflege"); assert.equal(processB.entwurf_daten.wartungsvertrag_beschreibung, "Wartung, Reinigung und dokumentierte Prüfung der Vereinsgeräte.");
  });
  await checked("no 90xxx exists before signature start", "positive", async () => { assert.equal((await rows("fachnummer_reservierung", `select=*&quelle_art=eq.wartungsvertrag&quelle_id=in.(${fixture.contract_a_id},${fixture.contract_b_id})`)).length, 0); });
  await checked("missing permission and individual revocation block preparation", "negative", async () => { for (const user of [users.outsider, users.revoked]) { const result = await rpc("ensure_wartungsvertrag_prepared_contract", user.token, { p_zuordnung_id: fixture.assignment_a_id }); assert.notEqual(result.response.status, 200); } });
  await checked("wrong maintenance assignment is rejected", "negative", async () => { const result = await rpc("ensure_wartungsvertrag_prepared_contract", users.manager.token, { p_zuordnung_id: 999999999 }); assert.notEqual(result.response.status, 200); });

  await checked("document-specific permission starts both maintenance signatures and reserves distinct 90xxx", "positive", async () => {
    for (const [name, current] of [["A", processA],["B", processB]]) { const started = await rpc("start_prepared_contract_signature", users.manager.token, { p_kommunikationsvorgang_id: current.id, p_expected_revision: current.revision, p_assigned_to: users.manager.id }); assert.equal(started.response.status, 200, `${name}: ${JSON.stringify(started.data)}`); Object.assign(current, started.data); await ensurePositions(users.manager, current); }
    const a = await one("fachnummer_reservierung", `select=*&quelle_art=eq.wartungsvertrag&quelle_id=eq.${fixture.contract_a_id}`); const b = await one("fachnummer_reservierung", `select=*&quelle_art=eq.wartungsvertrag&quelle_id=eq.${fixture.contract_b_id}`);
    numberA = Number(a.nummer); numberB = Number(b.nummer); assert.ok(numberA >= 90000 && numberA <= 99999); assert.ok(numberB >= 90000 && numberB <= 99999); assert.notEqual(numberA, numberB); assert.equal(Number(a.quelle_id), fixture.contract_a_id);
  });
  await checked("signature-start retry retains revision, number, and positions", "positive", async () => { const retry = await rpc("start_prepared_contract_signature", users.manager.token, { p_kommunikationsvorgang_id: processA.id, p_expected_revision: processA.revision, p_assigned_to: users.manager.id }); assert.equal(retry.response.status, 200, JSON.stringify(retry.data)); assert.equal((await one("fachnummer_reservierung", `select=nummer&quelle_art=eq.wartungsvertrag&quelle_id=eq.${fixture.contract_a_id}`)).nummer, numberA); assert.equal((await rows("prepared_contract_signatur", `select=id&kommunikationsvorgang_id=eq.${processA.id}`)).length, 3); });
  await checked("early discard keeps its newly allocated 90xxx permanently reserved", "positive", async () => {
    const prepared = await rpc("ensure_wartungsvertrag_prepared_contract", users.manager.token, { p_zuordnung_id: fixture.assignment_abort_id }); assert.equal(prepared.response.status, 200, JSON.stringify(prepared.data));
    assert.equal((await rows("fachnummer_reservierung", `select=nummer&quelle_art=eq.wartungsvertrag&quelle_id=eq.${fixture.contract_abort_id}`)).length, 0);
    const started = await rpc("start_prepared_contract_signature", users.manager.token, { p_kommunikationsvorgang_id: prepared.data.id, p_expected_revision: prepared.data.revision, p_assigned_to: users.manager.id }); assert.equal(started.response.status, 200, JSON.stringify(started.data));
    numberAbort = Number((await one("fachnummer_reservierung", `select=nummer&quelle_art=eq.wartungsvertrag&quelle_id=eq.${fixture.contract_abort_id}`)).nummer); assert.ok(numberAbort >= 90000 && numberAbort <= 99999); assert.equal(new Set([numberA, numberB, numberAbort]).size, 3);
    const aborted = await service(`/rest/v1/kommunikationsvorgang?id=eq.${started.data.id}`, { method: "PATCH", body: { status: "abgebrochen" } }); assert.equal(aborted.response.status, 204, JSON.stringify(aborted.data));
    assert.equal((await one("kommunikationsvorgang", `select=status&id=eq.${started.data.id}`)).status, "abgebrochen"); assert.equal(Number((await one("fachnummer_reservierung", `select=nummer,quelle_id&quelle_art=eq.wartungsvertrag&quelle_id=eq.${fixture.contract_abort_id}`)).nummer), numberAbort);
  });
  await checked("unauthorised user cannot alter a 90xxx reservation", "negative", async () => { const result = await http(`/rest/v1/fachnummer_reservierung?quelle_art=eq.wartungsvertrag&quelle_id=eq.${fixture.contract_a_id}`, { method: "PATCH", token: users.outsider.token, body: { quelle_id: fixture.contract_b_id } }); assert.notEqual(result.response.status, 204); assert.equal(Number((await one("fachnummer_reservierung", `select=quelle_id&quelle_art=eq.wartungsvertrag&nummer=eq.${numberA}`)).quelle_id), fixture.contract_a_id); });
  await checked("frozen maintenance snapshot rejects later source mutation", "negative", async () => { const result = await service(`/rest/v1/wartungsvertraege?id=eq.${fixture.contract_b_id}`, { method: "PATCH", body: { beschreibung: "manipuliert" } }); assert.equal(result.response.status, 500); assert.equal(result.data.code,"55000"); const unchanged=await one("wartungsvertraege",`select=beschreibung&id=eq.${fixture.contract_b_id}`); assert.equal(unchanged.beschreibung,processB.entwurf_daten.wartungsvertrag_beschreibung); const fresh = await one("kommunikationsvorgang", `select=entwurf_daten&id=eq.${processB.id}`); assert.equal(fresh.entwurf_daten.wartungsvertrag_beschreibung, processB.entwurf_daten.wartungsvertrag_beschreibung); });
  await checked("invalid PNG and wrong signature revision are rejected by the real Edge function", "negative", async () => { const invalid = await upload(users.secondary.token, processB.id, processB.signatur_revision, "mitglied", new Uint8Array([1,2,3])); assert.equal(invalid.response.status, 400); const wrong = await upload(users.secondary.token, processB.id, processB.signatur_revision + 1, "mitglied", pngs.member); assert.equal(wrong.response.status, 409); });
  await checked("completion is blocked while a mandatory signature is missing", "negative", async () => { const fake = await makeLocalFinalDocument(processA, fixture.assignment_a_id, fixture.main_member_id, "case-a"); prematureDocument = fake; const result = await rpc("complete_prepared_wartungsvertrag", users.manager.token, { p_kommunikationsvorgang_id: processA.id, p_dokument_id: fake.id }); assert.equal(result.response.status, 403); assert.equal(result.data.code, "42501"); console.log("KGV_FR2_CX06_CHECKPOINT incomplete-user-denied"); const serviceResult = await serviceRpc("complete_prepared_wartungsvertrag", { p_kommunikationsvorgang_id: processA.id, p_dokument_id: fake.id }); assert.equal(serviceResult.response.status, 400); assert.equal(serviceResult.data.code, "23514"); assert.equal(serviceResult.data.message,"Alle erforderlichen Wartungsvertragsunterschriften müssen vor dem Abschluss vorliegen."); console.log("KGV_FR2_CX06_CHECKPOINT incomplete-service-denied"); const blocked = await service(`/rest/v1/dokument?id=eq.${fake.id}`, { method: "DELETE" }); assert.equal(blocked.response.status, 500); assert.equal(blocked.data.code, "55000"); console.log("KGV_FR2_CX06_CHECKPOINT document-delete-denied"); assert.equal((await one("dokument", `select=id&id=eq.${fake.id}`)).id,fake.id); const stillOpen=await one("kommunikationsvorgang",`select=status,final_dokument_id&id=eq.${processA.id}`); assert.equal(stillOpen.status,"zur_signatur"); assert.equal(stillOpen.final_dokument_id,null); const flow=await one("wartungsvertrag_dokument_workflow",`select=status,dokument_id&wartungsvertrag_zuordnung_id=eq.${fixture.assignment_a_id}`); assert.equal(flow.status,"signatur_vorgang"); assert.equal(flow.dokument_id,null); console.log("KGV_FR2_CX06_CHECKPOINT incomplete-state-unchanged"); });

  await checked("case A stores three real PNG signatures with frozen distinct board signers", "positive", async () => { await signCase(processA, users.main, fixture, users, false); assert.equal((await rpc("prepared_contract_signaturen_vollstaendig", users.manager.token, { p_kommunikationsvorgang_id: processA.id, p_signatur_revision: processA.signatur_revision })).data, true); });
  await checked("case B signs the actual secondary party without an extra main-member slot", "positive", async () => { await signCase(processB, users.secondary, fixture, users, true); const positions = await rows("prepared_contract_signatur", `select=*&kommunikationsvorgang_id=eq.${processB.id}&order=sort_order`); assert.deepEqual(positions.map(x => x.signatur_schluessel), ["mitglied","vorstand_1","vorstand_2"]); assert.equal(positions[0].mitglied_id, fixture.secondary_member_id); assert.equal(new Set(positions.slice(1).map(x => x.mitglied_id)).size, 2); });

  let pdfBytes;
  await checked("v2 artifacts have exact normalized hashes and render a signed maintenance PDF", "positive", async () => {
    const template = new Uint8Array(await readFile(templatePath)); const specText = (await readFile(fieldSpecPath, "utf8")).replace(/\r\n/g, "\n"); const specBytes = new TextEncoder().encode(specText); const spec = JSON.parse(specText);
    assert.equal(await sha256Hex(template), "7fe8ee60173e9634f1f299f24e1e42f8c64e6bc5efd285e5b07d8f0ce027a050"); assert.equal(await sha256Hex(specBytes), "13ce4cd971cd48a4f75bb5f08f325d335ffb6805789d531e320ae775abefb755");
    const positions = await rows("prepared_contract_signatur", `select=*&kommunikationsvorgang_id=eq.${processB.id}&order=sort_order`); const signatures = {}; for (const [key, field] of [["mitglied","unterschrift_mitglied"],["vorstand_1","unterschrift_vorstand_1"],["vorstand_2","unterschrift_vorstand_2"]]) { const p = positions.find(x => x.signatur_schluessel === key); const data = await service(`/storage/v1/object/authenticated/${p.storage_bucket}/${p.storage_path}`, { raw: true }); signatures[field] = `data:image/png;base64,${Buffer.from(data.data).toString("base64")}`; }
    const values = { vertragsnummer: `Vertrags-Nr. ${numberB}`, mitglied_name: "Nora CX06 Nebenmitglied", mitgliedsnummer: String(fixture.secondary_member_number), parzelle_nummer: "-", wartungsvertrag_titel: processB.entwurf_daten.wartungsvertrag_titel, wartungsvertrag_bereich: processB.entwurf_daten.wartungsvertrag_bereich, saisonjahr: "2026", wartungsvertrag_beschreibung: processB.entwurf_daten.wartungsvertrag_beschreibung, arbeitsstundenwirkung: "Befreiung von Pflichtstunden", wartungsvertrag_bemerkung: processB.entwurf_daten.bemerkung, ort_datum: "Zwickau, 14.11.2026", vereinsunterzeichner_1: positions.find(x => x.signatur_schluessel === "vorstand_1").unterzeichner_name_snapshot, vereinsunterzeichner_2: positions.find(x => x.signatur_schluessel === "vorstand_2").unterzeichner_name_snapshot };
    pdfBytes = await drawPdfTemplate({ PDFDocument, PDFName, StandardFonts, rgb }, template, spec.fields, values, signatures, "none"); await mkdir(dirname(pdfPath), { recursive: true }); await writeFile(pdfPath, pdfBytes);
    const pdf = await PDFDocument.load(pdfBytes); const source = await PDFDocument.load(template); assert.equal(pdf.getPageCount(), 2); for (const page of pdf.getPages()) { assert.ok(Math.abs(page.getWidth()-595.28)<1); assert.ok(Math.abs(page.getHeight()-841.89)<1); const annotations = page.node.get(PDFName.of("Annots")); assert.ok(!annotations || annotations.size() === 0, "PDF page contains annotations"); } assert.equal(pdf.catalog.get(PDFName.of("AcroForm")), undefined); assert.ok(imageCount(pdf)-imageCount(source) >= 3);
  });

  let documentA, documentB;
  await checked("wrong member and wrong revision final-document metadata are rejected atomically", "negative", async () => {
    // B has no document yet. Each malformed row is inserted, tested and rolled back
    // separately, preserving uniqueness and the immutable document guard.
    const before=await one("kommunikationsvorgang", `select=status,final_dokument_id&id=eq.${processB.id}`);
    const flowBefore=await one("wartungsvertrag_dokument_workflow", `select=status,dokument_id&wartungsvertrag_zuordnung_id=eq.${fixture.assignment_b_id}`);
    assert.equal(before.status,"zur_signatur"); assert.equal(before.final_dokument_id,null);
    assert.equal((await rows("dokument", `select=id&source_kommunikationsvorgang_id=eq.${processB.id}`)).length,0);
    fr2RejectInvalidMetadata(processB,fixture.main_member_id,processB.signatur_revision,"wrong-member");
    fr2RejectInvalidMetadata(processB,fixture.secondary_member_id,processB.signatur_revision+1,"wrong-revision");
    assert.equal((await rows("dokument", `select=id&source_kommunikationsvorgang_id=eq.${processB.id}`)).length,0);
    assert.deepEqual(await one("kommunikationsvorgang", `select=status,final_dokument_id&id=eq.${processB.id}`),before);
    assert.deepEqual(await one("wartungsvertrag_dokument_workflow", `select=status,dokument_id&wartungsvertrag_zuordnung_id=eq.${fixture.assignment_b_id}`),flowBefore);
    assert.equal((await one("kommunikationsvorgang", `select=status&id=eq.${processA.id}`)).status, "zur_signatur");
  });
  await checked("service-only completion binds both local-only final documents and workflows", "positive", async () => { assert.ok(prematureDocument); documentA = prematureDocument; documentB = await makeLocalFinalDocument(processB, fixture.assignment_b_id, fixture.secondary_member_id, "case-b"); for (const [process, document, assignment] of [[processA,documentA,fixture.assignment_a_id],[processB,documentB,fixture.assignment_b_id]]) { const done = await serviceRpc("complete_prepared_wartungsvertrag", { p_kommunikationsvorgang_id: process.id, p_dokument_id: document.id }); assert.equal(done.response.status, 200, JSON.stringify(done.data)); const workflow = await one("wartungsvertrag_dokument_workflow", `select=*&wartungsvertrag_zuordnung_id=eq.${assignment}`); assert.equal(workflow.status, "abgeschlossen"); assert.equal(workflow.dokument_id, document.id); assert.equal((await one("kommunikationsvorgang", `select=status,final_dokument_id&id=eq.${process.id}`)).status, "erledigt"); } });
  await checked("exact completion retry is idempotent and conflicting completion is rejected", "negative", async () => { const retry = await serviceRpc("complete_prepared_wartungsvertrag", { p_kommunikationsvorgang_id: processB.id, p_dokument_id: documentB.id }); assert.equal(retry.response.status, 200); assert.equal(retry.data[0].bereits_abgeschlossen, true); const conflict = await serviceRpc("complete_prepared_wartungsvertrag", { p_kommunikationsvorgang_id: processB.id, p_dokument_id: documentA.id }); assert.notEqual(conflict.response.status, 200); assert.equal((await rows("wartungsvertrag_dokument_workflow", `select=id&wartungsvertrag_zuordnung_id=eq.${fixture.assignment_b_id}`)).length, 1); });

  await checked("normal successor assignment keeps the same wartungsvertrag 90xxx", "positive", async () => { const insert = await service("/rest/v1/wartungsvertrag_zuordnungen", { method: "POST", headers: { Prefer: "return=representation" }, body: { wartungsvertrag_id: fixture.contract_a_id, hauptmitglied_id: fixture.main_member_id, vertragspartei_mitglied_id: fixture.main_member_id, gueltig_ab: "2027-01-01", gueltig_bis: "2027-12-31", vorgaenger_zuordnung_id: fixture.assignment_a_id, bemerkung: "CX06 reine Nummerninvariante" } }); assert.equal(insert.response.status, 201, JSON.stringify(insert.data)); assert.equal((await one("v_wartungsvertrag_fachnummer", `select=*&wartungsvertrag_id=eq.${fixture.contract_a_id}`)).vertragsnummer, numberA); });
  await checked("direct workflow write is denied to an unauthorised user", "negative", async () => { const result = await http("/rest/v1/wartungsvertrag_dokument_workflow", { method: "POST", token: users.outsider.token, body: { wartungsvertrag_zuordnung_id: fixture.assignment_a_id, status: "abgeschlossen" } }); assert.notEqual(result.response.status, 201); });

  report = { suite: "CX-06 real maintenance contract document workflow", ...counters, complete: true, png_upload_requests: uploads.requests, unique_png_paths: uploads.paths.size, png_read_backs: uploads.reads, contract_numbers: [numberA, numberB], discarded_contract_number: numberAbort, main_party_process_id: processA.id, secondary_party_process_id: processB.id, pdf_path: pdfPath, pdf_sha256: createHash("sha256").update(pdfBytes).digest("hex"), external_storage: false, final_document_fixture: "clearly local-only metadata", cases };
  console.log(`CX06_RESULT ${JSON.stringify(report)}`);
} catch (error) {
  report = { suite: "CX-06 real maintenance contract document workflow", ...counters, complete: false, error: error.message, cases }; console.error(`CX06_RESULT ${JSON.stringify(report)}`); process.exitCode = 1;
} finally { if (resultPath && report) { await mkdir(dirname(resultPath), { recursive: true }); await writeFile(resultPath, `${JSON.stringify(report, (key, value) => ["error", "message"].includes(key) ? undefined : value, 2)}\n`, "utf8"); } }
