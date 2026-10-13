import { getBrowserClientInstanceId } from "../../lib/browser/client-instance";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import { deleteSupabaseRest, writeSupabaseRest, type SupabaseQuery, type SupabaseWriteMethod } from "../../lib/supabase/rest-client";
import type { SupabaseClientContext } from "../../lib/supabase/client-context";

export type BrowserEditLockResult = {
  acquired: boolean;
  lockedByUserId: string | null;
  lockedByDisplayName: string;
  expiresAt: string | null;
};

const protectedEditTables: Record<string, string> = {
  mitglied: "id",
  parzelle: "id",
  parzellen_belegung: "id",
  zaehler_ablesung: "id",
  arbeitsstunde: "id",
  arbeitseinsatz: "id",
  arbeitseinsatz_anmeldung: "id",
  termin: "id",
  bekanntmachung: "id",
  wartungsvertraege: "id",
  wartungsvertrag_zuordnungen: "id",
  app_user: "user_id",
  saison: "id",
  vereinskonfiguration: "id",
};

function mutationEntityId(table: string, query: SupabaseQuery) {
  const idColumn = protectedEditTables[table];
  if (!idColumn) return null;
  const filter = query[idColumn];
  return filter?.startsWith("eq.") ? filter.slice(3) : null;
}

async function acquireLockForMutation(context: SupabaseClientContext, table: string, query: SupabaseQuery) {
  const entityId = mutationEntityId(table, query);
  if (!entityId) return;
  const result = await acquireBrowserEditLock(context, table, entityId);
  if (!result.acquired) {
    throw new Error(`Dieser Datensatz wird gerade von ${result.lockedByDisplayName} bearbeitet.`);
  }
}

export async function acquireBrowserEditLock(context: SupabaseClientContext, entityType: string, entityId: string | number, timeoutSeconds = 600): Promise<BrowserEditLockResult> {
  const clientInstanceId = await getBrowserClientInstanceId();
  const rows = await callSupabaseRpc<Array<{ acquired?: boolean; locked_by_user_id?: string | null; locked_by_display_name?: string | null; expires_at?: string | null }>>(
    context,
    "acquire_browser_edit_lock_for_client",
    { p_entity_type: entityType, p_entity_id: String(entityId), p_client_instance_id: clientInstanceId, p_timeout_seconds: timeoutSeconds },
  );
  const row = rows[0];
  return {
    acquired: row?.acquired === true,
    lockedByUserId: row?.locked_by_user_id ?? null,
    lockedByDisplayName: row?.locked_by_display_name || "einem anderen Benutzer",
    expiresAt: row?.expires_at ?? null,
  };
}

export async function releaseBrowserEditLock(context: SupabaseClientContext, entityType: string, entityId: string | number) {
  const clientInstanceId = await getBrowserClientInstanceId();
  await callSupabaseRpc<boolean>(context, "release_browser_edit_lock_for_client", { p_entity_type: entityType, p_entity_id: String(entityId), p_client_instance_id: clientInstanceId });
}

export async function releaseAllBrowserEditLocks(context: SupabaseClientContext) {
  const clientInstanceId = await getBrowserClientInstanceId();
  await callSupabaseRpc<boolean>(context, "release_all_browser_edit_locks_for_client", { p_client_instance_id: clientInstanceId });
}

export async function writeWithBrowserEditLock<T>(context: SupabaseClientContext, table: string, method: SupabaseWriteMethod, payload: unknown, query: SupabaseQuery = {}): Promise<T[]> {
  if (method === "PATCH") await acquireLockForMutation(context, table, query);
  return writeSupabaseRest<T>(context, table, method, payload, query);
}

export async function deleteWithBrowserEditLock(context: SupabaseClientContext, table: string, query: SupabaseQuery) {
  await acquireLockForMutation(context, table, query);
  await deleteSupabaseRest(context, table, query);
}
