import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { ExportColumn } from "../../models/export/export-column";
import type { ExportResult } from "../../models/export/export-result";
import { resolveSupabaseConfig } from "../../lib/supabase/client-context";
import { loadClubConfiguration } from "../configuration/club-configuration-service";
import { exportColumnLabel, formatExportValue } from "./export-output-service";

// Binding layout contract: margins, typography and the repeating club header are also
// the values used by KGV.Core.Utilities.ExportPdfBuilder for generic export PDFs.
export const exportPdfDocumentDefinition = { page: "A4-landscape", margin: 28, clubFont: 12, metaFont: 8, titleFont: 14, headerFont: 8, cellFont: 8, accent: "#2e7d32" } as const;
const escapePdf = (value: string) => value.replaceAll("\\", "\\\\").replaceAll("(", "\\(").replaceAll(")", "\\)").replace(/[\r\n]+/g, " ");

function createPdf(lines: string[]) {
  let pageLines: string[] = []; const pages: string[][] = [];
  for (const line of lines) { pageLines.push(line); if (pageLines.length === 48) { pages.push(pageLines); pageLines = []; } } if (pageLines.length || !pages.length) pages.push(pageLines);
  const objects: string[] = ["<< /Type /Catalog /Pages 2 0 R >>", ""]; const pageIds: number[] = [];
  for (const page of pages) { const content = `BT /F1 8 Tf 28 570 Td ${page.map((line, index) => `${index ? "0 -11 Td " : ""}(${escapePdf(line)}) Tj`).join(" ")} ET`; const contentId = objects.length + 2; pageIds.push(objects.length + 1); objects.push(`<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 ${objects.length + 3} 0 R >> >> /Contents ${contentId} 0 R >>`, `<< /Length ${content.length} >>\nstream\n${content}\nendstream`, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"); }
  objects[1] = `<< /Type /Pages /Kids [${pageIds.map((id) => `${id} 0 R`).join(" ")}] /Count ${pageIds.length} >>`;
  let pdf = "%PDF-1.4\n"; const offsets = [0]; objects.forEach((object, index) => { offsets.push(pdf.length); pdf += `${index + 1} 0 obj\n${object}\nendobj\n`; }); const xref = pdf.length;
  pdf += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n${offsets.slice(1).map((offset) => `${String(offset).padStart(10, "0")} 00000 n \n`).join("")}trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF`;
  return new Blob([pdf], { type: "application/pdf" });
}

export type CentralExportRequest = { export_key: string; format: "pdf" | "csv" | "xlsx"; parameters: Record<string, unknown>; columns: string[]; orientation: "auto" | "portrait" | "landscape" };

function responseFileName(response: Response, fallback: string) {
  const header = response.headers.get("content-disposition") ?? "";
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(header)?.[1];
  const quoted = /filename="?([^";]+)"?/i.exec(header)?.[1];
  let name = fallback;
  try { name = encoded ? decodeURIComponent(encoded) : quoted || fallback; } catch { name = quoted || fallback; }
  return name.replace(/[\\/:*?"<>|]/g, "_") || fallback;
}

function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob); const link = document.createElement("a"); link.href = url; link.download = fileName; link.click(); URL.revokeObjectURL(url);
}

async function errorMessage(response: Response) {
  const detail = await response.json().catch(() => null) as { message?: string } | null;
  if (response.status === 401) return "Die Anmeldung ist abgelaufen. Bitte erneut anmelden.";
  if (response.status === 403) return "Sie sind nicht berechtigt, diesen Export zu erzeugen.";
  if (response.status === 400) return detail?.message ?? "Die Exportparameter sind ungültig.";
  return "Der zentrale PDF-Export konnte nicht erzeugt werden. Bitte versuchen Sie es später erneut.";
}

export async function generateExportPdf(club: ClubContext, session: BrowserSession, exportRequest: CentralExportRequest) {
  if (!session.accessToken) throw new Error("Die Anmeldung ist abgelaufen. Bitte erneut anmelden.");
  const { url, publishableKey } = resolveSupabaseConfig(club);
  let response: Response;
  try {
    response = await fetch(`${url}/functions/v1/kgv-generate-export-pdf`, {
      method: "POST",
      headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}`, "Content-Type": "application/json", Accept: "application/pdf" },
      body: JSON.stringify(exportRequest),
    });
  } catch { throw new Error("Der zentrale PDF-Export ist derzeit nicht erreichbar."); }
  if (!response.ok) throw new Error(await errorMessage(response));
  if (response.headers.get("content-type")?.split(";", 1)[0]?.toLowerCase() !== "application/pdf") throw new Error("Der zentrale PDF-Export lieferte keine PDF-Datei.");
  const bytes = await response.arrayBuffer(); const signature = new Uint8Array(bytes);
  if (signature.length < 5 || String.fromCharCode(...signature.slice(0, 5)) !== "%PDF-") throw new Error("Der zentrale PDF-Export lieferte eine ungültige PDF-Datei.");
  const fallback = `${exportRequest.export_key || "Export"}_${new Date().toISOString().slice(0, 10)}.pdf`;
  downloadBlob(new Blob([bytes], { type: "application/pdf" }), responseFileName(response, fallback));
}

export async function downloadExportPdf(club: ClubContext, session: BrowserSession, fileName: string, result: ExportResult, columns: ExportColumn[]) {
  if (!result.definition.erlaubt_pdf) throw new Error("PDF-Ausgabe ist für diesen Export nicht erlaubt.");
  const { draft } = await loadClubConfiguration(club, session); const clubHeader = [draft.vereinsname, draft.registerangabe, draft.standard_email ? `E-Mail: ${draft.standard_email}` : "", [draft.strasse, [draft.plz, draft.ort].filter(Boolean).join(" ")].filter(Boolean).join(", ")].filter((value): value is string => Boolean(value));
  const header = columns.map(exportColumnLabel).join(" | ");
  const lines = [...clubHeader, "", result.definition.titel ?? result.definition.export_key, `Stand: ${new Date().toLocaleDateString("de-DE")}`, "", header, ...result.rows.map((row) => columns.map((column) => formatExportValue(row[column.column_key])).join(" | "))];
  downloadBlob(createPdf(lines), fileName);
}
