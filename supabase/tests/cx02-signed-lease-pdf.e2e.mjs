// CX-02 real local E2E: published v9 artifacts from private Storage, the
// productive renderer core, persisted PNG signatures, and a signed PDF.
import assert from "node:assert/strict";
import { readFile, writeFile, mkdir } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { spawnSync } from "node:child_process";
import { PDFDict, PDFDocument, PDFName, PDFStream, StandardFonts, rgb } from "pdf-lib";
import {
  assertLeaseSignaturePartyMatch,
  drawPdfTemplate,
  loadPersistentLeaseV4Signatures,
  loadPublishedPdfTemplate,
  sha256Hex,
} from "../functions/kgv-generate-contract/pdf-rendering-core.mjs";

const apiUrl = required("P11_LOCAL_API_URL").replace(/\/$/, "");
const serviceKey = required("P11_LOCAL_SERVICE_ROLE_KEY");
const dbUrl = required("P11_LOCAL_DB_URL");
const cx01ResultPath = required("CX01_RESULT_PATH");
const outputPath = required("CX02_PDF_PATH");
const resultPath = process.env.CX02_RESULT_PATH;
const templatePath = resolve("KGV.Core/Templates/pachtvertrag/v9/template.pdf");
const fieldSpecPath = resolve("KGV.Core/Templates/pachtvertrag/v9/field-spec.json");
const expectedReason = "Anpassung der Gartenfläche und Aufnahme des Stromanschlusses";
const expectedOld = "Bisheriger Vertragsstand: Fläche 100 m²; Strom nein; Wasser nein.";
const expectedNew = `Vertragsänderung (verbindlich): Fläche 120 m²; Strom ja; Wasser nein. Begründung: ${expectedReason}`;
const counters = { passed: 0, failed: 0, positive_passed: 0, negative_passed: 0 };
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

async function request(path, { method = "GET", body, contentType = "application/json", headers = {}, raw = false } = {}) {
  const response = await fetch(`${apiUrl}${path}`, {
    method,
    headers: {
      apikey: serviceKey,
      Authorization: `Bearer ${serviceKey}`,
      ...(body === undefined ? {} : { "Content-Type": contentType }),
      ...headers,
    },
    body: body === undefined ? undefined : body instanceof Uint8Array ? body : contentType === "application/json" ? JSON.stringify(body) : body,
  });
  const responseType = response.headers.get("content-type") ?? "";
  const data = !raw && responseType.includes("application/json") ? await response.json() : new Uint8Array(await response.arrayBuffer());
  if (!response.ok) throw new Error(`${method} ${path} failed (${response.status}): ${JSON.stringify(data)}`);
  return data;
}

async function one(table, query) {
  const rows = await request(`/rest/v1/${table}?${query}`);
  assert.equal(rows.length, 1, `${table}: expected one row, got ${rows.length}`);
  return rows[0];
}

async function uploadTemplateObject(path, bytes, contentType) {
  await request(`/storage/v1/object/pdf-templates/${path}`, {
    method: "POST",
    body: bytes,
    contentType,
    headers: { "x-upsert": "true" },
  });
}

function removeLocalReplayPlaceholders() {
  const sql = `begin;
set local session_replication_role=replica;
delete from storage.objects
 where bucket_id='pdf-templates'
   and name in ('templates/pachtvertrag/v9/template.pdf','templates/pachtvertrag/v9/field-spec.json')
   and metadata->>'local_replay_fixture'='true';
commit;`;
  const command = process.platform === "win32" ? "docker" : "psql";
  const args = process.platform === "win32"
    ? ["exec", "-i", "supabase_db_03_Arbeitsstand", "psql", "-U", "postgres", "-d", "postgres", "-X", "-v", "ON_ERROR_STOP=1"]
    : [dbUrl, "-X", "-v", "ON_ERROR_STOP=1"];
  const result = spawnSync(command, args, { encoding: "utf8", input: sql });
  if (result.status !== 0) throw new Error(`local replay placeholder cleanup failed: ${result.stdout}\n${result.stderr}`);
}

const templateStore = {
  async loadActiveCatalog(templateKey) {
    const rows = await request(`/rest/v1/pdf_template_catalog?select=*&template_key=eq.${templateKey}&status=eq.published&aktiv=eq.true&order=created_at.desc&limit=1`);
    return rows[0] ?? null;
  },
  async download(bucket, path) {
    return request(`/storage/v1/object/authenticated/${bucket}/${path}`, { raw: true });
  },
};

const signatureStore = {
  async loadProcess(id) {
    const rows = await request(`/rest/v1/kommunikationsvorgang?select=id,vorgangstyp,dokumenttyp,mitglied_id,parzelle_id,revision,signatur_revision&id=eq.${id}`);
    return rows[0] ?? null;
  },
  async signaturesComplete(id, revision) {
    return request("/rest/v1/rpc/prepared_contract_signaturen_vollstaendig", {
      method: "POST",
      body: { p_kommunikationsvorgang_id: id, p_signatur_revision: revision },
    });
  },
  async loadSignedPositions(id, revision) {
    return request(`/rest/v1/prepared_contract_signatur?select=signatur_schluessel,status,storage_bucket,storage_path,signatur_sha256,unterzeichner_name_snapshot,vereinsfunktion_bezeichnung_snapshot,vertretung_id,vertretene_funktion_bezeichnung_snapshot&kommunikationsvorgang_id=eq.${id}&signatur_revision=eq.${revision}&status=eq.unterschrieben`);
  },
  async download(bucket, path) {
    return request(`/storage/v1/object/authenticated/${bucket}/${path}`, { raw: true });
  },
};

const descriptor = {
  templateKey: "pachtvertrag",
  expectedFieldSpecVersions: ["pachtvertrag-field-spec/v9"],
  expectedRendererVersion: "top-left/v1",
  requiredFieldsByVersion: {
    "pachtvertrag-field-spec/v9": [
      "vertragsnummer", "verein_name", "vereinsregister", "paechter_name", "mitgliedsnummer",
      "paechter_geburtsdatum", "paechter_anschrift", "vereinsangaben_fusszeile", "vorstand_fusszeile",
      "weitere_vereinbarungen", "unterschrift_paechter_1", "unterschrift_vorstand_1",
      "unterschrift_vorstand_2", "vereinsunterzeichner_1", "vereinsunterzeichner_2",
    ],
  },
  displayName: "Pachtvertrag",
};

function imageCount(pdf) {
  let count = 0;
  for (const page of pdf.getPages()) {
    const resources = page.node.Resources();
    const xObjects = resources?.lookupMaybe(PDFName.of("XObject"), PDFDict);
    if (!xObjects) continue;
    for (const [, value] of xObjects.entries()) {
      const object = pdf.context.lookup(value);
      if (object instanceof PDFStream && object.dict.get(PDFName.of("Subtype"))?.toString() === "/Image") count++;
    }
  }
  return count;
}

async function expectReject(pattern, action) {
  await assert.rejects(action, pattern);
}

let finalReport;
try {
  const cx01 = JSON.parse(await readFile(cx01ResultPath, "utf8"));
  assert.equal(cx01.complete, true, "CX-01 prerequisite is incomplete");
  const processId = Number(cx01.kommunikationsvorgang_id);
  const revision = Number(cx01.signatur_revision);
  const memberId = Number(cx01.tenant_member_id);
  const parcelId = Number(cx01.parcel_id);
  assert.ok([processId, revision, memberId, parcelId].every(Number.isSafeInteger));

  const repositoryTemplate = new Uint8Array(await readFile(templatePath));
  // Git stores this text artifact with LF. Normalize Windows autocrlf checkouts
  // before hashing/uploading so the bytes equal the published v9 checksum.
  const repositoryFieldSpec = new TextEncoder().encode((await readFile(fieldSpecPath, "utf8")).replace(/\r\n/g, "\n"));
  await checked("repository v9 artifacts are uploaded to private local Storage", "positive", async () => {
    removeLocalReplayPlaceholders();
    await uploadTemplateObject("templates/pachtvertrag/v9/template.pdf", repositoryTemplate, "application/pdf");
    await uploadTemplateObject("templates/pachtvertrag/v9/field-spec.json", repositoryFieldSpec, "application/json");
  });

  let loaded;
  await checked("published v9 catalog artifacts load with exact hashes and renderer contract", "positive", async () => {
    loaded = await loadPublishedPdfTemplate(templateStore, descriptor);
    assert.equal(loaded.fieldSpecVersion, "pachtvertrag-field-spec/v9");
    assert.equal(await sha256Hex(loaded.pdfBytes), loaded.catalog.checksum);
    assert.equal(await sha256Hex(repositoryTemplate), loaded.catalog.checksum);
    assert.equal(await sha256Hex(repositoryFieldSpec), loaded.catalog.field_spec_checksum);
  });

  const process = await one("kommunikationsvorgang", `select=id,entwurf_daten,mitglied_id,parzelle_id,revision,signatur_revision&id=eq.${processId}`);
  const member = await one("mitglied", `select=id,vorname,name,mitgliedsnummer,geburtsdatum,adresse,plz,ort,telefon,email&id=eq.${memberId}`);
  const parcel = await one("parzelle", `select=id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser&id=eq.${parcelId}`);
  const reservation = await one("fachnummer_reservierung", `select=nummer,art,quelle_id&quelle_art=eq.kommunikationsvorgang&quelle_id=eq.${processId}&art=eq.pachtvertrag`);
  // The reduced local replay deliberately has no Vereinskonfiguration table;
  // use the immutable identity printed in the v9 source template.
  const configuration = {
    vereinsname: "Kleingartenverein Oberrothenbach e.V.",
    kurzname: "KGV Oberrothenbach e.V.",
    registerangabe: "Amtsgericht Chemnitz VR 70502",
    strasse: "Scheringer Str. 12",
    plz: "08056",
    ort: "Zwickau",
    standard_email: "kgvoberrothenbach@gmx.de",
    dokument_ort: "Zwickau",
  };
  const additionalAgreements = process.entwurf_daten?.draft?.additionalAgreements ?? "";

  await checked("frozen draft contains the complete visible old/new change text", "positive", async () => {
    assert.ok(additionalAgreements.includes(expectedOld), additionalAgreements);
    assert.ok(additionalAgreements.includes(expectedNew), additionalAgreements);
    assert.equal(parcel.flaeche_qm, 100);
    assert.equal(parcel.hat_strom, false);
    assert.equal(parcel.hat_wasser, false);
    assert.ok(Number(reservation.nummer) >= 20000 && Number(reservation.nummer) <= 29999);
  });

  let persistent;
  await checked("three persisted PNG signatures are reloaded and SHA-256 verified", "positive", async () => {
    persistent = await loadPersistentLeaseV4Signatures(signatureStore, processId, memberId, parcelId, revision);
    assert.equal(Object.keys(persistent.signatures).length, 3);
    assert.equal(persistent.hasSecondParty, false);
    assert.match(persistent.board1Snapshot, /CX-01 Vorstand 1/);
    assert.match(persistent.board2Snapshot, /CX-01 Vorstand 2/);
  });

  const values = {
    vertragsnummer: `Vertrags-Nr. ${reservation.nummer}`,
    verein_name: configuration.vereinsname || configuration.kurzname,
    vereinsregister: "VR 70502",
    paechter_name: `${member.vorname} ${member.name}`,
    mitgliedsnummer: String(member.mitgliedsnummer),
    paechter_geburtsdatum: new Intl.DateTimeFormat("de-DE").format(new Date(`${member.geburtsdatum}T12:00:00Z`)),
    paechter_anschrift: `${member.adresse}, ${member.plz} ${member.ort}`,
    zweite_partei_name: "",
    zweite_partei_mitgliedsnummer: "",
    zweite_partei_geburtsdatum: "",
    zweite_partei_anschrift: "",
    vertragsbeginn: "01.01.2026",
    uebergabe_datum: "01.01.2026",
    vereinsangaben_fusszeile: `${configuration.vereinsname || configuration.kurzname}\n${configuration.strasse}, ${configuration.plz} ${configuration.ort}\n${configuration.standard_email}`,
    vorstand_fusszeile: `${persistent.board1Snapshot}\n${persistent.board2Snapshot}`,
    weitere_vereinbarungen: additionalAgreements,
    ort_datum: `${configuration.dokument_ort}, ${new Intl.DateTimeFormat("de-DE").format(new Date())}`,
    vereinsunterzeichner_1: persistent.board1Snapshot,
    vereinsunterzeichner_2: persistent.board2Snapshot,
  };

  let pdfBytes;
  await checked("productive shared renderer creates the signed three-page PDF", "positive", async () => {
    assertLeaseSignaturePartyMatch(persistent.hasSecondParty, false);
    pdfBytes = await drawPdfTemplate({ PDFDocument, PDFName, StandardFonts, rgb }, loaded.pdfBytes, loaded.fields, values, persistent.signatures, "none");
    await mkdir(dirname(outputPath), { recursive: true });
    await writeFile(outputPath, pdfBytes);
    assert.ok(pdfBytes.length > loaded.pdfBytes.length);
  });

  await checked("PDF is A4, annotation/form free, and contains three embedded signature images", "positive", async () => {
    const pdf = await PDFDocument.load(pdfBytes);
    assert.equal(pdf.getPageCount(), 3);
    for (const page of pdf.getPages()) {
      assert.ok(Math.abs(page.getWidth() - 595.28) < 1);
      assert.ok(Math.abs(page.getHeight() - 841.89) < 1);
      const annotations = page.node.get(PDFName.of("Annots"));
      assert.ok(!annotations || annotations.size() === 0, "PDF page contains annotations");
    }
    assert.equal(pdf.catalog.get(PDFName.of("AcroForm")), undefined);
    const source = await PDFDocument.load(loaded.pdfBytes);
    assert.ok(imageCount(pdf) - imageCount(source) >= 3, `embedded image delta ${imageCount(pdf) - imageCount(source)}`);
  });

  await checked("missing mandatory signature is rejected", "negative", async () => {
    await expectReject(/Pflichtsignaturen/, () => loadPersistentLeaseV4Signatures({ ...signatureStore, signaturesComplete: async () => false }, processId, memberId, parcelId, revision));
  });
  await checked("missing private Storage object is rejected", "negative", async () => {
    await expectReject(/privaten Storage/, () => loadPersistentLeaseV4Signatures({ ...signatureStore, download: async () => { throw new Error("missing"); } }, processId, memberId, parcelId, revision));
  });
  await checked("changed PNG bytes versus persisted hash are rejected", "negative", async () => {
    await expectReject(/Integritätsfehler/, () => loadPersistentLeaseV4Signatures({ ...signatureStore, download: async () => new Uint8Array([137, 80, 78, 71, 0]) }, processId, memberId, parcelId, revision));
  });
  await checked("wrong frozen revision is rejected", "negative", async () => {
    await expectReject(/Vertragsrevision/, () => loadPersistentLeaseV4Signatures(signatureStore, processId, memberId, parcelId, revision + 1));
  });
  await checked("mismatched second party and signature set are rejected", "negative", async () => {
    assert.throws(() => assertLeaseSignaturePartyMatch(false, true), /Vertragsparteien/);
  });
  await checked("invalid PDF bytes are rejected", "negative", async () => {
    await expectReject(/PDF|header|parse/i, () => drawPdfTemplate({ PDFDocument, PDFName, StandardFonts, rgb }, new Uint8Array([1, 2, 3]), loaded.fields, values, persistent.signatures, "none"));
  });
  await checked("catalog checksum mismatch is rejected", "negative", async () => {
    const badStore = { ...templateStore, loadActiveCatalog: async (key) => ({ ...(await templateStore.loadActiveCatalog(key)), checksum: "0".repeat(64) }) };
    await expectReject(/Prüfsumme/, () => loadPublishedPdfTemplate(badStore, descriptor));
  });
  await checked("incomplete or clipped change text is rejected", "negative", async () => {
    const tinyFields = loaded.fields.map(field => field.name === "weitere_vereinbarungen" ? { ...field, rect: { ...field.rect, h: 5 } } : field);
    await expectReject(/unvollständig|abgeschnitten/, () => drawPdfTemplate({ PDFDocument, PDFName, StandardFonts, rgb }, loaded.pdfBytes, tinyFields, values, persistent.signatures, "none"));
  });

  finalReport = {
    suite: "CX-02 real signed lease PDF",
    passed: counters.passed,
    failed: counters.failed,
    positive_passed: counters.positive_passed,
    negative_passed: counters.negative_passed,
    kommunikationsvorgang_id: processId,
    signatur_revision: revision,
    contract_number: Number(reservation.nummer),
    template_version: loaded.catalog.version,
    template_checksum: loaded.catalog.checksum,
    field_spec_checksum: loaded.catalog.field_spec_checksum,
    signature_count: Object.keys(persistent.signatures).length,
    pdf_path: outputPath,
    complete: true,
    cases,
  };
  console.log(`CX02_RESULT ${JSON.stringify(finalReport)}`);
} catch (error) {
  finalReport = { suite: "CX-02 real signed lease PDF", ...counters, complete: false, error: error.message, cases };
  console.error(`CX02_RESULT ${JSON.stringify(finalReport)}`);
  process.exitCode = 1;
} finally {
  if (resultPath && finalReport) {
    await mkdir(dirname(resultPath), { recursive: true });
    await writeFile(resultPath, `${JSON.stringify(finalReport, null, 2)}\n`, "utf8");
  }
}
