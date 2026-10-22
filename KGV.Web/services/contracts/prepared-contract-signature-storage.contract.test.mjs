import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const root = new URL("../../", import.meta.url);

test("prepared signature uploads are authorized server-side and do not expose a browser storage policy", async () => {
  const [edge, migration, storage] = await Promise.all([
    readFile(new URL("../../../supabase/functions/upload-prepared-contract-signature/index.ts", import.meta.url), "utf8"),
    readFile(new URL("../../../supabase/migrations/20261021160000_prepared_contract_signatures.sql", import.meta.url), "utf8"),
    readFile(new URL("../KGV.Infrastructure/Services/SupabasePreparedContractSignatureStorage.cs", root), "utf8"),
  ]);
  assert.match(edge, /caller\.auth\.getUser/);
  assert.match(edge, /current_app_user_context/);
  assert.match(edge, /status !== "zur_signatur"/);
  assert.match(edge, /position\.status !== "offen"/);
  assert.match(edge, /crypto\.randomUUID\(\)/);
  assert.match(edge, /crypto\.subtle\.digest\("SHA-256"/);
  assert.match(edge, /admin\.storage\.from\(bucket\)\.upload/);
  assert.doesNotMatch(migration, /create policy prepared_contract_signatures_read_admin/);
  assert.doesNotMatch(storage, /\/storage\/v1\/object\//);
  assert.match(storage, /functions\/v1\/upload-prepared-contract-signature/);
});
