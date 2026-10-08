export type PendingMeterPhoto = {
  id: string;
  clubId: string;
  readingId: number;
  fileName: string;
  contentType: string;
  content: Blob;
  details: { datum: string; medium: string; anlage: string; garten: string; zaehlernummer: string; kind?: "ablesung" | "einbau" };
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

function database(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(databaseName, 1);
    request.onupgradeneeded = () => {
      if (!request.result.objectStoreNames.contains(storeName)) request.result.createObjectStore(storeName, { keyPath: "id" });
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error ?? new Error("Lokaler Fotospeicher konnte nicht geöffnet werden."));
  });
}

function transaction<T>(mode: IDBTransactionMode, action: (store: IDBObjectStore, resolve: (value: T) => void, reject: (reason?: unknown) => void) => void): Promise<T> {
  return database().then((db) => new Promise<T>((resolve, reject) => {
    const tx = db.transaction(storeName, mode);
    action(tx.objectStore(storeName), resolve, reject);
    tx.oncomplete = () => db.close();
    tx.onerror = () => reject(tx.error ?? new Error("Lokaler Fotospeicher konnte nicht aktualisiert werden."));
  }));
}

export async function enqueueMeterPhoto(input: Omit<PendingMeterPhoto, "id" | "status" | "createdAt" | "lastAttemptAt" | "attemptCount" | "lastError">) {
  const item: PendingMeterPhoto = { ...input, id: crypto.randomUUID(), status: "pending", createdAt: new Date().toISOString(), lastAttemptAt: null, attemptCount: 0, lastError: null, uploadedFileId: null, uploadedFileName: null };
  await putPendingMeterPhoto(item);
  return item;
}

export async function putPendingMeterPhoto(item: PendingMeterPhoto) {
  return transaction<void>("readwrite", (store, resolve, reject) => { const request = store.put(item); request.onsuccess = () => resolve(); request.onerror = () => reject(request.error); });
}

export async function getPendingMeterPhoto(id: string) {
  return transaction<PendingMeterPhoto | null>("readonly", (store, resolve, reject) => {
    const request = store.get(id);
    request.onsuccess = () => resolve((request.result as PendingMeterPhoto | undefined) ?? null);
    request.onerror = () => reject(request.error);
  });
}

export async function listPendingMeterPhotos(clubId: string) {
  const items = await transaction<PendingMeterPhoto[]>("readonly", (store, resolve, reject) => { const request = store.getAll(); request.onsuccess = () => resolve(request.result as PendingMeterPhoto[]); request.onerror = () => reject(request.error); });
  return items.filter((item) => item.clubId === clubId).sort((a, b) => b.createdAt.localeCompare(a.createdAt));
}

export async function countPendingMeterPhotos(clubId: string) {
  return (await listPendingMeterPhotos(clubId)).length;
}

export async function removePendingMeterPhoto(id: string) {
  return transaction<void>("readwrite", (store, resolve, reject) => { const request = store.delete(id); request.onsuccess = () => resolve(); request.onerror = () => reject(request.error); });
}

export function pendingPhotoFile(item: PendingMeterPhoto) {
  return new File([item.content], item.fileName, { type: item.contentType || "image/jpeg", lastModified: Date.now() });
}

export type BarcodeDetectorLike = { detect(source: ImageBitmapSource): Promise<Array<{ rawValue?: string }>> };
export type BarcodeDetectorConstructor = new (options?: { formats?: string[] }) => BarcodeDetectorLike;

export function barcodeDetectorConstructor() {
  return (globalThis as typeof globalThis & { BarcodeDetector?: BarcodeDetectorConstructor }).BarcodeDetector ?? null;
}

export type NdefReaderLike = EventTarget & { scan(): Promise<void>; onreading: ((event: Event & { message?: { records?: Array<{ recordType?: string; data?: DataView; encoding?: string }> }; serialNumber?: string }) => void) | null; onreadingerror: ((event: Event) => void) | null };
export type NdefReaderConstructor = new () => NdefReaderLike;

export function ndefReaderConstructor() {
  return (globalThis as typeof globalThis & { NDEFReader?: NdefReaderConstructor }).NDEFReader ?? null;
}
