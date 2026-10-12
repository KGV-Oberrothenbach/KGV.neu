"use client";
import type { ExportColumn } from "../../models/export/export-column";
import { defaultExportColumnKeys, exportColumnLabel } from "../../services/export/export-output-service";

export function ExportColumns({ columns, selectedKeys, onChange }: { columns: ExportColumn[]; selectedKeys: string[]; onChange: (keys: string[]) => void }) {
  const selected = new Set(selectedKeys);
  const ordered = [...columns].sort((a, b) => a.sortierung - b.sortierung);
  const toggle = (key: string) => onChange(ordered.filter((column) => key === column.column_key ? !selected.has(key) : selected.has(column.column_key)).map((column) => column.column_key));
  return <fieldset className="export-columns"><legend>Spalten</legend><div className="editor-actions"><button type="button" className="secondary-action" onClick={() => onChange(ordered.map((column) => column.column_key))}>Alle auswählen</button><button type="button" className="secondary-action" onClick={() => onChange(defaultExportColumnKeys(ordered))}>Standard wiederherstellen</button></div><div>{ordered.map((column) => <label key={column.column_key}><input type="checkbox" checked={selected.has(column.column_key)} onChange={() => toggle(column.column_key)} />{exportColumnLabel(column)}</label>)}</div></fieldset>;
}
