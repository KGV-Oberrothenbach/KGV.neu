import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type ExportColumn } from "../../models/export/export-column";
import { type ExportDefinition } from "../../models/export/export-definition";
import { type ExportFilter, type ExportFilterValue } from "../../models/export/export-filter";
import { type ExportResult } from "../../models/export/export-result";
import { executeRpcExport, readTableOrViewExport } from "../../repositories/export/export-data-repository";
import { buildExportRpcPayload, validateExportDefinition } from "./export-metadata-service";

export type ExecuteExportRequest = { definition: ExportDefinition; filters: ExportFilter[]; columns: ExportColumn[]; values: Record<string, ExportFilterValue> };
export type ExportDataAccess = { executeRpcExport: (exportKey: string, parameters: Record<string, unknown>) => Promise<Array<Record<string, unknown>>>; readTableOrViewExport: (sourceName: string) => Promise<Array<Record<string, unknown>>> };

function assertRequiredFilters(filters: ExportFilter[], values: Record<string, ExportFilterValue>): void {
  for (const filter of filters) if (filter.pflicht && (values[filter.filter_key] === null || values[filter.filter_key] === undefined || values[filter.filter_key] === "")) throw new Error(`„${filter.label ?? filter.filter_key}“ ist erforderlich.`);
}
function isTableOrView(sourceType: string | null): boolean { return sourceType === "table" || sourceType === "view"; }

export async function executeExportWithDataAccess(dataAccess: ExportDataAccess, request: ExecuteExportRequest): Promise<ExportResult> {
  const { definition, filters, columns, values } = request;
  validateExportDefinition(definition);
  assertRequiredFilters(filters, values);
  const sourceName = definition.quelle_name!;
  const rows = isTableOrView(definition.quelle_typ)
    ? await dataAccess.readTableOrViewExport(sourceName)
    : definition.quelle_typ === "rpc"
      ? await dataAccess.executeRpcExport(definition.export_key, buildExportRpcPayload(filters, values))
      : (() => { throw new Error(`Export „${definition.export_key}“ besitzt den unbekannten Quellentyp „${definition.quelle_typ ?? "leer"}“.`); })();
  return { definition, columns, rows, recordCount: rows.length, filters: values, standardSortierung: definition.standard_sortierung };
}
export function executeExport(club: ClubContext, session: BrowserSession, request: ExecuteExportRequest): Promise<ExportResult> {
  return executeExportWithDataAccess({ executeRpcExport: (exportKey, parameters) => executeRpcExport(club, session, exportKey, parameters), readTableOrViewExport: (sourceName) => readTableOrViewExport(club, session, sourceName) }, request);
}
