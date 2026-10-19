import { resolveSupabaseConfig } from "../../lib/supabase/client-context";
import { readSupabaseRest } from "../../lib/supabase/rest-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { ClubDocumentType, ClubDocumentVersion } from "../../models/club-documents/club-document";

const authHeaders = (club: ClubContext, session: BrowserSession) => ({ apikey: resolveSupabaseConfig(club).publishableKey, Authorization: `Bearer ${session.accessToken}` });
export const getDocumentTypes = (club: ClubContext, session: BrowserSession) => readSupabaseRest<ClubDocumentType>({ club, session }, "vereinsdokument_typ", { select: "code,bezeichnung,pflicht", order: "code" });
export const getVersions = (club: ClubContext, session: BrowserSession, type: string) => readSupabaseRest<ClubDocumentVersion>({ club, session }, "vereinsdokument_fassung", { select: "*", dokumenttyp_code: `eq.${type}`, order: "gueltig_ab.desc" });

export async function uploadAndCreateVersion(club: ClubContext, session: BrowserSession, file: File, values: Pick<ClubDocumentVersion, "dokumenttyp_code" | "fassung" | "gueltig_ab" | "storage_path" | "dateiname">): Promise<ClubDocumentVersion> {
  if (file.type !== "application/pdf" && !/\.pdf$/i.test(file.name)) throw new Error("Es sind ausschließlich PDF-Dateien zulässig.");
  const config = resolveSupabaseConfig(club); const content = await file.arrayBuffer(); const sha256 = Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", content))).map(x => x.toString(16).padStart(2, "0")).join("");
  const path = values.storage_path!;
  const upload = await fetch(`${config.url}/storage/v1/object/vereinsregelwerke/${path}`, { method: "POST", headers: { ...authHeaders(club, session), "Content-Type": "application/pdf", "x-upsert": "false" }, body: content });
  if (!upload.ok) throw new Error("Die PDF konnte nicht unveränderlich gespeichert werden.");
  try {
    const rpc = await fetch(`${config.url}/rest/v1/rpc/create_vereinsdokument_fassung`, { method: "POST", headers: { ...authHeaders(club, session), "Content-Type": "application/json" }, body: JSON.stringify({ p_dokumenttyp_code: values.dokumenttyp_code, p_fassung: values.fassung, p_gueltig_ab: values.gueltig_ab, p_storage_path: path, p_dateiname: values.dateiname, p_sha256: sha256 }) });
    if (!rpc.ok) throw new Error("Die Fassung konnte nicht atomar angelegt werden.");
    return await rpc.json() as ClubDocumentVersion;
  } catch (error) {
    await fetch(`${config.url}/storage/v1/object/vereinsregelwerke/${path}`, { method: "DELETE", headers: authHeaders(club, session) });
    throw error;
  }
}
