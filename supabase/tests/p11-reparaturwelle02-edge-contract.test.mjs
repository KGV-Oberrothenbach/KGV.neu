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
  assert.match(s, /caller\.rpc\("ap02_has_effective_right",\s*\{\s*p_fachrecht:\s*"vereinsunterlagen\.verwalten"\s*\}/);
  assertOrdered(s, 'caller.auth.getUser()', 'caller.rpc("ap02_has_effective_right"');
  assertOrdered(s, 'caller.rpc("ap02_has_effective_right"', 'caller.from("mitglied")');
  assertOrdered(s, 'caller.rpc("ap02_has_effective_right"', 'admin = createAdminClient(url)');
  assertOrdered(s, 'fr209_reserve_club_mail', 'transport.sendMail');
  assert.doesNotMatch(s, /item\.role\s*===\s*["'](?:admin|vorstand)["']/);
});

test('RW02: Vertragsabschluss und Verwerfen berücksichtigen effektive Rechte', () => {
  const completion = source('kgv-complete-prepared-lease-contract');
  const discard = source('kgv-discard-prepared-contract');
  assert.match(completion, /ap02_has_effective_right",\s*\{\s*p_fachrecht:\s*"pachtvertrag\.verwalten"/);
  assert.match(discard, /ap02_has_effective_right",\s*\{\s*p_fachrecht:\s*"mitglied\.anlegen"/);
  assert.match(discard, /ap02_has_effective_right",\s*\{\s*p_fachrecht:\s*"dokument\.verwalten"/);
  assert.match(discard, /createError \|\| docError \|\| mayCreate !== true \|\| mayManageDocuments !== true/);
  for (const s of [completion, discard]) {
    assertOrdered(s, 'caller.auth.getUser()', 'caller.rpc("ap02_has_effective_right"');
    assert.doesNotMatch(s, /(?:entry|item|x)\.role\s*===\s*["'](?:admin|vorstand)["']/);
  }
});

test('RW02: Dokumentupload verweigert direkten Drive-DELETE', () => {
  const s = source('kgv-upload-document');
  assert.match(s, /req\.method\s*===\s*"DELETE"/);
  assert.match(s, /return errorResponse\(405,/);
  assert.match(s, /if\s*\(req\.method\s*===\s*"DELETE"\)\s*\{\s*return errorResponse\(405,/);
  assert.doesNotMatch(s, /const payload = await req\.json\(\) as DeleteDocumentRequest/);
  assert.match(s, /auth\.supabaseUser\.from\("dokument"\)/);
  assert.match(s, /fr208_can_write_document_owner/);
  assert.match(s, /ap02_has_effective_right",\s*\{\s*p_fachrecht:\s*"dokument\.verwalten"/);
  assert.match(s, /ap02_has_effective_right",\s*\{\s*p_fachrecht:\s*"dokument\.archivieren"/);
  assert.doesNotMatch(s, /is_demo_account/);
});

test('RW02: Vertragsversand prüft Identität vor Versand-Claim', () => {
  const s = source('kgv-send-contract-documents');
  assertOrdered(s, 'caller.auth.getUser()', 'claim_contract_dispatch');
  assert.match(s, /identityError\s*\|\|\s*!identity\.user/);
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
  const current = readFileSync(join(root,'supabase','migrations','20261124192619_fr209_vorgaenge_kommunikation.sql'),'utf8');
  assert.match(current, /ALTER FUNCTION public\.claim_contract_dispatch\(bigint\) RENAME TO fr209_claim_contract_dispatch/);
  assert.match(current, /ERRCODE='PT403'/);
  assert.doesNotMatch(current, /ERRCODE\s*=\s*'40001'/i);
  assert.match(edge, /caller\.rpc\("fr209_claim_contract_dispatch"/);
  assert.match(edge, /if \(claimError \|\| !Array\.isArray\(claim\) \|\| claim\.length !== 1 \|\| String\(claim\[0\]\.id\) !== String\(dispatchId\)\) return json\([^;]+, 403\)/);
});

test('RW02: Signaturupload prüft Berechtigungen und die eingefrorene Signaturposition', () => {
  const s = source('upload-prepared-contract-signature');
  assert.match(s, /current_app_user_context/);
  assert.match(s, /ap02_has_effective_right",\s*\{\s*p_fachrecht:\s*"vertrag\.vorbereiten"/);
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
