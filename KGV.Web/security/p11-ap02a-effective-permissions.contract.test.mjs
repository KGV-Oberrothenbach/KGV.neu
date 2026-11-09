import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const migration = await readFile(new URL("../../supabase/migrations/20261108120000_p11_ap02a_effective_permissions.sql", import.meta.url), "utf8");
const core = await readFile(new URL("../../KGV.Core/Security/PermissionChecks.cs", import.meta.url), "utf8");
const capabilities = await readFile(new URL("./capabilities.ts", import.meta.url), "utf8");

// P11-T01-P/N/H: role base is augmented then revocations win, including server-side meter access.
assert.match(migration, /\(public\.permission_role_base\(au\.role\) \| coalesce\(au\.permission_grants, 0\)\)[\s\S]*& ~coalesce\(au\.permission_revocations, 0\)/);
assert.match(migration, /zaehler_ablesung_insert_effective_meter_permission[\s\S]*has_effective_permission\(32\)/);
assert.match(migration, /drop policy if exists zaehler_ablesung_admin_full/);

// P11-T02-P/N/H: administration is its own effective right and the protected-account
// guard retains only that right, independent of a role name.
assert.match(migration, /CanManageRoles required/);
assert.match(migration, /Protected account must retain CanManageRoles/);
assert.doesNotMatch(migration, /p_role <> 'admin'/);
assert.match(core, /CanManageRoleManagement[\s\S]*CanManageRoles/);
assert.doesNotMatch(capabilities, /context\?\.role === "admin"/);
