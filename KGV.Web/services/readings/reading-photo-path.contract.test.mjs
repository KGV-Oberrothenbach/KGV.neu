import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const repository = readFileSync(new URL("../../repositories/readings/reading-photo-repository.ts", import.meta.url), "utf8");
const edgeFunction = readFileSync(new URL("../../../supabase/functions/kgv-upload-photo/index.ts", import.meta.url), "utf8");
const page = readFileSync(new URL("../../app/page.tsx", import.meta.url), "utf8");

test("web reading photos bind the upload to an authenticated reading and never persist Drive ids in the browser", () => {
  assert.match(repository, /form\.set\("ablesung_id", String\(readingId\)\)/);
  assert.doesNotMatch(repository, /foto_drive_file_id|foto_pfad|writeWithBrowserEditLock/);
  assert.match(edgeFunction, /mayReadMeterReadingPhoto\(auth, ablesungId\)/);
  assert.match(edgeFunction, /\.is\("foto_drive_file_id", null\)/);
  assert.doesNotMatch(edgeFunction, /auth\.role === "admin"/);
  assert.match(edgeFunction, /"Cache-Control": "private, no-store"/);
  assert.doesNotMatch(page, /path && \^https\?:\\\/\\\//);
});
