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
  retryLeaseOwnerId?: string | null;
  retryLeaseUntil?: string | null;
};

const databaseName = "kgv-browser-media-v1";
const storeName = "pending-meter-photos";
const keyPath = "id";
const retryLeaseDurationMs = 15 * 60 * 1000;
const broadcastChannelName = "kgv-pending-media-v1";

export type PendingMediaChangeListener = () => void;
export type RetryClaimResult = { status: "claimed"; item: PendingMeterPhoto } | { status: "not-found" | "not-owned" | "already-in-flight" };
export type RemovePendingMediaResult = { status: "removed" | "not-found" | "not-owned" | "in-flight" };

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
  await withStore<void>(databaseName, storeName, keyPath, "readwrite", (store, resolve, reject) => { const request = store.put(item); request.onsuccess = () => resolve(); request.onerror = () => reject(request.error); });
  notifyPendingMediaChanged();
}

function hasActiveLease(item: PendingMeterPhoto, now = Date.now()) {
  const until = item.retryLeaseUntil ? Date.parse(item.retryLeaseUntil) : Number.NaN;
  return Boolean(item.retryLeaseOwnerId && Number.isFinite(until) && until > now);
}

function notifyPendingMediaChanged() {
  if (typeof BroadcastChannel === "undefined") return;
  const channel = new BroadcastChannel(broadcastChannelName);
  channel.postMessage({ type: "changed" });
  channel.close();
}

export function subscribePendingMediaChanges(listener: PendingMediaChangeListener) {
  if (typeof BroadcastChannel === "undefined") return () => undefined;
  const channel = new BroadcastChannel(broadcastChannelName);
  channel.onmessage = (event: MessageEvent<{ type?: string }>) => { if (event.data?.type === "changed") listener(); };
  return () => channel.close();
}

export async function claimPendingMeterPhotoRetry(id: string, clubId: string, ownerUserId: string, clientInstanceId: string): Promise<RetryClaimResult> {
  const result = await withStore<RetryClaimResult>(databaseName, storeName, keyPath, "readwrite", (store, resolve, reject) => {
    const request = store.get(id);
    request.onerror = () => reject(request.error);
    request.onsuccess = () => {
      if (!request.result) { resolve({ status: "not-found" }); return; }
      const item = normalizePendingMeterPhoto(request.result as PendingMeterPhoto);
      if (!belongsToOwner(item, clubId, ownerUserId)) { resolve({ status: "not-owned" }); return; }
      if (hasActiveLease(item)) { resolve({ status: "already-in-flight" }); return; }
      const claimed = { ...item, retryLeaseOwnerId: clientInstanceId, retryLeaseUntil: new Date(Date.now() + retryLeaseDurationMs).toISOString() };
      const write = store.put(claimed);
      write.onsuccess = () => resolve({ status: "claimed", item: claimed });
      write.onerror = () => reject(write.error);
    };
  });
  if (result.status === "claimed") notifyPendingMediaChanged();
  return result;
}

export async function releasePendingMeterPhotoRetryLease(id: string, clubId: string, ownerUserId: string, clientInstanceId: string) {
  const released = await withStore<boolean>(databaseName, storeName, keyPath, "readwrite", (store, resolve, reject) => {
    const request = store.get(id);
    request.onerror = () => reject(request.error);
    request.onsuccess = () => {
      if (!request.result) { resolve(false); return; }
      const item = normalizePendingMeterPhoto(request.result as PendingMeterPhoto);
      if (!belongsToOwner(item, clubId, ownerUserId) || item.retryLeaseOwnerId !== clientInstanceId) { resolve(false); return; }
      const write = store.put({ ...item, retryLeaseOwnerId: null, retryLeaseUntil: null });
      write.onsuccess = () => resolve(true);
      write.onerror = () => reject(write.error);
    };
  });
  if (released) notifyPendingMediaChanged();
  return released;
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
  await withStore<void>(databaseName, storeName, keyPath, "readwrite", (store, resolve, reject) => { const request = store.delete(id); request.onsuccess = () => resolve(); request.onerror = () => reject(request.error); });
  notifyPendingMediaChanged();
}

export async function removeOwnedPendingMeterPhoto(id: string, clubId: string, ownerUserId: string): Promise<RemovePendingMediaResult> {
  const result = await withStore<RemovePendingMediaResult>(databaseName, storeName, keyPath, "readwrite", (store, resolve, reject) => {
    const request = store.get(id);
    request.onerror = () => reject(request.error);
    request.onsuccess = () => {
      if (!request.result) { resolve({ status: "not-found" }); return; }
      const item = normalizePendingMeterPhoto(request.result as PendingMeterPhoto);
      if (!belongsToOwner(item, clubId, ownerUserId)) { resolve({ status: "not-owned" }); return; }
      if (hasActiveLease(item)) { resolve({ status: "in-flight" }); return; }
      const remove = store.delete(id);
      remove.onsuccess = () => resolve({ status: "removed" });
      remove.onerror = () => reject(remove.error);
    };
  });
  if (result.status === "removed") notifyPendingMediaChanged();
  return result;
}

export async function countLegacyPendingMeterPhotos(clubId: string) {
  const items = await withStore<PendingMeterPhoto[]>(databaseName, storeName, keyPath, "readonly", (store, resolve, reject) => { const request = store.getAll(); request.onsuccess = () => resolve((request.result as PendingMeterPhoto[]).map(normalizePendingMeterPhoto)); request.onerror = () => reject(request.error); });
  return items.filter((item) => item.clubId === clubId && item.ownerUserId === null).length;
}

export async function removeLegacyPendingMeterPhotos(clubId: string) {
  const removed = await withStore<number>(databaseName, storeName, keyPath, "readwrite", (store, resolve, reject) => {
    const request = store.getAll();
    request.onerror = () => reject(request.error);
    request.onsuccess = () => {
      const legacy = (request.result as PendingMeterPhoto[]).map(normalizePendingMeterPhoto).filter((item) => item.clubId === clubId && item.ownerUserId === null);
      if (!legacy.length) { resolve(0); return; }
      let completed = 0;
      for (const item of legacy) {
        const remove = store.delete(item.id);
        remove.onerror = () => reject(remove.error);
        remove.onsuccess = () => { completed += 1; if (completed === legacy.length) resolve(completed); };
      }
    };
  });
  if (removed) notifyPendingMediaChanged();
  return removed;
}

export function pendingPhotoFile(item: PendingMeterPhoto) {
  return new File([item.content], item.fileName, { type: item.contentType || "image/jpeg", lastModified: Date.now() });
}
