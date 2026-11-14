// CX-01 real local E2E: GoTrue sessions, PostgREST RPCs, Edge Function upload,
// private Storage download, and immutable signature persistence.
import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import { dirname } from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { deflateSync } from "node:zlib";

const apiUrl = required("P11_LOCAL_API_URL").replace(/\/$/, "");
const anonKey = required("P11_LOCAL_ANON_KEY");
const serviceKey = required("P11_LOCAL_SERVICE_ROLE_KEY");
const dbUrl = required("P11_LOCAL_DB_URL");
const resultPath = process.env.CX01_RESULT_PATH;
const fixturePath = fileURLToPath(new URL("./cx01-signature-workflow.fixture.sql", import.meta.url));
const bucket = "prepared-contract-signatures";
const counters = { passed: 0, failed: 0, positive_passed: 0, negative_passed: 0 };
const uploads = { successful_requests: 0, unique_paths: new Set(), read_back: 0 };
const cases = [];

function required(name) {
  const value = process.env[name];
  if (!value) throw new Error(`${name} is required`);
  return value;
}

async function checked(name, kind, action) {
  try {
    await action();
    counters.passed++;
    counters[`${kind}_passed`]++;
    cases.push({ name, kind, status: "passed" });
    console.log(`ok - ${name}`);
  } catch (error) {
    counters.failed++;
    cases.push({ name, kind, status: "failed", message: error.message });
    console.error(`not ok - ${name}: ${error.message}`);
    throw error;
  }
}

async function http(path, { method = "GET", token, body, key = anonKey, headers = {} } = {}) {
  const response = await fetch(`${apiUrl}${path}`, {
    method,
    headers: {
      apikey: key,
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(body === undefined ? {} : { "Content-Type": "application/json" }),
      ...headers,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const contentType = response.headers.get("content-type") ?? "";
  const data = contentType.includes("application/json") ? await response.json() : await response.arrayBuffer();
  return { response, data };
}

async function signup(label) {
  const email = `cx01.${label}@example.test`;
  const { response, data } = await http("/auth/v1/signup", {
    method: "POST",
    body: { email, password: "CX01-local-test-password-2026!" },
  });
  assert.equal(response.status, 200, `signup ${label}: ${JSON.stringify(data)}`);
  assert.match(data.access_token ?? "", /^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$/);
  assert.match(data.user?.id ?? "", /^[0-9a-f-]{36}$/);
  return { id: data.user.id, token: data.access_token };
}

function psql(args, input) {
  const command = process.platform === "win32" ? "docker" : "psql";
  const commandArgs = process.platform === "win32"
    ? ["exec", "-i", "supabase_db_03_Arbeitsstand", "psql", "-U", "postgres", "-d", "postgres", "-X", ...args]
    : [dbUrl, "-X", ...args];
  const result = spawnSync(command, commandArgs, { encoding: "utf8", input });
  if (result.status !== 0) throw new Error(`psql failed:\n${result.stdout}\n${result.stderr}`);
  return result.stdout;
}

async function createFixture(users) {
  const sql = await readFile(fixturePath, "utf8");
  const args = ["-q", "-t", "-A", "-v", "ON_ERROR_STOP=1"];
  for (const [name, user] of Object.entries(users)) args.push("-v", `${name}_user_id=${user.id}`);
  const output = psql(args, sql);
  const jsonLine = output.split(/\r?\n/).map(line => line.trim()).findLast(line => line.startsWith("{"));
  if (!jsonLine) throw new Error(`fixture returned no JSON: ${output}`);
  return JSON.parse(jsonLine);
}

function dbExec(sql) {
  psql(["-q", "-v", "ON_ERROR_STOP=1", "-c", sql]);
}

async function rpc(name, token, body) {
  return http(`/rest/v1/rpc/${name}`, { method: "POST", token, body });
}

function one(value) {
  return Array.isArray(value) ? value[0] : value;
}

function crc32(bytes) {
  let crc = 0xffffffff;
  for (const byte of bytes) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ (0xedb88320 & -(crc & 1));
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function pngChunk(type, data) {
  const typeBytes = Buffer.from(type, "ascii");
  const output = Buffer.alloc(12 + data.length);
  output.writeUInt32BE(data.length, 0);
  typeBytes.copy(output, 4);
  data.copy(output, 8);
  output.writeUInt32BE(crc32(Buffer.concat([typeBytes, data])), 8 + data.length);
  return output;
}

function makePng(red, green, blue) {
  const width = 120;
  const height = 40;
  const header = Buffer.alloc(13);
  header.writeUInt32BE(width, 0);
  header.writeUInt32BE(height, 4);
  header.set([8, 6, 0, 0, 0], 8);
  const rows = [];
  for (let y = 0; y < height; y++) {
    const row = Buffer.alloc(1 + width * 4);
    for (let x = 0; x < width; x++) {
      const curve = Math.round(20 + 7 * Math.sin(x / 8) + 3 * Math.sin(x / 3));
      const ink = (x >= 4 && x <= 115 && Math.abs(y - curve) <= 1) || (x >= 18 && x <= 108 && Math.abs(y - 31) <= 1);
      if (ink) row.set([red, green, blue, 255], 1 + x * 4);
    }
    rows.push(row);
  }
  return Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    pngChunk("IHDR", header),
    pngChunk("IDAT", deflateSync(Buffer.concat(rows))),
    pngChunk("IEND", Buffer.alloc(0)),
  ]);
}

function signature(bytes) {
  return { base64: bytes.toString("base64"), sha256: createHash("sha256").update(bytes).digest("hex"), bytes };
}

async function upload(token, processId, revision, key, image) {
  const result = await http("/functions/v1/upload-prepared-contract-signature", {
    method: "POST",
    token,
    body: { kommunikationsvorgang_id: processId, signatur_revision: revision, signatur_schluessel: key, png_base64: image.base64 },
  });
  if (result.response.ok) {
    uploads.successful_requests++;
    uploads.unique_paths.add(result.data.storage_path);
  }
  return result;
}

async function save(token, processId, revision, key, memberId, source, uploadResult, hash = uploadResult.sha256) {
  return rpc("speichere_prepared_contract_signatur", token, {
    p_kommunikationsvorgang_id: processId,
    p_signatur_revision: revision,
    p_signatur_schluessel: key,
    p_mitglied_id: memberId,
    p_unterzeichner_name_snapshot: null,
    p_vereinsfunktion_besetzung_id: source?.vereinsfunktion_besetzung_id ?? null,
    p_vertretung_id: source?.vertretung_id ?? null,
    p_vertretene_besetzung_id: source?.vertretene_besetzung_id ?? null,
    p_storage_path: uploadResult.storage_path,
    p_signatur_sha256: hash,
    p_unterzeichnet_am: new Date().toISOString(),
  });
}

async function storageDownload(path, token = serviceKey, key = serviceKey) {
  return http(`/storage/v1/object/authenticated/${bucket}/${path}`, { token, key });
}

let finalReport;
try {
  const users = {};
  await checked("five real local Auth sessions are issued", "positive", async () => {
    for (const label of ["manager", "tenant", "board1", "board2", "outsider"]) users[label] = await signup(label);
    assert.equal(new Set(Object.values(users).map(user => user.id)).size, 5);
  });

  let fixture;
  await checked("isolated lease, members, functions, and assignments are created", "positive", async () => {
    fixture = await createFixture(users);
    assert.notEqual(fixture.board1_member_id, fixture.board2_member_id);
  });

  let processId;
  let revision;
  await checked("prepared lease amendment is created through its RPC", "positive", async () => {
    const { response, data } = await rpc("bereite_signierte_parzellen_vertragsaenderung_vor", users.manager.token, {
      p_vorgaenger_fassung_id: fixture.predecessor_id,
      p_flaeche_qm: 120,
      p_hat_strom: true,
      p_hat_wasser: false,
      p_begruendung: "Anpassung der Gartenfläche und Aufnahme des Stromanschlusses",
    });
    assert.equal(response.status, 200, JSON.stringify(data));
    processId = Number(one(data).kommunikationsvorgang_id);
    assert.ok(Number.isSafeInteger(processId));
    revision = 1;
  });

  await checked("signature start freezes revision and board snapshot", "positive", async () => {
    const started = await rpc("start_prepared_contract_signature", users.manager.token, {
      p_kommunikationsvorgang_id: processId,
      p_expected_revision: revision,
      p_assigned_to: users.manager.id,
    });
    assert.equal(started.response.status, 200, JSON.stringify(started.data));
    const row = one(started.data);
    assert.equal(row.status, "zur_signatur");
    assert.equal(row.signatur_revision, revision);
    assert.ok(row.signatur_gestartet_am);
  });

  let snapshot;
  let board1Source;
  let board2Source;
  await checked("frozen snapshot contains two distinct entitled natural persons", "positive", async () => {
    const result = await rpc("prepared_contract_snapshot_unterzeichner", users.manager.token, {
      p_kommunikationsvorgang_id: processId,
      p_signatur_revision: revision,
    });
    assert.equal(result.response.status, 200, JSON.stringify(result.data));
    snapshot = result.data;
    board1Source = snapshot.find(row => Number(row.mitglied_id) === Number(fixture.board1_member_id));
    board2Source = snapshot.find(row => Number(row.mitglied_id) === Number(fixture.board2_member_id));
    assert.ok(board1Source);
    assert.ok(board2Source);
    assert.notEqual(board1Source.mitglied_id, board2Source.mitglied_id);
    assert.equal(new Set(snapshot.map(row => row.mitglied_id)).size, 2);
  });

  await checked("three required signature positions are prepared through RPCs", "positive", async () => {
    const positions = [
      ["lease.member", 10, "vertragspartner", fixture.tenant_member_id],
      ["vorstand_1", 20, "vereinsunterzeichner", null],
      ["vorstand_2", 30, "vereinsunterzeichner", null],
    ];
    for (const [key, order, type, memberId] of positions) {
      const result = await rpc("ensure_prepared_contract_signatur_position", users.manager.token, {
        p_kommunikationsvorgang_id: processId,
        p_signatur_revision: revision,
        p_signatur_schluessel: key,
        p_sort_order: order,
        p_pflicht: true,
        p_unterzeichner_typ: type,
        p_mitglied_id: memberId,
        p_unterzeichner_name_snapshot: null,
      });
      assert.equal(result.response.status, 200, `${key}: ${JSON.stringify(result.data)}`);
    }
  });

  const tenantPng = signature(makePng(20, 80, 180));
  const board1Png = signature(makePng(20, 150, 70));
  const board2Png = signature(makePng(180, 70, 30));

  await checked("missing JWT is rejected by the Edge Function", "negative", async () => {
    const result = await http("/functions/v1/upload-prepared-contract-signature", { method: "POST", body: {} });
    assert.equal(result.response.status, 401);
  });
  await checked("invalid JWT is rejected by the Edge Function", "negative", async () => {
    const result = await http("/functions/v1/upload-prepared-contract-signature", { method: "POST", token: "invalid.jwt.value", body: {} });
    assert.equal(result.response.status, 401);
  });
  await checked("invalid PNG is rejected", "negative", async () => {
    const result = await upload(users.tenant.token, processId, revision, "lease.member", { base64: Buffer.from("not a png").toString("base64") });
    assert.equal(result.response.status, 400);
  });
  await checked("wrong communication process is rejected", "negative", async () => {
    const result = await upload(users.tenant.token, processId + 999999, revision, "lease.member", tenantPng);
    assert.equal(result.response.status, 409);
  });
  await checked("wrong signature revision is rejected", "negative", async () => {
    const result = await upload(users.tenant.token, processId, revision + 1, "lease.member", tenantPng);
    assert.equal(result.response.status, 409);
  });
  await checked("foreign signature position is rejected", "negative", async () => {
    const result = await upload(users.tenant.token, processId, revision, "vorstand_1", tenantPng);
    assert.equal(result.response.status, 403);
  });
  await checked("club signer outside the frozen snapshot is rejected", "negative", async () => {
    const result = await upload(users.outsider.token, processId, revision, "vorstand_1", board1Png);
    assert.equal(result.response.status, 403);
  });
  await checked("individual permission revocation removes administrative upload authority", "negative", async () => {
    dbExec(`update public.app_user set permission_revocations=524288 where user_id='${users.manager.id}'::uuid`);
    try {
      const result = await upload(users.manager.token, processId, revision, "lease.member", tenantPng);
      assert.equal(result.response.status, 403);
    } finally {
      dbExec(`update public.app_user set permission_revocations=0 where user_id='${users.manager.id}'::uuid`);
    }
  });

  let tenantUpload;
  let board1Upload;
  let board2Upload;
  await checked("three decodable PNGs are uploaded through the real Edge Function", "positive", async () => {
    tenantUpload = (await upload(users.tenant.token, processId, revision, "lease.member", tenantPng)).data;
    board1Upload = (await upload(users.board1.token, processId, revision, "vorstand_1", board1Png)).data;
    const stagedBoard2 = await upload(users.board1.token, processId, revision, "vorstand_2", board2Png);
    assert.equal(stagedBoard2.response.status, 200, JSON.stringify(stagedBoard2.data));
    board2Upload = stagedBoard2.data;
    assert.equal(tenantUpload.sha256, tenantPng.sha256);
    assert.equal(board1Upload.sha256, board1Png.sha256);
    assert.equal(board2Upload.sha256, board2Png.sha256);
    assert.equal(uploads.unique_paths.size, 3);
  });

  await checked("private Storage denies unauthenticated reads", "negative", async () => {
    const result = await storageDownload(tenantUpload.storage_path, null, anonKey);
    assert.ok([400, 401, 403].includes(result.response.status), `status ${result.response.status}`);
  });

  await checked("all private PNGs are read back and byte/hash verified", "positive", async () => {
    for (const [uploadResult, image] of [[tenantUpload, tenantPng], [board1Upload, board1Png], [board2Upload, board2Png]]) {
      const result = await storageDownload(uploadResult.storage_path);
      assert.equal(result.response.status, 200);
      const bytes = Buffer.from(result.data);
      assert.deepEqual(bytes.subarray(0, 8), Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]));
      assert.deepEqual(bytes, image.bytes);
      assert.equal(createHash("sha256").update(bytes).digest("hex"), uploadResult.sha256);
      uploads.read_back++;
    }
  });

  await checked("missing Storage reference is rejected", "negative", async () => {
    const result = await save(users.tenant.token, processId, revision, "lease.member", fixture.tenant_member_id, null, {
      storage_path: `${processId}/${revision}/lease.member/00000000-0000-0000-0000-000000000000.png`,
      sha256: "0".repeat(64),
    });
    assert.notEqual(result.response.status, 200);
  });
  await checked("manipulated SHA-256 is rejected against the uploaded object", "negative", async () => {
    const manipulated = `${tenantUpload.sha256.slice(0, -1)}${tenantUpload.sha256.endsWith("0") ? "1" : "0"}`;
    const result = await save(users.tenant.token, processId, revision, "lease.member", fixture.tenant_member_id, null, tenantUpload, manipulated);
    assert.notEqual(result.response.status, 200);
  });

  let tenantSigned;
  let board1Signed;
  let board2Signed;
  await checked("tenant and first board signature are bound by their real users", "positive", async () => {
    tenantSigned = await save(users.tenant.token, processId, revision, "lease.member", fixture.tenant_member_id, null, tenantUpload);
    board1Signed = await save(users.board1.token, processId, revision, "vorstand_1", fixture.board1_member_id, board1Source, board1Upload);
    assert.equal(tenantSigned.response.status, 200, JSON.stringify(tenantSigned.data));
    assert.equal(board1Signed.response.status, 200, JSON.stringify(board1Signed.data));
  });

  await checked("same natural person cannot bind both board signatures", "negative", async () => {
    const result = await save(users.board1.token, processId, revision, "vorstand_2", fixture.board1_member_id, board1Source, board2Upload);
    assert.notEqual(result.response.status, 200);
  });

  await checked("missing required board signature keeps completeness false", "negative", async () => {
    const result = await rpc("prepared_contract_signaturen_vollstaendig", users.manager.token, {
      p_kommunikationsvorgang_id: processId,
      p_signatur_revision: revision,
    });
    assert.equal(result.response.status, 200, JSON.stringify(result.data));
    assert.equal(result.data, false);
  });

  await checked("second distinct board user reuses the staged object and binds it", "positive", async () => {
    const uploadResult = await upload(users.board2.token, processId, revision, "vorstand_2", board2Png);
    assert.equal(uploadResult.response.status, 200, JSON.stringify(uploadResult.data));
    assert.equal(uploadResult.data.storage_path, board2Upload.storage_path);
    board2Signed = await save(users.board2.token, processId, revision, "vorstand_2", fixture.board2_member_id, board2Source, uploadResult.data);
    assert.equal(board2Signed.response.status, 200, JSON.stringify(board2Signed.data));
  });

  await checked("differing replay is rejected and exact replay is idempotent", "negative", async () => {
    const exact = await save(users.tenant.token, processId, revision, "lease.member", fixture.tenant_member_id, null, tenantUpload);
    assert.equal(exact.response.status, 200, JSON.stringify(exact.data));
    const changedHash = `${tenantUpload.sha256.slice(0, -1)}${tenantUpload.sha256.endsWith("f") ? "e" : "f"}`;
    const changed = await save(users.tenant.token, processId, revision, "lease.member", fixture.tenant_member_id, null, tenantUpload, changedHash);
    assert.notEqual(changed.response.status, 200);
  });

  await checked("persisted rows contain users, members, revision, paths, and frozen sources", "positive", async () => {
    const query = `/rest/v1/prepared_contract_signatur?select=*&kommunikationsvorgang_id=eq.${processId}&signatur_revision=eq.${revision}&order=sort_order`;
    const result = await http(query, { token: serviceKey, key: serviceKey });
    assert.equal(result.response.status, 200, JSON.stringify(result.data));
    assert.equal(result.data.length, 3);
    const byKey = Object.fromEntries(result.data.map(row => [row.signatur_schluessel, row]));
    for (const row of result.data) {
      assert.equal(row.status, "unterschrieben");
      assert.equal(row.signatur_revision, revision);
      assert.equal(row.storage_bucket, bucket);
      assert.ok(uploads.unique_paths.has(row.storage_path));
    }
    assert.equal(byKey["lease.member"].mitglied_id, fixture.tenant_member_id);
    assert.equal(byKey["lease.member"].unterzeichnet_von_auth_user_id, users.tenant.id);
    assert.equal(byKey.vorstand_1.mitglied_id, fixture.board1_member_id);
    assert.equal(byKey.vorstand_1.unterzeichnet_von_auth_user_id, users.board1.id);
    assert.equal(byKey.vorstand_1.vereinsfunktion_besetzung_id, board1Source.vereinsfunktion_besetzung_id);
    assert.equal(byKey.vorstand_2.mitglied_id, fixture.board2_member_id);
    assert.equal(byKey.vorstand_2.unterzeichnet_von_auth_user_id, users.board2.id);
    assert.equal(byKey.vorstand_2.vereinsfunktion_besetzung_id, board2Source.vereinsfunktion_besetzung_id);
  });

  await checked("all required signatures are complete", "positive", async () => {
    const result = await rpc("prepared_contract_signaturen_vollstaendig", users.manager.token, {
      p_kommunikationsvorgang_id: processId,
      p_signatur_revision: revision,
    });
    assert.equal(result.response.status, 200, JSON.stringify(result.data));
    assert.equal(result.data, true);
  });

  finalReport = {
    suite: "CX-01 real local signature workflow",
    passed: counters.passed,
    failed: counters.failed,
    positive_passed: counters.positive_passed,
    negative_passed: counters.negative_passed,
    successful_upload_requests: uploads.successful_requests,
    unique_pngs_uploaded: uploads.unique_paths.size,
    pngs_read_back: uploads.read_back,
    signatures_bound: 3,
    kommunikationsvorgang_id: processId,
    signatur_revision: revision,
    tenant_member_id: fixture.tenant_member_id,
    parcel_id: fixture.parcel_id,
    complete: true,
    cases,
  };
  console.log(`CX01_RESULT ${JSON.stringify(finalReport)}`);
} catch (error) {
  finalReport = {
    suite: "CX-01 real local signature workflow",
    passed: counters.passed,
    failed: counters.failed || 1,
    positive_passed: counters.positive_passed,
    negative_passed: counters.negative_passed,
    successful_upload_requests: uploads.successful_requests,
    unique_pngs_uploaded: uploads.unique_paths.size,
    pngs_read_back: uploads.read_back,
    complete: false,
    error: error.message,
    cases,
  };
  console.error(`CX01_RESULT ${JSON.stringify(finalReport)}`);
  process.exitCode = 1;
} finally {
  if (resultPath && finalReport) {
    await mkdir(dirname(resultPath), { recursive: true });
    await writeFile(resultPath, `${JSON.stringify(finalReport, null, 2)}\n`, "utf8");
  }
}
