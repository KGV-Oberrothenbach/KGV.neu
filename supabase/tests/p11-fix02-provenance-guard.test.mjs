import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const repoRoot = join(dirname(fileURLToPath(import.meta.url)), '../..');
const text = path => readFileSync(join(repoRoot, path), 'utf8');

test('FIX-02: immutable photo provenance requires database service role AND service JWT', () => {
  const sql = text('supabase/migrations/20261120181935_p11_fix02_meter_photo_provenance_role_claim.sql');
  assert.match(sql, /CREATE OR REPLACE FUNCTION public\.guard_meter_reading_photo_provenance\(\)/);
  assert.match(sql, /SECURITY DEFINER/);
  assert.match(sql, /coalesce\(current_setting\('role', true\), ''\)\s*=\s*'service_role'/);
  assert.match(sql, /AND auth\.role\(\)\s*=\s*'service_role'/);
  assert.match(sql, /NOT coalesce\(v_trusted_service, false\)/);
  assert.match(sql, /IF OLD\.foto_drive_file_id IS NOT NULL THEN/);
  assert.match(sql, /ERRCODE = '55000'/);
  assert.match(sql, /REVOKE ALL.*guard_meter_reading_photo_provenance.*PUBLIC, anon, authenticated/);
  assert.doesNotMatch(sql, /DISABLE TRIGGER|DROP TRIGGER|SET ROLE authenticated/i);
});

test('FIX-02: server-side photo metadata is linked conditionally, never supplied by clients', () => {
  const edge = text('supabase/functions/kgv-upload-photo/index.ts');
  assert.match(edge, /supabaseUser\.rpc\(\s*"can_upload_meter_reading_photo"/);
  assert.match(edge, /reading\.zaehler\.zaehlernummer/);
  assert.match(edge, /\.is\("foto_drive_file_id", null\)/);
  assert.match(edge, /\.is\("foto_pfad", null\)/);
  assert.match(edge, /\.is\("foto_dateiname", null\)/);
  assert.match(edge, /const isoMatch = .*\(\?:\[T \].*\.exec\(v\);/);
});
