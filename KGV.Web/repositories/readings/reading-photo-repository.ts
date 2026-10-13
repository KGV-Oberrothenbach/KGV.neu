import { writeWithBrowserEditLock } from "../../services/concurrency/edit-lock-service";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { invokeEdgeFunctionRaw } from "../../lib/supabase/edge-client";

export type MeterPhotoKind = "ablesung" | "einbau" | "ausbau";
export type ReadingPhotoKind = MeterPhotoKind;
export type ReadingPhotoDetails = { datum: string; medium: string; anlage: string; garten: string; zaehlernummer: string; kind?: ReadingPhotoKind };
export type UploadedReadingPhoto = { fileId: string; fileName: string };

export async function uploadReadingPhoto(club: ClubContext, session: BrowserSession, file: File, details: ReadingPhotoDetails): Promise<UploadedReadingPhoto> {
  const form = new FormData();
  form.set("file", file);
  form.set("kind", details.kind ?? "ablesung");
  form.set("datum", details.datum);
  form.set("medium", details.medium);
  form.set("anlage", details.anlage);
  form.set("garten", details.garten);
  form.set("zaehlernummer", details.zaehlernummer);
  const response = await invokeEdgeFunctionRaw({ club, session }, "kgv-upload-photo", { method: "POST", body: form });
  const payload = await response.json().catch(() => null) as { file_id?: string; file_name?: string; message?: string } | null;
  if (!response.ok || !payload?.file_id) throw new Error(payload?.message ?? "Das Ablesefoto konnte nicht hochgeladen werden.");
  return { fileId: payload.file_id, fileName: payload.file_name ?? file.name };
}

export const linkReadingPhoto = (club: ClubContext, session: BrowserSession, readingId: number, photo: UploadedReadingPhoto) =>
  writeWithBrowserEditLock({ club, session }, "zaehler_ablesung", "PATCH", { foto_drive_file_id: photo.fileId, foto_dateiname: photo.fileName }, { id: `eq.${readingId}` });

export async function openReadingPhoto(club: ClubContext, session: BrowserSession, readingId: number) {
  const response = await invokeEdgeFunctionRaw({ club, session }, "kgv-upload-photo", { method: "POST", contentType: "application/json", body: JSON.stringify({ action: "download", ablesung_id: readingId }) });
  if (!response.ok) {
    const detail = await response.json().catch(() => null) as { message?: string } | null;
    throw new Error(detail?.message ?? "Das Ablesungsfoto konnte nicht geladen werden.");
  }
  return URL.createObjectURL(await response.blob());
}
