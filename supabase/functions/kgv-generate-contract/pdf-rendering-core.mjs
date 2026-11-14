const clean = (value) => typeof value === "string" ? value.trim() : "";

export async function sha256Hex(bytes) {
  const digest = await crypto.subtle.digest("SHA-256", bytes);
  return Array.from(new Uint8Array(digest), (value) => value.toString(16).padStart(2, "0")).join("");
}

function parseFieldSpec(input) {
  if (input?.schema_version !== undefined && input.schema_version !== "kgv-pdf-field-spec/v1") return null;
  if (input?.coordinate_system !== undefined && input.coordinate_system !== "top-left-points") return null;
  if (input?.settings?.coordinateOrigin !== "topLeft" || !Array.isArray(input.fields) || !input.fields.length) return null;
  const names = new Set();
  const fields = [];
  for (const candidate of input.fields) {
    const name = typeof candidate?.name === "string" ? candidate.name.trim() : "";
    const type = candidate?.type === "text" && candidate?.flags?.multiline === true ? "multiline" : candidate?.type;
    const rect = candidate?.rect;
    if (!name || names.has(name) || !["text", "multiline", "checkbox", "signature"].includes(type)
      || !Number.isInteger(candidate?.page) || candidate.page < 1 || !rect
      || ![rect.x, rect.y, rect.w, rect.h].every(Number.isFinite)
      || rect.x < 0 || rect.y < 0 || rect.w <= 0 || rect.h <= 0) return null;
    names.add(name);
    fields.push({ name, type, page: candidate.page, font_size: candidate.font_size, rect });
  }
  return fields;
}

const templateFailure = (descriptor, stage, message) => new Error(`PDF-Vorlage '${descriptor.templateKey}' – Stage '${stage}': ${message}`);

export function createSupabaseTemplateStore(admin) {
  return {
    async loadActiveCatalog(templateKey) {
      const { data, error } = await admin.from("pdf_template_catalog")
        .select("template_key,version,storage_bucket,storage_path,content_type,checksum,field_spec_path,field_spec_checksum,field_spec_version,renderer_version,status,aktiv,created_at")
        .eq("template_key", templateKey).eq("status", "published").eq("aktiv", true)
        .order("created_at", { ascending: false }).limit(1).maybeSingle();
      if (error) throw new Error(`Der PDF-Template-Katalog konnte nicht geladen werden: ${error.message}`);
      return data;
    },
    async download(bucket, path) {
      const { data, error } = await admin.storage.from(bucket).download(path);
      if (error || !data) throw new Error(error?.message ?? "keine Daten");
      return new Uint8Array(await data.arrayBuffer());
    },
  };
}

export async function loadPublishedPdfTemplate(store, descriptor) {
  let catalog;
  try { catalog = await store.loadActiveCatalog(descriptor.templateKey); }
  catch (error) { throw templateFailure(descriptor, "catalog-load", error.message); }
  if (!catalog) throw templateFailure(descriptor, "catalog-load", "Keine aktive veröffentlichte Version ist hinterlegt.");
  if (catalog.content_type !== "application/pdf"
    || !descriptor.expectedFieldSpecVersions.includes(catalog.field_spec_version)
    || catalog.renderer_version !== descriptor.expectedRendererVersion) {
    throw templateFailure(descriptor, "compatibility-check", `Die veröffentlichte ${descriptor.displayName}-Vorlage ist nicht mit dem unterstützten Renderer kompatibel.`);
  }
  let pdfBytes;
  let fieldSpecBytes;
  try { [pdfBytes, fieldSpecBytes] = await Promise.all([store.download(catalog.storage_bucket, catalog.storage_path), store.download(catalog.storage_bucket, catalog.field_spec_path)]); }
  catch (error) { throw templateFailure(descriptor, "download", `Die ${descriptor.displayName}-Vorlagenartefakte konnten nicht aus dem privaten Template-Bucket geladen werden: ${error.message}`); }
  if (await sha256Hex(pdfBytes) !== clean(catalog.checksum).toLowerCase()
    || await sha256Hex(fieldSpecBytes) !== clean(catalog.field_spec_checksum).toLowerCase()) {
    throw templateFailure(descriptor, "checksum", "Die Prüfsumme stimmt nicht mit dem Template-Katalog überein.");
  }
  if (pdfBytes.length < 5 || new TextDecoder().decode(pdfBytes.subarray(0, 5)) !== "%PDF-") throw templateFailure(descriptor, "pdf-parse", "Die Vorlagendatei ist kein PDF.");
  let parsed;
  try { parsed = JSON.parse(new TextDecoder().decode(fieldSpecBytes)); }
  catch { throw templateFailure(descriptor, "field-spec-parse", "Die Feldspezifikation ist kein gültiges JSON."); }
  const fields = parseFieldSpec(parsed);
  if (!fields) throw templateFailure(descriptor, "field-spec-parse", "Die Feldspezifikation ist grundlegend ungültig.");
  if ((descriptor.requiredFieldsByVersion[catalog.field_spec_version] ?? []).some((name) => !fields.some((field) => field.name === name))) {
    throw templateFailure(descriptor, "compatibility-check", `Die ${descriptor.displayName}-Feldspezifikation enthält nicht alle erforderlichen Felder.`);
  }
  return { pdfBytes, fields, fieldSpecVersion: catalog.field_spec_version, catalog };
}

export const leaseV4SignatureFields = {
  "lease.member": "unterschrift_paechter_1",
  "lease.second_party": "unterschrift_paechter_2",
  vorstand_1: "unterschrift_vorstand_1",
  vorstand_2: "unterschrift_vorstand_2",
};

const boardSnapshot = (position) => {
  const name = clean(position?.unterzeichner_name_snapshot);
  const functionName = clean(position?.vereinsfunktion_bezeichnung_snapshot);
  if (!name || !functionName) throw new Error("Der gespeicherte Vorstandssnapshot ist unvollständig.");
  const represented = clean(position.vertretene_funktion_bezeichnung_snapshot);
  return [name, functionName, position.vertretung_id !== null && represented ? `in Vertretung für ${represented}` : ""].filter(Boolean).join("\n");
};

const pngDataUrl = (bytes) => {
  let binary = "";
  for (let offset = 0; offset < bytes.length; offset += 0x8000) binary += String.fromCharCode(...bytes.subarray(offset, offset + 0x8000));
  return `data:image/png;base64,${btoa(binary)}`;
};

export function createSupabaseLeaseSignatureStore(admin) {
  return {
    async loadProcess(id) {
      const { data, error } = await admin.from("kommunikationsvorgang")
        .select("id,vorgangstyp,dokumenttyp,mitglied_id,parzelle_id,revision,signatur_revision")
        .eq("id", id).maybeSingle();
      if (error) throw error;
      return data;
    },
    async signaturesComplete(id, revision) {
      const { data, error } = await admin.rpc("prepared_contract_signaturen_vollstaendig", { p_kommunikationsvorgang_id: id, p_signatur_revision: revision });
      if (error) throw error;
      return data === true;
    },
    async loadSignedPositions(id, revision) {
      const { data, error } = await admin.from("prepared_contract_signatur")
        .select("signatur_schluessel,status,storage_bucket,storage_path,signatur_sha256,unterzeichner_name_snapshot,vereinsfunktion_bezeichnung_snapshot,vertretung_id,vertretene_funktion_bezeichnung_snapshot")
        .eq("kommunikationsvorgang_id", id).eq("signatur_revision", revision).eq("status", "unterschrieben");
      if (error) throw error;
      return data ?? [];
    },
    async download(bucket, path) {
      const { data, error } = await admin.storage.from(bucket).download(path);
      if (error || !data) throw new Error(error?.message ?? "keine Daten");
      return new Uint8Array(await data.arrayBuffer());
    },
  };
}

export async function loadPersistentLeaseV4Signatures(store, kommunikationsvorgangId, memberId, parcelId, expectedRevision = null) {
  const vorgang = await store.loadProcess(kommunikationsvorgangId);
  if (!vorgang || vorgang.vorgangstyp !== "vertrag_signatur" || vorgang.dokumenttyp !== "pachtvertrag"
    || Number(vorgang.mitglied_id) !== memberId || Number(vorgang.parzelle_id) !== parcelId
    || !Number.isSafeInteger(vorgang.signatur_revision)) throw new Error("Der Pachtvertrags-Signaturvorgang ist ungültig.");
  const revision = Number(vorgang.signatur_revision);
  if (expectedRevision !== null && Number(expectedRevision) !== revision) throw new Error("Die angeforderte Vertragsrevision stimmt nicht mit der eingefrorenen Signaturrevision überein.");
  if (!await store.signaturesComplete(kommunikationsvorgangId, revision)) throw new Error("Alle Pflichtsignaturen des Pachtvertrags müssen vor der Finalisierung vorliegen.");
  const positions = new Map((await store.loadSignedPositions(kommunikationsvorgangId, revision)).map((item) => [item.signatur_schluessel, item]));
  for (const key of ["lease.member", "vorstand_1", "vorstand_2"]) if (!positions.has(key)) throw new Error("Für die PV-v4-Finalisierung fehlen erforderliche persistente Signaturen.");
  const signatures = {};
  for (const [key, field] of Object.entries(leaseV4SignatureFields)) {
    const position = positions.get(key);
    if (!position) continue;
    if (clean(position.storage_bucket) !== "prepared-contract-signatures" || !clean(position.storage_path) || !/^[0-9a-f]{64}$/i.test(clean(position.signatur_sha256))) throw new Error(`Persistente Signatur '${key}' enthält keine gültige Storage-Referenz.`);
    let bytes;
    try { bytes = await store.download(position.storage_bucket, position.storage_path); }
    catch { throw new Error(`Signaturdatei '${key}' konnte nicht aus dem privaten Storage gelesen werden.`); }
    if (!bytes.length || await sha256Hex(bytes) !== clean(position.signatur_sha256).toLowerCase()) throw new Error(`Integritätsfehler: Die Signaturdatei '${key}' stimmt nicht mit ihrem SHA-256-Hash überein.`);
    signatures[field] = pngDataUrl(bytes);
  }
  return { revision, signatures, board1Snapshot: boardSnapshot(positions.get("vorstand_1")), board2Snapshot: boardSnapshot(positions.get("vorstand_2")), hasSecondParty: positions.has("lease.second_party") };
}

export function assertLeaseSignaturePartyMatch(hasSecondPartySignature, hasSecondPartyRequest) {
  if (Boolean(hasSecondPartySignature) !== Boolean(hasSecondPartyRequest)) throw new Error("Die persistierten Signaturen passen nicht zu den eingefrorenen Vertragsparteien.");
}

function decodePngDataUrl(value) {
  if (!value?.startsWith("data:image/png;base64,")) return null;
  return Uint8Array.from(atob(value.split(",", 2)[1]), (char) => char.charCodeAt(0));
}

function wrapLine(line, font, size, maxWidth) {
  if (!line) return [""];
  const words = line.trim().split(/\s+/);
  const result = [];
  let current = "";
  for (const word of words) {
    const candidate = current ? `${current} ${word}` : word;
    if (font.widthOfTextAtSize(candidate, size) <= maxWidth) { current = candidate; continue; }
    if (current) result.push(current);
    if (font.widthOfTextAtSize(word, size) <= maxWidth) { current = word; continue; }
    let fragment = "";
    for (const character of word) {
      const next = fragment + character;
      if (fragment && font.widthOfTextAtSize(next, size) > maxWidth) { result.push(fragment); fragment = character; }
      else fragment = next;
    }
    current = fragment;
  }
  if (current) result.push(current);
  return result;
}

export function layoutMultilineText(value, font, width, height, configuredSize) {
  const preferred = Number.isFinite(Number(configuredSize)) && Number(configuredSize) >= 5 && Number(configuredSize) <= 32
    ? Number(configuredSize) : value.length > 55 ? 6.2 : 7.2;
  for (let size = preferred; size >= 5; size = Math.round((size - 0.2) * 10) / 10) {
    const lines = value.split(/\r?\n/).flatMap((line) => wrapLine(line, font, size, Math.max(1, width - 3.6)));
    if (lines.length <= Math.max(1, Math.floor(height / (size + 1)))) return { lines, size };
  }
  throw new Error("Der vollständige Text passt nicht in das vorgesehene PDF-Feld.");
}

function drawSecondPrivatePartyLabels(pdf, role, font, pdfLib) {
  const { rgb } = pdfLib;
  const page = pdf.getPages()[0];
  if (!page) return;
  const party = role === "representative" ? "gesetzliche/r Vertreter/in" : role === "secondary" ? "Mitpächter/in" : "";
  if (!party) return;
  const drawText = (x, y, label) => { if (label) page.drawText(label, { x, y: page.getHeight() - y - 8, size: 7, font, color: rgb(.25, .28, .27) }); };
  drawText(42, 302, `Name, Vorname ${party}`); drawText(302.64, 302, `Mitgliedsnummer ${party}`);
  drawText(42, 330, `Anschrift ${party}`); drawText(302.64, 330, `Telefon / E-Mail ${party}`);
  const signaturePage = pdf.getPages()[2];
  if (signaturePage) signaturePage.drawText(`Unterschrift ${party}`, { x: 42, y: signaturePage.getHeight() - 618, size: 8, font, color: rgb(.25, .28, .27) });
}

export async function drawPdfTemplate(pdfLib, templateBytes, specs, values, signatures, secondPrivatePartyRole = "representative") {
  const { PDFDocument, PDFName, StandardFonts, rgb } = pdfLib;
  const pdf = await PDFDocument.load(templateBytes);
  const font = await pdf.embedFont(StandardFonts.Helvetica);
  pdf.getPages().forEach((page) => page.node.delete(PDFName.of("Annots")));
  pdf.catalog.delete(PDFName.of("AcroForm"));
  for (const field of specs) {
    const page = pdf.getPages()[field.page - 1];
    if (!page) throw new Error(`PDF-Feld '${field.name}' verweist auf eine fehlende Seite.`);
    const { x, y, w, h } = field.rect;
    const pdfY = page.getHeight() - y - h;
    const signature = signatures[field.name];
    if (signature) {
      const png = decodePngDataUrl(signature);
      if (!png) throw new Error(`Signaturfeld '${field.name}' enthält kein gültiges PNG-Datenformat.`);
      const image = await pdf.embedPng(png);
      const scale = Math.min(w / image.width, h / image.height);
      page.drawImage(image, { x: x + (w - image.width * scale) / 2, y: pdfY + (h - image.height * scale) / 2, width: image.width * scale, height: image.height * scale });
      continue;
    }
    if (field.type === "signature") continue;
    const value = values[field.name] ?? "";
    if (field.type === "checkbox") {
      page.drawRectangle({ x, y: pdfY, width: w, height: h, borderWidth: 0.6, borderColor: rgb(0, 0, 0) });
      if (value === "true") page.drawText("X", { x: x + 1, y: pdfY + 0.5, size: Math.max(6, h - 1), font });
      continue;
    }
    if (!value) continue;
    if (field.type === "multiline") {
      let layout;
      try { layout = layoutMultilineText(value, font, w, h, field.font_size); }
      catch { throw new Error(`PDF-Feld '${field.name}' würde unvollständig oder abgeschnitten dargestellt.`); }
      layout.lines.forEach((line, index) => page.drawText(line, { x: x + 1.8, y: pdfY + h - layout.size - index * (layout.size + 1), size: layout.size, font, color: rgb(0, 0, 0) }));
    } else {
      const configuredSize = Number(field.font_size);
      let size = Number.isFinite(configuredSize) && configuredSize >= 5 && configuredSize <= 32 ? configuredSize : value.length > 55 ? 6.2 : 7.2;
      const maxWidth = Math.max(1, w - 3.6);
      while (size > 5 && font.widthOfTextAtSize(value, size) > maxWidth) size = Math.max(5, Math.round((size - .2) * 10) / 10);
      if (font.widthOfTextAtSize(value, size) > maxWidth) throw new Error(`PDF-Feld '${field.name}' würde unvollständig oder abgeschnitten dargestellt.`);
      page.drawText(value, { x: x + 1.8, y: pdfY + h - size, size, font, color: rgb(0, 0, 0) });
    }
  }
  drawSecondPrivatePartyLabels(pdf, secondPrivatePartyRole, font, pdfLib);
  return await pdf.save();
}
