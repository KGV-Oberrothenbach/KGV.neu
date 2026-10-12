import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const [baseMigration, demoMigration, hardeningMigration] = await Promise.all([
  readFile(new URL("../../supabase/migrations/20261011140000_g13_app_user_security.sql", import.meta.url), "utf8"),
  readFile(new URL("../../supabase/migrations/20261012193000_g13_protect_demo_main_admin.sql", import.meta.url), "utf8"),
  readFile(new URL("../../supabase/migrations/20261011180000_g13_final_security_hardening.sql", import.meta.url), "utf8"),
]);

assert.match(baseMigration, /update public\.app_user set is_protected = true where mitglied_id = 7 and is_protected = false/i);
assert.match(demoMigration, /au\.mitglied_id = 9001/i);
assert.match(demoMigration, /au\.is_demo_account = true/i);
assert.match(demoMigration, /au\.role = 'admin'/i);
assert.match(demoMigration, /m\.id = au\.mitglied_id[\s\S]*m\.auth_user_id = au\.user_id/i);
assert.match(demoMigration, /for v_demo_admin_user_id, v_is_protected in[\s\S]*for update of au, m[\s\S]*v_eligible_count := v_eligible_count \+ 1;[\s\S]*if v_eligible_count > 1 then\s+return;/i);
assert.match(demoMigration, /if v_eligible_count <> 1 or v_is_protected then\s+return;/i);
assert.match(demoMigration, /if exists \([\s\S]*au\.is_protected = true[\s\S]*au\.user_id <> v_demo_admin_user_id[\s\S]*raise exception/i);

const updates = [...demoMigration.matchAll(/update public\.app_user\s+set([\s\S]*?)where\b/gi)];
assert.equal(updates.length, 1);
assert.match(updates[0][1], /^\s*is_protected\s*=\s*true\s*$/i);
assert.doesNotMatch(updates[0][1], /\b(?:role|permission_grants|permission_revocations|is_demo_account)\s*=/i);

assert.match(baseMigration, /if v_target\.is_protected then raise exception 'Protected app_user cannot be unlinked'/i);
assert.match(baseMigration, /if v_target\.is_protected and \(p_role <> 'admin' or \(v_effective & 512\) <> 512\) then raise exception 'Protected account must retain admin role management'/i);
assert.match(hardeningMigration, /if v_target\.is_protected and \(p_role <> 'admin' or \(v_effective & 512\) <> 512\) then raise exception 'Protected account must retain admin role management'/i);
