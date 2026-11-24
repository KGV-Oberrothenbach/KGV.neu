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
const demoHost = "ymxhszxfutjnljiuciyj.supabase.co";
async function verifyBytes(bytes: Uint8Array, expected: unknown) {
  if (bytes.length < 5 || new TextDecoder().decode(bytes.subarray(0, 5)) !== "%PDF-") throw new Error("Invalid PDF");
  if (expected !== null && expected !== undefined && String(expected).trim() !== "") {
    const digest = await crypto.subtle.digest("SHA-256", new Uint8Array(bytes));
    const hex = Array.from(new Uint8Array(digest), x => x.toString(16).padStart(2, "0")).join("");
    if (hex !== String(expected).toLowerCase()) throw new Error("Snapshot hash mismatch");
  }
}
async function token() {
  const body = new URLSearchParams({ client_id: required("GOOGLE_DRIVE_CLIENT_ID"), client_secret: required("GOOGLE_DRIVE_CLIENT_SECRET"), refresh_token: required("GOOGLE_DRIVE_REFRESH_TOKEN"), grant_type: "refresh_token" });
  const response = await fetch("https://oauth2.googleapis.com/token", { method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" }, body, signal: AbortSignal.timeout(15000) });
  const result = await response.json(); if (!response.ok || !result.access_token) throw new Error("Drive token failed"); return String(result.access_token);
}
async function driveFile(access: string, id: string) {
  const response = await fetch("https://www.googleapis.com/drive/v3/files/" + encodeURIComponent(id) + "?alt=media&supportsAllDrives=true", { headers: { Authorization: "Bearer " + access }, signal: AbortSignal.timeout(30000) });
  if (!response.ok) throw new Error("Drive download failed"); return new Uint8Array(await response.arrayBuffer());
}

Deno.serve(async request => {
  if (request.method === "OPTIONS") return new Response("ok", { headers: cors });
  if (request.method !== "POST") return json({ success: false, message: "Methode nicht erlaubt." }, 405);
  let dispatchId: number | null = null;
  let admin: ReturnType<typeof createAdminClient> | null = null;
  let claimed = false;
  let reservedKey: string | null = null;
  let smtpStarted = false;
  try {
    const authorization = request.headers.get("Authorization") ?? "";
    if (!authorization.startsWith("Bearer ")) return json({ success: false, message: "Keine gültige Session." }, 401);
    const body = await request.json();
    dispatchId = Number(body?.versandinstanz_id);
    if (!Number.isSafeInteger(dispatchId) || dispatchId <= 0) return json({ success: false, message: "Ungültige Versandinstanz." }, 400);
    const url = required("SUPABASE_URL");
    const caller = createClient(url, required("SUPABASE_ANON_KEY"), { global: { headers: { Authorization: authorization } } });
    const { data: identity, error: identityError } = await caller.auth.getUser();
    if (identityError || !identity.user) return json({ success: false, message: "Keine gültige Session." }, 401);
    const test = String(Deno.env.get("SMTP_TEST_RECIPIENT") ?? "").trim();
    if ((new URL(url).hostname === demoHost && !test) || (test && !address(test))) return json({ success: false, message: "Sicherer Testempfänger fehlt." }, 503);
    admin = createAdminClient(url);
    const { data: claim, error: claimError } = await caller.rpc("fr209_claim_contract_dispatch", { p_versandinstanz_id: dispatchId });
    if (claimError || !Array.isArray(claim) || claim.length !== 1 || String(claim[0].id) !== String(dispatchId)) return json({ success: false, message: "Versand nicht berechtigt oder nicht beanspruchbar." }, 403);
    claimed = true;
    const [{ data: dispatch, error: dispatchError }, { data: docs, error: docError }, { data: recipients, error: recipientError }] = await Promise.all([
      admin.from("versandinstanz").select("idempotenzschluessel").eq("id", dispatchId).single(),
      admin.from("versandinstanz_dokument_snapshot").select("*").eq("versandinstanz_id", dispatchId).eq("bindungsstatus", "digital_gebunden").order("sortierung"),
      admin.from("versandinstanz_empfaenger_snapshot").select("*").eq("versandinstanz_id", dispatchId).eq("versand_erlaubt", true).in("status", ["bereit", "fehler"]),
    ]);
    if (dispatchError || docError || recipientError || !dispatch || !docs?.length || !recipients?.length) throw new Error("Snapshot incomplete");
    if (recipients.some(r => !address(String(r.email_normalisiert ?? "")))) throw new Error("Invalid recipient");
    const access = docs.some(d => d.drive_file_id_snapshot) ? await token() : "";
    const attachments = await Promise.all(docs.map(async d => {
      let content: Uint8Array;
      if (d.drive_file_id_snapshot) content = await driveFile(access, d.drive_file_id_snapshot);
      else {
        if (!["dokumente", "vereinsregelwerke"].includes(d.bucket_snapshot)) throw new Error("Invalid snapshot bucket");
        const download = await admin!.storage.from(d.bucket_snapshot).download(d.storage_path_snapshot);
        if (download.error || !download.data) throw new Error("Storage download failed"); content = new Uint8Array(await download.data.arrayBuffer());
      }
      await verifyBytes(content, d.sha256_snapshot);
      return { filename: d.dateiname_snapshot, content: Buffer.from(content), contentType: "application/pdf" };
    }));
    const port = Number(required("SMTP_PORT")); const from = Deno.env.get("SMTP_FROM") ?? required("SMTP_USER");
    const domain = (from.split("@").at(-1) ?? "kgv.local").replace(/[^A-Za-z0-9.-]/g, "") || "kgv.local";
    const transport = nodemailer.createTransport({ host: required("SMTP_HOST"), port, secure: port === 465, auth: { user: required("SMTP_USER"), pass: required("smtpPassword") }, connectionTimeout: 15000, greetingTimeout: 15000, socketTimeout: 30000 });
    for (const recipient of recipients) {
      const original = String(recipient.email_normalisiert); const to = test || original;
      const messageId = "<" + dispatch.idempotenzschluessel + "." + recipient.id + "@" + domain + ">";
      // Durable authorization/idempotency fence. Failure is closed BEFORE SMTP.
      const { data: reserved, error: reserveError } = await admin.rpc("fr209_reserve_contract_mail", { p_dispatch: dispatchId, p_recipient: recipient.id, p_actor: identity.user.id });
      if (reserveError || reserved !== true) throw new Error("Reservation denied");
      reservedKey = `contract:${dispatchId}:${recipient.id}`;
      smtpStarted = true;
      const result = await transport.sendMail({ from, to: { name: "", address: to }, messageId, subject: (test ? "[DEMO TEST] " : "") + claim[0].betreff, text: claim[0].mailtext + (test ? "\n\nDEMO-Testversand; ursprünglich: " + original : ""), attachments });
      const ok = result.accepted.some(a => String(a).trim().toLowerCase() === to.toLowerCase()) && !result.rejected.some(a => String(a).trim().toLowerCase() === to.toLowerCase());
      if (!ok) throw new Error("SMTP result uncertain");
      const { error: attemptError } = await admin.from("versandinstanz_versuch").insert({ versandinstanz_id: dispatchId, empfaenger_snapshot_id: recipient.id, status: "versendet", abgeschlossen_am: new Date().toISOString(), smtp_message_id: messageId });
      if (attemptError) throw new Error("Attempt persistence failed");
      const { data: updated, error: updateError } = await admin.from("versandinstanz_empfaenger_snapshot").update({ status: "versendet", versendet_am: new Date().toISOString(), letzter_fehler: null }).eq("id", recipient.id).eq("versandinstanz_id", dispatchId).select("id");
      if (updateError || updated?.length !== 1) throw new Error("Recipient persistence failed");
      const { data: confirmed, error: confirmError } = await admin.from("fr209_mail_reservation").update({ state: "confirmed", confirmed_at: new Date().toISOString() }).eq("reservation_key", reservedKey).eq("state", "reserved").select("reservation_key");
      if (confirmError || confirmed?.length !== 1) throw new Error("Reservation confirmation failed");
      reservedKey = null;
    }
    const { count, error: countError } = await admin.from("versandinstanz_empfaenger_snapshot").select("id", { count: "exact", head: true }).eq("versandinstanz_id", dispatchId).eq("versand_erlaubt", true).neq("status", "versendet");
    if (countError || count !== 0) throw new Error("Unconfirmed recipients");
    const { data: finished, error: finishError } = await admin.from("versandinstanz").update({ status: "versendet", versendet_am: new Date().toISOString(), letzter_fehler: null }).eq("id", dispatchId).eq("status", "wird_versendet").select("id");
    if (finishError || finished?.length !== 1) throw new Error("Dispatch persistence failed");
    return json({ success: true, versandinstanz_id: dispatchId, fehlgeschlagen: 0 });
  } catch {
    // Never expose SMTP addresses, provider payloads, or credentials in logs.
    if (admin && reservedKey) await admin.from("fr209_mail_reservation").update({ state: "unknown" }).eq("reservation_key", reservedKey).eq("state", "reserved");
    if (claimed && admin && dispatchId !== null) {
      // Once any SMTP attempt started, preserve the in-flight fence. Operator reconciliation is required.
      await admin.from("versandinstanz").update(smtpStarted ? { letzter_fehler: "Versandstatus unklar; manuelle Klärung erforderlich." } : { status: "fehler", letzter_fehler: "Versand vor SMTP abgebrochen." }).eq("id", dispatchId).eq("status", "wird_versendet");
    }
    return json({ success: false, message: smtpStarted ? "Versandstatus unklar. Nicht erneut senden; Klärung erforderlich." : "Der Vertragsversand konnte nicht gestartet werden." }, 500);
  }
});
