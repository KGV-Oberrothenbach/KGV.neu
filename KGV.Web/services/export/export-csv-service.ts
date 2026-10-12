import type { ExportColumn } from "../../models/export/export-column";
import type { ExportResult } from "../../models/export/export-result";
import { exportColumnLabel, formatExportValue } from "./export-output-service";

function csvCell(value: unknown) { const text = formatExportValue(value); return /[;"\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text; }

export function createExportCsv(result: ExportResult, columns: ExportColumn[]) {
  if (!result.definition.erlaubt_csv) throw new Error("CSV-Ausgabe ist für diesen Export nicht erlaubt.");
  return `\ufeff${[columns.map(exportColumnLabel), ...result.rows.map((row) => columns.map((column) => row[column.column_key]))].map((row) => row.map(csvCell).join(";")).join("\r\n")}`;
}

export function downloadExportCsv(fileName: string, result: ExportResult, columns: ExportColumn[]) {
  const url = URL.createObjectURL(new Blob([createExportCsv(result, columns)], { type: "text/csv;charset=utf-8" }));
  const link = document.createElement("a"); link.href = url; link.download = fileName; link.click(); URL.revokeObjectURL(url);
}
