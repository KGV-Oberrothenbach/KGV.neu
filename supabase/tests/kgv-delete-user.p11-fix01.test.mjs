import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { performAccountDeletion } from '../functions/kgv-delete-user/delete-account.mjs';

const ACTOR = '11111111-1111-4111-8111-111111111111';
const TARGET = '22222222-2222-4222-8222-222222222222';
const ID = 123;

function createFixture(options = {}) {
  const s = {
    allow: options.allow ?? true,
    member: { id: ID, auth_user_id: options.linked === false ? null : TARGET },
    app: options.appAbsent ? null : {
      user_id: TARGET, mitglied_id: ID, role: options.targetRole ?? 'user',
      permission_grants: options.grants ?? 0,
      permission_revocations: options.revocations ?? 0,
      is_protected: options.protected ?? false,
    },
    authExists: true, rpcUnlinkError: options.rpcUnlinkError ?? false,
    rpcUnlinkErrorAfterWrite: options.rpcUnlinkErrorAfterWrite ?? false,
    authDeleteError: options.authDeleteError ?? false,
    authStateUnknown: options.authStateUnknown ?? false,
    base: options.base ?? (options.targetRole === 'admin' ? 512 : 0),
    writes: [], reads: [],
  };

  function tableClient(table) {
    const q = { table, action: 'read', eqKey: null, eqValue: null, isKey: null, isValue: null, values: null };
    const chain = {
      select() { return chain; },
      update(v) { q.action = 'update'; q.values = v; return chain; },
      eq(k, v) { q.eqKey = k; q.eqValue = v; return chain; },
      is(k, v) { q.isKey = k; q.isValue = v; return chain; },
      async maybeSingle() {
        s.reads.push({ table, action: q.action });
        if (q.action === 'read') return { data: table === 'mitglied' ? { ...s.member } : s.app && { ...s.app }, error: null };
        s.writes.push({ table, values: q.values });
        if (table === 'mitglied') {
          if (q.isKey && s.member[q.isKey] !== q.isValue) return { data: null, error: null };
          Object.assign(s.member, q.values);
          if (s.member.auth_user_id && !s.app) s.app = {
            user_id: TARGET, mitglied_id: ID, role: 'user', permission_grants: 0,
            permission_revocations: 0, is_protected: false,
          };
          return { data: { id: ID }, error: null };
        }
        if (table === 'app_user' && s.app) { Object.assign(s.app, q.values); return { data: { user_id: TARGET }, error: null }; }
        return { data: null, error: null };
      },
    };
    return chain;
  }

  const callerClient = {
    async rpc(name, args) {
      if (name === 'has_effective_permission') {
        assert.equal(args.p_permission, 512);
        return { data: s.allow, error: null };
      }
      if (name === 'unlink_app_user') {
        assert.equal(args.p_user_id, TARGET);
        if (s.rpcUnlinkError) return { data: null, error: { message: 'blocked' } };
        s.writes.push({ rpc: name });
        s.member.auth_user_id = null;
        s.app = null;
        if (s.rpcUnlinkErrorAfterWrite) return { data: null, error: { message: 'timeout after commit' } };
        return { data: null, error: null };
      }
      throw new Error('Unexpected caller RPC: ' + name);
    },
  };
  const adminClient = {
    from: tableClient,
    async rpc(name, args) {
      if (name !== 'permission_role_base') throw new Error('Unexpected admin RPC: ' + name);
      assert.equal(args.p_role, s.app?.role);
      return { data: s.base, error: null };
    },
    auth: { admin: {
      async deleteUser(uid) {
        assert.equal(uid, TARGET);
        s.writes.push({ authDelete: uid });
        if (s.authDeleteError) return { error: { message: 'external failure' } };
        s.authExists = false;
        return { error: null };
      },
      async getUserById(uid) {
        assert.equal(uid, TARGET);
        return s.authStateUnknown ? { error: { message: 'unavailable' } } :
          { error: null, data: { user: s.authExists ? { id: TARGET } : null } };
      },
    } },
  };
  return { state: s, callerClient, adminClient };
}

async function run(f, override = {}) {
  return performAccountDeletion({ callerClient: f.callerClient, adminClient: f.adminClient,
    callerId: ACTOR, memberId: ID, ...override });
}

test('01: revoked CanManageRoles is rejected before any target read or write', async () => {
  const f = createFixture({ allow: false });
  const r = await run(f);
  assert.equal(r.httpStatus, 403);
  assert.deepEqual(f.state.writes, []);
  assert.deepEqual(f.state.reads, []);
});

test('02: protected target cannot be deleted', async () => {
  const f = createFixture({ protected: true });
  assert.equal((await run(f)).httpStatus, 403);
  assert.deepEqual(f.state.writes, []);
});

test('03: any target with CanManageRoles is blocked, even if not marked protected', async () => {
  const f = createFixture({ targetRole: 'admin' });
  assert.equal((await run(f)).httpStatus, 403);
  assert.deepEqual(f.state.writes, []);
});

test('04: individually granted CanManageRoles on target also blocks deletion', async () => {
  const f = createFixture({ grants: 512 });
  assert.equal((await run(f)).httpStatus, 403);
});

test('05: revoked target permission is not erroneously treated as active', async () => {
  const f = createFixture({ targetRole: 'admin', revocations: 512 });
  const r = await run(f, { dryRun: true });
  assert.equal(r.outcome, 'eligible');
  assert.deepEqual(f.state.writes, []);
});

test('06: deletion of caller own account is blocked', async () => {
  const f = createFixture();
  assert.equal((await run(f, { callerId: TARGET })).httpStatus, 403);
  assert.deepEqual(f.state.writes, []);
});

test('07: unlinked member has nothing to delete', async () => {
  const f = createFixture({ linked: false });
  assert.equal((await run(f)).outcome, 'no_user');
  assert.deepEqual(f.state.writes, []);
});

test('08: orphaned app_user blocks deletion', async () => {
  const f = createFixture({ appAbsent: true });
  assert.equal((await run(f)).httpStatus, 409);
  assert.deepEqual(f.state.writes, []);
});

test('09: dryRun verifies authorization and changes nothing', async () => {
  const f = createFixture();
  assert.equal((await run(f, { dryRun: true })).outcome, 'eligible');
  assert.deepEqual(f.state.writes, []);
});

test('10: normal permitted deletion uses protected unlink RPC then Auth admin', async () => {
  const f = createFixture();
  const r = await run(f);
  assert.equal(r.outcome, 'deleted');
  assert.equal(f.state.authExists, false);
  assert.equal(f.state.member.auth_user_id, null);
  assert.deepEqual(f.state.writes.map(x => x.rpc ?? x.authDelete), ['unlink_app_user', TARGET]);
});

test('11: failed DB unlink blocks Auth deletion', async () => {
  const f = createFixture({ rpcUnlinkError: true });
  assert.equal((await run(f)).httpStatus, 409);
  assert.equal(f.state.authExists, true);
  assert.equal(f.state.writes.length, 0);
});

test('12: Auth API failure with confirmed existing user restores member link and permissions', async () => {
  const f = createFixture({ authDeleteError: true, grants: 8 });
  const r = await run(f);
  assert.equal(r.outcome, 'delete_failed_restored');
  assert.equal(f.state.authExists, true);
  assert.equal(f.state.member.auth_user_id, TARGET);
  assert.equal(f.state.app.permission_grants, 8);
});

test('13: ambiguous Auth status reports manual review instead of falsely claiming success', async () => {
  const f = createFixture({ authDeleteError: true, authStateUnknown: true });
  assert.equal((await run(f)).outcome, 'manual_review_required');
});

test('14: noninteger input is rejected without any read or write', async () => {
  const f = createFixture();
  assert.equal((await run(f, { memberId: -1 })).httpStatus, 400);
  assert.deepEqual(f.state.reads, []);
});

test('15: index exposes only verified JWT and not role-string authorisation', () => {
  const index = readFileSync(new URL('../functions/kgv-delete-user/index.ts', import.meta.url), 'utf8');
  const core = readFileSync(new URL('../functions/kgv-delete-user/delete-account.mjs', import.meta.url), 'utf8');
  assert.match(index, /callerClient\.auth\.getUser\(\)/);
  assert.match(core, /has_effective_permission/);
  assert.match(core, /unlink_app_user/);
  assert.doesNotMatch(core, /callerRole\s*===\s*['"](?:admin|vorstand)/);
  assert.doesNotMatch(index, /callerRole\s*===\s*['"](?:admin|vorstand)/);
});

test('16: migration maintains caller and target effective-rights gates under a target row lock', () => {
  const root = fileURLToPath(new URL('../../', import.meta.url));
  const migrationsDir = join(root, 'supabase', 'migrations');
  const matchingMigration = existsSync(migrationsDir)
    ? readdirSync(migrationsDir).find(name => /^\d+_p11_fix01_secure_unlink_app_user\.sql$/.test(name))
    : null;
  const file = matchingMigration
    ? join(migrationsDir, matchingMigration)
    : join(root, 'migration-template', 'p11_fix01_secure_unlink_app_user.sql');
  const sql = readFileSync(file, 'utf8');
  assert.match(sql, /CREATE OR REPLACE FUNCTION public\.unlink_app_user\(p_user_id uuid\)/);
  assert.match(sql, /SECURITY DEFINER/);
  assert.match(sql, /IF auth\.uid\(\) IS NULL OR NOT public\.has_effective_permission\(512\)/);
  assert.match(sql, /FOR UPDATE/);
  assert.match(sql, /v_target\.is_protected/);
  assert.match(sql, /permission_role_base\(v_target\.role\)/);
  assert.match(sql, /IF \(v_effective & 512\) = 512/);
  assert.match(sql, /REVOKE ALL ON FUNCTION public\.unlink_app_user\(uuid\) FROM PUBLIC, anon/);
  assert.doesNotMatch(sql, /DROP TABLE|TRUNCATE|DELETE FROM auth\.users/i);
});


test('17: ambiguous unlink response after commit restores still-existing Auth account', async () => {
  const f = createFixture({ rpcUnlinkErrorAfterWrite: true, grants: 64 });
  const r = await run(f);
  assert.equal(r.outcome, 'unlink_outcome_restored');
  assert.equal(f.state.member.auth_user_id, TARGET);
  assert.equal(f.state.app.permission_grants, 64);
  assert.equal(f.state.authExists, true);
});
