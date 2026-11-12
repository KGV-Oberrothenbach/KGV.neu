import assert from "node:assert/strict";
import test from "node:test";
import { edgeHarness } from "./edge-test-runtime.mjs";

const request = (documentId = 77) => new Request("https://synthetic.invalid", {
  method: "POST",
  headers: { Authorization: "Bearer user-token", "Content-Type": "application/json" },
  body: JSON.stringify({ action: "download", document_id: documentId }),
});

test("former member download uses the caller-bound R05 RPC", async () => {
  const permitted = edgeHarness({ grants: 0, ownHistoryAccess: true });
  assert.equal((await permitted.request(request())).status, 200);
  assert.ok(permitted.calls.rpc.some((call) => call.name === "can_access_own_member_history_document" && call.args.p_dokument_id === 77));

  const expired = edgeHarness({ grants: 0, ownHistoryAccess: false });
  assert.equal((await expired.request(request())).status, 403);
  assert.ok(expired.calls.rpc.some((call) => call.name === "can_access_own_member_history_document"));
});

test("document managers use effective permissions and revoked administrators are denied", async () => {
  const granted = edgeHarness({ grants: 65536 });
  assert.equal((await granted.request(request())).status, 200);

  const revokedAdmin = edgeHarness({ role: "admin", basePermissions: 65536, grants: 0, revocations: 65536 });
  assert.equal((await revokedAdmin.request(request())).status, 403);
});

test("a forged document id is never accepted as a former-member identity", async () => {
  const harness = edgeHarness({ grants: 0, ownHistoryAccess: true });
  assert.equal((await harness.request(request(12345))).status, 403);
  assert.equal(harness.calls.rpc.some((call) => call.name === "can_access_own_member_history_document"), false);
});
