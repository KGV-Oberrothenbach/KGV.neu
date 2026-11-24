import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { Buffer } from "node:buffer";
// @deno-types="npm:@types/nodemailer@6.4.17"
import nodemailer from "npm:nodemailer@6.9.10";

const cors = { "Access-Control-Allow-Origin": "*", "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type" };
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { ...cors, "Content-Type": "application/json" } });
const required = (key: string) => { const value = Deno.env.get(key); if (!value) throw new Error("Missing " + key); return value; };
// Infer the instantiated client type, not the uninstantiated generic factory.
const createAdminClient = (url: string) => createClient(url, required("SUPABASE_SERVICE_ROLE_KEY"));
const address = (value: string) => /^[^\s@,;<>"\\]+@(?:[A-Za-z0-9-]+\.)+[A-Za-z0-9-]+$/.test(value);
Deno.serve(async request => {
  if (request.method === "OPTIONS") return new Response("ok", { headers: cors });
  if (request.method !== "POST") return json({ success: false, message: "Methode nicht erlaubt." }, 405);
  let admin: ReturnType<typeof createAdminClient> | null = null;
  let reservation: string | null = null;
  try {
    const authorization = request.headers.get("Authorization") ?? "";
    if (!authorization.startsWith("Bearer ")) return json({ success: false, message: "Keine gültige Session." }, 401);
    const url = required("SUPABASE_URL");
    const caller = createClient(url, required("SUPABASE_ANON_KEY"), { global: { headers: { Authorization: authorization } } });
    const { data: identity, error: identityError } = await caller.auth.getUser();
    if (identityError || !identity.user) return json({ success: false, message: "Keine gültige Session." }, 401);
    const { data: permitted, error: permissionError } = await caller.rpc("ap02_has_effective_right", { p_fachrecht: "vereinsunterlagen.verwalten" });
    if (permissionError || permitted !== true) return json({ success: false, message: "Keine Berechtigung." }, 403);
    const body = await request.json(); const memberId = Number(body?.mitglied_id);
    const ids: number[] = Array.isArray(body?.vereinsdokument_fassung_ids) ? body.vereinsdokument_fassung_ids.map(Number) : [];
    if (!Number.isSafeInteger(memberId) || memberId <= 0 || ids.length < 1 || ids.length > 50 || ids.some(id => !Number.isSafeInteger(id) || id <= 0) || new Set(ids).size !== ids.length) return json({ success: false, message: "Mitglied und eindeutige Fassungen sind erforderlich." }, 400);
    const { data: member, error: memberError } = await caller.from("mitglied").select("id,vorname,email").eq("id", memberId).maybeSingle();
    if (memberError || !member) return json({ success: false, message: "Mitglied nicht zugänglich." }, 403);
    const { data: versions, error: versionError } = await caller.from("vereinsdokument_fassung").select("id,dateiname,storage_path,sha256").in("id", ids);
    if (versionError || !versions || versions.length !== ids.length) return json({ success: false, message: "Fassungen nicht zugänglich." }, 403);
    const original = String(member.email ?? "").trim();
    if (!address(original)) return json({ success: false, message: "Keine gültige E-Mail-Adresse." }, 400);
    const test = String(Deno.env.get("SMTP_TEST_RECIPIENT") ?? "").trim();
    if ((new URL(url).hostname === "ymxhszxfutjnljiuciyj.supabase.co" && !test) || (test && !address(test))) return json({ success: false, message: "Sicherer Testempfänger fehlt." }, 503);
    admin = createAdminClient(url);
    const attachments = await Promise.all(versions.map(async version => {
      if (!version.storage_path || !version.dateiname || !/^[0-9a-f]{64}$/i.test(String(version.sha256 ?? ""))) throw new Error("Immutable PDF metadata missing");
      const download = await admin!.storage.from("vereinsregelwerke").download(version.storage_path);
      if (download.error || !download.data) throw new Error("Download failed");
      const content = new Uint8Array(await download.data.arrayBuffer());
      const hash = Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", content)), x => x.toString(16).padStart(2, "0")).join("");
      if (hash !== String(version.sha256).toLowerCase() || new TextDecoder().decode(content.subarray(0, 5)) !== "%PDF-") throw new Error("PDF hash mismatch");
      return { filename: version.dateiname, content: Buffer.from(content), contentType: "application/pdf" };
    }));
    const port = Number(required("SMTP_PORT")); const from = Deno.env.get("SMTP_FROM") ?? required("SMTP_USER");
    const transport = nodemailer.createTransport({ host: required("SMTP_HOST"), port, secure: port === 465, auth: { user: required("SMTP_USER"), pass: required("smtpPassword") }, connectionTimeout: 15000, greetingTimeout: 15000, socketTimeout: 30000 });
    // Exact selection and current authorization are checked atomically BEFORE SMTP.
    const { data: key, error: reserveError } = await admin.rpc("fr209_reserve_club_mail", { p_member: memberId, p_versions: ids, p_actor: identity.user.id, p_email: original });
    if (reserveError) return json({ success: false, message: "Versand nicht berechtigt oder nicht reservierbar." }, 403);
    if (typeof key !== "string" || !key) return json({ success: false, message: "Diese Auswahl wurde bereits reserviert oder versandt. Nicht erneut senden." }, 409);
    reservation = key;
    const recipient = test || original;
    const domain = (from.split("@").at(-1) ?? "kgv.local").replace(/[^A-Za-z0-9.-]/g, "") || "kgv.local";
    const mail = await transport.sendMail({ from, to: { name: "", address: recipient }, messageId: `<${key.replaceAll(":", ".")}@${domain}>`, subject: `${test ? "[DEMO TEST] " : ""}Ihre Vereinsunterlagen`, text: `Guten Tag ${member.vorname ?? ""},\n\nanbei erhalten Sie die ausgewählten Vereinsunterlagen.${test ? "\n\nDEMO-Testversand; ursprünglich: " + original : ""}`, attachments });
    if (!mail.accepted.some(a => String(a).trim().toLowerCase() === recipient.toLowerCase()) || mail.rejected.some(a => String(a).trim().toLowerCase() === recipient.toLowerCase())) throw new Error("Uncertain SMTP outcome");
    const { data: records, error: recordError } = await admin.rpc("create_mitglied_vereinsdokument_bereitstellungen_digital", { p_mitglied_id: memberId, p_vereinsdokument_fassung_ids: ids, p_bereitgestellt_von: identity.user.id });
    if (recordError || !Array.isArray(records) || records.length !== ids.length) throw new Error("Delivery persistence failed");
    const { data: confirmed, error: confirmError } = await admin.from("fr209_mail_reservation").update({ state: "confirmed", confirmed_at: new Date().toISOString() }).eq("reservation_key", reservation).eq("state", "reserved").select("reservation_key");
    if (confirmError || confirmed?.length !== 1) throw new Error("Reservation confirmation failed");
    reservation = null;
    return json({ success: true, message: "Die Vereinsunterlagen wurden per E-Mail versandt.", diagnostic_code: "OK", bereitgestellt_am: records[0]?.bereitgestellt_am });
  } catch {
    if (admin && reservation) await admin.from("fr209_mail_reservation").update({ state: "unknown" }).eq("reservation_key", reservation).eq("state", "reserved");
    return json({ success: false, message: reservation ? "Versandstatus unklar. Nicht erneut senden; Klärung erforderlich." : "Der Versand konnte nicht gestartet werden.", diagnostic_code: reservation ? "DELIVERY_RECORD_FAILED" : "DIGITAL_SEND_FAILED" }, 500);
  }
});
