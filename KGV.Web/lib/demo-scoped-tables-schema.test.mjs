import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import ts from "typescript";

const tables = [
  "arbeitseinsatz",
  "bekanntmachung",
  "mitglied",
  "parzelle",
  "termin",
  "wartungsvertraege",
  "app_user",
  "mitglied_gesetzlicher_vertreter",
  "mitglied_saison",
  "saison",
  "vereinskonfiguration",
  "wartungsvertrag_zuordnungen",
  "zaehler",
  "zaehler_ablesung",
];

async function transpile(path) {
  const source = await readFile(new URL(path, import.meta.url), "utf8");
  const { outputText } = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 },
  });
  return outputText;
}

async function loadRestClient() {
  const clientContext = await transpile("./supabase/client-context.ts");
  const restClient = (await transpile("./supabase/rest-client.ts")).replace(/^import .*?;\r?\n/m, "");
  return import(`data:text/javascript;base64,${Buffer.from(`${clientContext}\n${restClient}`).toString("base64")}`);
}

test("REST transport leaves row scope to the configured database and RLS", async () => {
  const { readSupabaseRest } = await loadRestClient();
  const requests = [];
  globalThis.fetch = async (input) => {
    const url = new URL(input);
    requests.push(url);
    return new Response(JSON.stringify([]), { status: 200, headers: { "content-type": "application/json" } });
  };

  const context = { club: { vereinId: "club", vereinsCode: "test", vereinsname: "Testverein", supabaseUrl: "https://example.supabase.co/", supabasePublishableKey: "test-key" }, session: { accessToken: "test-token", isDemoAccount: false } };
  for (const table of tables) {
    await readSupabaseRest(context, table, { select: "id" });
    assert.equal(requests.at(-1).searchParams.has("is_demo"), false, `${table} must not receive a client-side demo filter`);
  }
  await readSupabaseRest({ ...context, session: { ...context.session, isDemoAccount: true } }, "mitglied", { select: "id" });
  assert.equal(requests.at(-1).searchParams.has("is_demo"), false);
  await readSupabaseRest(context, "mitglied", { select: "id", is_demo: "eq.custom" });
  assert.equal(requests.at(-1).searchParams.get("is_demo"), "eq.custom");
});
