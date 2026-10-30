import { readSupabaseRest } from "../../lib/supabase/rest-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type ExportColumn } from "../../models/export/export-column";
import { type ExportDefinition } from "../../models/export/export-definition";
import { type ExportFilter } from "../../models/export/export-filter";

const definitionSelect = "export_key,titel,beschreibung,quelle_typ,quelle_name,aktiv,standard_sortierung,standard_ausgabe,erlaubt_csv,erlaubt_pdf,serienbrief_erlaubt,serienbrief_mitglied_id_feld,serienbrief_parzelle_id_feld";
const filterSelect = "export_key,filter_key,label,typ,optionen_json,parameter_name,wert_typ,standardwert_json,pflicht,sortierung";
const columnSelect = "export_key,column_key,label_kurz,label_lang,sortierung,standard_sichtbar,ist_sortierspalte,serienbrief_freigegeben,serienbrief_gruppe";

export const getActiveExportDefinitions = (club: ClubContext, session: BrowserSession) => readSupabaseRest<ExportDefinition>({ club, session }, "app_export_definition", { select: definitionSelect, aktiv: "eq.true", order: "titel.asc" });
export const getExportFilters = (club: ClubContext, session: BrowserSession, exportKey: string) => readSupabaseRest<ExportFilter>({ club, session }, "app_export_filter_definition", { select: filterSelect, export_key: `eq.${exportKey}`, order: "sortierung.asc" });
export const getExportColumns = (club: ClubContext, session: BrowserSession, exportKey: string) => readSupabaseRest<ExportColumn>({ club, session }, "app_export_column_definition", { select: columnSelect, export_key: `eq.${exportKey}`, order: "sortierung.asc" });
