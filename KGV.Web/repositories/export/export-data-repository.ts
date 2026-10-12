import { type BrowserSession, callSupabaseRpc } from "../../lib/supabase-auth";

type ProtectedExportRow = { row?: Record<string, unknown> };

function unwrapRows(rows: ProtectedExportRow[]): Array<Record<string, unknown>> {
  return rows.map((item) => item.row ?? (item as Record<string, unknown>).rpc_execute_export ?? (item as Record<string, unknown>).rpc_execute_table_or_view_export ?? item).filter((item): item is Record<string, unknown> => typeof item === "object" && item !== null);
}

export async function executeRpcExport(session: BrowserSession, sourceName: string, parameters: Record<string, unknown>): Promise<Array<Record<string, unknown>>> {
  return unwrapRows(await callSupabaseRpc<ProtectedExportRow[]>(session, "rpc_execute_export", { p_source_name: sourceName, p_parameters: parameters }));
}

export async function readTableOrViewExport(session: BrowserSession, sourceName: string): Promise<Array<Record<string, unknown>>> {
  return unwrapRows(await callSupabaseRpc<ProtectedExportRow[]>(session, "rpc_execute_table_or_view_export", { p_source_name: sourceName }));
}
