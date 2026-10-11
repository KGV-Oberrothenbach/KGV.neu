import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import ts from "typescript";

async function loadTypeScriptModule(path, replacements = []) {
  let source = await readFile(new URL(path, import.meta.url), "utf8");
  for (const [specifier, replacement] of replacements) {
    source = source.replaceAll(JSON.stringify(specifier), JSON.stringify(replacement));
  }
  const { outputText } = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 },
  });
  return `data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`;
}

test("member documents request only the selected member's active documents and leave demo scope to RLS", async () => {
  const supabaseAuthUrl = await loadTypeScriptModule("../../lib/supabase-auth.ts");
  const documentModelUrl = await loadTypeScriptModule("../../models/documents/document.ts");
  const repositoryUrl = await loadTypeScriptModule(
    "../../repositories/documents/document-repository.ts",
    [
      ["../../lib/supabase-auth", supabaseAuthUrl],
      ["../../models/documents/document", documentModelUrl],
    ],
  );
  const serviceUrl = await loadTypeScriptModule(
    "./document-service.ts",
    [["../../repositories/documents/document-repository", repositoryUrl]],
  );
  const { readSupabase } = await import(supabaseAuthUrl);
  const { loadDocumentsForOwner } = await import(serviceUrl);
  const requests = [];
  const documents = [
    { id: 10, mitglied_id: 314, parzelle_id: null, archiviert_at: null },
    { id: 11, mitglied_id: 314, parzelle_id: null, archiviert_at: "2026-10-01T00:00:00Z" },
    { id: 12, mitglied_id: null, parzelle_id: 905, archiviert_at: null },
  ];

  globalThis.window = {
    localStorage: {
      getItem(key) {
        assert.equal(key, "kgv.browser.club.v1");
        return JSON.stringify({
          vereinsCode: "test",
          vereinsname: "Testverein",
          supabaseUrl: "https://example.supabase.co",
          supabasePublishableKey: "test-key",
        });
      },
    },
  };
  globalThis.fetch = async (input) => {
    const url = new URL(input);
    requests.push(url);
    if (url.pathname === "/rest/v1/dokument") {
      assert.equal(url.searchParams.has("is_demo"), false);
      const rows = documents.filter((document) => {
        if (url.searchParams.has("mitglied_id")) return String(document.mitglied_id) === url.searchParams.get("mitglied_id").slice(3);
        if (url.searchParams.has("parzelle_id")) return String(document.parzelle_id) === url.searchParams.get("parzelle_id").slice(3);
        return false;
      }).filter((document) => url.searchParams.get("archiviert_at") !== "is.null" || document.archiviert_at === null);
      return new Response(JSON.stringify(rows), { status: 200, headers: { "content-type": "application/json" } });
    }
    return new Response("[]", { status: 200, headers: { "content-type": "application/json" } });
  };

  const session = { accessToken: "test-token", refreshToken: "test-refresh", expiresAt: 0, user: { id: "test-user" }, isDemoAccount: false };
  const memberDocuments = await loadDocumentsForOwner(session, { kind: "member", id: 314 });
  const memberRequest = requests.find((request) => request.pathname === "/rest/v1/dokument");

  assert.deepEqual(memberDocuments, [documents[0]], "a listed active member document should reach the caller but archived documents should not");
  assert.ok(memberRequest);
  assert.equal(memberRequest.searchParams.get("mitglied_id"), "eq.314");
  assert.equal(memberRequest.searchParams.has("parzelle_id"), false);
  assert.equal(memberRequest.searchParams.get("archiviert_at"), "is.null");

  const noDocuments = await loadDocumentsForOwner(session, { kind: "member", id: 271 });
  assert.deepEqual(noDocuments, [], "a member with no documents stays empty");

  await loadDocumentsForOwner({ ...session, isDemoAccount: true }, { kind: "member", id: 314 });
  const demoMemberDocumentRequest = requests.filter((request) => request.pathname === "/rest/v1/dokument").at(-1);
  assert.equal(demoMemberDocumentRequest.searchParams.has("is_demo"), false, "demo document scope must also be left to RLS");

  await loadDocumentsForOwner(session, { kind: "parcel", id: 905 });
  const parcelRequest = requests.filter((request) => request.pathname === "/rest/v1/dokument").at(-1);
  assert.equal(parcelRequest.searchParams.get("parzelle_id"), "eq.905");
  assert.equal(parcelRequest.searchParams.has("mitglied_id"), false, "parcel documents remain a separate owner scope");
  assert.deepEqual(
    await loadDocumentsForOwner(session, { kind: "parcel", id: 905 }),
    [documents[2]],
    "parcel-owned documents remain outside the member result and visible in the separate parcel request",
  );

  await readSupabase(session, "mitglied", { select: "id", id: "eq.314" });
  const memberScopeRequest = requests.find((request) => request.pathname === "/rest/v1/mitglied");
  assert.equal(memberScopeRequest.searchParams.get("is_demo"), "eq.false");

  await readSupabase({ ...session, isDemoAccount: true }, "mitglied", { select: "id", id: "eq.314" });
  const demoMemberScopeRequest = requests.filter((request) => request.pathname === "/rest/v1/mitglied").at(-1);
  assert.equal(demoMemberScopeRequest.searchParams.get("is_demo"), "eq.true");

  const page = await readFile(new URL("../../app/page.tsx", import.meta.url), "utf8");
  assert.match(page, /activeId === "mitglied-dokumente" && selectedMemberId && <DocumentList session=\{session\} owner=\{memberDocumentOwner\(selectedMemberId\)\}/);
  const component = await readFile(new URL("../../features/documents/DocumentList.tsx", import.meta.url), "utf8");
  assert.match(component, /loadDocumentsForOwner\(session, currentOwner\)/);
});
