import { enqueueMeterPhoto, getPendingMeterPhoto, listPendingMeterPhotos, pendingPhotoFile, putPendingMeterPhoto, removePendingMeterPhoto, type PendingMeterPhoto } from "../../lib/browser-media";
import { type BrowserSession } from "../../lib/supabase-auth";
import { linkReadingPhoto, openReadingPhoto, uploadReadingPhoto, type ReadingPhotoDetails, type UploadedReadingPhoto } from "../../repositories/readings/reading-photo-repository";

export type ReadingPhotoResult = { status: "uploaded" | "queued" | "failed" | "no-photo" | "already-in-flight"; message: string };
export type ReadingPhotoInput = { clubId: string; readingId: number; file: File | null; details: ReadingPhotoDetails; wifiOnly: boolean };

const now = () => new Date().toISOString();
const pendingPhotoAttempts = new Set<string>();
const uploadDecision = (wifiOnly: boolean) => {
  if (typeof navigator === "undefined" || !navigator.onLine) return { allowed: false, reason: "Keine Internetverbindung. Foto bleibt lokal gespeichert." };
  if (!wifiOnly) return { allowed: true, reason: "" };
  const connection = (navigator as Navigator & { connection?: { type?: string } }).connection;
  return connection?.type === "wifi"
    ? { allowed: true, reason: "" }
    : { allowed: false, reason: connection?.type ? "Upload ist aktuell auf WLAN beschränkt." : "WLAN-Verbindung ist unbekannt. Foto bleibt vorsorglich lokal gespeichert." };
};

const failed = async (item: PendingMeterPhoto, error: string, incrementAttempt: boolean) => {
  const next = { ...item, status: "failed" as const, lastError: error, lastAttemptAt: now(), attemptCount: item.attemptCount + (incrementAttempt ? 1 : 0) };
  await putPendingMeterPhoto(next);
  return next;
};

const completeLink = async (session: BrowserSession, item: PendingMeterPhoto, photo: UploadedReadingPhoto) => {
  await linkReadingPhoto(session, item.readingId, photo);
  await removePendingMeterPhoto(item.id);
};

export async function handleReadingPhoto(session: BrowserSession, input: ReadingPhotoInput): Promise<ReadingPhotoResult> {
  if (!input.file) return { status: "no-photo", message: "Ablesung wurde ohne Foto gespeichert." };
  const decision = uploadDecision(input.wifiOnly);
  if (!decision.allowed) {
    await enqueueMeterPhoto({ clubId: input.clubId, readingId: input.readingId, fileName: input.file.name || `ablesung-${input.readingId}.jpg`, contentType: input.file.type || "image/jpeg", content: input.file, details: input.details });
    return { status: "queued", message: decision.reason };
  }
  try {
    const photo = await uploadReadingPhoto(session, input.file, input.details);
    try {
      await linkReadingPhoto(session, input.readingId, photo);
      return { status: "uploaded", message: "Foto wurde hochgeladen und mit der Ablesung verknüpft." };
    } catch (cause) {
      const item = await enqueueMeterPhoto({ clubId: input.clubId, readingId: input.readingId, fileName: input.file.name || `ablesung-${input.readingId}.jpg`, contentType: input.file.type || "image/jpeg", content: input.file, details: input.details });
      await failed({ ...item, uploadedFileId: photo.fileId, uploadedFileName: photo.fileName }, cause instanceof Error ? cause.message : "Foto konnte nicht mit der Ablesung verknüpft werden.", true);
      return { status: "failed", message: "Foto wurde hochgeladen, die Verknüpfung wird später erneut versucht." };
    }
  } catch (cause) {
    const item = await enqueueMeterPhoto({ clubId: input.clubId, readingId: input.readingId, fileName: input.file.name || `ablesung-${input.readingId}.jpg`, contentType: input.file.type || "image/jpeg", content: input.file, details: input.details });
    await failed(item, cause instanceof Error ? cause.message : "Upload fehlgeschlagen.", true);
    return { status: "failed", message: "Ablesung wurde gespeichert. Das Foto bleibt lokal und wird erneut versucht." };
  }
}

export async function retryPendingReadingPhoto(session: BrowserSession, clubId: string, wifiOnly: boolean, item: PendingMeterPhoto): Promise<ReadingPhotoResult> {
  try {
    const pending = await getPendingMeterPhoto(item.id);
    if (!pending) return { status: "failed", message: "Das lokale Foto wurde nicht mehr gefunden." };
    if (pending.clubId !== clubId) return { status: "failed", message: "Dieses Foto gehört zu einem anderen Verein und wird nicht hochgeladen." };
    if (pendingPhotoAttempts.has(pending.id) || pending.status === "uploading") return { status: "already-in-flight", message: "Für dieses Foto läuft bereits ein Upload-Versuch." };
    const decision = uploadDecision(wifiOnly);
    if (!decision.allowed) return { status: "queued", message: decision.reason };

    pendingPhotoAttempts.add(pending.id);
    let attempted = pending;
    try {
      attempted = { ...pending, status: "uploading" as const, lastError: null, lastAttemptAt: now(), attemptCount: pending.attemptCount + 1 };
      await putPendingMeterPhoto(attempted);
      const photo = attempted.uploadedFileId ? { fileId: attempted.uploadedFileId, fileName: attempted.uploadedFileName || attempted.fileName } : await uploadReadingPhoto(session, pendingPhotoFile(attempted), attempted.details);
      if (!attempted.uploadedFileId) {
        attempted = { ...attempted, uploadedFileId: photo.fileId, uploadedFileName: photo.fileName };
        await putPendingMeterPhoto(attempted);
      }
      await completeLink(session, attempted, photo);
      return { status: "uploaded", message: "Foto wurde hochgeladen und mit der Ablesung verknüpft." };
    } catch (cause) {
      try {
        await failed(attempted, cause instanceof Error ? cause.message : "Upload oder Verknüpfung fehlgeschlagen.", false);
      } catch {
        // The original retry error is reported below; automatic sync must not reject in the background.
      }
      return { status: "failed", message: "Upload fehlgeschlagen. Das Foto bleibt lokal gespeichert." };
    } finally {
      pendingPhotoAttempts.delete(pending.id);
    }
  } catch (cause) {
    return { status: "failed", message: cause instanceof Error ? cause.message : "Lokaler Fotospeicher konnte nicht gelesen werden." };
  }
}

export async function retryAllPendingReadingPhotos(session: BrowserSession, clubId: string, wifiOnly: boolean) {
  try {
    const items = await listPendingMeterPhotos(clubId);
    const results = await Promise.all(items.map(async (pending) => {
      try {
        return await retryPendingReadingPhoto(session, clubId, wifiOnly, pending);
      } catch {
        return { status: "failed" as const, message: "Upload fehlgeschlagen. Das Foto bleibt lokal gespeichert." };
      }
    }));
    return {
      uploaded: results.filter((result) => result.status === "uploaded").length,
      failed: results.filter((result) => result.status === "failed").length,
      queued: results.filter((result) => result.status === "queued").length,
      alreadyInFlight: results.filter((result) => result.status === "already-in-flight").length,
    };
  } catch {
    return { uploaded: 0, failed: 0, queued: 0, alreadyInFlight: 0 };
  }
}

export const listPendingReadingPhotos = listPendingMeterPhotos;
export const openStoredReadingPhoto = openReadingPhoto;
export type { PendingMeterPhoto };
