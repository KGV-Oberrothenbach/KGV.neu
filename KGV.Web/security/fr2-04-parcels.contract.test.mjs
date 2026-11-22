import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
const root = new URL('../../', import.meta.url);
const load = (path) => readFileSync(new URL(path, root), 'utf8');

test('FR2-04: Web separates parcel editing, assignment and lease-contract rights', () => {
  const page = load('KGV.Web/app/page.tsx');
  const parcel = load('KGV.Web/app/parcel-workspaces.tsx');
  assert.match(page, /canEditMasterData=\{hasRight\("parzelle\.bearbeiten"\)\}/);
  assert.match(page, /canManageAssignments=\{hasRight\("belegung\.verwalten"\)\}/);
  assert.match(page, /canAssign=\{hasRight\("belegung\.verwalten"\)\}/);
  assert.match(page, /canCreateLeaseContract=\{hasRight\("pachtvertrag\.verwalten"\)\}/);
  assert.match(parcel, /canEditMasterData && !editing/);
  assert.match(parcel, /canEdit=\{canManageAssignments\}/);
});

test('FR2-04: MAUI gets independent server-calculated named rights', () => {
  const checks = load('KGV.Core/Security/PermissionChecks.cs');
  const memberAssign = load('KGV.Maui/Pages/MemberGardenAssignPage.cs');
  const memberGardens = load('KGV.Maui/Pages/MemberGardensPage.cs');
  const vm = load('KGV.Maui/ViewModels/ParzellenViewModel.cs');
  const userContextService = load('KGV.Infrastructure/Services/UserContextService.cs');
  assert.match(checks, /HasFachrecht\(FachrechteV2\.BelegungVerwalten\)/);
  assert.match(checks, /HasFachrecht\(FachrechteV2\.PachtvertragVerwalten\)/);
  assert.match(userContextService, /grantedFachrechte\)/);
  assert.doesNotMatch(memberAssign, /CanCreateMitglied/);
  assert.doesNotMatch(memberGardens, /CanCreateMitglied/);
  assert.match(vm, /CanManageAssignment => PermissionChecks\.CanManageBelegungen/);
});

test('FR2-04: RLS splits write rights and removes direct hard deletion', () => {
  const sql = load('supabase/migrations/20261122095500_fr2_04_parzelle_belegung_rls_write_split.sql');
  assert.match(sql, /CREATE POLICY parzellen_belegung_insert_effective/);
  assert.match(sql, /CREATE POLICY parzellen_belegung_update_effective/);
  assert.match(sql, /CREATE POLICY parzelle_insert_effective/);
  assert.match(sql, /CREATE POLICY parzelle_update_effective/);
  assert.doesNotMatch(sql, /CREATE POLICY[^;]+FOR DELETE/s);
});
