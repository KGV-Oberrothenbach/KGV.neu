import assert from "node:assert/strict";
import { readdir, readFile } from "node:fs/promises";
import test from "node:test";
import ts from "typescript";

const clubA = {
  vereinId: "verein-a",
  vereinsCode: "A",
  vereinsname: "Verein A",
  supabaseUrl: "https://verein-a.supabase.co/",
  supabasePublishableKey: "key-a",
};

const clubB = {
  vereinId: "verein-b",
  vereinsCode: "B",
  vereinsname: "Verein B",
  supabaseUrl: "https://verein-b.supabase.co/",
  supabasePublishableKey: "key-b",
};

function session(userId) {
  return { accessToken: `access-${userId}`, refreshToken: `refresh-${userId}`, expiresAt: Date.now() + 60_000, user: { id: userId } };
}

async function transpile(path) {
  const source = await readFile(new URL(path, import.meta.url), "utf8");
  return ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 } }).outputText;
}

async function loadBrowserAuth() {
  const context = await transpile("../lib/supabase/client-context.ts");
  const auth = (await transpile("../lib/supabase-auth.ts")).replace(/^import .*?;\r?\n/m, "");
  return import(`data:text/javascript;base64,${Buffer.from(`${context}\n${auth}`).toString("base64")}`);
}

function storage() {
  const values = new Map();
  return {
    getItem: (key) => values.get(key) ?? null,
    setItem: (key, value) => values.set(key, String(value)),
    removeItem: (key) => values.delete(key),
    values,
  };
}

test("Web sessions, credentials and endpoints remain bound to the selected club", async () => {
  const windowDescriptor = Object.getOwnPropertyDescriptor(globalThis, "window");
  const localStorage = storage();
  Object.defineProperty(globalThis, "window", { configurable: true, writable: true, value: { localStorage } });
  try {
    const auth = await loadBrowserAuth();
    auth.saveSession(clubA, session("user-a"));

    assert.equal(auth.loadStoredSession(clubB), null, "A session must never be a B session");
    assert.equal(auth.loadStoredSession(clubA)?.user.id, "user-a");
    assert.notEqual(auth.browserSessionStorageKey(clubA), auth.browserSessionStorageKey(clubB));

    auth.saveSession(clubB, session("user-b"));
    assert.equal(auth.loadStoredSession(clubA)?.user.id, "user-a");
    assert.equal(auth.loadStoredSession(clubB)?.user.id, "user-b");

    localStorage.setItem("kgv.browser.club.v1", JSON.stringify(clubA));
    auth.clearClub();
    assert.equal(auth.loadStoredSession(clubA), null, "club switch must discard the active club session");
    assert.equal(auth.loadStoredSession(clubB)?.user.id, "user-b", "unrelated project storage remains isolated");

    assert.throws(() => auth.resolveSupabaseConfig({ ...clubA, supabaseUrl: "" }), /noch nicht mit Supabase konfiguriert/);
    assert.throws(() => auth.resolveSupabaseConfig({ ...clubA, supabasePublishableKey: " " }), /noch nicht mit Supabase konfiguriert/);
  } finally {
    if (windowDescriptor) Object.defineProperty(globalThis, "window", windowDescriptor);
    else delete globalThis.window;
  }
});

test("club switch and MAUI photo transport cannot retain a prior project's connection", async () => {
  const provider = await readFile(new URL("../features/auth/AuthProvider.tsx", import.meta.url), "utf8");
  const mauiApp = await readFile(new URL("../../KGV.Maui/App.xaml.cs", import.meta.url), "utf8");
  const photoService = await readFile(new URL("../../KGV.Infrastructure/Services/PhotoUploadTestService.cs", import.meta.url), "utf8");
  const registrations = await readFile(new URL("../../KGV.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs", import.meta.url), "utf8");

  assert.match(provider, /clearSession\(club \?\? loadClub\(\)\);[\s\S]*?resolveClub\(code\)/);
  assert.match(mauiApp, /WechselVereinAsync[\s\S]*?LogoutAsync\(\)[\s\S]*?ClearTransientState\(\)[\s\S]*?_vereinskontext\.Loeschen\(\)[\s\S]*?Quit\(\)/);
  assert.match(photoService, /PhotoUploadTestService\(IAuthService authService, ISupabaseClientFactory clientFactory/);
  assert.match(photoService, /_supabaseUrl = clientFactory\.Url\.Trim\(\)/);
  assert.match(photoService, /_publishableKey = clientFactory\.Key\.Trim\(\)/);
  assert.doesNotMatch(photoService, /IConfiguration|configuration\["Supabase:/);
  assert.match(registrations, /new PhotoUploadTestService\([\s\S]*?GetRequiredService<ISupabaseClientFactory>\(\)/);
});

test("privileged Edge Functions derive project connection material only from deployment secrets", async () => {
  const root = new URL("../../supabase/functions/", import.meta.url);
  const files = await collectIndexFiles(root);
  const privileged = [];
  for (const file of files) {
    const source = await readFile(file, "utf8");
    if (!/SUPABASE_SERVICE_ROLE_KEY/.test(source)) continue;
    privileged.push(file.pathname);
    assert.match(source, /(?:Deno\.env\.get|required)\(["']SUPABASE_URL["']\)/, `${file.pathname} must use deployment URL`);
    assert.match(source, /(?:Deno\.env\.get|required)\(["']SUPABASE_SERVICE_ROLE_KEY["']\)/, `${file.pathname} must use deployment service role`);
    assert.doesNotMatch(source, /(?:req|request|body|payload)\s*(?:\?\.|\.)\s*(?:supabase_?url|service_?role(?:_?key)?|service_?key)/i, `${file.pathname} must not accept privileged connection material from a request`);
  }
  assert.ok(privileged.length > 0, "privileged Edge Function inventory must not be empty");
});

async function collectIndexFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const nested = await Promise.all(entries.map(async (entry) => {
    const path = new URL(`${entry.name}${entry.isDirectory() ? "/" : ""}`, directory);
    if (entry.isDirectory()) return collectIndexFiles(path);
    return entry.name === "index.ts" ? [path] : [];
  }));
  return nested.flat();
}
