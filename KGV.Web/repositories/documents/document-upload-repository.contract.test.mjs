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
  assert.match(edgeClient, /headers: edgeHeaders\(context, options\.contentType, options\.accept\), body: options\.body/);
  assert.doesNotMatch(repository, /Content-Type/);
});

test("edge upload remains project-local and validates the owner without demo fields", async () => {
  const edge = await readFile(new URL("../../../supabase/functions/kgv-upload-document/index.ts", import.meta.url), "utf8");
  assert.match(edge, /supabaseAdmin\.auth\.getUser\(jwt\)/);
  assert.match(edge, /select\("role,mitglied_id"\)/);
  assert.doesNotMatch(edge, /permission_grants|permission_revocations|permissionGrants|permissionRevocations/);
  assert.match(edge, /global:\s*\{ headers:\s*\{ Authorization: authHeader \} \}/);
  assert.match(edge, /auth\.supabaseUser\.rpc\("ap02_has_effective_right", \{ p_fachrecht: "dokument\.verwalten" \}\)/);
  assert.match(edge, /if \(permissionError \|\| permitted !== true\)\s*return \{ ok: false as const, status: 403/);
  assert.match(edge, /if \(!historicalUpload && !hasProvenance\)\s*\{[\s\S]*?auth\.supabaseUser\.rpc\(\s*"fr208_can_write_document_owner", \{ p_owner_kind: ownerKind, p_owner_id: ownerId \}\);\s*if \(ownerRightError \|\| mayWriteOwner !== true\)\s*return errorResponse\(403, "FORBIDDEN"/);
  assert.match(edge, /auth\.supabaseUser\.from\("dokument"\)[\s\S]*?\.eq\("id", documentId\)\.is\("archiviert_at", null\)/);
  assert.match(edge, /uploadOwnerExists\(auth, ownerKind, ownerId\)/);
  assert.doesNotMatch(edge, /is_demo|isDemoAccount/);
  assert.match(edge, /errorResponse\(403, "FORBIDDEN", "Dieses Dokument ist für den aktuellen Benutzer nicht freigegeben.", requestId\)/);
});

test("document download and contract generation retain an edge Support-ID", async () => {
  const [downloadRepository, contractService, contractEdge] = await Promise.all([
    readFile(new URL("./document-file-repository.ts", import.meta.url), "utf8"),
    readFile(new URL("../../services/contracts/contract-generation-service.ts", import.meta.url), "utf8"),
    readFile(new URL("../../../supabase/functions/kgv-generate-contract/index.ts", import.meta.url), "utf8"),
  ]);

  assert.match(downloadRepository, /new DocumentUploadError\(response\.status, detail/);
  assert.match(contractService, /new DocumentUploadError\(response\.status, detail/);
  assert.match(contractService, /request_id\?: string/);
  assert.match(contractEdge, /error_code: errorCode, message, request_id: requestId/);
  assert.match(contractEdge, /errorResponse\(status, status === 403 \? "FORBIDDEN" : "INTERNAL_ERROR", message, requestId\)/);
});

test("edge upload preserves meaningful authorization and Drive failure codes", async () => {
  const edge = await readFile(new URL("../../../supabase/functions/kgv-upload-document/index.ts", import.meta.url), "utf8");

  assert.match(edge, /errorResponse\(403, "FORBIDDEN"/);
  assert.match(edge, /errorResponse\(502, "GOOGLE_DRIVE_ERROR"/);
  assert.match(edge, /errorResponse\(503, "GOOGLE_AUTH_ERROR"/);
  assert.match(edge, /request_id: requestId/);
});
