import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const migration = await readFile(new URL("../../supabase/migrations/20261030150000_fix_app_user_provisioning_trigger.sql", import.meta.url), "utf8");

assert.match(migration, /update public\.mitglied set auth_user_id = p_user_id/);
assert.match(migration, /update public\.app_user[\s\S]*where user_id = p_user_id/);
assert.doesNotMatch(migration, /insert into public\.app_user/);
assert.match(migration, /grant execute on function public\.provision_app_user_link[\s\S]*to service_role/);
