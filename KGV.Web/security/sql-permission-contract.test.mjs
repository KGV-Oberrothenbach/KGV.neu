import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const migration = await readFile(new URL("../../supabase/migrations/20261011130000_g13_permission_contract.sql", import.meta.url), "utf8");
const coreFlags = await readFile(new URL("../../KGV.Core/Security/PermissionFlags.cs", import.meta.url), "utf8");
const coreTests = await readFile(new URL("../../KGV.Tests/PermissionServiceTests.cs", import.meta.url), "utf8");

assert.match(coreFlags, /CanPerformYearEndClosing\s*=\s*1\s*<<\s*23/);
assert.match(coreTests, /16769015L/);
assert.match(coreTests, /7855607L/);
assert.match(coreTests, /10L/);

assert.match(migration, /create or replace function public\.permission_role_base\(p_role text\)/);
assert.match(migration, /when 'admin' then 16769015::bigint/);
assert.match(migration, /when 'vorstand' then 7855607::bigint/);
assert.match(migration, /else 10::bigint/);
assert.match(migration, /create or replace function public\.known_permission_mask\(\)/);
assert.match(migration, /CanPerformYearEndClosing, has the value 8388608/);
assert.match(migration, /select 16777215::bigint/);
assert.match(migration, /create or replace function public\.current_effective_permissions\(\)/);
assert.match(migration, /\| coalesce\(au\.permission_grants, 0\)/);
assert.match(migration, /& ~coalesce\(au\.permission_revocations, 0\)/);
assert.match(migration, /where au\.user_id = auth\.uid\(\)/);
assert.match(migration, /\), 0::bigint\)/);

const effectivePermissionFunction = migration.match(/create or replace function public\.has_effective_permission[\s\S]*?\$\$;/)?.[0] ?? "";
assert.match(effectivePermissionFunction, /public\.current_effective_permissions\(\)/);
assert.match(effectivePermissionFunction, /public\.known_permission_mask\(\)/);
assert.doesNotMatch(effectivePermissionFunction, /8380407/);
assert.doesNotMatch(effectivePermissionFunction, /case\s+au\.role/i);
assert.match(effectivePermissionFunction, /p_permission > 0/);
