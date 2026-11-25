import assert from "node:assert/strict";
import test from "node:test";
import { edgeHarness } from "./edge-test-runtime.mjs";

const request = (documentId = 77) => new Request("https://synthetic.invalid", {
  method: "POST",
  headers: { Authorization: "Bearer user-token", "Content-Type": "application/json" },
  body: JSON.stringify({ action: "download", document_id: documentId }),
});

test("former tenant download uses caller-bound RLS with R10 lease-history scope", async () => {
  const beforeHandover = edgeHarness({ grants: 0, ownLeaseHistoryAccess: true });
  assert.equal((await beforeHandover.request(request())).status, 200);
  assert.ok(beforeHandover.calls.selects.some(call => call.table === "dokument" && call.caller && call.ids.includes(77)));
  assert.ok(!beforeHandover.calls.selects.some(call => call.table === "dokument" && !call.caller));

  const afterHandover = edgeHarness({ grants: 0, ownLeaseHistoryAccess: false });
  assert.equal((await afterHandover.request(request())).status, 403);
  assert.ok(afterHandover.calls.selects.some(call => call.table === "dokument" && call.caller));
});

test("a forged document id cannot turn into a former-tenant authorization", async () => {
  const harness = edgeHarness({ grants: 0, ownLeaseHistoryAccess: true });
  assert.equal((await harness.request(request(12345))).status, 403);
  assert.ok(harness.calls.selects.some(call => call.table === "dokument" && call.caller && call.ids.length === 0));
  assert.ok(!harness.calls.selects.some(call => call.table === "dokument" && !call.caller));
});
