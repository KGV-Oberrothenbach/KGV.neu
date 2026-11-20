import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const service = readFileSync(new URL('../services/users/app-user-service.ts', import.meta.url), 'utf8');
const page = readFileSync(new URL('../app/page.tsx', import.meta.url), 'utf8');
const maui = readFileSync(new URL('../../KGV.Infrastructure/Authentication/AuthService.cs', import.meta.url), 'utf8');
const edge = readFileSync(new URL('../../supabase/functions/kgv-delete-user/index.ts', import.meta.url), 'utf8');

test('Web invokes the protected account deletion endpoint bound to the selected auth ID', () => {
  assert.match(service, /deleteAppUser\(/);
  assert.match(service, /"kgv-delete-user"/);
  assert.match(service, /expectedAuthUserId/);
  assert.match(service, /verified\?\.contractVersion !== 2/);
  assert.match(service, /verified\?\.outcome !== "eligible"/);
  assert.match(service, /outcome\?\.outcome\s*!==\s*"deleted"/);
  assert.match(page, /deleteAppUser\(club, session, selectedMemberId, selectedUser\.user_id\)/);
  assert.doesNotMatch(page, /callSupabaseRpc<void>\(\{ club, session \}, "unlink_app_user"/);
});

test('MAUI does not mutate member or app_user directly during account deletion', () => {
  const section = maui.slice(maui.indexOf('public async Task<bool> RemoveUserAsync(AppUserDTO user)'),
    maui.indexOf('public async Task<bool> RequestEmailChangeAsync'));
  assert.match(section, /\/functions\/v1\/kgv-delete-user/);
  assert.match(section, /expectedAuthUserId/);
  assert.match(section, /new AuthenticationHeaderValue\("Bearer", accessToken\)/);
  assert.match(section, /contractVersion != 2/);
  assert.match(section, /dryRun \? "eligible" : "deleted"/);
  assert.doesNotMatch(section, /\.From<MitgliedRecord>\(\)/);
  assert.doesNotMatch(section, /\.From<AppUserRecord>\(\)/);
});

test('Edge validates bearer token and the expected auth ID before deleting', () => {
  assert.match(edge, /callerClient\.auth\.getUser\(\)/);
  assert.match(edge, /expectedAuthUserId/);
  assert.match(edge, /typeof body\?\.expectedAuthUserId !== 'string'/);
  assert.match(edge, /contractVersion: 2/);
});
