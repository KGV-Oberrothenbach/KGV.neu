import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
const source = await readFile(new URL("./index.ts", import.meta.url), "utf8");
test("signature upload validates JWT, signer identity and effective administrative capability", () => {
  assert.match(source, /caller\.auth\.getUser\(\)/);
  assert.match(source, /callerMemberId/);
  assert.match(source, /prepared_contract_signatur_snapshot_person/);
  assert.match(source, /position\.mitglied_id !== callerMemberId/);
  assert.match(source, /current_effective_permissions/);
  assert.match(source, /effectivePermissions\) & 524288/);
  assert.doesNotMatch(source, /\["admin", "vorstand"\]\.includes/);
});
