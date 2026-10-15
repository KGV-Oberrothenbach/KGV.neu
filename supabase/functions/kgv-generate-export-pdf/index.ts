import { createClient } from "npm:@supabase/supabase-js@2";
import { PDFDocument, StandardFonts, rgb, type PDFFont, type PDFPage, type PDFEmbeddedPage } from "npm:pdf-lib@1.17.1";

const cors = { "Access-Control-Allow-Origin": "*", "Access-Control-Allow-Headers": "authorization, apikey, content-type", "Access-Control-Allow-Methods": "POST, OPTIONS" };
const json = (status: number, body: unknown) => new Response(JSON.stringify(body), { status, headers: { ...cors, "Content-Type": "application/json" } });
const pageWidth = 841.89;
const pageHeight = 595.28;
const margin = 24;
const contentWidth = pageWidth - margin * 2;
type ExportRequest = { export_key?: string; parameters?: { aktiv_filter?: string; sortierung?: string } };
type MemberRow = Record<string, unknown>;

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

async function loadOptionalBackground(admin: ReturnType<typeof createClient>): Promise<Uint8Array | null> {
  // This is a deliberately optional extension point. No catalog entry or an
  // unusable template always results in the normal white A4 page.
  try {
    const { data } = await admin.from("pdf_template_catalog").select("storage_bucket,storage_path").eq("template_key", "export_mitgliederliste").eq("status", "published").eq("aktiv", true).limit(1).maybeSingle();
    if (!data) return null;
    const { data: blob, error } = await admin.storage.from(data.storage_bucket).download(data.storage_path);
    if (error || !blob) return null;
    return new Uint8Array(await blob.arrayBuffer());
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

function memberCells(row: MemberRow) {
  const address = [text(row.strasse_hsnr) || text(row.adresse), [text(row.plz), text(row.ort)].filter(Boolean).join(" ")].filter(Boolean).join("\n");
  const contact = [text(row.telefon), text(row.handy) || text(row.mobil)].filter(Boolean).join("\n");
  return [text(row.mitgliedsnr) || text(row.id), [text(row.name), text(row.vorname)].filter(Boolean).join(", "), address, contact, text(row.email), date(row.geburtsdatum), date(row.mitglied_seit), text(row.gartennummern) || text(row.gaerten), mark(row.wa ?? row.whatsapp_einwilligung), mark(row.re ?? row.email_rechnung_einwilligung), mark(row.info ?? row.email_info_einwilligung), mark(row.app_vorhanden ?? row.app), mark(row.aktiv_export)];
}

async function buildMembersPdf(rows: MemberRow[], admin: ReturnType<typeof createClient>) {
  const pdf = await PDFDocument.create();
  const font = await pdf.embedFont(StandardFonts.Helvetica);
  const bold = await pdf.embedFont(StandardFonts.HelveticaBold);
  const backgroundBytes = await loadOptionalBackground(admin);
  let background: PDFEmbeddedPage | null = null;
  if (backgroundBytes) {
    try { background = (await pdf.embedPdf(backgroundBytes, [0]))[0] ?? null; } catch { background = null; }
  }
  const headers = ["Nr.", "Mitglied", "Adresse", "Kontakt", "E-Mail", "Geb.", "Seit", "Garten", "WA", "RE", "Info", "App", "Aktiv"];
  const widths = [31, 76, 100, 75, 104, 45, 45, 52, 25, 25, 28, 27, 32];
  const size = 6.7; const lineHeight = 8.1; const headerHeight = 18;
  let page: PDFPage; let y = 0; let pageNumber = 0;
  const startPage = () => {
    pageNumber++; page = pdf.addPage([pageWidth, pageHeight]);
    if (background) page.drawPage(background, { x: 0, y: 0, width: pageWidth, height: pageHeight });
    page.drawText("Mitgliederliste", { x: margin, y: pageHeight - margin - 12, size: 14, font: bold, color: rgb(0.12, 0.2, 0.18) });
    page.drawText(`Erstellt am ${date(today())}   |   Seite ${pageNumber}`, { x: margin, y: pageHeight - margin - 25, size: 7, font, color: rgb(0.3, 0.3, 0.3) });
    y = pageHeight - margin - 48;
    let x = margin;
    headers.forEach((header, i) => { page.drawRectangle({ x, y: y - headerHeight, width: widths[i], height: headerHeight, color: rgb(0.9, 0.92, 0.9), borderColor: rgb(0.72, 0.75, 0.72), borderWidth: 0.35 }); page.drawText(header, { x: x + 2, y: y - 12, size: 6.3, font: bold }); x += widths[i]; });
    y -= headerHeight;
  };
  startPage();
  for (const row of rows) {
    const cells = memberCells(row); const wrapped = cells.map((cell, i) => wrap(cell, font, size, widths[i] - 4));
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
    if (body.export_key !== "mitgliederliste") return json(400, { message: "Unbekannter export_key. Unterstützt wird derzeit nur mitgliederliste." });
    const parameters = normalizeParameters(body.parameters);
    const auth = await authenticate(req);
    const { data, error } = await auth.userClient.rpc("rpc_execute_export", { p_source_name: "rpc_export_mitgliederliste", p_parameters: parameters });
    if (error) {
      if (error.code === "42501") return json(403, { message: "Keine Berechtigung für diesen Export." });
      throw error;
    }
    const pdf = await buildMembersPdf((data ?? []) as MemberRow[], auth.admin);
    const fileName = `Mitgliederliste_${today()}.pdf`;
    return new Response(pdf, { status: 200, headers: { ...cors, "Content-Type": "application/pdf", "Content-Disposition": `attachment; filename=\"${fileName}\"` } });
  } catch (error) {
    const message = error instanceof Error ? error.message : "PDF-Export konnte nicht erzeugt werden.";
    if (message.startsWith("AUTH:")) return json(401, { message: message.slice(5) });
    if (message.startsWith("PARAM:")) return json(400, { message: message.slice(6) });
    return json(500, { message: "PDF-Export konnte nicht erzeugt werden.", detail: message });
  }
});
