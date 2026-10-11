import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const migration = await readFile(new URL("../../supabase/migrations/20261011160000_g13_valid_app_user_access.sql", import.meta.url), "utf8");
const auth = await readFile(new URL("../lib/supabase-auth.ts", import.meta.url), "utf8");
const provider = await readFile(new URL("../features/auth/AuthProvider.tsx", import.meta.url), "utf8");
const contextService = await readFile(new URL("../../KGV.Infrastructure/Services/UserContextService.cs", import.meta.url), "utf8");

assert.match(migration, /function public\.current_app_user_context/);
assert.match(migration, /au\.role in \('admin', 'vorstand', 'user'\)/);
assert.match(migration, /m\.auth_user_id = au\.user_id/);
assert.match(migration, /current_effective_permissions[\s\S]*current_app_user_context/);
assert.match(migration, /current_mitglied_id[\s\S]*current_app_user_context/);
assert.match(migration, /InvalidAppUserLink/);
assert.match(auth, /current_app_user_context/);
assert.doesNotMatch(auth, /role === "admin" \|\| role === "vorstand" \? role : "user"/);
assert.match(auth, /kein gültiger KGV-Zugang/);
assert.match(provider, /setStatus\("access-error"\)/);
assert.match(provider, /setAppUserContext\(null\)/);
assert.match(contextService, /current_app_user_context/);
assert.doesNotMatch(contextService, /Falling back to role 'user'/);
assert.match(contextService, /KgvAccessDeniedException/);
