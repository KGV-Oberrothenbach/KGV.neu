import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { ExportColumn } from "../../models/export/export-column";
import type { ExportResult } from "../../models/export/export-result";
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

export async function downloadExportPdf(club: ClubContext, session: BrowserSession, fileName: string, result: ExportResult, columns: ExportColumn[]) {
  if (!result.definition.erlaubt_pdf) throw new Error("PDF-Ausgabe ist für diesen Export nicht erlaubt.");
  const { draft } = await loadClubConfiguration(club, session); const clubHeader = [draft.vereinsname, draft.registerangabe, draft.standard_email ? `E-Mail: ${draft.standard_email}` : "", [draft.strasse, [draft.plz, draft.ort].filter(Boolean).join(" ")].filter(Boolean).join(", ")].filter((value): value is string => Boolean(value));
  const header = columns.map(exportColumnLabel).join(" | ");
  const lines = [...clubHeader, "", result.definition.titel ?? result.definition.export_key, `Stand: ${new Date().toLocaleDateString("de-DE")}`, "", header, ...result.rows.map((row) => columns.map((column) => formatExportValue(row[column.column_key])).join(" | "))];
  const url = URL.createObjectURL(createPdf(lines)); const link = document.createElement("a"); link.href = url; link.download = fileName; link.click(); URL.revokeObjectURL(url);
}
