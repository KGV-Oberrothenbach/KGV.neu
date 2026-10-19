import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const corsHeaders = { "Access-Control-Allow-Origin": "*", "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type" };
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { ...corsHeaders, "Content-Type": "application/json" } });
const required = (name: string) => { const value = Deno.env.get(name) ?? ""; if (!value) throw new Error(`Missing ${name}`); return value; };

Deno.serve(async (request) => {
  if (request.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });
  if (request.method !== "POST") return json({ success: false, code: "METHOD_NOT_ALLOWED", message: "Methode nicht erlaubt." }, 405);
  try {
    const authorization = request.headers.get("Authorization") ?? "";
    if (!authorization.startsWith("Bearer ")) return json({ success: false, code: "UNAUTHENTICATED", message: "Keine gültige Session." }, 401);
    const url = required("SUPABASE_URL"), anonKey = required("SUPABASE_ANON_KEY"), serviceKey = required("SUPABASE_SERVICE_ROLE_KEY");
    const caller = createClient(url, anonKey, { global: { headers: { Authorization: authorization } } });
    const admin = createClient(url, serviceKey);
    const { data: userData, error: userError } = await caller.auth.getUser();
    if (userError || !userData.user) return json({ success: false, code: "UNAUTHENTICATED", message: "Keine gültige Session." }, 401);
    const { data: context, error: contextError } = await caller.rpc("current_app_user_context");
    if (contextError || !context?.some((item: { role?: string }) => item.role === "admin" || item.role === "vorstand")) return json({ success: false, code: "FORBIDDEN", message: "Keine Berechtigung." }, 403);
    const body = await request.json(); const id = Number(body?.kommunikationsvorgang_id), revision = Number(body?.expected_revision);
    if (!Number.isSafeInteger(id) || id <= 0 || !Number.isSafeInteger(revision) || revision < 1) return json({ success: false, code: "INVALID_REQUEST", message: "Kommunikationsvorgang und Revision sind erforderlich." }, 400);
    const { data, error } = await admin.rpc("complete_prepared_pachtvertrag", { p_kommunikationsvorgang_id: id, p_expected_revision: revision, p_created_by: userData.user.id });
    if (error) return json({ success: false, code: error.code === "42501" ? "FORBIDDEN" : "COMPLETE_FAILED", message: error.message }, error.code === "42501" ? 403 : 409);
    const result = data?.[0];
    return json({ success: true, pachtverhaeltnis_id: result?.pachtverhaeltnis_id, pachtvertragsfassung_id: result?.pachtvertragsfassung_id, bereits_abgeschlossen: Boolean(result?.bereits_abgeschlossen) });
  } catch (error) { console.error("kgv-complete-prepared-lease-contract", error); return json({ success: false, code: "COMPLETE_FAILED", message: "Der Pachtvertragsabschluss konnte nicht bestätigt werden." }, 500); }
});
