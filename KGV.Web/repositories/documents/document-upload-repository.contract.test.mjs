import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("document upload keeps the multipart edge contract and forwards diagnostic fields", async () => {
  const [repository, edgeClient] = await Promise.all([
    readFile(new URL("./document-upload-repository.ts", import.meta.url), "utf8"),
    readFile(new URL("../../lib/supabase/edge-client.ts", import.meta.url), "utf8"),
  ]);

  assert.match(repository, /form\.set\("file", file\)/);
  assert.match(repository, /form\.set\("owner_kind", owner\.kind === "member" \? "mitglied" : "parzelle"\)/);
  assert.match(repository, /form\.set\("owner_id", String\(owner\.id\)\)/);
  assert.match(repository, /form\.set\("titel", title\)/);
  assert.match(repository, /new DocumentUploadError\(response\.status, payload\)/);
  assert.match(repository, /error_code\?: string/);
  assert.match(repository, /request_id\?: string/);
  assert.match(repository, /Support-ID: \$\{requestId\}/);
  assert.match(edgeClient, /Authorization: `Bearer \$\{context\.session\.accessToken\}`/);
  assert.match(edgeClient, /apikey: publishableKey/);
  assert.match(edgeClient, /headers: edgeHeaders\(context, options\.contentType\), body: options\.body/);
  assert.doesNotMatch(repository, /Content-Type/);
});

test("edge upload scope remains a strict demo parity check", async () => {
  const edge = await readFile(new URL("../../../supabase/functions/kgv-upload-document/index.ts", import.meta.url), "utf8");
  assert.match(edge, /select\("role,mitglied_id,is_demo_account"\)/);
  assert.match(edge, /isDemoAccount: appUser\?\.is_demo_account === true/);
  assert.match(edge, /resolveUploadOwnerScope\(auth, ownerKind, ownerId\)/);
  assert.match(edge, /ownerIsDemo === null \|\| ownerIsDemo !== auth\.isDemoAccount/);
  assert.match(edge, /errorResponse\(403, "FORBIDDEN", "Dieses Dokument ist für den aktuellen Benutzer nicht freigegeben.", requestId\)/);
});
