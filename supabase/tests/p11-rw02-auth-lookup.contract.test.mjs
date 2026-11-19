import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const sql = readFileSync(
  new URL("../migrations/20261119122237_p11_rw02_auth_lookup_null_failclosed.sql", import.meta.url),
  "utf8"
);

test("RW02 auth lookup fails closed for missing JWT role", () => {
  assert.match(sql, /auth\.role\(\) is distinct from ''service_role''/);
  assert.match(sql, /has_effective_permission\(512\) is not true/);
});

test("RW02 anonymous RPC execution is revoked", () => {
  assert.match(sql, /revoke execute on function public\.find_auth_user_id_by_email\(text\)/);
  assert.match(sql, /from public, anon/);
});

test("RW02 authorized client and service access remain", () => {
  assert.match(sql, /to authenticated, service_role/);
  assert.match(sql, /has_function_privilege/);
});