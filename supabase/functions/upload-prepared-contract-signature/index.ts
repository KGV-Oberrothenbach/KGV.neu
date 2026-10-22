import { createClient } from "npm:@supabase/supabase-js@2";

const bucket = "prepared-contract-signatures";
const corsHeaders = { "Access-Control-Allow-Origin": "*", "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type" };
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { ...corsHeaders, "Content-Type": "application/json" } });
const keyPattern = /^[A-Za-z0-9._-]+$/;

function isPng(bytes: Uint8Array) { return bytes.length >= 8 && bytes[0] === 137 && bytes[1] === 80 && bytes[2] === 78 && bytes[3] === 71 && bytes[4] === 13 && bytes[5] === 10 && bytes[6] === 26 && bytes[7] === 10; }
function decodePng(value: unknown) {
  if (typeof value !== "string" || !/^[A-Za-z0-9+/]+={0,2}$/.test(value)) return null;
  try { const binary = atob(value); const bytes = Uint8Array.from(binary, char => char.charCodeAt(0)); return bytes.length <= 1048576 && isPng(bytes) ? bytes : null; } catch { return null; }
}
async function sha256(bytes: Uint8Array) { const digest = await crypto.subtle.digest("SHA-256", bytes); return Array.from(new Uint8Array(digest), value => value.toString(16).padStart(2, "0")).join(""); }

Deno.serve(async (request) => {
  if (request.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });
  if (request.method !== "POST") return json({ success: false, message: "Methode nicht erlaubt." }, 405);
  try {
    const authorization = request.headers.get("Authorization") ?? "";
    if (!authorization.startsWith("Bearer ")) return json({ success: false, message: "Keine gültige Session." }, 401);
    const url = Deno.env.get("SUPABASE_URL") ?? "", anonKey = Deno.env.get("SUPABASE_ANON_KEY") ?? "", serviceKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
    if (!url || !anonKey || !serviceKey) throw new Error("Supabase-Konfiguration fehlt.");
    const caller = createClient(url, anonKey, { global: { headers: { Authorization: authorization } }, auth: { persistSession: false, autoRefreshToken: false } });
    const admin = createClient(url, serviceKey, { auth: { persistSession: false, autoRefreshToken: false } });
    const { data: userData, error: userError } = await caller.auth.getUser();
    if (userError || !userData.user) return json({ success: false, message: "Keine gültige Session." }, 401);
    const { data: context, error: contextError } = await caller.rpc("current_app_user_context");
    if (contextError || !context?.some((entry: { role?: string }) => entry.role === "admin" || entry.role === "vorstand")) return json({ success: false, message: "Keine Berechtigung." }, 403);
    const body = await request.json();
    const id = Number(body?.kommunikationsvorgang_id), revision = Number(body?.signatur_revision), key = body?.signatur_schluessel;
    const png = decodePng(body?.png_base64);
    if (!Number.isSafeInteger(id) || id <= 0 || !Number.isSafeInteger(revision) || revision < 1 || typeof key !== "string" || !keyPattern.test(key) || !png)
      return json({ success: false, message: "Die Signatur-Uploadanfrage ist ungültig." }, 400);
    const { data: contract, error: contractError } = await admin.from("kommunikationsvorgang").select("id,status,revision,signatur_revision").eq("id", id).maybeSingle();
    if (contractError || !contract || contract.status !== "zur_signatur" || contract.revision !== revision || contract.signatur_revision !== revision)
      return json({ success: false, message: "Der Vertragsvorgang ist nicht für diese Signaturrevision geöffnet." }, 409);
    const { data: position, error: positionError } = await admin.from("prepared_contract_signatur").select("id,status").eq("kommunikationsvorgang_id", id).eq("signatur_revision", revision).eq("signatur_schluessel", key).maybeSingle();
    if (positionError || !position) return json({ success: false, message: "Die Signaturposition wurde nicht vorbereitet." }, 409);
    if (position.status !== "offen") return json({ success: false, message: "Diese Signaturposition ist bereits unterschrieben." }, 409);
    const objectId = crypto.randomUUID();
    const storagePath = `${id}/${revision}/${key}/${objectId}.png`;
    const hash = await sha256(png);
    const { error: uploadError } = await admin.storage.from(bucket).upload(storagePath, png, { contentType: "image/png", upsert: false });
    if (uploadError) throw new Error(uploadError.message);
    return json({ success: true, storage_path: storagePath, sha256: hash });
  } catch (error) {
    console.error("upload-prepared-contract-signature", error);
    return json({ success: false, message: "Die Signatur konnte nicht sicher hochgeladen werden." }, 500);
  }
});
