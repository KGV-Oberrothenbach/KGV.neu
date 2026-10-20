import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
const cors = { "Access-Control-Allow-Origin": "*", "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type" };
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { ...cors, "Content-Type": "application/json" } });
const required = (name: string) => { const value = Deno.env.get(name); if (!value) throw new Error(`Missing ${name}`); return value; };
Deno.serve(async request => {
  if (request.method === "OPTIONS") return new Response("ok", { headers: cors });
  if (request.method !== "POST") return json({ success: false, message: "Methode nicht erlaubt." }, 405);
  try {
    const authorization = request.headers.get("Authorization") ?? "";
    if (!authorization.startsWith("Bearer ")) return json({ success: false, message: "Keine gültige Session." }, 401);
    const url = required("SUPABASE_URL"), anon = required("SUPABASE_ANON_KEY"), service = required("SUPABASE_SERVICE_ROLE_KEY");
    const caller = createClient(url, anon, { global: { headers: { Authorization: authorization } } }); const admin = createClient(url, service);
    const { data: user } = await caller.auth.getUser(); if (!user.user) return json({ success: false, message: "Keine gültige Session." }, 401);
    const { data: context, error: contextError } = await caller.rpc("current_app_user_context");
    if (contextError || !context?.some((x: { role?: string }) => x.role === "admin" || x.role === "vorstand")) return json({ success: false, message: "Keine Berechtigung." }, 403);
    const body = await request.json(); const id = Number(body?.kommunikationsvorgang_id), revision = Number(body?.expected_revision);
    if (!Number.isSafeInteger(id) || id <= 0 || !Number.isSafeInteger(revision) || revision < 1) return json({ success: false, message: "Kommunikationsvorgang und Revision sind erforderlich." }, 400);
    const { data, error } = await admin.rpc("discard_prepared_contract", { p_kommunikationsvorgang_id: id, p_expected_revision: revision, p_discarded_by: user.user.id });
    if (error) return json({ success: false, message: error.message }, error.code === "42501" ? 403 : 409);
    return json({ success: true, contract: data?.[0] });
  } catch (error) { console.error("kgv-discard-prepared-contract", error); return json({ success: false, message: "Der Vertragsvorgang konnte nicht verworfen werden." }, 500); }
});
