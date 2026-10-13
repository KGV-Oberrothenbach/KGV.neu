export function openKeyValueStore(databaseName: string, storeName: string, keyPath: string): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(databaseName, 1);
    request.onupgradeneeded = () => {
      if (!request.result.objectStoreNames.contains(storeName)) request.result.createObjectStore(storeName, { keyPath });
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error ?? new Error("Lokaler Speicher konnte nicht geöffnet werden."));
  });
}

export async function withStore<T>(databaseName: string, storeName: string, keyPath: string, mode: IDBTransactionMode, action: (store: IDBObjectStore, resolve: (value: T) => void, reject: (reason?: unknown) => void) => void): Promise<T> {
  const database = await openKeyValueStore(databaseName, storeName, keyPath);
  return new Promise<T>((resolve, reject) => {
    const transaction = database.transaction(storeName, mode);
    action(transaction.objectStore(storeName), resolve, reject);
    transaction.oncomplete = () => database.close();
    transaction.onerror = () => reject(transaction.error ?? new Error("Lokaler Speicher konnte nicht aktualisiert werden."));
  });
}
