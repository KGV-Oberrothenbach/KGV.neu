import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
const lease = await readFile(new URL("./kgv-complete-prepared-lease-contract/index.ts", import.meta.url), "utf8");
const discard = await readFile(new URL("./kgv-discard-prepared-contract/index.ts", import.meta.url), "utf8");
const documentUpload = await readFile(new URL("./kgv-upload-document/index.ts", import.meta.url), "utf8");
for (const [name, source, rights, rpc, actor] of [
  ["prepared lease completion",lease,["pachtvertrag.verwalten"],"complete_prepared_pachtvertrag","p_created_by"],
  ["prepared contract discard",discard,["mitglied.anlegen","dokument.verwalten"],"discard_prepared_contract","p_discarded_by"],
]) {
  test(`${name} validates JWT and named rights before privileged RPC`, () => {
    assert.match(source, /authorization\.startsWith\("Bearer "\)/);
    assert.match(source, /caller\.auth\.getUser\(\)/);
    for (const right of rights) assert.ok(source.includes(`p_fachrecht: "${right}"`));
    assert.doesNotMatch(source, /caller\.rpc\("has_effective_permission"/);
    assert.ok(source.includes(`admin.rpc("${rpc}"`));
    assert.ok(source.includes(`${actor}: user`));
  });
}
test("document archive keeps authenticated live-document lookup and new document right", () => {
  assert.ok(documentUpload.includes('p_fachrecht: "dokument.verwalten"'));
  assert.match(documentUpload, /const documentId = Number\(payload\.document_id\)/);
  assert.match(documentUpload, /const document = await mayReadDocument\(auth, documentId\)/);
  assert.match(documentUpload, /action === "archive"/);
});