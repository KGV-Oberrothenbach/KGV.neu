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
  assert.equal(await createSignedStorageUrl(context(), "bucket with space", "folder/a b"), "https://example.supabase.co/storage/v1/object/sign/bucket/path?token=one");
  assert.equal(await createSignedStorageUrl(context(), "bucket", "file", 42), "https://download.example/file");

  assert.equal(requests[0].url.pathname, "/rest/v1/rpc/generic_rpc");
  assert.equal(requests[0].init.headers["Content-Type"], "application/json");
  assert.equal(requests[1].url.pathname, "/functions/v1/generic-function");
  assert.equal(requests[2].init.headers["Content-Type"], "text/plain");
  assert.equal(requests[3].url.pathname, "/storage/v1/object/sign/bucket%20with%20space/folder/a%20b");
  assert.deepEqual(JSON.parse(requests[3].init.body), { expiresIn: 600 });
  assert.deepEqual(JSON.parse(requests[4].init.body), { expiresIn: 42 });
});

test("transport clients have no browser club lookup, global context, or upward feature imports", async () => {
  const directory = new URL("../lib/supabase/", import.meta.url);
  const files = (await readdir(directory)).filter((file) => file.endsWith(".ts"));
  for (const file of files) {
    const source = await readFile(new URL(file, directory), "utf8");
    assert.doesNotMatch(source, /loadClub\s*\(|localStorage|kgv\.browser\.club|currentClub|SupabaseGlobalContext/);
    assert.doesNotMatch(source, /from ["'][^"']*(?:services|repositories|features)\//);
  }
});
