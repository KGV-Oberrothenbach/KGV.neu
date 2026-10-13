import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const migration = await readFile(new URL("../../supabase/migrations/20261011160000_g13_valid_app_user_access.sql", import.meta.url), "utf8");
const historicalSchema = await readFile(new URL("../../supabase/migrations/20260323093513_remote_schema.sql", import.meta.url), "utf8");
const appUserService = await readFile(new URL("../services/users/app-user-service.ts", import.meta.url), "utf8");
const provider = await readFile(new URL("../features/auth/AuthProvider.tsx", import.meta.url), "utf8");
const contextService = await readFile(new URL("../../KGV.Infrastructure/Services/UserContextService.cs", import.meta.url), "utf8");

assert.match(migration, /function public\.current_app_user_context/);
assert.match(migration, /au\.role in \('admin', 'vorstand', 'user'\)/);
assert.match(migration, /m\.auth_user_id = au\.user_id/);
assert.match(migration, /current_effective_permissions[\s\S]*current_app_user_context/);
assert.match(migration, /current_mitglied_id[\s\S]*current_app_user_context/);
assert.match(historicalSchema, /"public"\."current_mitglied_id"\(\)\s+RETURNS\s+integer/i);
assert.match(migration, /function public\.current_mitglied_id\(\)\s*returns integer/i);
assert.match(migration, /function public\.current_mitglied_id\(\)[\s\S]*current_app_user_context/);
assert.doesNotMatch(migration, /function public\.current_mitglied_id\(\)\s*returns bigint/i);
assert.doesNotMatch(migration, /drop\s+function\s+(if\s+exists\s+)?public\.current_mitglied_id|current_mitglied_id\s*\(\)\s*cascade/i);
assert.match(migration, /InvalidAppUserLink/);
assert.match(appUserService, /current_app_user_context/);
assert.doesNotMatch(appUserService, /role === "admin" \|\| role === "vorstand" \? role : "user"/);
assert.match(appUserService, /kein gültiger KGV-Zugang/);
assert.match(provider, /setStatus\("access-error"\)/);
assert.match(provider, /setAppUserContext\(null\)/);
assert.match(contextService, /current_app_user_context/);
assert.doesNotMatch(contextService, /Falling back to role 'user'/);
assert.match(contextService, /KgvAccessDeniedException/);
