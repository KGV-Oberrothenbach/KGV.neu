import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
const source = await readFile(new URL("./index.ts", import.meta.url), "utf8");
test("signature upload binds signer and administrative workflow to named right", () => {
  assert.match(source, /caller\.auth\.getUser\(\)/);
  assert.match(source, /callerMemberId/);
  assert.match(source, /prepared_contract_signatur_snapshot_person/);
  assert.match(source, /position\.mitglied_id !== callerMemberId/);
  assert.match(source, /ap02_has_effective_right/);
  assert.match(source, /p_fachrecht: "vertrag\.vorbereiten"/);
  assert.match(source, /canPrepareContract === true/);
  const signerGate = source.indexOf('const administrativeCaller = canPrepareContract === true');
  const replay = source.indexOf('if (position.status === "unterschrieben")');
  assert.ok(signerGate >= 0 && replay > signerGate);
  assert.ok(source.indexOf('position.mitglied_id !== callerMemberId') < replay);
  assert.doesNotMatch(source, /current_effective_permissions/);
  assert.doesNotMatch(source, /effectivePermissions\) & 524288/);
});