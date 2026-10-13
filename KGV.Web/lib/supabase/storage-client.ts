import { resolveSupabaseConfig, type SupabaseClientContext } from "./client-context";

export async function createSignedStorageUrl(context: SupabaseClientContext, bucket: string, storagePath: string, expiresIn = 600): Promise<string> {
  const { url, publishableKey } = resolveSupabaseConfig(context.club);
  const path = storagePath.split("/").map(encodeURIComponent).join("/");
  const response = await fetch(`${url}/storage/v1/object/sign/${encodeURIComponent(bucket)}/${path}`, {
    method: "POST",
    headers: { apikey: publishableKey, Authorization: `Bearer ${context.session.accessToken}`, "Content-Type": "application/json" },
    body: JSON.stringify({ expiresIn }),
  });
  if (!response.ok) throw new Error("Für das Storage-Objekt konnte kein Zugriffslink erstellt werden.");
  const payload = await response.json() as { signedURL?: string; signedUrl?: string };
  const signedPath = payload.signedURL ?? payload.signedUrl;
  if (!signedPath) throw new Error("Für das Storage-Objekt konnte kein Zugriffslink erstellt werden.");
  return signedPath.startsWith("http") ? signedPath : `${url}/storage/v1${signedPath}`;
}
