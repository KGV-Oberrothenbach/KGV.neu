import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const repository = readFileSync(new URL("../../repositories/readings/reading-photo-repository.ts", import.meta.url), "utf8");
const edgeFunction = readFileSync(new URL("../../../supabase/functions/kgv-upload-photo/index.ts", import.meta.url), "utf8");

test("web reading photos persist the Edge Function's canonical relative path", () => {
  assert.match(repository, /relative_path/);
  assert.match(repository, /foto_pfad: photo\.relativePath/);
  assert.match(repository, /foto_drive_file_id: photo\.fileId/);
  assert.match(edgeFunction, /relative_path: relativePath/);
});
