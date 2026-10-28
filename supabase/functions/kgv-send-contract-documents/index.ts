import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import nodemailer from "npm:nodemailer@6.9.10";

const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
};
const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { ...cors, "Content-Type": "application/json" } });
const required = (key: string) => {
  const value = Deno.env.get(key);
  if (!value) throw new Error("Missing " + key);
  return value;
};

async function token() {
  const body = new URLSearchParams({
    client_id: required("GOOGLE_DRIVE_CLIENT_ID"),
    client_secret: required("GOOGLE_DRIVE_CLIENT_SECRET"),
    refresh_token: required("GOOGLE_DRIVE_REFRESH_TOKEN"),
    grant_type: "refresh_token",
  });
  const response = await fetch("https://oauth2.googleapis.com/token", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body,
    signal: AbortSignal.timeout(15000),
  });
  const result = await response.json();
  if (!response.ok || !result.access_token) throw new Error("Google Drive token refresh failed");
  return String(result.access_token);
}

async function driveFile(access: string, id: string) {
  const response = await fetch(
    "https://www.googleapis.com/drive/v3/files/" + encodeURIComponent(id) + "?alt=media&supportsAllDrives=true",
    { headers: { Authorization: "Bearer " + access }, signal: AbortSignal.timeout(30000) },
  );
  if (!response.ok) throw new Error("Drive download failed");
  return new Uint8Array(await response.arrayBuffer());
}

Deno.serve(async request => {
  if (request.method === "OPTIONS") return new Response("ok", { headers: cors });
  if (request.method !== "POST") return json({ success: false, message: "Methode nicht erlaubt." }, 405);

  let dispatchId: number | null = null;
  let admin: ReturnType<typeof createClient> | null = null;
  let claimed = false;
  try {
    const authorization = request.headers.get("Authorization") ?? "";
    if (!authorization.startsWith("Bearer ")) return json({ success: false, message: "Keine gültige Session." }, 401);
    dispatchId = Number((await request.json())?.versandinstanz_id);
    if (!Number.isSafeInteger(dispatchId)) return json({ success: false, message: "versandinstanz_id fehlt." }, 400);

    const url = required("SUPABASE_URL");
    const caller = createClient(url, required("SUPABASE_ANON_KEY"), { global: { headers: { Authorization: authorization } } });
    admin = createClient(url, required("SUPABASE_SERVICE_ROLE_KEY"));
    const { data: claim, error: claimError } = await caller.rpc("claim_contract_dispatch", { p_versandinstanz_id: dispatchId });
    if (claimError || !claim?.[0]) return json({ success: false, message: "Versand nicht berechtigt oder nicht beanspruchbar." }, 403);
    claimed = true;

    const [{ data: dispatch, error: dispatchError }, { data: docs, error: docError }, { data: recipients, error: recipientError }] =
      await Promise.all([
        admin.from("versandinstanz").select("idempotenzschluessel").eq("id", dispatchId).single(),
        admin.from("versandinstanz_dokument_snapshot").select("*").eq("versandinstanz_id", dispatchId).eq("bindungsstatus", "digital_gebunden").order("sortierung"),
        admin.from("versandinstanz_empfaenger_snapshot").select("*").eq("versandinstanz_id", dispatchId).in("status", ["bereit", "fehler"]),
      ]);
    if (dispatchError || docError || recipientError || !dispatch || !docs?.length || !recipients?.length)
      throw new Error("Versand-Snapshot unvollständig");

    const access = await token();
    const attachments = await Promise.all(docs.map(async document => {
      if (document.drive_file_id_snapshot) {
        return { filename: document.dateiname_snapshot, content: await driveFile(access, document.drive_file_id_snapshot), contentType: document.mime_type_snapshot };
      }
      const download = await admin!.storage.from(document.bucket_snapshot).download(document.storage_path_snapshot);
      if (download.error || !download.data) throw new Error("Storage download failed");
      return { filename: document.dateiname_snapshot, content: new Uint8Array(await download.data.arrayBuffer()), contentType: document.mime_type_snapshot };
    }));

    const port = Number(required("SMTP_PORT"));
    const from = Deno.env.get("SMTP_FROM") ?? required("SMTP_USER");
    const messageDomain = (from.split("@").at(-1) ?? "kgv.local").replace(/[^A-Za-z0-9.-]/g, "") || "kgv.local";
    const transport = nodemailer.createTransport({
      host: required("SMTP_HOST"),
      port,
      secure: port === 465,
      auth: { user: required("SMTP_USER"), pass: required("smtpPassword") },
    });

    let failed = 0;
    for (const recipient of recipients) {
      const stableMessageId = "<" + dispatch.idempotenzschluessel + "." + recipient.id + "@" + messageDomain + ">";
      try {
        const original = String(recipient.email_normalisiert);
        const test = String(Deno.env.get("SMTP_TEST_RECIPIENT") ?? "").trim();
        const to = test || original;
        const result = await transport.sendMail({
          from,
          to,
          messageId: stableMessageId,
          subject: (test ? "[DEMO TEST] " : "") + claim[0].betreff,
          text: claim[0].mailtext + (test ? "\n\nDEMO-Testversand; ursprünglich: " + original : ""),
          attachments,
        });
        const ok = result.accepted.some((value: string) => value.toLowerCase() === to.toLowerCase())
          && !result.rejected.some((value: string) => value.toLowerCase() === to.toLowerCase());
        if (!ok) throw new Error("SMTP akzeptierte Empfänger nicht");
        const { error: attemptError } = await admin.from("versandinstanz_versuch").insert({
          versandinstanz_id: dispatchId, empfaenger_snapshot_id: recipient.id, status: "versendet",
          abgeschlossen_am: new Date().toISOString(), smtp_message_id: stableMessageId,
          smtp_antwort: { accepted: result.accepted, rejected: result.rejected },
        });
        if (attemptError) throw attemptError;
        const { error: recipientUpdateError } = await admin.from("versandinstanz_empfaenger_snapshot")
          .update({ status: "versendet", versendet_am: new Date().toISOString(), letzter_fehler: null }).eq("id", recipient.id);
        if (recipientUpdateError) throw recipientUpdateError;
      } catch (error) {
        failed++;
        const message = error instanceof Error ? error.message : "Versandfehler";
        await admin.from("versandinstanz_empfaenger_snapshot").update({ status: "fehler", letzter_fehler: message }).eq("id", recipient.id);
        await admin.from("versandinstanz_versuch").insert({
          versandinstanz_id: dispatchId, empfaenger_snapshot_id: recipient.id, status: "fehler",
          abgeschlossen_am: new Date().toISOString(), smtp_message_id: stableMessageId, fehlertext: message,
        });
      }
    }

    const { count } = await admin.from("versandinstanz_empfaenger_snapshot")
      .select("id", { count: "exact", head: true }).eq("versandinstanz_id", dispatchId).eq("status", "versendet");
    await admin.from("versandinstanz").update({
      status: failed ? (count ? "teilweise_fehlerhaft" : "fehler") : "versendet",
      versendet_am: failed ? null : new Date().toISOString(),
      letzter_fehler: failed ? String(failed) + " Empfänger fehlgeschlagen" : null,
    }).eq("id", dispatchId);
    return json({ success: failed === 0, versandinstanz_id: dispatchId, fehlgeschlagen: failed });
  } catch (error) {
    console.error("kgv-send-contract-documents", error);
    if (claimed && admin && dispatchId !== null) {
      await admin.from("versandinstanz").update({
        status: "fehler",
        letzter_fehler: error instanceof Error ? error.message : "Versandfehler",
      }).eq("id", dispatchId).eq("status", "wird_versendet");
    }
    return json({ success: false, message: "Der Vertragsversand konnte nicht bestätigt werden." }, 500);
  }
});
