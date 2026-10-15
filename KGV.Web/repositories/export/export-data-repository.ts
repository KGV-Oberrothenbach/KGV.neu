import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";

type ProtectedExportRow = { row?: Record<string, unknown> };

function unwrapRows(rows: ProtectedExportRow[]): Array<Record<string, unknown>> {
  return rows.map((item) => item.row ?? (item as Record<string, unknown>).rpc_execute_export ?? (item as Record<string, unknown>).rpc_execute_table_or_view_export ?? item).filter((item): item is Record<string, unknown> => typeof item === "object" && item !== null);
}

export async function executeRpcExport(club: ClubContext, session: BrowserSession, exportKey: string, parameters: Record<string, unknown>): Promise<Array<Record<string, unknown>>> {
  return unwrapRows(await callSupabaseRpc<ProtectedExportRow[]>({ club, session }, "rpc_execute_export", { p_export_key: exportKey, p_parameters: parameters }));
}

export async function readTableOrViewExport(club: ClubContext, session: BrowserSession, sourceName: string): Promise<Array<Record<string, unknown>>> {
  return unwrapRows(await callSupabaseRpc<ProtectedExportRow[]>({ club, session }, "rpc_execute_table_or_view_export", { p_source_name: sourceName }));
}

export async function readExportOptions(club: ClubContext, session: BrowserSession, rpcName: string): Promise<Array<{ label?: unknown; value?: unknown }>> {
  return callSupabaseRpc<Array<{ label?: unknown; value?: unknown }>>({ club, session }, rpcName, {});
}
