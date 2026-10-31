import { readSupabaseRest } from "../../lib/supabase/rest-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type ExportColumn } from "../../models/export/export-column";
import { type ExportDefinition } from "../../models/export/export-definition";
import { type ExportFilter } from "../../models/export/export-filter";
import { withOptionalColumnSerialLetterMetadata, withOptionalDefinitionSerialLetterMetadata } from "./export-serial-letter-metadata";

type DefinitionCore = Omit<ExportDefinition, "serienbrief_erlaubt" | "serienbrief_mitglied_id_feld" | "serienbrief_parzelle_id_feld">;
type DefinitionSerialLetterMetadata = Pick<ExportDefinition, "export_key" | "serienbrief_erlaubt" | "serienbrief_mitglied_id_feld" | "serienbrief_parzelle_id_feld">;
type ColumnCore = Omit<ExportColumn, "serienbrief_freigegeben" | "serienbrief_gruppe">;
type ColumnSerialLetterMetadata = Pick<ExportColumn, "export_key" | "column_key" | "serienbrief_freigegeben" | "serienbrief_gruppe">;

const definitionCoreSelect = "export_key,titel,beschreibung,quelle_typ,quelle_name,aktiv,standard_sortierung,standard_ausgabe,erlaubt_csv,erlaubt_pdf";
const definitionSerialLetterSelect = "export_key,serienbrief_erlaubt,serienbrief_mitglied_id_feld,serienbrief_parzelle_id_feld";
const filterSelect = "export_key,filter_key,label,typ,optionen_json,parameter_name,wert_typ,standardwert_json,pflicht,sortierung";
const columnCoreSelect = "export_key,column_key,label_kurz,label_lang,sortierung,standard_sichtbar,ist_sortierspalte";
const columnSerialLetterSelect = "export_key,column_key,serienbrief_freigegeben,serienbrief_gruppe";

async function optionalMetadata<T>(load: () => Promise<T[]>): Promise<T[] | null> { try { return await load(); } catch { return null; } }

export async function getActiveExportDefinitions(club: ClubContext, session: BrowserSession): Promise<ExportDefinition[]> {
  const context = { club, session };
  const core = await readSupabaseRest<DefinitionCore>(context, "app_export_definition", { select: definitionCoreSelect, aktiv: "eq.true", order: "titel.asc" });
  const metadata = await optionalMetadata(() => readSupabaseRest<DefinitionSerialLetterMetadata>(context, "app_export_definition", { select: definitionSerialLetterSelect, aktiv: "eq.true" }));
  return withOptionalDefinitionSerialLetterMetadata(core, metadata);
}
export const getExportFilters = (club: ClubContext, session: BrowserSession, exportKey: string) => readSupabaseRest<ExportFilter>({ club, session }, "app_export_filter_definition", { select: filterSelect, export_key: `eq.${exportKey}`, order: "sortierung.asc" });
export async function getExportColumns(club: ClubContext, session: BrowserSession, exportKey: string): Promise<ExportColumn[]> {
  const context = { club, session };
  const core = await readSupabaseRest<ColumnCore>(context, "app_export_column_definition", { select: columnCoreSelect, export_key: `eq.${exportKey}`, order: "sortierung.asc" });
  const metadata = await optionalMetadata(() => readSupabaseRest<ColumnSerialLetterMetadata>(context, "app_export_column_definition", { select: columnSerialLetterSelect, export_key: `eq.${exportKey}` }));
  return withOptionalColumnSerialLetterMetadata(core, metadata);
}
