import { resolveSupabaseConfig, type SupabaseClientContext } from "./client-context";

export type SupabaseQuery = Record<string, string>;
export type SupabaseWriteMethod = "POST" | "PATCH";

function restUrl(context: SupabaseClientContext, table: string, query: SupabaseQuery) {
  const { url } = resolveSupabaseConfig(context.club);
  return `${url}/rest/v1/${encodeURIComponent(table)}?${new URLSearchParams(query)}`;
}

function headers(context: SupabaseClientContext, contentType = false, representation = false) {
  const { publishableKey } = resolveSupabaseConfig(context.club);
  return {
    apikey: publishableKey,
    Authorization: `Bearer ${context.session.accessToken}`,
    ...(contentType ? { "Content-Type": "application/json" } : {}),
    ...(representation ? { Prefer: "return=representation" } : {}),
  };
}

async function responseMessage(response: Response, fallback: string) {
  const detail = await response.json().catch(() => null) as { message?: string } | null;
  return detail?.message ?? fallback;
}

export async function readSupabaseRest<T>(context: SupabaseClientContext, table: string, query: SupabaseQuery): Promise<T[]> {
  const response = await fetch(restUrl(context, table, query), { headers: headers(context) });
  if (!response.ok) throw new Error("Die Vereinsdaten konnten nicht geladen werden.");
  return await response.json() as T[];
}

export async function writeSupabaseRest<T>(context: SupabaseClientContext, table: string, method: SupabaseWriteMethod, payload: unknown, query: SupabaseQuery = {}): Promise<T[]> {
  const response = await fetch(restUrl(context, table, query), {
    method,
    headers: headers(context, true, true),
    body: JSON.stringify(payload),
  });
  if (!response.ok) throw new Error(await responseMessage(response, "Die Änderung konnte nicht gespeichert werden."));
  return await response.json() as T[];
}

export async function deleteSupabaseRest(context: SupabaseClientContext, table: string, query: SupabaseQuery) {
  const response = await fetch(restUrl(context, table, query), { method: "DELETE", headers: headers(context) });
  if (!response.ok) throw new Error("Der Eintrag konnte nicht gelöscht werden.");
}
