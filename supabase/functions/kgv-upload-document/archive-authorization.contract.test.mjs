import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
const source = await readFile(new URL("./index.ts", import.meta.url), "utf8");
test("archive uses authenticated named document archive permission", () => {
  assert.match(source, /supabaseAdmin\.auth\.getUser\(jwt\)/);
  assert.match(source, /ap02_has_effective_right", \{ p_fachrecht: "dokument\.archivieren" \}/);
  assert.match(source, /archiviert_at: new Date\(\)\.toISOString\(\)/);
  assert.doesNotMatch(source, /has_effective_permission", \{ p_permission: 2147483648 \}/);
});