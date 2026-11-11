import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";

const source = await readFile(new URL("./index.ts", import.meta.url), "utf8");

test("contract dispatch validates the caller JWT before the privileged dispatch path", () => {
  assert.match(source, /authorization\.startsWith\("Bearer "\)/);
  assert.match(source, /createClient\(url, required\("SUPABASE_ANON_KEY"\), \{ global: \{ headers: \{ Authorization: authorization \} \} \}\)/);
  assert.match(source, /caller\.auth\.getUser\(\)/);
  assert.match(source, /Keine gültige Session/);
  assert.match(source, /caller\.rpc\("claim_contract_dispatch", \{ p_versandinstanz_id: dispatchId \}\)/);
});
