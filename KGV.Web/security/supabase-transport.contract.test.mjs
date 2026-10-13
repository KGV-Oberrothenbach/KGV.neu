import assert from "node:assert/strict";
import { readdir, readFile } from "node:fs/promises";
import test from "node:test";
import ts from "typescript";

async function transpile(path) {
  const source = await readFile(new URL(path, import.meta.url), "utf8");
  return ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 } }).outputText;
}

async function loadTransport() {
  const context = await transpile("../lib/supabase/client-context.ts");
  const files = ["rest-client.ts", "rpc-client.ts", "edge-client.ts", "storage-client.ts"];
  const clients = await Promise.all(files.map(async (file) => (await transpile(`../lib/supabase/${file}`)).replace(/^import .*?;\r?\n/m, "")));
  return import(`data:text/javascript;base64,${Buffer.from([context, ...clients].join("\n")).toString("base64")}`);
}

function context(overrides = {}) {
  return {
    club: { vereinId: "club-id", vereinsCode: "club", vereinsname: "Testverein", supabaseUrl: "https://example.supabase.co/", supabasePublishableKey: "public-key" },
    session: { accessToken: "access-token", isDemoAccount: false },
    ...overrides,
  };
}

test("client context validates the explicit club configuration", async () => {
  const { isSupabaseClubConfigured, resolveSupabaseConfig } = await loadTransport();
  assert.deepEqual(resolveSupabaseConfig(context().club), { url: "https://example.supabase.co", publishableKey: "public-key" });
  assert.throws(() => resolveSupabaseConfig({ ...context().club, supabaseUrl: " " }), /noch nicht mit Supabase konfiguriert/);
  assert.throws(() => resolveSupabaseConfig({ ...context().club, supabasePublishableKey: " " }), /noch nicht mit Supabase konfiguriert/);
  assert.equal(isSupabaseClubConfigured(context().club), true);
  assert.equal(isSupabaseClubConfigured(null), false);
});

test("REST transport sends explicit credentials, preserves demo scope, and has no lock orchestration", async () => {
  const { readSupabaseRest, writeSupabaseRest, deleteSupabaseRest } = await loadTransport();
  const requests = [];
  globalThis.fetch = async (input, init = {}) => {
    requests.push({ url: new URL(input), init });
    return new Response(JSON.stringify([{ id: 1 }]), { status: 200, headers: { "content-type": "application/json" } });
  };
  await readSupabaseRest(context(), "mitglied", { select: "id" });
  await writeSupabaseRest(context(), "termin", "POST", { titel: "Test" });
  await writeSupabaseRest(context(), "termin", "PATCH", { titel: "Neu" }, { id: "eq.1" });
  await deleteSupabaseRest(context(), "termin", { id: "eq.1" });

  assert.equal(requests[0].url.href, "https://example.supabase.co/rest/v1/mitglied?select=id&is_demo=eq.false");
  assert.equal(requests[0].init.headers.apikey, "public-key");
  assert.equal(requests[0].init.headers.Authorization, "Bearer access-token");
  assert.equal(requests[1].init.headers["Content-Type"], "application/json");
  assert.equal(requests[1].init.headers.Prefer, "return=representation");
  assert.equal(requests[2].init.method, "PATCH");
  assert.equal(requests[3].init.method, "DELETE");
  assert.equal(requests[3].url.searchParams.get("id"), "eq.1");
});

test("RPC, Edge and Storage transports construct generic endpoints", async () => {
  const { callSupabaseRpc, createSignedStorageUrl, invokeEdgeFunction, invokeEdgeFunctionRaw } = await loadTransport();
  const requests = [];
  const responses = [
    new Response(JSON.stringify({ ok: true }), { status: 200 }),
    new Response(JSON.stringify({ ok: true }), { status: 200 }),
    new Response("raw", { status: 200 }),
    new Response("public", { status: 200 }),
    new Response(JSON.stringify({ signedURL: "/object/sign/bucket/path?token=one" }), { status: 200 }),
    new Response(JSON.stringify({ signedUrl: "https://download.example/file" }), { status: 200 }),
  ];
  globalThis.fetch = async (input, init = {}) => {
    requests.push({ url: new URL(input), init });
    return responses.shift();
  };

  assert.deepEqual(await callSupabaseRpc(context(), "generic_rpc", { value: 1 }), { ok: true });
  assert.deepEqual(await invokeEdgeFunction(context(), "generic-function", { value: 2 }), { ok: true });
  await invokeEdgeFunctionRaw(context(), "generic-raw", { method: "PUT", body: "raw", contentType: "text/plain" });
  await invokeEdgeFunctionRaw({ club: context().club }, "public-edge", { method: "POST", body: "{}" });
  assert.equal(await createSignedStorageUrl(context(), "bucket with space", "folder/a b"), "https://example.supabase.co/storage/v1/object/sign/bucket/path?token=one");
  assert.equal(await createSignedStorageUrl(context(), "bucket", "file", 42), "https://download.example/file");

  assert.equal(requests[0].url.pathname, "/rest/v1/rpc/generic_rpc");
  assert.equal(requests[0].init.headers["Content-Type"], "application/json");
  assert.equal(requests[1].url.pathname, "/functions/v1/generic-function");
  assert.equal(requests[2].init.headers["Content-Type"], "text/plain");
  assert.equal(requests[3].url.pathname, "/functions/v1/public-edge");
  assert.equal(requests[3].init.headers.Authorization, undefined);
  assert.equal(requests[3].init.headers.apikey, "public-key");
  assert.equal(requests[4].url.pathname, "/storage/v1/object/sign/bucket%20with%20space/folder/a%20b");
  assert.deepEqual(JSON.parse(requests[4].init.body), { expiresIn: 600 });
  assert.deepEqual(JSON.parse(requests[5].init.body), { expiresIn: 42 });
});

test("transport clients have no browser club lookup, global context, or upward feature imports", async () => {
  const directory = new URL("../lib/supabase/", import.meta.url);
  const files = (await readdir(directory)).filter((file) => file.endsWith(".ts"));
  for (const file of files) {
    const source = await readFile(new URL(file, directory), "utf8");
    assert.doesNotMatch(source, /loadClub\s*\(|localStorage|kgv\.browser\.club|currentClub|globalClub|setCurrentClub|getCurrentClub|SupabaseSingleton\.club|SupabaseGlobalContext/);
    assert.doesNotMatch(source, /from ["'][^"']*(?:services|repositories|features)\//);
  }
});

test("repositories and services use explicit transport clients instead of the auth compatibility facade", async () => {
  const root = new URL("../", import.meta.url);
  const legacyTransportImports = /import\s*\{[^}]*\b(?:readSupabase|writeSupabase|deleteSupabase|callSupabaseRpc|callSupabaseFunctionRaw|createStorageSignedUrl)\b[^}]*\}\s*from\s*["'][^"']*supabase-auth["']/;
  for (const directoryName of ["repositories", "services", "features"]) {
    const directory = new URL(`${directoryName}/`, root);
    const files = await collectTypeScriptFiles(directory);
    for (const file of files) {
      const source = await readFile(file, "utf8");
      assert.doesNotMatch(source, legacyTransportImports, `${file.pathname} must not import legacy Supabase transport`);
      if (!file.pathname.endsWith("/features/auth/AuthProvider.tsx")) {
        assert.doesNotMatch(source, /loadClub\s*\(/, `${file.pathname} must not resolve a club from persistence`);
      }
      if (directoryName === "repositories" && /from ["'][^"']*lib\/supabase\/(?:rest|rpc|edge|storage)-client["']/.test(source)) {
        assert.match(source, /club\s*:\s*ClubContext/, `${file.pathname} must accept an explicit ClubContext`);
        assert.match(source, /\{\s*club(?:,\s*session)?\s*\}/, `${file.pathname} must pass the explicit club context to its transport`);
      }
      if (/\/functions\/v1\//.test(source)) assert.match(file.pathname, /\/lib\/supabase\/edge-client\.ts$/, `${file.pathname} must use the generic Edge transport`);
    }
  }

  const authFacade = await readFile(new URL("../lib/supabase-auth.ts", import.meta.url), "utf8");
  assert.doesNotMatch(authFacade, /legacyTransportContext|export\s+(?:async\s+)?function\s+(?:readSupabase|writeSupabase|deleteSupabase|callSupabaseRpc|callSupabaseFunctionRaw|createStorageSignedUrl|generateContract|uploadMeterPhoto|openMeterPhoto|inviteAppUser|sendPasswordReset|acquireBrowserEditLock|releaseBrowserEditLock|releaseAllBrowserEditLocks)\b/);
  assert.doesNotMatch(authFacade, /\bcallSupabaseRpc\b/);
  assert.doesNotMatch(authFacade, /ContractGenerationRequest|MeterPhotoKind/);

  const appUserService = await readFile(new URL("services/users/app-user-service.ts", root), "utf8");
  assert.match(appUserService, /loadAppUserContext\s*\(\s*club:\s*ClubContext,\s*session:\s*BrowserSession/);
  assert.match(appUserService, /callSupabaseRpc[\s\S]*?\{\s*club,\s*session\s*\}/);
});

async function collectTypeScriptFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const nested = await Promise.all(entries.map(async (entry) => {
    const path = new URL(`${entry.name}${entry.isDirectory() ? "/" : ""}`, directory);
    if (entry.isDirectory()) return collectTypeScriptFiles(path);
    return /\.(?:ts|tsx)$/.test(entry.name) ? [path] : [];
  }));
  return nested.flat();
}
