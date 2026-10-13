import { resolveSupabaseConfig, type SupabaseClientContext, type SupabaseEdgeContext } from "./client-context";

export type EdgeRawOptions = {
  method: "POST" | "PUT" | "PATCH" | "DELETE";
  body?: BodyInit | null;
  contentType?: string;
};

function edgeUrl(context: SupabaseEdgeContext, name: string) {
  const { url } = resolveSupabaseConfig(context.club);
  return `${url}/functions/v1/${encodeURIComponent(name)}`;
}

function edgeHeaders(context: SupabaseEdgeContext, contentType?: string) {
  const { publishableKey } = resolveSupabaseConfig(context.club);
  return { apikey: publishableKey, ...(context.session ? { Authorization: `Bearer ${context.session.accessToken}` } : {}), ...(contentType ? { "Content-Type": contentType } : {}) };
}

export async function invokeEdgeFunction<T>(context: SupabaseClientContext, name: string, payload: Record<string, unknown>): Promise<T> {
  const response = await fetch(edgeUrl(context, name), { method: "POST", headers: edgeHeaders(context, "application/json"), body: JSON.stringify(payload) });
  const result = await response.json().catch(() => null) as (T & { message?: string }) | null;
  if (!response.ok || !result) throw new Error(result?.message ?? "Der Servervorgang konnte nicht ausgeführt werden.");
  return result;
}

export function invokeEdgeFunctionRaw(context: SupabaseEdgeContext, name: string, options: EdgeRawOptions): Promise<Response> {
  return fetch(edgeUrl(context, name), { method: options.method, headers: edgeHeaders(context, options.contentType), body: options.body });
}
