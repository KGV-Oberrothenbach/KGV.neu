import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const migration = await readFile(new URL("../../supabase/migrations/20261108120000_p11_ap02a_effective_permissions.sql", import.meta.url), "utf8");
const coreFlags = await readFile(new URL("../../KGV.Core/Security/PermissionFlags.cs", import.meta.url), "utf8");
const coreTests = await readFile(new URL("../../KGV.Tests/PermissionServiceTests.cs", import.meta.url), "utf8");

assert.match(coreFlags, /CanPerformYearEndClosing\s*=\s*1L?\s*<<\s*23/);
assert.match(coreTests, /4278181879L/);
assert.match(coreTests, /1920458231L/);
assert.match(coreTests, /10L/);

assert.match(migration, /create or replace function public\.permission_role_base\(p_role text\)/);
assert.match(migration, /when 'admin' then 4278181879::bigint/);
assert.match(migration, /when 'vorstand' then 1920458231::bigint/);
assert.match(migration, /else 10::bigint/);
assert.match(migration, /create or replace function public\.known_permission_mask\(\)/);
assert.match(migration, /select 4294967295::bigint/);
assert.match(migration, /create or replace function public\.current_effective_permissions\(\)/);
assert.match(migration, /\| coalesce\(au\.permission_grants, 0\)/);
assert.match(migration, /& ~coalesce\(au\.permission_revocations, 0\)/);
assert.match(migration, /where au\.user_id = auth\.uid\(\)/);
assert.match(migration, /\), 0::bigint\)/);

const effectivePermissionFunction = migration.match(/create or replace function public\.has_effective_permission[\s\S]*?\$\$;/)?.[0] ?? "";
assert.match(effectivePermissionFunction, /public\.current_effective_permissions\(\)/);
assert.match(effectivePermissionFunction, /public\.known_permission_mask\(\)/);
assert.doesNotMatch(effectivePermissionFunction, /16769015/);
assert.doesNotMatch(effectivePermissionFunction, /case\s+au\.role/i);
assert.match(effectivePermissionFunction, /p_permission > 0/);
