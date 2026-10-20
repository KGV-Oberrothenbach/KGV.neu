import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import nodemailer from "npm:nodemailer@6.9.10";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
};
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { ...corsHeaders, "Content-Type": "application/json" } });
const required = (name: string) => { const value = Deno.env.get(name) ?? ""; if (!value) throw new Error(`Missing ${name}`); return value; };

Deno.serve(async (request) => {
  if (request.method === "OPTIONS") return new Response("ok", { headers: corsHeaders });
  if (request.method !== "POST") return json({ success: false, message: "Methode nicht erlaubt." }, 405);
  try {
    const authorization = request.headers.get("Authorization") ?? "";
    if (!authorization.startsWith("Bearer ")) return json({ success: false, message: "Keine gültige Session." }, 401);
    const url = required("SUPABASE_URL"), anonKey = required("SUPABASE_ANON_KEY"), serviceKey = required("SUPABASE_SERVICE_ROLE_KEY");
    const caller = createClient(url, anonKey, { global: { headers: { Authorization: authorization } } });
    const admin = createClient(url, serviceKey);
    const { data: userData, error: userError } = await caller.auth.getUser();
    if (userError || !userData.user) return json({ success: false, message: "Keine gültige Session." }, 401);
    const { data: context, error: contextError } = await caller.rpc("current_app_user_context");
    if (contextError || !context?.some((item: { role?: string }) => item.role === "admin" || item.role === "vorstand")) return json({ success: false, message: "Keine Berechtigung." }, 403);
    const body = await request.json(); const memberId = Number(body?.mitglied_id); const versionIds = Array.isArray(body?.vereinsdokument_fassung_ids) ? body.vereinsdokument_fassung_ids.map(Number) : [];
    if (!Number.isSafeInteger(memberId) || versionIds.length === 0 || versionIds.some((id) => !Number.isSafeInteger(id)) || new Set(versionIds).size !== versionIds.length) return json({ success: false, message: "Mitglied und eindeutige Dokumentfassungen sind erforderlich." }, 400);
    const { data: member, error: memberError } = await admin.from("mitglied").select("id,vorname,name,email").eq("id", memberId).maybeSingle();
    if (memberError || !member) return json({ success: false, message: "Mitglied wurde nicht gefunden." }, 404);
    if (!String(member.email ?? "").trim()) return json({ success: false, message: "Für dieses Mitglied ist keine E-Mail-Adresse hinterlegt." }, 400);
    const { data: versions, error: versionsError } = await admin.from("vereinsdokument_fassung").select("id,fassung,dateiname,storage_path").in("id", versionIds);
    if (versionsError || !versions || versions.length !== versionIds.length) return json({ success: false, message: "Mindestens eine Dokumentfassung ist unbekannt." }, 400);
    if (versions.some((version) => !String(version.storage_path ?? "").trim() || !String(version.dateiname ?? "").trim())) return json({ success: false, message: "Mindestens eine Dokumentfassung hat keine hinterlegte PDF-Datei.", diagnostic_code: "DOCUMENT_FILE_MISSING" }, 400);
    const attachments = await Promise.all(versions.map(async (version) => { const { data, error } = await admin.storage.from("vereinsregelwerke").download(version.storage_path); if (error || !data) throw new Error("Dokumentdatei konnte nicht geladen werden."); return { filename: version.dateiname, content: new Uint8Array(await data.arrayBuffer()), contentType: "application/pdf" }; }));
    const port = Number(required("SMTP_PORT")); const transport = nodemailer.createTransport({ host: required("SMTP_HOST"), port, secure: port === 465, auth: { user: required("SMTP_USER"), pass: required("smtpPassword") } });
    const originalRecipient = String(member.email).trim();
    const testRecipient = String(Deno.env.get("SMTP_TEST_RECIPIENT") ?? "").trim();
    const recipient = testRecipient || originalRecipient;
    const subject = `${testRecipient ? "[DEMO TEST] " : ""}Ihre Vereinsunterlagen`;
    const text = `Guten Tag ${member.vorname ?? ""},\n\nanbei erhalten Sie die ausgewählten Vereinsunterlagen.${testRecipient ? `\n\n---\nDEMO-Testversand\nUrsprünglich vorgesehener Empfänger:\n${originalRecipient}\n---` : ""}`;
    const mailResult = await transport.sendMail({ from: Deno.env.get("SMTP_FROM") ?? required("SMTP_USER"), to: recipient, subject, text, attachments });
    const accepted = mailResult.accepted.some((address) => String(address).trim().toLowerCase() === recipient.toLowerCase());
    const rejected = mailResult.rejected.some((address) => String(address).trim().toLowerCase() === recipient.toLowerCase());
    if (!accepted || rejected) return json({ success: false, message: "Der SMTP-Server hat den Empfänger nicht akzeptiert.", diagnostic_code: "DIGITAL_SEND_FAILED" }, 502);
    const { data: records, error: recordError } = await admin.rpc("create_mitglied_vereinsdokument_bereitstellungen_digital", { p_mitglied_id: memberId, p_vereinsdokument_fassung_ids: versionIds, p_bereitgestellt_von: userData.user.id });
    if (recordError) return json({ success: false, message: "E-Mail wurde versandt, der Bereitstellungsnachweis konnte aber nicht angelegt werden.", diagnostic_code: "DELIVERY_RECORD_FAILED" }, 500);
    return json({ success: true, message: "Die Vereinsunterlagen wurden per E-Mail versandt.", diagnostic_code: "OK", bereitgestellt_am: records?.[0]?.bereitgestellt_am });
  } catch (error) { console.error("kgv-send-club-documents", error); return json({ success: false, message: "Der Versand der Vereinsunterlagen konnte nicht bestätigt werden.", diagnostic_code: "DIGITAL_SEND_FAILED" }, 500); }
});
