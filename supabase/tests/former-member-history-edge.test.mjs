import assert from "node:assert/strict";
import test from "node:test";
import { edgeHarness } from "./edge-test-runtime.mjs";

const request = (documentId = 77) => new Request("https://synthetic.invalid", {
  method: "POST",
  headers: { Authorization: "Bearer user-token", "Content-Type": "application/json" },
  body: JSON.stringify({ action: "download", document_id: documentId }),
});

test("former member download uses caller-bound RLS with R05 scope", async () => {
  const permitted = edgeHarness({ grants: 0, ownHistoryAccess: true });
  assert.equal((await permitted.request(request())).status, 200);
  assert.ok(permitted.calls.selects.some(call => call.table === "dokument" && call.caller && call.ids.includes(77)));
  assert.ok(!permitted.calls.selects.some(call => call.table === "dokument" && !call.caller));

  const expired = edgeHarness({ grants: 0, ownHistoryAccess: false });
  assert.equal((await expired.request(request())).status, 403);
  assert.ok(expired.calls.selects.some(call => call.table === "dokument" && call.caller));
  assert.ok(!expired.calls.selects.some(call => call.table === "dokument" && !call.caller));
});

test("document readers need named read right and object scope; revoked administrators are denied", async () => {
  const granted = edgeHarness({ grants: 0, rights: ["dokument.lesen"], memberScope: true });
  assert.equal((await granted.request(request())).status, 200);

  const revokedAdmin = edgeHarness({ role: "admin", grants: 0, rights: ["dokument.lesen"], deniedRights: ["dokument.lesen"], memberScope: true });
  assert.equal((await revokedAdmin.request(request())).status, 403);
  for (const options of [{rights:["dokument.lesen"],memberScope:false},{rights:["dokument.verwalten"],memberScope:true}]) {
    const denied = edgeHarness({grants:0,...options});
    assert.equal((await denied.request(request())).status,403);
  }
});

test("a forged document id is never accepted as a former-member identity", async () => {
  const harness = edgeHarness({ grants: 0, ownHistoryAccess: true });
  assert.equal((await harness.request(request(12345))).status, 403);
  assert.ok(harness.calls.selects.some(call => call.table === "dokument" && call.caller && call.ids.length === 0));
  assert.ok(!harness.calls.selects.some(call => call.table === "dokument" && !call.caller));
});
