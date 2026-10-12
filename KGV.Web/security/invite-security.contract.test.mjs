import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const functionSource = await readFile(new URL("../../supabase/functions/kgv-invite-user/index.ts", import.meta.url), "utf8");
const migration = await readFile(new URL("../../supabase/migrations/20261011150000_g13_invite_user_security.sql", import.meta.url), "utf8");
const page = await readFile(new URL("../app/page.tsx", import.meta.url), "utf8");
const maui = await readFile(new URL("../../KGV.Maui/ViewModels/UserManagementViewModel.cs", import.meta.url), "utf8");

assert.match(functionSource, /supabaseUser\s*\.rpc\("has_effective_permission", \{ p_permission: 512 \}\)/);
assert.doesNotMatch(functionSource, /callerRole !== "admin"/);
assert.match(functionSource, /canManageRoles !== true/);
assert.match(functionSource, /inviteMethod !== "otp"/);
assert.match(functionSource, /UnsupportedInviteMethod/);
assert.match(functionSource, /allowedRoles = new Set\(\["admin", "vorstand", "user"\]\)/);
assert.match(functionSource, /supabaseAdmin\.rpc\("provision_app_user_link"/);
assert.doesNotMatch(functionSource, /\.from\("app_user"\)\s*\.upsert/);
assert.match(functionSource, /SMTP_HOST"\) \?\? Deno\.env\.get\("SMTP_Host/);
assert.match(functionSource, /SMTP_FROM"\) \?\? Deno\.env\.get\("SMTP_From/);
assert.match(functionSource, /prepared_mail_failed/);
assert.match(migration, /function public\.provision_app_user_link/);
assert.match(migration, /permission_grants, permission_revocations, is_protected/);
assert.match(migration, /values \(p_user_id, p_mitglied_id, p_role, 0, 0, false/);
assert.match(migration, /revoke all .* from authenticated/);
assert.match(migration, /grant execute .* to service_role/);
assert.match(page, /prepared_mail_failed/);
assert.match(maui, /prepared_mail_failed/);
