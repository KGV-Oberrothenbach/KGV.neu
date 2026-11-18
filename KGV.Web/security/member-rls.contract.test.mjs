import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const migration = await readFile(new URL("../../supabase/migrations/20261118162525_ap014_secure_member_reads.sql", import.meta.url), "utf8");
const proofStatusView = await readFile(new URL("../../supabase/migrations/20261022153000_alt3_relational_document_proof_statuses.sql", import.meta.url), "utf8");
const effectivePermissions = await readFile(new URL("../../supabase/migrations/20261108120000_p11_ap02a_effective_permissions.sql", import.meta.url), "utf8");
const dutyHours = await readFile(new URL("../../supabase/migrations/20261116210100_ap011_secure_duty_hours.sql", import.meta.url), "utf8");

test("member SELECT uses one fail-closed effective-scope policy", () => {
  assert.match(migration, /create policy mitglied_select_authorized_scope[\s\S]*for select\s+to authenticated[\s\S]*can_read_mitglied\(id, auth_user_id\)/i);
  assert.match(migration, /drop policy if exists mitglied_select_effective_or_own/i);
  assert.doesNotMatch(migration, /has_any_effective_permission\s*\(\s*2\s*\|\s*4\s*\)/i);
});

test("own, global-grant, revocation and legal-representative scopes remain explicit", () => {
  assert.match(migration, /permission_role_base\(au\.role\)[\s\S]*permission_grants[\s\S]*~coalesce\(au\.permission_revocations/i);
  assert.match(migration, /a\.mitglied_id = p_mitglied_id[\s\S]*p_auth_user_id = \(select auth\.uid\(\)\)/i);
  assert.match(migration, /permission_grants\s*&\s*2::bigint/i);
  assert.match(migration, /mitglied_gesetzlicher_vertreter[\s\S]*gueltig_ab <= current_date[\s\S]*gueltig_bis is null/i);
});

test("member authorization helper cannot become an anonymous RPC", () => {
  assert.match(migration, /security definer\s+set search_path = public, pg_temp/i);
  assert.match(migration, /revoke all on function public\.can_read_mitglied\(bigint, uuid\)[\s\S]*from public, anon, authenticated/i);
  assert.match(migration, /grant execute on function public\.can_read_mitglied\(bigint, uuid\)[\s\S]*to authenticated/i);
});

test("known member-data view and privileged data paths retain their own guards", () => {
  assert.match(proofStatusView, /create or replace view public\.v_mitglied_mitgliedsantrag_nachweisstatus\s+with \(security_invoker = true\)/i);
  assert.match(effectivePermissions, /create or replace function public\.assert_export_access\(\)[\s\S]*has_effective_permission\(33554432\)/i);
  assert.match(dutyHours, /fn_pflichtstunden_sichtbare_mitglieder\(\)[\s\S]*security definer[\s\S]*has_effective_permission\(131072::bigint\)/i);
  assert.match(dutyHours, /create or replace view public\.v_pflichtstunden_uebersicht\s+with \(security_invoker = true\)/i);
});
