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
function stableObjectId(hash: string) { return `${hash.slice(0, 8)}-${hash.slice(8, 12)}-${hash.slice(12, 16)}-${hash.slice(16, 20)}-${hash.slice(20, 32)}`; }

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
    const callerMemberId = context?.[0]?.mitglied_id;
    if (contextError || !Number.isSafeInteger(callerMemberId)) return json({ success: false, message: "Keine zugeordnete Unterzeichneridentität." }, 403);
    const { data: effectivePermissions, error: permissionError } = await caller.rpc("current_effective_permissions");
    if (permissionError) return json({ success: false, message: "Berechtigungen konnten nicht geprüft werden." }, 403);
    const body = await request.json();
    const id = Number(body?.kommunikationsvorgang_id), revision = Number(body?.signatur_revision), key = body?.signatur_schluessel;
    const png = decodePng(body?.png_base64);
    if (!Number.isSafeInteger(id) || id <= 0 || !Number.isSafeInteger(revision) || revision < 1 || typeof key !== "string" || !keyPattern.test(key) || !png)
      return json({ success: false, message: "Die Signatur-Uploadanfrage ist ungültig." }, 400);
    const hash = await sha256(png);
    const { data: contract, error: contractError } = await admin.from("kommunikationsvorgang").select("id,status,revision,signatur_revision").eq("id", id).maybeSingle();
    if (contractError || !contract || !["zur_signatur", "letzte_pruefung", "erledigt"].includes(contract.status) || contract.revision !== revision || contract.signatur_revision !== revision)
      return json({ success: false, message: "Der Vertragsvorgang ist nicht für diese Signaturrevision geöffnet." }, 409);
    const { data: position, error: positionError } = await admin.from("prepared_contract_signatur").select("id,status,storage_path,signatur_sha256,unterzeichner_typ,mitglied_id").eq("kommunikationsvorgang_id", id).eq("signatur_revision", revision).eq("signatur_schluessel", key).maybeSingle();
    if (positionError || !position) return json({ success: false, message: "Die Signaturposition wurde nicht vorbereitet." }, 409);
    if (position.status === "unterschrieben") {
      if (position.signatur_sha256 === hash && position.storage_path)
        return json({ success: true, storage_path: position.storage_path, sha256: hash, already_stored: true });
      return json({ success: false, message: "Diese Signaturposition wurde bereits abweichend unterschrieben." }, 409);
    }
    if (position.status !== "offen" || contract.status !== "zur_signatur")
      return json({ success: false, message: "Diese Signaturposition ist nicht mehr unterschreibbar." }, 409);
    // The contract workstation is operated by an authorised administrator who
    // records the signer selected in the frozen position.  Requiring that
    // workstation account to be the tenant made the Web flow unable to submit
    // a tenant signature at all.  The guarded save RPC still records and
    // validates the actual signer (and the frozen board snapshot).
    // Role labels alone are insufficient: an individual revocation must take
    // effect immediately.  524288 is the contract-administration capability
    // required by the guarded signature persistence RPC as well.
    const administrativeCaller = (Number(effectivePermissions) & 524288) === 524288;
    if (position.unterzeichner_typ === "vereinsunterzeichner" && !administrativeCaller) {
      const { data: source } = await admin.from("prepared_contract_signatur_snapshot_person").select("id").eq("kommunikationsvorgang_id", id).eq("signatur_revision", revision).eq("mitglied_id", callerMemberId).maybeSingle();
      if (!source) return json({ success: false, message: "Keine gültige Unterzeichnerberechtigung." }, 403);
    } else if (!administrativeCaller && position.unterzeichner_typ !== "vereinsunterzeichner" && position.mitglied_id !== callerMemberId) return json({ success: false, message: "Die Signatur gehört nicht zum angemeldeten Mitglied." }, 403);
    const objectId = stableObjectId(hash);
    const storagePath = `${id}/${revision}/${key}/${objectId}.png`;
    const { error: uploadError } = await admin.storage.from(bucket).upload(storagePath, png, {
      contentType: "image/png",
      upsert: true,
      metadata: { sha256: hash },
    });
    if (uploadError) throw new Error(uploadError.message);
    return json({ success: true, storage_path: storagePath, sha256: hash });
  } catch (error) {
    console.error("upload-prepared-contract-signature", error);
    return json({ success: false, message: "Die Signatur konnte nicht sicher hochgeladen werden." }, 500);
  }
});
