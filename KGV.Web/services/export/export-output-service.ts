import type { ExportColumn } from "../../models/export/export-column";
import type { ExportResult } from "../../models/export/export-result";

export function selectedExportColumns(columns: ExportColumn[], selectedKeys: readonly string[]) {
  const selected = new Set(selectedKeys);
  return [...columns].sort((a, b) => a.sortierung - b.sortierung).filter((column) => selected.has(column.column_key));
}

export function defaultExportColumnKeys(columns: ExportColumn[]) {
  return [...columns].sort((a, b) => a.sortierung - b.sortierung).filter((column) => column.standard_sichtbar).map((column) => column.column_key);
}

export function exportColumnLabel(column: ExportColumn) { return column.label_lang ?? column.label_kurz ?? column.column_key; }

export function formatExportValue(value: unknown) {
  if (value == null) return "";
  if (typeof value === "boolean") return value ? "Ja" : "Nein";
  return String(value);
}

export function exportFileName(result: ExportResult, extension: "csv" | "pdf", date = new Date()) {
  const title = (result.definition.titel ?? result.definition.export_key).normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-zA-Z0-9]+/g, "-").replace(/^-|-$/g, "") || "Export";
  return `${date.toISOString().slice(0, 10)}_${title}.${extension}`;
}
