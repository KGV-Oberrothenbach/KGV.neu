import { type BrowserSession, readSupabase } from "../../lib/supabase-auth";
import { type ExportColumn } from "../../models/export/export-column";
import { type ExportDefinition } from "../../models/export/export-definition";
import { type ExportFilter } from "../../models/export/export-filter";

const definitionSelect = "export_key,titel,beschreibung,quelle_typ,quelle_name,aktiv,standard_sortierung,standard_ausgabe,erlaubt_csv,erlaubt_pdf";
const filterSelect = "export_key,filter_key,label,typ,optionen_json,parameter_name,standardwert_json,pflicht,sortierung";
const columnSelect = "export_key,column_key,label_kurz,label_lang,sortierung,standard_sichtbar,ist_sortierspalte";

export const getActiveExportDefinitions = (session: BrowserSession) => readSupabase<ExportDefinition>(session, "app_export_definition", { select: definitionSelect, aktiv: "eq.true", order: "titel.asc" });
export const getExportFilters = (session: BrowserSession, exportKey: string) => readSupabase<ExportFilter>(session, "app_export_filter_definition", { select: filterSelect, export_key: `eq.${exportKey}`, order: "sortierung.asc" });
export const getExportColumns = (session: BrowserSession, exportKey: string) => readSupabase<ExportColumn>(session, "app_export_column_definition", { select: columnSelect, export_key: `eq.${exportKey}`, order: "sortierung.asc" });
