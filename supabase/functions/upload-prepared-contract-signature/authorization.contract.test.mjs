import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
const source = await readFile(new URL("./index.ts", import.meta.url), "utf8");
test("signature upload validates JWT and signer identity, not an administrative role", () => {
  assert.match(source, /caller\.auth\.getUser\(\)/);
  assert.match(source, /callerMemberId/);
  assert.match(source, /prepared_contract_signatur_snapshot_person/);
  assert.match(source, /position\.mitglied_id !== callerMemberId/);
  assert.doesNotMatch(source, /entry\.role === "admin"/);
});
