import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const migration = await readFile(new URL("../../supabase/migrations/20261011180000_g13_final_security_hardening.sql", import.meta.url), "utf8");
assert.match(migration, /v_target\.permission_grants, 0\) & ~v_known_mask/);
assert.match(migration, /v_target\.permission_revocations, 0\) & ~v_known_mask/);
assert.match(migration, /v_requested_grants & v_known_mask/);
assert.match(migration, /v_requested_revocations & v_known_mask/);
assert.match(migration, /v_effective := \(public\.permission_role_base\(p_role\) \| \(v_grants & v_known_mask\)\) & ~\(v_revocations & v_known_mask\)/);
assert.match(migration, /v_target\.is_protected/);
assert.match(migration, /revoke all on function public\.update_app_user_access[\s\S]*from public/);
assert.match(migration, /grant execute on function public\.update_app_user_access[\s\S]*to authenticated/);
assert.match(migration, /grant execute on function public\.provision_app_user_link[\s\S]*to service_role/);
