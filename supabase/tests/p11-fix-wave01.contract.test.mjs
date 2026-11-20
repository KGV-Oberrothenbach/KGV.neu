import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import test from 'node:test';

const root = join(dirname(fileURLToPath(import.meta.url)), '../..');
const file = path => readFileSync(join(root, path), 'utf8');

test('FIX-02: photo write entitlement differs from historical read entitlement', () => {
  const sql = file('supabase/migrations/20261119010000_p11_fix02_meter_photo_write_acl.sql');
  const edge = file('supabase/functions/kgv-upload-photo/index.ts');
  assert.match(sql, /erfasst_von_auth_user_id\s*=\s*auth\.uid\(\)/);
  assert.match(sql, /has_effective_permission\(32\)/);
  assert.match(sql, /current_user_has_active_lease_for_parcel/);
  assert.match(sql, /REVOKE ALL.*can_upload_meter_reading_photo.*PUBLIC, anon/i);
  assert.match(edge, /rpc\(\s*"can_upload_meter_reading_photo"/);
  assert.match(edge, /canUpload\s*!==\s*true/);
  assert.match(edge, /"normal"\s*\|\|\s*v\s*===\s*"jea"/);
  assert.match(edge, /"X-Content-Type-Options": "nosniff"/);
  assert.match(edge, /zaehler:zaehler_id\(parzelle_id,medium,zaehlernummer,/);
  assert.match(edge, /const meterNumberRaw = reading\.zaehler\.zaehlernummer;/);
  assert.match(edge, /const isoMatch = .*\(\?:\[T \].*\.exec\(v\);/);
});

test('FIX-02: Web and MAUI send file plus reading ID, not trusted photo metadata', () => {
  const web = file('KGV.Web/repositories/readings/reading-photo-repository.ts');
  const maui = file('KGV.Maui/Pages/AblesungErfassenPage.cs');
  const request = file('KGV.Core/Models/PhotoUploadTestRequest.cs');
  const transport = file('KGV.Infrastructure/Services/PhotoUploadTestService.cs');
  const pending = file('KGV.Maui/Models/PendingPhotoUpload.cs');
  assert.match(web, /form\.set\("ablesung_id", String\(readingId\)\)/);
  assert.match(request, /long AblesungId/);
  assert.match(pending, /long AblesungId/);
  assert.match(transport, /"ablesung_id"/);
  assert.doesNotMatch(transport.slice(transport.indexOf('private static MultipartFormDataContent CreateMultipartContent'), transport.indexOf('private void LogFailure')), /multipart\.Add\(.*"(?:kind|anlage|garten|datum|medium)"/);
  assert.ok(maui.indexOf('CaptureMeterReadingWithIdAsync(') < maui.indexOf('_pendingPhotoService.SaveAndEnqueue('));
  assert.doesNotMatch(maui, /FotoDriveFileId\s*=\s*photoResult/);
});

test('FIX-03: member role cannot override app_user permissions', () => {
  const sql = file('supabase/migrations/20261119011000_p11_fix03_role_drift.sql');
  assert.match(sql, /current_app_role\(\)/);
  assert.match(sql, /UPDATE OF auth_user_id ON public\.mitglied/);
  assert.doesNotMatch(sql, /role\s*=\s*excluded\.role/i);
  assert.doesNotMatch(sql, /new\.role/i);
  assert.match(sql, /SET DEFAULT 'eingereicht'/);
});

test('FIX-05: exclusion constraint now includes previously unlinked occupancy', () => {
  const sql = file('supabase/migrations/20261119012000_p11_fix05_unlinked_occupancy_exclusion.sql');
  assert.match(sql, /EXCLUDE USING gist/i);
  assert.doesNotMatch(sql, /WHERE\s*\(\(?pachtverhaeltnis_id\s+IS NOT NULL/i);
  assert.match(sql, /existing overlapping occupancy requires manual data review/i);
});

test('FIX-04: all 13 repo functions have an explicit documented JWT configuration', () => {
  const fs = file('supabase/config.toml');
  const names = ['kgv-delete-user', 'kgv-upload-photo', 'kgv-upload-document',
    'kgv-generate-contract','kgv-generate-export-pdf','kgv-invite-user',
    'kgv-send-club-documents','kgv-send-contract-documents','kgv-request-first-login-otp',
    'kgv-complete-prepared-membership-application','kgv-complete-prepared-lease-contract',
    'kgv-discard-prepared-contract','upload-prepared-contract-signature'];
  for (const name of names) {
    const start = fs.indexOf(`[functions.${name}]`);
    assert.ok(start >= 0, `missing function ${name}`);
    const section = fs.slice(start).split(/\n\[/, 1)[0];
    assert.match(section, /verify_jwt\s*=\s*(?:true|false)/);
    assert.match(section, /entrypoint\s*=/);
  }
});
