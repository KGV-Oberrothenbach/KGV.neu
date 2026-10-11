import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import ts from "typescript";

const schemaFlagTables = [
  "arbeitseinsatz",
  "bekanntmachung",
  "mitglied",
  "parzelle",
  "termin",
  "wartungsvertraege",
];

const noFlagTables = [
  "app_user",
  "mitglied_gesetzlicher_vertreter",
  "mitglied_saison",
  "saison",
  "vereinskonfiguration",
  "wartungsvertrag_zuordnungen",
  "zaehler",
  "zaehler_ablesung",
];

async function loadTypeScriptModule(path) {
  const source = await readFile(new URL(path, import.meta.url), "utf8");
  const { outputText } = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 },
  });
  return `data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`;
}

function rowHasIsDemo(types, table) {
  const tableBlock = types.match(new RegExp(`\\r?\\n      ${table}: \\{\\r?\\n        Row: \\{([\\s\\S]*?)\\r?\\n        \\}\\r?\\n        Insert:`));
  return tableBlock ? /^\s{10}is_demo: boolean$/m.test(tableBlock[1]) : false;
}

test("demo-scoped tables have their own schema is_demo column", async () => {
  const authSource = await readFile(new URL("./supabase-auth.ts", import.meta.url), "utf8");
  const catalog = authSource.match(/const demoScopedTables = new Set\(\[([\s\S]*?)\]\);/)?.[1]
    .match(/"([^"]+)"/g)
    .map((name) => name.slice(1, -1));
  assert.ok(catalog, "demoScopedTables catalog should exist");

  const types = await readFile(new URL("../../database.types.ts", import.meta.url), "utf8");
  const aiTypes = await readFile(new URL("../../_AI_DB_EXPORT/database.types.ts", import.meta.url), "utf8");
  const migration = await readFile(new URL("../../supabase/migrations/20260408153000_demo_admin_data_scope.sql", import.meta.url), "utf8");
  for (const table of schemaFlagTables) {
    const inTypes = rowHasIsDemo(types, table) || rowHasIsDemo(aiTypes, table);
    const addedByMigration = new RegExp(`alter table public\\.${table}\\s+add column if not exists is_demo boolean`, "i").test(migration);
    assert.ok(inTypes || addedByMigration, `${table} must have an is_demo column in generated types or an additive schema migration`);
  }

  assert.deepEqual([...catalog].sort(), [...schemaFlagTables].sort(), "the client demo filter must only apply to tables with their own is_demo column");
  for (const table of noFlagTables) {
    assert.equal(rowHasIsDemo(types, table), false, `${table} must not be treated as a row with its own is_demo column`);
    assert.equal(rowHasIsDemo(aiTypes, table), false, `${table} must not be treated as a row with its own is_demo column in the AI export`);
  }
});

test("readSupabase filters only flagged tables and app_user auth uses its actual demo field", async () => {
  const supabaseAuthUrl = await loadTypeScriptModule("./supabase-auth.ts");
  const { loadAppUserContext, readSupabase } = await import(supabaseAuthUrl);
  const requests = [];
  globalThis.window = {
    localStorage: {
      getItem(key) {
        if (key !== "kgv.browser.club.v1") return null;
        return JSON.stringify({
          vereinsCode: "test",
          vereinsname: "Testverein",
          supabaseUrl: "https://example.supabase.co",
          supabasePublishableKey: "test-key",
        });
      },
      setItem() {},
    },
  };
  globalThis.fetch = async (input) => {
    const url = new URL(input);
    requests.push(url);
    const body = url.pathname === "/rest/v1/app_user"
      ? [{ user_id: "test-user", is_demo_account: true }]
      : [];
    return new Response(JSON.stringify(body), { status: 200, headers: { "content-type": "application/json" } });
  };

  const session = { accessToken: "test-token", refreshToken: "test-refresh", expiresAt: 0, user: { id: "test-user" }, isDemoAccount: false };
  for (const table of schemaFlagTables) {
    await readSupabase(session, table, { select: "id" });
    assert.equal(requests.at(-1).searchParams.get("is_demo"), "eq.false", `${table} should retain its client-side demo filter`);
  }
  for (const table of noFlagTables) {
    await readSupabase(session, table, { select: "id" });
    assert.equal(requests.at(-1).searchParams.has("is_demo"), false, `${table} must leave demo scope to its schema/RLS path`);
  }

  const context = await loadAppUserContext(session);
  const authRequest = requests.at(-1);
  assert.equal(authRequest.pathname, "/rest/v1/app_user");
  assert.equal(authRequest.searchParams.get("select"), "user_id,role,mitglied_id,permission_grants,permission_revocations,is_demo_account");
  assert.equal(authRequest.searchParams.get("select").split(",").includes("is_demo"), false);
  assert.equal(context.isDemoAccount, true);
});
