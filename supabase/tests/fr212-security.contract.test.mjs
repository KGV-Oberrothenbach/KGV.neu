import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const src = (relative) => readFileSync(new URL(`../../${relative}`, import.meta.url), 'utf8');

test('Benutzer-Einladung: V2-User-Recht und Privileg-Grenze vor Provisionierung', () => {
  const code = src('supabase/functions/kgv-invite-user/index.ts');
  assert.match(code, /\.rpc\("ap02_has_effective_right",\s*\{\s*p_fachrecht:\s*"benutzer\.verwalten"\s*\}\)/);
  assert.match(code, /canManageUsers !== true/);
  assert.match(code, /if \(role !== "user"\)/);
  assert.match(code, /Privilegierte Rollen werden getrennt/);
  assert.ok(code.indexOf('if (role !== "user")') < code.indexOf('supabaseAdmin.rpc("provision_app_user_link"'));
  assert.match(code, /supabaseUser\.auth\.getUser\(bearerToken\)/);
});

test('Benutzer-Löschung: eigene V2-Berechtigung, Zielkonten geschützt', () => {
  const code = src('supabase/functions/kgv-delete-user/delete-account.mjs');
  assert.match(code, /callerClient\.rpc\('ap02_has_effective_right', \{ p_fachrecht: 'benutzer\.verwalten' \}\)/);
  assert.match(code, /target\.data\.is_protected/);
  assert.match(code, /ap02_calculate_user_rights/);
  assert.match(code, /begin_auth_user_deletion/);
  assert.match(code, /finish_auth_user_deletion/);
});

test('MAUI prüft Benutzerverwaltung ohne altes Rollenrecht', () => {
  const code = src('KGV.Maui/ViewModels/UserManagementViewModel.cs');
  assert.match(code, /CanManageUsers => .*HasFachrecht\(FachrechteV2\.BenutzerVerwalten\)/);
  assert.match(code, /CanInvite => CanManageUsers/);
  assert.match(code, /CanRemoveUser => CanManageUsers/);
  assert.match(code, /if \(!CanReadUsers\)/);
});

test('Web trennt Benutzer- und Fachrechte-Ansicht', () => {
  const code = src('KGV.Web/app/page.tsx');
  assert.match(code, /BenutzerkontoAdministration/);
  assert.match(code, /canManageRights=\{hasRight\("berechtigungen\.verwalten"\)\}/);
  assert.match(code, /canManage=\{hasRight\("benutzer\.verwalten"\)\}/);
  const screen = src('KGV.Web/features/user-access/FachrechteV2Administration.tsx');
  assert.match(screen, /if \(!canManageRights \|\| isWorking/);
});

test('FR212 SQL-Entwurf: keine neue Rolle und keine permissiven Tabellenrechte', () => {
  const draft = src('tools/FR212/FR212_MIGRATION_ENTWURF_OHNE_TIMESTAMP.sql');
  assert.match(draft, /benutzer\.verwalten/);
  assert.match(draft, /berechtigungen\.lesen/);
  assert.match(draft, /ON CONFLICT \(user_id\) DO NOTHING/);
  assert.match(draft, /fr212_user_account/);
  assert.match(draft, /fr212_rights_snapshot/);
  assert.match(draft, /jsonb_populate_recordset/);
  assert.match(draft, /GRANT EXECUTE ON FUNCTION public\.fr212_user_account/);
  assert.doesNotMatch(draft, /GRANT\s+ALL\s+ON\s+TABLE/i);
});


test('FR212: current account context and five system rights fail closed', () => {
  const draft = src('tools/FR212/FR212_MIGRATION_ENTWURF_OHNE_TIMESTAMP.sql');
  assert.match(draft, /JOIN public\.mitglied m ON m\.id=au\.mitglied_id AND m\.auth_user_id=au\.user_id/);
  assert.match(draft, /JOIN auth\.users u ON u\.id=au\.user_id AND u\.deleted_at IS NULL/);
  assert.match(draft, /u\.banned_until IS NULL OR u\.banned_until<=now\(\)/);
  assert.match(draft, /find_auth_user_id_by_email\(text\)/);
  assert.match(draft, /p_fachrecht IN \(''benutzer\.lesen'',''benutzer\.verwalten'',''berechtigungen\.lesen'',''berechtigungen\.verwalten'',''vereinsdaten\.alle_lesen''\)/);
});

test('FR212: MAUI account and role menus have distinct rights and write gates', () => {
  const menu=src('KGV.Maui/Pages/AdminMenuPage.cs');
  assert.match(menu,/CanReadUsers\(currentContext\)/);
  assert.match(menu,/CanReadRoleManagement\(currentContext\)/);
  const visible=menu.slice(menu.indexOf('Content = new ScrollView'),menu.indexOf('protected override void OnAppearing'));
  assert.match(visible,/_v2RightsButton/);
  assert.doesNotMatch(visible,/_saveRoleButton|_permissionOverridesLayout/);
  const screen=src('KGV.Maui/Pages/FachrechteV2AdministrationPage.cs');
  assert.match(screen,/var ready = _canManageRights &&/);
  assert.match(screen,/save.IsEnabled = _canManageRights &&/);
  assert.match(screen,/if \(!_canManageRights \|\| _busy/);
  assert.match(screen,/await client.Rpc<bool>\("ap02_has_effective_right"/);
});
