import type { ExportColumn } from "../../models/export/export-column";
import type { ExportDefinition } from "../../models/export/export-definition";

type DefinitionCore = Omit<ExportDefinition, "serienbrief_erlaubt" | "serienbrief_mitglied_id_feld" | "serienbrief_parzelle_id_feld">;
type DefinitionSerialLetterMetadata = Pick<ExportDefinition, "export_key" | "serienbrief_erlaubt" | "serienbrief_mitglied_id_feld" | "serienbrief_parzelle_id_feld">;
type ColumnCore = Omit<ExportColumn, "serienbrief_freigegeben" | "serienbrief_gruppe">;
type ColumnSerialLetterMetadata = Pick<ExportColumn, "export_key" | "column_key" | "serienbrief_freigegeben" | "serienbrief_gruppe">;

export function withOptionalDefinitionSerialLetterMetadata(core: DefinitionCore[], metadata: DefinitionSerialLetterMetadata[] | null): ExportDefinition[] {
  const byKey = new Map((metadata ?? []).map(item => [item.export_key, item]));
  return core.map(item => { const optional = byKey.get(item.export_key); return { ...item, serienbrief_erlaubt: optional?.serienbrief_erlaubt === true, serienbrief_mitglied_id_feld: optional?.serienbrief_mitglied_id_feld ?? null, serienbrief_parzelle_id_feld: optional?.serienbrief_parzelle_id_feld ?? null }; });
}

export function withOptionalColumnSerialLetterMetadata(core: ColumnCore[], metadata: ColumnSerialLetterMetadata[] | null): ExportColumn[] {
  const byKey = new Map((metadata ?? []).map(item => [`${item.export_key}\u0000${item.column_key}`, item]));
  return core.map(item => { const optional = byKey.get(`${item.export_key}\u0000${item.column_key}`); return { ...item, serienbrief_freigegeben: optional?.serienbrief_freigegeben === true, serienbrief_gruppe: optional?.serienbrief_gruppe ?? null }; });
}
