import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { invokeEdgeFunctionRaw } from "../../lib/supabase/edge-client";

export type MeterPhotoKind = "ablesung" | "einbau" | "ausbau";
export type ReadingPhotoKind = MeterPhotoKind;
export type ReadingPhotoDetails = { datum: string; medium: string; anlage: string; garten: string; zaehlernummer: string; kind?: ReadingPhotoKind };
export type UploadedReadingPhoto = { message: string };

export async function uploadReadingPhoto(club: ClubContext, session: BrowserSession, readingId: number, file: File, _details: ReadingPhotoDetails): Promise<UploadedReadingPhoto> {
  const form = new FormData();
  form.set("file", file);
  form.set("ablesung_id", String(readingId));
  const response = await invokeEdgeFunctionRaw({ club, session }, "kgv-upload-photo", { method: "POST", body: form });
  const payload = await response.json().catch(() => null) as { message?: string } | null;
  if (!response.ok) throw new Error(payload?.message ?? "Das Ablesefoto konnte nicht hochgeladen werden.");
  return { message: payload?.message ?? "Ablesungsfoto wurde geschützt gespeichert." };
}

export async function openReadingPhoto(club: ClubContext, session: BrowserSession, readingId: number) {
  const response = await invokeEdgeFunctionRaw({ club, session }, "kgv-upload-photo", { method: "POST", contentType: "application/json", body: JSON.stringify({ action: "download", ablesung_id: readingId }) });
  if (!response.ok) {
    const detail = await response.json().catch(() => null) as { message?: string } | null;
    throw new Error(detail?.message ?? "Das Ablesungsfoto konnte nicht geladen werden.");
  }
  return URL.createObjectURL(await response.blob());
}
