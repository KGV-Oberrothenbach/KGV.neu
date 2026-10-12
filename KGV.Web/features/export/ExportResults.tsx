"use client";
import { useState } from "react";
import type { ExportColumn } from "../../models/export/export-column";
import type { ExportResult } from "../../models/export/export-result";
import { exportColumnLabel, formatExportValue } from "../../services/export/export-output-service";

export function ExportResults({ result, columns }: { result: ExportResult; columns: ExportColumn[] }) {
  const [record, setRecord] = useState(0); const preview = result.rows.slice(0, 500); const current = result.rows[record];
  if (!columns.length) return <p className="empty-state">Bitte mindestens eine Spalte auswählen.</p>;
  return <div className="export-result"><div className="export-desktop data-table-wrap"><table><thead><tr>{columns.map((column) => <th key={column.column_key}>{exportColumnLabel(column)}</th>)}</tr></thead><tbody>{preview.map((row, index) => <tr key={index}>{columns.map((column) => <td key={column.column_key}>{formatExportValue(row[column.column_key])}</td>)}</tr>)}</tbody></table></div><section className="export-mobile" aria-label="Datensatzansicht">{current && <><p>{record + 1} von {result.recordCount}</p><dl>{columns.map((column) => <div key={column.column_key}><dt>{exportColumnLabel(column)}</dt><dd>{formatExportValue(current[column.column_key])}</dd></div>)}</dl><div className="editor-actions"><button type="button" className="secondary-action" disabled={record === 0} onClick={() => setRecord(record - 1)}>Vorheriger Datensatz</button><button type="button" className="secondary-action" disabled={record >= result.rows.length - 1} onClick={() => setRecord(record + 1)}>Nächster Datensatz</button></div></>}</section>{result.rows.length > 500 && <p className="empty-state">Vorschau zeigt 500 von {result.recordCount} Datensätzen. CSV und PDF enthalten alle Datensätze.</p>}</div>;
}
