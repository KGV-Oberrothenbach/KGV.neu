import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const migrationUrl = new URL(
  '../migrations/20261121204118_ap02_1b_role_management_rpcs.sql',
  import.meta.url,
);
const workflowUrl = new URL(
  '../../.github/workflows/p11-ap04d-validation.yml',
  import.meta.url,
);
const memberLinkGuardUrl = new URL(
  '../migrations/20261121220403_ap02_4c_effective_rights_member_link_guard.sql',
  import.meta.url,
);
const associationReplayUrls = [
  '../migrations/20261121222300_ap02_4f0_vereinsfunktionen_labels.sql',
  '../migrations/20261121223111_ap02_4f1_bauausschuss_konsolidierung.sql',
  '../migrations/20261121223640_ap02_4f2a_bestands_hauptrollen.sql',
].map((path) => new URL(path, import.meta.url));
const finalRlsGuardUrl = new URL(
  '../migrations/20261121235430_ap02_final_indirect_rls_guards.sql',
  import.meta.url,
);
const ap03GuardUrl = new URL(
  '../migrations/20261122004343_ap03_current_membership_rights_and_board_guards.sql',
  import.meta.url,
);
const fr211GuardUrl = new URL(
  '../migrations/20261124215220_fr211_export_security.sql',
  import.meta.url,
);
const fr212GuardUrl = new URL(
  '../migrations/20261125190037_fr212_user_permissions_session.sql',
  import.meta.url,
);
const s01S02GuardUrl = new URL(
  '../migrations/20261125212731_fr2_security_s01_s02_identity_session_acl.sql',
  import.meta.url,
);
const rb04bGuardUrl = new URL(
  '../migrations/20261126211737_fr2_rb04b_helper_rpc_acl.sql',
  import.meta.url,
);
const rb04cdGuardUrl = new URL(
  '../migrations/20261126212839_fr2_rb04cd_internal_rpc_acl.sql',
  import.meta.url,
);
const rb04d2GuardUrl = new URL(
  '../migrations/20261126215018_fr2_rb04d2_lock_scope.sql',
  import.meta.url,
);
const remainingRb04dGuardUrls = [
  '../migrations/20261127161129_fr2_rb04d3_contact_session.sql',
  '../migrations/20261127164601_fr2_rb04d4_authorization_precheck.sql',
].map((path) => new URL(path, import.meta.url));
const replayGuardUrls = [
  '../migrations/20261121213910_ap02_4a_legacy_user_rights_gate.sql',
  '../migrations/20261121230038_ap02_4g1_rls_fachrechte_v2.sql',
  '../migrations/20261121231042_ap02_4h1_named_business_rpc_guards.sql',
  '../migrations/20261121231803_ap02_4h2_remaining_legacy_rpc_cutover.sql',
  '../migrations/20261121233322_ap02_4l_disable_legacy_permission_paths.sql',
  '../migrations/20261121235430_ap02_final_indirect_rls_guards.sql',
  '../migrations/20261122004343_ap03_current_membership_rights_and_board_guards.sql',
].map((path) => new URL(path, import.meta.url));

test('AP-02.1B accepts an empty fresh database without weakening protected-admin checks', async () => {
  const sql = await readFile(migrationUrl, 'utf8');

  assert.doesNotMatch(
    sql,
    /OR\s+NOT\s+EXISTS\s*\(\s*SELECT\s+1\s+FROM\s+public\.protected_admin_identity\s*\)/i,
  );
  assert.match(
    sql,
    /SELECT\s+a\.user_id[\s\S]*WHERE\s+a\.is_protected\s*=\s*true[\s\S]*EXCEPT[\s\S]*SELECT\s+p\.user_id[\s\S]*FROM\s+public\.protected_admin_identity\s+p/i,
  );
  assert.match(
    sql,
    /SELECT\s+p\.user_id[\s\S]*FROM\s+public\.protected_admin_identity\s+p[\s\S]*EXCEPT[\s\S]*SELECT\s+a\.user_id[\s\S]*WHERE\s+a\.is_protected\s*=\s*true/i,
  );
  assert.match(
    sql,
    /FROM\s+public\.app_user_zusatzrolle\s+z[\s\S]*z\.zusatzrolle_code\s*=\s*'admin'/i,
  );
});

test('later AP-02/AP-03 replay guards compare protected identities to protected users', async () => {
  for (const url of replayGuardUrls) {
    const sql = await readFile(url, 'utf8');

    assert.match(
      sql,
      /SELECT\s+a\.user_id[\s\S]*a\.is_protected\s*=\s*true[\s\S]*EXCEPT[\s\S]*SELECT\s+p\.user_id[\s\S]*public\.protected_admin_identity/i,
      url.pathname,
    );
    assert.match(
      sql,
      /SELECT\s+p\.user_id[\s\S]*public\.protected_admin_identity[\s\S]*EXCEPT[\s\S]*SELECT\s+a\.user_id[\s\S]*a\.is_protected\s*=\s*true/i,
      url.pathname,
    );
  }
});

test('AP-02.4C normalizes stored CRLF before its guarded function rewrite', async () => {
  const sql = await readFile(memberLinkGuardUrl, 'utf8');

  assert.match(
    sql,
    /replace\s*\(\s*pg_get_functiondef[\s\S]*E'\\r\\n'\s*,\s*E'\\n'/i,
  );
});

test('AP-02.4F data migrations preserve guards but allow an unseeded fresh schema', async () => {
  for (const url of associationReplayUrls) {
    const sql = await readFile(url, 'utf8');

    assert.match(
      sql,
      /(?:IF NOT EXISTS|IF EXISTS)\s*\(\s*SELECT 1 FROM public\.vereinsfunktion\s*\)/i,
      url.pathname,
    );
    assert.match(sql, /USING ERRCODE\s*=\s*'55000'/i, url.pathname);
  }
});

test('AP-02.4F.1 takes its table lock inside the transactional DO block', async () => {
  const sql = await readFile(associationReplayUrls[1], 'utf8');
  const doIndex = sql.indexOf('DO $merge$');
  const lockIndex = sql.indexOf('LOCK TABLE public.vereinsfunktion');

  assert.ok(doIndex >= 0);
  assert.ok(lockIndex > doIndex);
});

test('AP-02 final RLS guard verifies the named policy inventory', async () => {
  const sql = await readFile(finalRlsGuardUrl, 'utf8');
  const expectedPolicyRows = sql.match(/^\s*\('[a-z_]+','[a-z0-9_]+'\),?$/gm) ?? [];
  const createPolicyStatements = sql.match(/^CREATE POLICY /gm) ?? [];

  assert.equal(expectedPolicyRows.length, 15);
  assert.equal(createPolicyStatements.length, 17);
  assert.match(sql, /Erwartete 9 Policy-Zieltabellen/);
  assert.match(sql, /Unerwartete weitere Legacy-Policy vorhanden/);
  assert.match(sql, /Noch % alte RLS-Pruefungen|Noch % alte RLS-Prüfungen/);
});

test('AP-02 final RLS migration restores the missing club configuration schema', async () => {
  const sql = await readFile(finalRlsGuardUrl, 'utf8');

  assert.match(sql, /CREATE TABLE IF NOT EXISTS public\.vereinskonfiguration/);
  assert.match(sql, /CREATE UNIQUE INDEX IF NOT EXISTS ux_vereinskonfiguration_nur_eine_aktive/);
  assert.match(sql, /CREATE TRIGGER trg_browser_edit_lock[\s\S]*enforce_browser_edit_lock\('vereinskonfiguration','id'\)/);
  assert.match(sql, /CREATE POLICY vereinskonfiguration_select_admin_vorstand/);
  assert.match(sql, /GRANT SELECT, INSERT, UPDATE ON public\.vereinskonfiguration TO authenticated/);
});

test('AP-02 replay restores the two legacy select-policy anchors for their cleanup', async () => {
  const sql = await readFile(finalRlsGuardUrl, 'utf8');

  assert.match(sql, /CREATE OR REPLACE FUNCTION public\.is_kgv_admin_or_vorstand\(\)/);
  assert.match(sql, /CREATE POLICY mitglied_gesetzlicher_vertreter_select_admin_vorstand/);
  assert.match(sql, /CREATE POLICY vereinskonfiguration_select_admin_vorstand/);
});

test('AP-03 guarded function rewrites normalize CRLF on Windows', async () => {
  const sql = await readFile(ap03GuardUrl, 'utf8');
  const normalizedReads = sql.match(/replace\(pg_get_functiondef\([^;]+?E'\\r\\n', E'\\n'\)/g) ?? [];

  assert.equal(normalizedReads.length, 6);
});

test('local Supabase CI commands use the repository-pinned NPX CLI', async () => {
  const workflow = await readFile(workflowUrl, 'utf8');

  assert.doesNotMatch(workflow, /supabase\/setup-cli@/);
  assert.doesNotMatch(workflow, /run:\s+supabase\s/m);
  assert.match(workflow, /npx --yes supabase@2\.121\.0 start/);
  assert.match(workflow, /npx --yes supabase@2\.121\.0 db reset --local/);
  assert.match(workflow, /npx --yes supabase@2\.121\.0 stop --no-backup/);
});

test('FR211 accepts only the deployed and fresh-replay JEA source bodies', async () => {
  const sql = await readFile(fr211GuardUrl, 'utf8');

  assert.match(
    sql,
    /rpc_export_jahresablesung_status\(integer,boolean\)'\s*,\s*ARRAY\['70ba7ca448d1f89072ee242f95895cbc',\s*'93eee349ebc60c314aca27995612b680'\]/,
  );
  assert.match(sql, /=\s*ANY\(f\.source_md5s\)/);
});

test('FR212 normalizes stored CRLF before each guarded function rewrite', async () => {
  const sql = await readFile(fr212GuardUrl, 'utf8');
  const normalizedDefinitions = sql.match(
    /(?:v_definition|d)\s*:=\s*replace\((?:v_definition|d),\s*chr\(13\)\s*\|\|\s*chr\(10\),\s*chr\(10\)\);/g,
  ) ?? [];

  assert.equal(normalizedDefinitions.length, 9);
  assert.match(sql, /MD5 021fba7dd90f14711294755f20066bee/);
  assert.match(sql, /md5\(candidate\)<>\s*'021fba7dd90f14711294755f20066bee'/);
});

test('S01/S02 normalize stored CRLF before source fingerprint checks', async () => {
  const sql = await readFile(s01S02GuardUrl, 'utf8');
  const normalizedDefinitions = sql.match(
    /v_def\s*:=\s*replace\(v_def,\s*E'\\r\\n',\s*E'\\n'\);/g,
  ) ?? [];

  assert.equal(normalizedDefinitions.length, 2);
  assert.match(sql, /md5\(v_def\) <> 'f8eb796ce2dff96930d911ffdb2a21da'/);
  assert.match(sql, /md5\(v_def\) <> '782e7c4459030ea2e9ff72274b994ece'/);
});

test('RB04B normalizes helper definitions before its aggregate fingerprint', async () => {
  const sql = await readFile(rb04bGuardUrl, 'utf8');

  assert.match(
    sql,
    /md5\(replace\(pg_get_functiondef\(p\.oid\),\s*E'\\r\\n',\s*E'\\n'\)\)/,
  );
  assert.match(sql, /a8c914dbc3636a986f3b2bc279f47b50/);
});

test('RB04CD normalizes trigger definitions before its aggregate fingerprint', async () => {
  const sql = await readFile(rb04cdGuardUrl, 'utf8');

  assert.match(
    sql,
    /md5\(replace\(pg_get_functiondef\(p\.oid\),\s*E'\\r\\n',\s*E'\\n'\)\) AS fp/,
  );
  assert.match(sql, /<>\s*ALL\(ARRAY\['71f939404493b8501ab1cfb3f813a68b','9ab427664903b8672255fb9729237d3e'\]\)/);
  assert.match(sql, /replace\(pg_get_triggerdef\(t\.oid,true\),\s*E'\\r\\n',\s*E'\\n'\)/);
  assert.match(sql, /1dc8d6bb91461031f6b36967b21d1ad7/);
  assert.doesNotMatch(sql, /t\.tgqual::text/);
});

test('RB04D2 normalizes the lock-core definition for pre- and post-fingerprints', async () => {
  const sql = await readFile(rb04d2GuardUrl, 'utf8');
  const readIndex = sql.indexOf('SELECT pg_get_functiondef(f) INTO def;');
  const normalizeIndex = sql.indexOf("def := replace(def,E'\\r\\n',E'\\n');");
  const baselineIndex = sql.indexOf("IF md5(def) <> ALL(ARRAY['20b1b1f9d532e3ae205c648a31bfabc1','33527d999427b460846ce101327227b6'])");

  assert.ok(readIndex >= 0 && normalizeIndex > readIndex && baselineIndex > normalizeIndex);
  assert.match(
    sql,
    /md5\(replace\(pg_get_functiondef\(f\),E'\\r\\n',E'\\n'\)\) IS DISTINCT FROM 'a8dcf00755a2e01257c883379a5a222b'/,
  );
});

test('remaining RB04D guards normalize definitions before pre- and post-fingerprints', async () => {
  for (const url of remainingRb04dGuardUrls) {
    const sql = await readFile(url, 'utf8');
    const readIndex = sql.indexOf('SELECT pg_get_functiondef(f) INTO def;');
    const normalizeIndex = sql.indexOf("def:=replace(def,E'\\r\\n',E'\\n');");
    const baseline = url.pathname.includes('rb04d4_')
      ? "IF md5(def) <> ALL(ARRAY[expected_before, CASE"
      : 'IF md5(def) IS DISTINCT FROM expected_before';
    const baselineIndex = sql.indexOf(baseline);
    if (url.pathname.includes('rb04d4_')) {
      assert.match(sql, /WHEN f='public\.mark_meter_defective\(bigint\)'::regprocedure THEN 'f182264670ed8d0a1bae6904c684ae88'\s+ELSE '68be3395c139de1b6fd4b8d8d152f914' END\]\)/);
      assert.match(sql, /has_function_privilege\('anon',f,'execute'\)/);
      assert.match(sql, /NOT has_function_privilege\('authenticated',f,'execute'\)/);
      assert.match(sql, /NOT has_function_privilege\('service_role',f,'execute'\)/);
    }

    assert.ok(readIndex >= 0 && normalizeIndex > readIndex && baselineIndex > normalizeIndex, url.pathname);
    assert.match(
      sql,
      /md5\(replace\(pg_get_functiondef\(f\),E'\\r\\n',E'\\n'\)\) IS DISTINCT FROM expected_after/,
      url.pathname,
    );
  }
});
