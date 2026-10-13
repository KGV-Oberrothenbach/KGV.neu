import { withStore } from "../../lib/browser/indexed-db";

export type PendingMeterPhoto = {
  id: string;
  clubId: string;
  ownerUserId: string | null;
  readingId: number;
  fileName: string;
  contentType: string;
  content: Blob;
  details: { datum: string; medium: string; anlage: string; garten: string; zaehlernummer: string; kind?: "ablesung" | "einbau" | "ausbau" };
  status: "pending" | "uploading" | "failed";
  createdAt: string;
  lastAttemptAt: string | null;
  attemptCount: number;
  lastError: string | null;
  uploadedFileId?: string | null;
  uploadedFileName?: string | null;
};

const databaseName = "kgv-browser-media-v1";
const storeName = "pending-meter-photos";
const keyPath = "id";

export function normalizePendingMeterPhoto(item: Omit<PendingMeterPhoto, "ownerUserId"> & { ownerUserId?: string | null }): PendingMeterPhoto {
  return { ...item, ownerUserId: typeof item.ownerUserId === "string" && item.ownerUserId.trim() ? item.ownerUserId : null };
}

export function belongsToOwner(item: Pick<PendingMeterPhoto, "clubId" | "ownerUserId">, clubId: string, ownerUserId: string) {
  return item.clubId === clubId && item.ownerUserId === ownerUserId;
}

export async function enqueuePendingMeterPhoto(input: Omit<PendingMeterPhoto, "id" | "status" | "createdAt" | "lastAttemptAt" | "attemptCount" | "lastError" | "uploadedFileId" | "uploadedFileName"> & { ownerUserId: string }) {
  if (!input.ownerUserId.trim()) throw new Error("Lokale Fotos benötigen einen angemeldeten Benutzer.");
  const item: PendingMeterPhoto = { ...input, ownerUserId: input.ownerUserId.trim(), id: crypto.randomUUID(), status: "pending", createdAt: new Date().toISOString(), lastAttemptAt: null, attemptCount: 0, lastError: null, uploadedFileId: null, uploadedFileName: null };
  await putPendingMeterPhoto(item);
  return item;
}

export async function putPendingMeterPhoto(item: PendingMeterPhoto) {
  return withStore<void>(databaseName, storeName, keyPath, "readwrite", (store, resolve, reject) => { const request = store.put(item); request.onsuccess = () => resolve(); request.onerror = () => reject(request.error); });
}

export async function getPendingMeterPhoto(id: string) {
  return withStore<PendingMeterPhoto | null>(databaseName, storeName, keyPath, "readonly", (store, resolve, reject) => {
    const request = store.get(id);
    request.onsuccess = () => resolve(request.result ? normalizePendingMeterPhoto(request.result as PendingMeterPhoto) : null);
    request.onerror = () => reject(request.error);
  });
}

export async function listPendingMeterPhotos(clubId: string, ownerUserId: string) {
  const items = await withStore<PendingMeterPhoto[]>(databaseName, storeName, keyPath, "readonly", (store, resolve, reject) => { const request = store.getAll(); request.onsuccess = () => resolve((request.result as PendingMeterPhoto[]).map(normalizePendingMeterPhoto)); request.onerror = () => reject(request.error); });
  return items.filter((item) => belongsToOwner(item, clubId, ownerUserId)).sort((a, b) => b.createdAt.localeCompare(a.createdAt));
}

export async function countPendingMeterPhotos(clubId: string, ownerUserId: string) {
  return (await listPendingMeterPhotos(clubId, ownerUserId)).length;
}

export async function removePendingMeterPhoto(id: string) {
  return withStore<void>(databaseName, storeName, keyPath, "readwrite", (store, resolve, reject) => { const request = store.delete(id); request.onsuccess = () => resolve(); request.onerror = () => reject(request.error); });
}

export function pendingPhotoFile(item: PendingMeterPhoto) {
  return new File([item.content], item.fileName, { type: item.contentType || "image/jpeg", lastModified: Date.now() });
}
