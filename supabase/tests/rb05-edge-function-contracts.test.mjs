import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolveWelcomeMailRouting } from '../functions/kgv-invite-user/welcome-mail-routing.mjs';

const DEMO = 'https://ymxhszxfutjnljiuciyj.supabase.co';
const PROD = 'https://itjcabiibuodkxayhvjq.supabase.co';
const source = (slug) => readFileSync(new URL(`../functions/${slug}/index.ts`, import.meta.url), 'utf8');

test('DemoDB may redirect invitation only if explicitly configured', () => {
  assert.deepEqual(resolveWelcomeMailRouting(DEMO, 'mitglied@example.org', 'postfach@example.org'), {
    recipient: 'postfach@example.org', testRecipient: 'postfach@example.org',
  });
});

test('ProdDB fails closed if a test SMTP recipient is configured', () => {
  assert.throws(() => resolveWelcomeMailRouting(PROD, 'mitglied@example.org', 'postfach@example.org'), /DemoDB/);
});

test('No test SMTP override means ordinary delivery in either DB', () => {
  for (const url of [DEMO, PROD]) {
    assert.deepEqual(resolveWelcomeMailRouting(url, ' mitglied@example.org ', ''), {
      recipient: 'mitglied@example.org', testRecipient: '',
    });
  }
});

test('Spoofed or invalid DemoDB URL cannot activate SMTP override', () => {
  for (const url of [
    'https://ymxhszxfutjnljiuciyj.supabase.co.attacker.invalid',
    'http://ymxhszxfutjnljiuciyj.supabase.co',
    'https://ymxhszxfutjnljiuciyj.supabase.co:8443',
    'https://user:pass@ymxhszxfutjnljiuciyj.supabase.co',
    '',
  ]) {
    assert.throws(() => resolveWelcomeMailRouting(url, 'mitglied@example.org', 'postfach@example.org'));
  }
});

test('Invite user checks SMTP override before provisioning', () => {
  const code = source('kgv-invite-user');
  assert.match(code, /import \{ resolveWelcomeMailRouting \} from ['"]\.\/welcome-mail-routing\.mjs['"]/);
  assert.match(code, /resolveWelcomeMailRouting\(\s*supabaseUrl,\s*['"]['"],\s*Deno\.env\.get\(['"]SMTP_TEST_RECIPIENT['"]\)/);
  const preflightAt = code.search(/resolveWelcomeMailRouting\(\s*supabaseUrl/);
  const provisionAt = code.indexOf('provision_app_user_link');
  assert.ok(preflightAt >= 0 && provisionAt >= 0 && preflightAt < provisionAt);
  assert.match(code, /const \{ recipient, testRecipient \} = resolveWelcomeMailRouting/);
});

test('OTP does not filter legitimate accounts by demo/test/playstore words', () => {
  const code = source('kgv-request-first-login-otp');
  assert.doesNotMatch(code, /containsOperationalMarker|isOperationalMember|\.filter\(isOperationalMember\)/);
});

test('Club document send uses effective permission', () => {
  assert.match(source('kgv-send-club-documents'), /has_effective_permission.*268435456/);
});

test('Signature upload checks effective permissions and frozen signer identity', () => {
  const code = source('upload-prepared-contract-signature');
  assert.match(code, /current_effective_permissions/);
  assert.match(code, /unterzeichner_typ/);
  assert.match(code, /prepared_contract_signatur_snapshot_person/);
});

test('Document upload rejects direct DELETE method', () => {
  assert.match(source('kgv-upload-document'), /if \(req\.method === ['"]DELETE['"]\) \{\s*return errorResponse\(405,/);
});

test('Prepared lease completion and discard use effective rights', () => {
  assert.match(source('kgv-complete-prepared-lease-contract'), /has_effective_permission.*524288/);
  assert.match(source('kgv-discard-prepared-contract'), /has_effective_permission.*524304/);
});

test('Contract document dispatch authenticates user explicitly', () => {
  assert.match(source('kgv-send-contract-documents'), /caller\.auth\.getUser\(\)/);
});

test('PDF contract and export include current shared renderer and contract error handling', () => {
  assert.match(source('kgv-generate-contract'), /pdf-rendering-core\.mjs/);
  assert.match(source('kgv-generate-export-pdf'), /ExportContractError/);
});
