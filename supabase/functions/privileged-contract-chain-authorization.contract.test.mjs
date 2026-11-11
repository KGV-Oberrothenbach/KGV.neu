import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";

const lease = await readFile(new URL("./kgv-complete-prepared-lease-contract/index.ts", import.meta.url), "utf8");
const discard = await readFile(new URL("./kgv-discard-prepared-contract/index.ts", import.meta.url), "utf8");
const documentUpload = await readFile(new URL("./kgv-upload-document/index.ts", import.meta.url), "utf8");

for (const [name, source, requiredPermission, rpc, actorParameter] of [
  ["prepared lease completion", lease, 524288, "complete_prepared_pachtvertrag", "p_created_by"],
  ["prepared contract discard", discard, 524304, "discard_prepared_contract", "p_discarded_by"],
]) {
  test(`${name} validates the deployed-project JWT and effective caller permission before its service-only RPC`, () => {
    assert.match(source, /authorization\.startsWith\("Bearer "\)/);
    assert.match(source, /caller\.auth\.getUser\(\)/);
    assert.match(source, new RegExp(`caller\\.rpc\\("has_effective_permission", \\{ p_permission: ${requiredPermission} \\}\\)`));
    assert.doesNotMatch(source, /current_app_user_context[\s\S]*?role === "admin"[\s\S]*?role === "vorstand"/);
    assert.match(source, new RegExp(`admin\\.rpc\\("${rpc}", \\{[\\s\\S]*?${actorParameter}: user(?:Data)?\\.user\\.id`));
  });
}

test("document Drive deletion binds the requested file to a live document record and effective document management", () => {
  assert.match(documentUpload, /has_effective_permission", \{ p_permission: 16 \}/);
  assert.match(documentUpload, /const documentId = Number\(payload\.document_id\)/);
  assert.match(documentUpload, /\.eq\("id", documentId\)[\s\S]*?\.is\("archiviert_at", null\)/);
  assert.match(documentUpload, /String\(document\.drive_file_id \?\? ""\)\.trim\(\) !== driveFileId/);
  assert.match(documentUpload, /Die Drive-Datei gehört nicht zum angegebenen aktiven Dokument/);
});
