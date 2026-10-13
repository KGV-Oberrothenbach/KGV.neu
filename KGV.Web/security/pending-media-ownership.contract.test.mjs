import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const [store, photoService, syncService, queue, model] = await Promise.all([
  readFile(new URL("../services/offline/pending-media-store.ts", import.meta.url), "utf8"),
  readFile(new URL("../services/readings/reading-photo-service.ts", import.meta.url), "utf8"),
  readFile(new URL("../../KGV.Maui/Services/PendingPhotos/PendingPhotoSyncService.cs", import.meta.url), "utf8"),
  readFile(new URL("../../KGV.Maui/Services/PendingPhotos/PendingPhotoQueue.cs", import.meta.url), "utf8"),
  readFile(new URL("../../KGV.Maui/Models/PendingPhotoUpload.cs", import.meta.url), "utf8"),
]);
assert.match(store, /ownerUserId: string \| null/);
assert.match(store, /ownerUserId: typeof item\.ownerUserId === "string" && item\.ownerUserId\.trim\(\) \? item\.ownerUserId : null/);
assert.match(store, /item\.clubId === clubId && item\.ownerUserId === ownerUserId/);
assert.match(store, /filter\(\(item\) => belongsToOwner\(item, clubId, ownerUserId\)\)/);
assert.match(photoService, /ownerUserId: owner/);
assert.match(photoService, /pending\.ownerUserId !== owner/);
assert.match(photoService, /listPendingMeterPhotos\(clubId, ownerUserId\(session\)\)/);
assert.match(syncService, /GetForOwner\(vereinId\.Value, ownerUserId\.Value\)/);
assert.match(syncService, /latest\.OwnerUserId != ownerUserId\.Value/);
assert.match(queue, /GetOpenForOwner/);
assert.match(model, /Guid\? OwnerUserId/);
console.log("pending media ownership contract: ok");
