import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const root = new URL("../../", import.meta.url);

test("protected accounts retain the final effective-rights safety contract", async () => {
  const [baseSecurity, effectiveRights, cleanup, databaseTest, permissionService] = await Promise.all([
    readFile(new URL("supabase/migrations/20261011140000_g13_app_user_security.sql", root), "utf8"),
    readFile(new URL("supabase/migrations/20261108120000_p11_ap02a_effective_permissions.sql", root), "utf8"),
    readFile(new URL("supabase/migrations/20261109130000_p11_demo_cleanup_d5_physical_schema.sql", root), "utf8"),
    readFile(new URL("supabase/tests/20261109130000_p11_demo_cleanup_d5_schema_sicherheitstest.sql", root), "utf8"),
    readFile(new URL("KGV.Core/Security/PermissionService.cs", root), "utf8"),
  ]);

  assert.match(baseSecurity, /if v_target\.is_protected then raise exception 'Protected app_user cannot be unlinked'/i);
  assert.match(effectiveRights, /if not public\.has_effective_permission\(512\) then raise exception 'CanManageRoles required'/i);
  assert.match(effectiveRights, /v_effective := \(public\.permission_role_base\(p_role\) \| \(v_grants & v_known_mask\)\) & ~\(v_revocations & v_known_mask\)/i);
  assert.match(effectiveRights, /if v_target\.is_protected and \(v_effective & 512\) <> 512 then[\s\S]*Protected account must retain CanManageRoles/i);
  assert.match(cleanup, /returns table\(user_id uuid,mitglied_id bigint,role text,permission_grants bigint,permission_revocations bigint,is_protected boolean\)/i);
  assert.doesNotMatch(cleanup, /returns table\([^)]*is_demo_account/i);
  assert.match(databaseTest, /v_context\.is_protected is distinct from true/i);
  assert.match(databaseTest, /individual revocation did not override admin role basis/i);
  assert.match(permissionService, /PermissionFlags\.CanManageRoles/);
});
