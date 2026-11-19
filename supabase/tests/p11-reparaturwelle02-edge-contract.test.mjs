import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const root = join(dirname(fileURLToPath(import.meta.url)), '../..');
const source = (slug) => readFileSync(join(root, 'supabase', 'functions', slug, 'index.ts'), 'utf8');
function assertOrdered(text, before, after) {
  const start = text.indexOf(before);
  const end = text.indexOf(after);
  assert.ok(start >= 0 && end > start, `'${before}' must precede '${after}'`);
}

test('RW02: Vereinsdokumentversand prüft effektive Rechte vor Dienstzugriff', () => {
  const s = source('kgv-send-club-documents');
  assert.match(s, /caller\.rpc\("has_effective_permission",\s*\{\s*p_permission:\s*268435456\s*\}/);
  assertOrdered(s, 'caller.auth.getUser()', 'caller.rpc("has_effective_permission"');
  assertOrdered(s, 'caller.rpc("has_effective_permission"', 'admin.from("mitglied")');
  assert.doesNotMatch(s, /item\.role\s*===\s*["'](?:admin|vorstand)["']/);
});

test('RW02: Vertragsabschluss und Verwerfen berücksichtigen effektive Rechte', () => {
  const completion = source('kgv-complete-prepared-lease-contract');
  const discard = source('kgv-discard-prepared-contract');
  assert.match(completion, /p_permission:\s*524288/);
  assert.match(discard, /p_permission:\s*524304/);
  for (const s of [completion, discard]) {
    assertOrdered(s, 'caller.auth.getUser()', 'caller.rpc("has_effective_permission"');
    assert.doesNotMatch(s, /(?:entry|item|x)\.role\s*===\s*["'](?:admin|vorstand)["']/);
  }
});

test('RW02: Dokumentupload verweigert direkten Drive-DELETE', () => {
  const s = source('kgv-upload-document');
  assert.match(s, /req\.method\s*===\s*"DELETE"/);
  assert.match(s, /return errorResponse\(405,/);
  assert.match(s, /if\s*\(req\.method\s*===\s*"DELETE"\)\s*\{\s*return errorResponse\(405,/);
  assert.doesNotMatch(s, /const payload = await req\.json\(\) as DeleteDocumentRequest/);
  assert.match(s, /can_access_own_member_history_document/);
  assert.match(s, /can_access_own_historical_lease_document/);
  assert.match(s, /has_effective_permission/);
  assert.doesNotMatch(s, /is_demo_account/);
});

test('RW02: Vertragsversand prüft Identität vor Versand-Claim', () => {
  const s = source('kgv-send-contract-documents');
  assertOrdered(s, 'caller.auth.getUser()', 'claim_contract_dispatch');
  assert.match(s, /callerIdentityError\s*\|\|\s*!callerIdentity\.user/);
});

test('RW02: nicht beanspruchbarer Vertragsversand endet ohne retrybares SQLSTATE als HTTP 403', () => {
  const migration = readFileSync(join(
    root,
    'supabase',
    'migrations',
    '20261119163603_fix_contract_dispatch_retry_timeout.sql',
  ), 'utf8');
  const edge = source('kgv-send-contract-documents');
  assert.match(migration, /create or replace function public\.claim_contract_dispatch\(p_versandinstanz_id bigint\)/i);
  assert.match(migration, /raise sqlstate 'PT403'/i);
  assert.doesNotMatch(migration, /errcode\s*=\s*'40001'/i);
  assert.match(edge, /if \(claimError \|\| !claim\?\.\[0\]\) return json\([^;]+, 403\)/);
});

test('RW02: Signaturupload prüft Berechtigungen und die eingefrorene Signaturposition', () => {
  const s = source('upload-prepared-contract-signature');
  assert.match(s, /current_app_user_context/);
  assert.match(s, /current_effective_permissions/);
  assert.match(s, /administrativeCaller/);
  assert.match(s, /position\.mitglied_id\s*!==\s*callerMemberId/);
  assert.match(s, /prepared_contract_signatur_snapshot_person/);
  assert.doesNotMatch(s, /entry\.role\s*===\s*["'](?:admin|vorstand)["']/);
});

test('RW02: 13 kanonische Functions besitzen explizite JWT-Konfiguration', () => {
  const config = readFileSync(join(root, 'supabase', 'config.toml'), 'utf8');
  const slugs = [
    'kgv-complete-prepared-lease-contract',
    'kgv-complete-prepared-membership-application',
    'kgv-delete-user', 'kgv-discard-prepared-contract',
    'kgv-generate-contract', 'kgv-generate-export-pdf',
    'kgv-invite-user', 'kgv-request-first-login-otp',
    'kgv-send-club-documents', 'kgv-send-contract-documents',
    'kgv-upload-document', 'kgv-upload-photo',
    'upload-prepared-contract-signature',
  ];
  for (const slug of slugs) {
    const section = config.split(`[functions.${slug}]`)[1]?.split(/\n\[/)[0];
    assert.ok(section, `Fehlende Konfiguration für ${slug}`);
    assert.match(section, /verify_jwt\s*=\s*(?:true|false)/);
    assert.match(section, /entrypoint\s*=/);
  }
});
