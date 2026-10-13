import { resolveSupabaseConfig, type SupabaseClientContext } from "./client-context";

export async function callSupabaseRpc<T>(context: SupabaseClientContext, name: string, payload: Record<string, unknown>): Promise<T> {
  const { url, publishableKey } = resolveSupabaseConfig(context.club);
  const response = await fetch(`${url}/rest/v1/rpc/${encodeURIComponent(name)}`, {
    method: "POST",
    headers: { apikey: publishableKey, Authorization: `Bearer ${context.session.accessToken}`, "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });
  if (!response.ok) {
    const detail = await response.json().catch(() => null) as { message?: string } | null;
    throw new Error(detail?.message ?? "Der Vorgang konnte nicht gespeichert werden.");
  }
  return await response.json() as T;
}
