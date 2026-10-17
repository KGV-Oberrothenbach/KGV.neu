import { createClient } from "npm:@supabase/supabase-js@2";
import { PDFDocument, StandardFonts, rgb, type PDFFont, type PDFPage, type PDFEmbeddedPage } from "npm:pdf-lib@1.17.1";

const cors = { "Access-Control-Allow-Origin": "*", "Access-Control-Allow-Headers": "authorization, apikey, content-type", "Access-Control-Allow-Methods": "POST, OPTIONS" };
const json = (status: number, body: unknown) => new Response(JSON.stringify(body), { status, headers: { ...cors, "Content-Type": "application/json" } });
const pageWidth = 841.89;
const pageHeight = 595.28;
const margin = 24;
const contentWidth = pageWidth - margin * 2;
type ExportRequest = { export_key?: string; format?: string; parameters?: Record<string, unknown>; columns?: unknown; orientation?: string };
type MemberRow = Record<string, unknown>;
type MemberColumn = { key: string; header: string; baseWidth: number; flexWeight?: number; value: (row: MemberRow) => string };
type ExportDefinition = { export_key: string; titel: string | null; quelle_typ: string | null; quelle_name: string | null; aktiv: boolean; erlaubt_pdf: boolean };
type ExportColumn = { column_key: string; label_kurz: string | null; label_lang: string | null; sortierung: number; standard_sichtbar: boolean; ist_sortierspalte: boolean };
type PdfTemplateCatalogRecord = { template_key: string; version: string; storage_bucket: string; storage_path: string; content_type: string; checksum: string; field_spec_path: string; field_spec_checksum: string; field_spec_version: string; renderer_version: string; status: string; aktiv: boolean; created_at: string };
type TemplateField = { name: string; type: "text" | "multiline"; page: number; rect: { x: number; y: number; w: number; h: number } };
type LoadedPdfTemplate = { pdfBytes: Uint8Array; fields: TemplateField[] };

const text = (value: unknown) => typeof value === "string" ? value.trim() : value === null || value === undefined ? "" : String(value);
const mark = (value: unknown) => value === true || text(value).toLowerCase() === "true" || text(value) === "1" || text(value).toLowerCase() === "ja" ? "X" : "";
const date = (value: unknown) => { const raw = text(value).slice(0, 10); return /^\d{4}-\d{2}-\d{2}$/.test(raw) ? `${raw.slice(8, 10)}.${raw.slice(5, 7)}.${raw.slice(0, 4)}` : ""; };
const today = () => new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Berlin", year: "numeric", month: "2-digit", day: "2-digit" }).format(new Date());

function normalizeParameters(input: ExportRequest["parameters"]) {
  const aktiv = text(input?.aktiv_filter).toLowerCase() || "aktiv";
  const sort = text(input?.sortierung).toLowerCase() || "name";
  if (!["alle", "aktiv", "inaktiv"].includes(aktiv)) throw new Error("PARAM:aktiv_filter muss alle, aktiv oder inaktiv sein.");
  if (!["name", "mitgliedsnr", "garten"].includes(sort)) throw new Error("PARAM:sortierung muss name, mitgliedsnr oder garten sein.");
  return { aktiv_filter: aktiv, sortierung: sort };
}

function isJsonValue(value: unknown): boolean {
  if (value === null || typeof value === "string" || typeof value === "number" || typeof value === "boolean") return true;
  if (Array.isArray(value)) return value.every(isJsonValue);
  return typeof value === "object" && Object.values(value as Record<string, unknown>).every(isJsonValue);
}

function normalizeRequest(body: ExportRequest) {
  const exportKey = text(body.export_key).toLowerCase();
  if (!exportKey) throw new Error("PARAM:export_key ist erforderlich.");
  const format = text(body.format).toLowerCase() || "pdf";
  if (format !== "pdf") throw new Error("PARAM:Der zentrale Export unterstützt derzeit nur das Format pdf.");
  const orientation = text(body.orientation).toLowerCase() || "auto";
  if (!["auto", "portrait", "landscape"].includes(orientation)) throw new Error("PARAM:orientation muss auto, portrait oder landscape sein.");
  if (!body.parameters || Array.isArray(body.parameters) || !isJsonValue(body.parameters)) throw new Error("PARAM:parameters muss ein JSON-Objekt sein.");
  if (body.columns === undefined || body.columns === null) return { exportKey, parameters: body.parameters, columns: [], orientation };
  if (!Array.isArray(body.columns)) throw new Error("PARAM:columns muss eine Spaltenliste sein.");
  if (!body.columns.every((column) => typeof column === "string" && column.trim())) throw new Error("PARAM:columns darf nur nichtleere Spaltenschlüssel enthalten.");
  const columns = [...new Set(body.columns.map((column) => column.trim().toLowerCase()))];
  return { exportKey, parameters: body.parameters, columns, orientation };
}

async function loadExportMetadata(admin: ReturnType<typeof createClient>, exportKey: string) {
  const { data: definition, error } = await admin.from("app_export_definition").select("export_key,titel,quelle_typ,quelle_name,aktiv,erlaubt_pdf").eq("export_key", exportKey).maybeSingle();
  if (error || !definition) throw new Error("PARAM:EXPORT_NOT_FOUND");
  const item = definition as ExportDefinition;
  if (!item.aktiv || item.quelle_typ !== "rpc" || !item.quelle_name || !item.erlaubt_pdf || !text(item.titel)) throw new Error("PARAM:EXPORT_NOT_AVAILABLE");
  const { data: columns, error: columnsError } = await admin.from("app_export_column_definition").select("column_key,label_kurz,label_lang,sortierung,standard_sichtbar,ist_sortierspalte").eq("export_key", exportKey).order("sortierung");
  if (columnsError) throw new Error("PARAM:EXPORT_COLUMNS_NOT_AVAILABLE");
  return { definition: item, columns: (columns ?? []) as ExportColumn[] };
}

function selectedColumns(requested: string[], definitions: ExportColumn[]) {
  const known = new Map(definitions.map((column) => [column.column_key, column]));
  if (requested.some((key) => !known.has(key))) throw new Error("PARAM:columns enthält einen unbekannten Spaltenschlüssel.");
  const selected = requested.length ? requested.map((key) => known.get(key)!) : definitions.filter((column) => column.standard_sichtbar);
  if (!selected.length) throw new Error("PARAM:Keine PDF-Spalten konfiguriert.");
  return selected;
}

function genericValue(value: unknown) {
  if (value === null || value === undefined) return "";
  if (typeof value === "boolean") return value ? "X" : "";
  const raw = text(value); const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(raw.slice(0, 10));
  return match ? `${match[3]}.${match[2]}.${match[1]}` : raw;
}

function templateKeyForExport(exportKey: string, orientation: string) {
  // `export_mitgliederliste` was the former single-template integration. New
  // template-backed exports are registered in this orientation-aware catalog.
  const mappings: Record<string, Record<string, string>> = {
    zaehler_eichfaelligkeit: {
      portrait: "export-zaehler-eichfaelligkeit-hoch",
      landscape: "export-zaehler-eichfaelligkeit-quer",
    },
  };
  return mappings[exportKey]?.[orientation] ?? null;
}

async function sha256(bytes: Uint8Array) {
  const digest = await crypto.subtle.digest("SHA-256", bytes);
  return Array.from(new Uint8Array(digest), (value) => value.toString(16).padStart(2, "0")).join("");
}

function parseTemplateFields(input: unknown): TemplateField[] | null {
  const spec = input as { schema_version?: unknown; coordinate_system?: unknown; settings?: { coordinateOrigin?: unknown }; fields?: unknown };
  if (spec.schema_version !== "kgv-pdf-field-spec/v1" || spec.coordinate_system !== "top-left-points" || spec.settings?.coordinateOrigin !== "topLeft" || !Array.isArray(spec.fields)) return null;
  const names = new Set<string>(); const fields: TemplateField[] = [];
  for (const candidate of spec.fields) {
    const field = candidate as TemplateField;
    const name = text(field?.name); const rect = field?.rect;
    if (!name || names.has(name) || !["text", "multiline"].includes(field?.type) || !Number.isInteger(field?.page) || field.page !== 1 || !rect || ![rect.x, rect.y, rect.w, rect.h].every(Number.isFinite) || rect.x < 0 || rect.y < 0 || rect.w <= 0 || rect.h <= 0) return null;
    names.add(name); fields.push({ name, type: field.type, page: field.page, rect });
  }
  return fields;
}

async function loadPublishedTemplate(admin: ReturnType<typeof createClient>, templateKey: string): Promise<LoadedPdfTemplate> {
  const { data, error } = await admin.from("pdf_template_catalog").select("template_key,version,storage_bucket,storage_path,content_type,checksum,field_spec_path,field_spec_checksum,field_spec_version,renderer_version,status,aktiv,created_at").eq("template_key", templateKey).eq("status", "published").eq("aktiv", true).order("created_at", { ascending: false }).limit(1).maybeSingle();
  if (error) throw new Error(`TEMPLATE:Katalog konnte nicht geladen werden: ${error.message}`);
  const catalog = data as PdfTemplateCatalogRecord | null;
  if (!catalog) throw new Error(`TEMPLATE:Keine aktive veröffentlichte Vorlage für ${templateKey}.`);
  if (catalog.content_type !== "application/pdf" || catalog.renderer_version !== "top-left/v1") throw new Error(`TEMPLATE:Vorlage ${templateKey} ist nicht renderer-kompatibel.`);
  const [templateResult, fieldSpecResult] = await Promise.all([admin.storage.from(catalog.storage_bucket).download(catalog.storage_path), admin.storage.from(catalog.storage_bucket).download(catalog.field_spec_path)]);
  if (templateResult.error || !templateResult.data || fieldSpecResult.error || !fieldSpecResult.data) throw new Error(`TEMPLATE:Vorlage ${templateKey} konnte nicht vollständig geladen werden.`);
  const [pdfBytes, fieldSpecBytes] = await Promise.all([templateResult.data.arrayBuffer().then((value) => new Uint8Array(value)), fieldSpecResult.data.arrayBuffer().then((value) => new Uint8Array(value))]);
  if ((await sha256(pdfBytes)) !== text(catalog.checksum).toLowerCase() || (await sha256(fieldSpecBytes)) !== text(catalog.field_spec_checksum).toLowerCase()) throw new Error(`TEMPLATE:Prüfsumme der Vorlage ${templateKey} stimmt nicht.`);
  let fields: TemplateField[] | null = null;
  try { fields = parseTemplateFields(JSON.parse(new TextDecoder().decode(fieldSpecBytes))); } catch { /* handled below */ }
  if (!fields || ["exporttitel", "stand", "exporttabelle"].some((name) => !fields!.some((field) => field.name === name))) throw new Error(`TEMPLATE:Feldspezifikation der Vorlage ${templateKey} ist ungültig.`);
  return { pdfBytes, fields };
}

async function buildTemplateTablePdf(rows: MemberRow[], definition: ExportDefinition, columns: ExportColumn[], orientation: string, template: LoadedPdfTemplate) {
  const pdf = await PDFDocument.create(); const font = await pdf.embedFont(StandardFonts.Helvetica); const bold = await pdf.embedFont(StandardFonts.HelveticaBold);
  const background = (await pdf.embedPdf(template.pdfBytes, [0]))[0]; const width = background.width; const height = background.height;
  const titleField = template.fields.find((field) => field.name === "exporttitel")!; const standField = template.fields.find((field) => field.name === "stand")!; const tableField = template.fields.find((field) => field.name === "exporttabelle")!;
  const wishes = columns.map((column) => Math.max(34, font.widthOfTextAtSize(column.label_kurz || column.label_lang || column.column_key, 7) + 10, ...rows.slice(0, 40).map((row) => Math.min(150, font.widthOfTextAtSize(genericValue(row[column.column_key]), 6.7) + 8))));
  const widths = wishes.map((value) => tableField.rect.w * value / wishes.reduce((sum, item) => sum + item, 0)); widths[widths.length - 1] += tableField.rect.w - widths.reduce((sum, item) => sum + item, 0);
  const toPdfY = (field: TemplateField) => height - field.rect.y - field.rect.h;
  let page: PDFPage; let y = 0; let pageNumber = 0; const headerHeight = 18; const lineHeight = 8.1; const tableBottom = toPdfY(tableField);
  const startPage = () => { pageNumber++; page = pdf.addPage([width, height]); page.drawPage(background, { x: 0, y: 0, width, height }); page.drawText(definition.titel || definition.export_key, { x: titleField.rect.x, y: toPdfY(titleField) + 4, size: 14, font: bold }); page.drawText(`Erstellt am ${date(today())}   |   Seite ${pageNumber}`, { x: standField.rect.x, y: toPdfY(standField) + 3, size: 7, font }); y = height - tableField.rect.y; let x = tableField.rect.x; columns.forEach((column, index) => { page.drawRectangle({ x, y: y - headerHeight, width: widths[index], height: headerHeight, color: rgb(.9, .92, .9), borderColor: rgb(.72, .75, .72), borderWidth: .35 }); page.drawText(column.label_kurz || column.label_lang || column.column_key, { x: x + 2, y: y - 12, size: 6.3, font: bold }); x += widths[index]; }); y -= headerHeight; };
  startPage();
  for (const row of rows) { const cells = columns.map((column, index) => wrap(genericValue(row[column.column_key]), font, 6.7, widths[index] - 4)); const rowHeight = Math.max(14, ...cells.map((lines) => lines.length * lineHeight + 5)); if (y - rowHeight < tableBottom) startPage(); let x = tableField.rect.x; cells.forEach((lines, index) => { page.drawRectangle({ x, y: y - rowHeight, width: widths[index], height: rowHeight, borderColor: rgb(.8, .82, .8), borderWidth: .25 }); lines.forEach((line, lineIndex) => page.drawText(line, { x: x + 2, y: y - 9 - lineIndex * lineHeight, size: 6.7, font })); x += widths[index]; }); y -= rowHeight; }
  return { pdf: await pdf.save(), orientation };
}

async function buildGenericTablePdf(rows: MemberRow[], definition: ExportDefinition, columns: ExportColumn[], requestedOrientation: string, admin: ReturnType<typeof createClient>) {
  const fontProbe = await PDFDocument.create(); const probeFont = await fontProbe.embedFont(StandardFonts.Helvetica);
  const wishes = columns.map((column) => Math.max(34, probeFont.widthOfTextAtSize(column.label_kurz || column.label_lang || column.column_key, 7) + 10, ...rows.slice(0, 40).map((row) => Math.min(150, probeFont.widthOfTextAtSize(genericValue(row[column.column_key]), 6.7) + 8))));
  const portraitContent = 595.28 - margin * 2;
  const orientation = requestedOrientation === "auto" ? (wishes.reduce((sum, width) => sum + width, 0) <= portraitContent ? "portrait" : "landscape") : requestedOrientation;
  const width = orientation === "portrait" ? 595.28 : 841.89; const height = orientation === "portrait" ? 841.89 : 595.28; const usable = width - margin * 2;
  const widths = wishes.map((value) => usable * value / wishes.reduce((sum, item) => sum + item, 0)); widths[widths.length - 1] += usable - widths.reduce((sum, item) => sum + item, 0);
  const templateKey = templateKeyForExport(definition.export_key, orientation);
  if (templateKey) return buildTemplateTablePdf(rows, definition, columns, orientation, await loadPublishedTemplate(admin, templateKey));
  const pdf = await PDFDocument.create(); const font = await pdf.embedFont(StandardFonts.Helvetica); const bold = await pdf.embedFont(StandardFonts.HelveticaBold);
  let page: PDFPage; let y = 0; let pageNumber = 0; const headerHeight = 18; const lineHeight = 8.1;
  const startPage = () => { pageNumber++; page = pdf.addPage([width, height]); page.drawText(definition.titel || definition.export_key, { x: margin, y: height - margin - 12, size: 14, font: bold }); page.drawText(`Erstellt am ${date(today())}   |   Seite ${pageNumber}`, { x: margin, y: height - margin - 25, size: 7, font }); y = height - margin - 48; let x = margin; columns.forEach((column, index) => { page.drawRectangle({ x, y: y - headerHeight, width: widths[index], height: headerHeight, color: rgb(.9, .92, .9), borderColor: rgb(.72, .75, .72), borderWidth: .35 }); page.drawText(column.label_kurz || column.label_lang || column.column_key, { x: x + 2, y: y - 12, size: 6.3, font: bold }); x += widths[index]; }); y -= headerHeight; };
  startPage(); for (const row of rows) { const cells = columns.map((column, index) => wrap(genericValue(row[column.column_key]), font, 6.7, widths[index] - 4)); const rowHeight = Math.max(14, ...cells.map((lines) => lines.length * lineHeight + 5)); if (y - rowHeight < margin) startPage(); let x = margin; cells.forEach((lines, index) => { page.drawRectangle({ x, y: y - rowHeight, width: widths[index], height: rowHeight, borderColor: rgb(.8, .82, .8), borderWidth: .25 }); lines.forEach((line, lineIndex) => page.drawText(line, { x: x + 2, y: y - 9 - lineIndex * lineHeight, size: 6.7, font })); x += widths[index]; }); y -= rowHeight; }
  return { pdf: await pdf.save(), orientation };
}

async function authenticate(req: Request) {
  const url = Deno.env.get("SUPABASE_URL") ?? "";
  const serviceKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
  const authorization = req.headers.get("Authorization") ?? "";
  const jwt = authorization.replace(/^Bearer\s+/i, "");
  const admin = createClient(url, serviceKey, { auth: { persistSession: false } });
  const { data, error } = await admin.auth.getUser(jwt);
  if (error || !data.user) throw new Error("AUTH:Anmeldung ist abgelaufen.");
  // Use this JWT for the export RPC.  Its SECURITY DEFINER gateway performs the
  // canonical role check (assert_export_access), rather than duplicating it here.
  const userClient = createClient(url, Deno.env.get("SUPABASE_ANON_KEY") ?? "", { global: { headers: { Authorization: authorization } }, auth: { persistSession: false, autoRefreshToken: false } });
  return { admin, userClient };
}

// Retained for the older members-list export until it is migrated to a field-spec
// template. Template-backed exports added above always use loadPublishedTemplate.
async function loadOptionalBackground(admin: ReturnType<typeof createClient>): Promise<Uint8Array | null> {
  try {
    const { data } = await admin.from("pdf_template_catalog").select("storage_bucket,storage_path").eq("template_key", "export_mitgliederliste").eq("status", "published").eq("aktiv", true).limit(1).maybeSingle();
    if (!data) return null;
    const { data: blob, error } = await admin.storage.from(data.storage_bucket).download(data.storage_path);
    return error || !blob ? null : new Uint8Array(await blob.arrayBuffer());
  } catch { return null; }
}

function wrap(value: string, font: PDFFont, size: number, width: number) {
  const lines: string[] = [];
  for (const explicitLine of value.split(/\r?\n/)) {
    let line = "";
    for (const word of explicitLine.split(/\s+/).filter(Boolean)) {
      const candidate = line ? `${line} ${word}` : word;
      if (line && font.widthOfTextAtSize(candidate, size) > width) { lines.push(line); line = word; } else line = candidate;
    }
    if (line) lines.push(line);
  }
  return lines.length ? lines : [""];
}

const memberColumns: MemberColumn[] = [
  { key: "mitgliedsnr", header: "Nr.", baseWidth: 31, value: (row) => text(row.mitgliedsnr) || text(row.id) },
  { key: "name", header: "Mitglied", baseWidth: 76, flexWeight: 1.25, value: (row) => [text(row.name), text(row.vorname)].filter(Boolean).join(", ") },
  { key: "strasse_hsnr", header: "Adresse", baseWidth: 100, flexWeight: 1.5, value: (row) => [text(row.strasse_hsnr) || text(row.adresse), [text(row.plz), text(row.ort)].filter(Boolean).join(" ")].filter(Boolean).join("\n") },
  { key: "telefon", header: "Kontakt", baseWidth: 75, flexWeight: 1, value: (row) => [text(row.telefon), text(row.handy) || text(row.mobil)].filter(Boolean).join("\n") },
  { key: "email", header: "E-Mail", baseWidth: 104, flexWeight: 1.4, value: (row) => text(row.email) },
  { key: "geburtsdatum", header: "Geb.", baseWidth: 45, value: (row) => date(row.geburtsdatum) },
  { key: "mitglied_seit", header: "Seit", baseWidth: 45, value: (row) => date(row.mitglied_seit) },
  { key: "gartennummern", header: "Garten", baseWidth: 52, flexWeight: 0.75, value: (row) => text(row.gartennummern) || text(row.gaerten) },
  { key: "wa", header: "WA", baseWidth: 25, value: (row) => mark(row.wa ?? row.whatsapp_einwilligung) },
  { key: "re", header: "RE", baseWidth: 25, value: (row) => mark(row.re ?? row.email_rechnung_einwilligung) },
  { key: "info", header: "Info", baseWidth: 28, value: (row) => mark(row.info ?? row.email_info_einwilligung) },
  { key: "app", header: "App", baseWidth: 27, value: (row) => mark(row.app_vorhanden ?? row.app) },
  { key: "aktiv_export", header: "Aktiv", baseWidth: 32, value: (row) => mark(row.aktiv_export) },
];

function selectedMemberColumns(columnKeys: unknown, sortierung: string) {
  const selected = Array.isArray(columnKeys)
    ? new Set(columnKeys.filter((key): key is string => typeof key === "string").map((key) => key.trim().toLowerCase()))
    : null;
  const sourceKeys: Record<string, string[]> = {
    name: ["name", "vorname"],
    strasse_hsnr: ["strasse_hsnr", "plz", "ort"],
    telefon: ["telefon", "handy", "mobil"],
  };
  const columns = selected?.size
    ? memberColumns.filter((column) => (sourceKeys[column.key] ?? [column.key]).some((key) => selected.has(key)))
    : [...memberColumns];
  if (!columns.length) throw new Error("PARAM:Mindestens eine PDF-Spalte muss ausgewählt sein.");
  const sortColumnKey = sortierung === "mitgliedsnr" ? "mitgliedsnr" : sortierung === "garten" ? "gartennummern" : "name";
  const sortIndex = columns.findIndex((column) => column.key === sortColumnKey);
  if (sortIndex > 0) columns.unshift(columns.splice(sortIndex, 1)[0]);
  return columns;
}

function memberColumnWidths(columns: MemberColumn[]) {
  const widths = columns.map((column) => column.baseWidth);
  const flexibleIndexes = columns.flatMap((column, index) => column.flexWeight ? [index] : []);
  const distributableIndexes = flexibleIndexes.length ? flexibleIndexes : columns.map((_, index) => index);
  const totalWeight = distributableIndexes.reduce((sum, index) => sum + (columns[index].flexWeight ?? 1), 0);
  const remaining = contentWidth - widths.reduce((sum, width) => sum + width, 0);
  distributableIndexes.forEach((index) => { widths[index] += remaining * ((columns[index].flexWeight ?? 1) / totalWeight); });
  widths[widths.length - 1] += contentWidth - widths.reduce((sum, width) => sum + width, 0);
  return widths;
}

async function buildMembersPdf(rows: MemberRow[], admin: ReturnType<typeof createClient>, columnKeys: unknown, sortierung: string) {
  const pdf = await PDFDocument.create();
  const font = await pdf.embedFont(StandardFonts.Helvetica);
  const bold = await pdf.embedFont(StandardFonts.HelveticaBold);
  const backgroundBytes = await loadOptionalBackground(admin);
  let background: PDFEmbeddedPage | null = null;
  if (backgroundBytes) try { background = (await pdf.embedPdf(backgroundBytes, [0]))[0] ?? null; } catch { background = null; }
  const columns = selectedMemberColumns(columnKeys, sortierung);
  const widths = memberColumnWidths(columns);
  const size = 6.7; const lineHeight = 8.1; const headerHeight = 18;
  let page: PDFPage; let y = 0; let pageNumber = 0;
  const startPage = () => {
    pageNumber++; page = pdf.addPage([pageWidth, pageHeight]);
    if (background) page.drawPage(background, { x: 0, y: 0, width: pageWidth, height: pageHeight });
    page.drawText("Mitgliederliste", { x: margin, y: pageHeight - margin - 12, size: 14, font: bold, color: rgb(0.12, 0.2, 0.18) });
    page.drawText(`Erstellt am ${date(today())}   |   Seite ${pageNumber}`, { x: margin, y: pageHeight - margin - 25, size: 7, font, color: rgb(0.3, 0.3, 0.3) });
    y = pageHeight - margin - 48;
    let x = margin;
    columns.forEach((column, i) => { page.drawRectangle({ x, y: y - headerHeight, width: widths[i], height: headerHeight, color: rgb(0.9, 0.92, 0.9), borderColor: rgb(0.72, 0.75, 0.72), borderWidth: 0.35 }); page.drawText(column.header, { x: x + 2, y: y - 12, size: 6.3, font: bold }); x += widths[i]; });
    y -= headerHeight;
  };
  startPage();
  for (const row of rows) {
    const wrapped = columns.map((column, i) => wrap(column.value(row), font, size, widths[i] - 4));
    const rowHeight = Math.max(14, ...wrapped.map((lines) => lines.length * lineHeight + 5));
    if (y - rowHeight < margin) startPage();
    let x = margin;
    wrapped.forEach((lines, i) => { page.drawRectangle({ x, y: y - rowHeight, width: widths[i], height: rowHeight, borderColor: rgb(0.8, 0.82, 0.8), borderWidth: 0.25 }); lines.forEach((line, lineIndex) => page.drawText(line, { x: x + 2, y: y - 9 - lineIndex * lineHeight, size, font })); x += widths[i]; });
    // Bemerkung remains in the document without making the main table unreadable.
    const note = text(row.bemerkung);
    y -= rowHeight;
    if (note) {
      const noteLines = wrap(`Bemerkung: ${note}`, font, 6.3, contentWidth - 8); const noteHeight = noteLines.length * 7.5 + 5;
      if (y - noteHeight < margin) startPage();
      page.drawRectangle({ x: margin, y: y - noteHeight, width: contentWidth, height: noteHeight, color: rgb(0.975, 0.975, 0.96), borderColor: rgb(0.85, 0.85, 0.82), borderWidth: 0.25 });
      noteLines.forEach((line, index) => page.drawText(line, { x: margin + 4, y: y - 8 - index * 7.5, size: 6.3, font })); y -= noteHeight;
    }
  }
  return pdf.save();
}

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: cors });
  if (req.method !== "POST") return json(405, { message: "Methode nicht erlaubt." });
  try {
    const body = await req.json() as ExportRequest;
    const exportRequest = normalizeRequest(body);
    const auth = await authenticate(req);
    const metadata = await loadExportMetadata(auth.admin, exportRequest.exportKey);
    const columns = selectedColumns(exportRequest.columns, metadata.columns);
    const parameters = exportRequest.exportKey === "mitgliederliste" ? normalizeParameters(exportRequest.parameters) : exportRequest.parameters;
    const { data, error } = await auth.userClient.rpc("rpc_execute_export", { p_export_key: exportRequest.exportKey, p_parameters: parameters });
    if (error) {
      if (error.code === "42501") return json(403, { message: "Keine Berechtigung für diesen Export." });
      throw error;
    }
    const rendered = exportRequest.exportKey === "mitgliederliste"
      ? { pdf: await buildMembersPdf((data ?? []) as MemberRow[], auth.admin, exportRequest.columns, normalizeParameters(parameters).sortierung), orientation: "landscape" }
      : await buildGenericTablePdf((data ?? []) as MemberRow[], metadata.definition, columns, exportRequest.orientation, auth.admin);
    const fileName = `${text(metadata.definition.titel).replace(/[^a-z0-9äöüß_-]+/gi, "_") || "Export"}_${today()}.pdf`;
    return new Response(rendered.pdf, { status: 200, headers: { ...cors, "Content-Type": "application/pdf", "Content-Disposition": `attachment; filename=\"${fileName}\"`, "X-KGV-PDF-Orientation": rendered.orientation, "X-KGV-PDF-Orientation-Requested": exportRequest.orientation } });
  } catch (error) {
    const message = error instanceof Error ? error.message : "PDF-Export konnte nicht erzeugt werden.";
    if (message.startsWith("AUTH:")) return json(401, { message: message.slice(5) });
    if (message.startsWith("PARAM:")) return json(400, { message: message.slice(6) });
    return json(500, { message: "PDF-Export konnte nicht erzeugt werden.", detail: message });
  }
});
