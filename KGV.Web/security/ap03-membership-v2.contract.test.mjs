import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
const root = new URL('../../', import.meta.url);
const load = (path) => readFileSync(new URL(path, root), 'utf8');
const sql = load('supabase/migrations/20261122004343_ap03_current_membership_rights_and_board_guards.sql');

test('AP03.1-3: former members and applicants cannot exercise live rights or vote', () => {
  for (const name of ['ap02_calculate_user_rights', 'update_own_member_contact', 'ap02_can_read_mitglied', 'darf_mitgliedsaufnahme_lesen', 'stimme_mitgliedsaufnahme_ab', 'can_access_mitgliedsaufnahme_vorstand_document'])
    assert.match(sql, new RegExp(name));
  assert.match(sql, /aufnahme_status='aufgenommen'/);
  assert.match(sql, /mitglied_ende IS NULL/);
  assert.match(sql, /protected_admin_identity/);
  assert.doesNotMatch(sql, /UPDATE public\.mitglied SET mitglied_ende/);
});

test('AP03.2: Web membership end checks membership-management right independently', () => {
  const page = load('KGV.Web/app/page.tsx');
  const member = load('KGV.Web/features/members/MemberStammdaten.tsx');
  assert.match(page, /canManageMembership=\{hasRight\("mitgliedschaft\.verwalten"\)\}/);
  assert.match(member, /\{canManageMembership && <MembershipEnd/);
  assert.match(member, /canManageMembership=\{canManageMembership\}/);
  assert.doesNotMatch(member, /canManageMembership=\{canManageSecondary\}/);
});

test('AP03.2: MAUI UI uses only server-computed V2 named rights', () => {
  const flags = load('KGV.Core/Security/PermissionFlags.cs');
  const adapter = load('KGV.Core/Security/FachrechteV2EffectiveMask.cs');
  const checks = load('KGV.Core/Security/PermissionChecks.cs');
  const page = load('KGV.Maui/Pages/MemberDetailPage.cs');
  assert.match(flags, /CanManageMembership\s*=\s*1L\s*<<\s*32/);
  assert.match(adapter, /Allow\(PermissionFlags\.CanManageMembership,\s*"mitgliedschaft\.verwalten"\)/);
  assert.match(checks, /public static bool CanManageMembership\(UserContext\? context\)/);
  const gate = page.match(/private void UpdateCancelMembershipButton[\s\S]*?private async Task CancelMembershipAsync/)?.[0] ?? '';
  assert.match(gate, /PermissionChecks\.CanManageMembership/);
  assert.doesNotMatch(gate, /PermissionChecks\.CanEditAllMembers/);
});