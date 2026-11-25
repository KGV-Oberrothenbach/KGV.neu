import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { performAccountDeletion } from '../functions/kgv-delete-user/delete-account.mjs';

const ACTOR = '11111111-1111-4111-8111-111111111111';
const TARGET = '22222222-2222-4222-8222-222222222222';
const ALT = '33333333-3333-4333-8333-333333333333';
const ID = 123;

function fixture(options = {}) {
  const s = {
    permission: options.permission ?? true,
    member: options.noMember ? null : { id: ID, auth_user_id: options.unlinked ? null : TARGET },
    app: options.noApp ? null : {
      user_id: TARGET, mitglied_id: options.badMemberId ? 555 : ID,
      role: options.role ?? 'user', permission_grants: options.grants ?? 0,
      permission_revocations: options.revocations ?? 0, is_protected: options.protected ?? false,
    },
    lock: options.pending ? { user_id: TARGET, operation_id: ALT, mitglied_id: ID } : null,
    authExists: true, authFail: options.authFail ?? false, authUnknown: options.authUnknown ?? false,
    beginReject: options.beginReject ?? false, beginTimeout: options.beginTimeout ?? false,
    restoreFail: options.restoreFail ?? false, finishFail: options.finishFail ?? false,
    base: options.base ?? (options.role === 'admin' ? 512 : 0), writes: [], reads: [],
  };

  function table(name) {
    const q = { name, filters: [] };
    const chain = {
      select() { return chain; },
      eq(key, value) { q.filters.push([key, value]); return chain; },
      async maybeSingle() {
        s.reads.push(name);
        const data = name === 'mitglied' ? s.member
          : name === 'app_user' ? s.app
          : name === 'app_user_zusatzrolle' ? (options.additionalAdmin ? { user_id: TARGET, zusatzrolle_code: 'admin' } : null)
          : name === 'protected_admin_identity' ? (options.protectedIdentity ? { user_id: TARGET } : null)
          : s.lock;
        if (data && q.filters.some(([key, value]) => data[key] !== value)) return { data: null, error: null };
        return { data, error: null };
      },
    };
    return chain;
  }

  const callerClient = {
    async rpc(name, args) {
      if (name === 'ap02_has_effective_right') {
        assert.equal(args.p_fachrecht, 'benutzer.verwalten');
        return { data: s.permission, error: null };
      }
      if (name === 'begin_auth_user_deletion') {
        assert.equal(args.p_user_id, TARGET);
        assert.equal(args.p_mitglied_id, ID);
        assert.match(args.p_operation_id, /^[0-9a-f-]{36}$/i);
        if (s.beginReject) return { data: null, error: { message: 'rejected' } };
        s.writes.push('begin');
        s.lock = { user_id: TARGET, mitglied_id: ID, operation_id: args.p_operation_id };
        s.member.auth_user_id = null;
        s.app = null;
        if (s.beginTimeout) return { data: null, error: { message: 'timeout after commit' } };
        return { data: true, error: null };
      }
      throw new Error('Unexpected caller RPC: ' + name);
    },
  };
  const adminClient = {
    from: table,
    async rpc(name, args) {
      if (name === 'ap02_calculate_user_rights') {
        assert.equal(args.p_user_id, TARGET);
        return { data: Array.from({ length: 55 }, (_, i) => ({
          code: i === 0 ? 'berechtigungen.verwalten' : `other.right.${i}`,
          erlaubt: i === 0 && Boolean(options.effectiveRoleManager),
        })), error: null };
      }
      if (name === 'restore_auth_user_deletion') {
        if (!s.lock || s.lock.operation_id !== args.p_operation_id) throw new Error('Different operation');
        s.writes.push('restore');
        if (s.restoreFail) return { data: null, error: { message: 'restore failed' } };
        s.member.auth_user_id = TARGET;
        s.app = { user_id: TARGET, mitglied_id: ID, role: options.role ?? 'user',
          permission_grants: options.grants ?? 0, permission_revocations: options.revocations ?? 0,
          is_protected: false };
        s.lock = null;
        return { data: true, error: null };
      }
      if (name === 'finish_auth_user_deletion') {
        if (!s.lock || s.lock.operation_id !== args.p_operation_id) throw new Error('Different operation');
        s.writes.push('finish');
        if (s.finishFail) return { data: false, error: { message: 'finish failed' } };
        s.lock = null;
        return { data: true, error: null };
      }
      throw new Error('Unexpected admin RPC: ' + name);
    },
    auth: { admin: {
      async deleteUser(userId) {
        assert.equal(userId, TARGET);
        s.writes.push('auth-delete');
        if (s.authFail) return { error: { message: 'foreign key restrict' } };
        s.authExists = false;
        return { error: null };
      },
      async getUserById(userId) {
        assert.equal(userId, TARGET);
        if (s.authUnknown) return { error: { message: 'unknown' } };
        return { error: null, data: { user: s.authExists ? { id: TARGET } : null } };
      },
    } },
  };
  return { s, callerClient, adminClient };
}

function run(f, override = {}) {
  return performAccountDeletion({
    callerClient: f.callerClient, adminClient: f.adminClient,
    callerId: ACTOR, memberId: ID, expectedAuthUserId: TARGET, ...override,
  });
}

test('authorization: missing benutzer.verwalten prevents any target reads', async () => {
  const f = fixture({ permission: false });
  assert.equal((await run(f)).httpStatus, 403);
  assert.deepEqual(f.s.reads, []);
  assert.deepEqual(f.s.writes, []);
});
test('no member is not found', async () => {
  const f = fixture({ noMember: true });
  assert.equal((await run(f)).httpStatus, 404);
});
test('already unlinked does not delete Auth account', async () => {
  const f = fixture({ unlinked: true });
  assert.equal((await run(f)).outcome, 'no_user');
  assert.deepEqual(f.s.writes, []);
});
test('stale UI target ID cannot delete a replacement Auth link', async () => {
  const f = fixture();
  assert.equal((await run(f, { expectedAuthUserId: ALT })).httpStatus, 409);
  assert.deepEqual(f.s.writes, []);
});
test('own account is blocked', async () => {
  const f = fixture();
  assert.equal((await run(f, { callerId: TARGET })).httpStatus, 403);
});
test('protected target is blocked', async () => {
  const f = fixture({ protected: true });
  assert.equal((await run(f)).httpStatus, 403);
});
test('role-manager target is blocked regardless of is_protected', async () => {
  const f = fixture({ additionalAdmin: true });
  assert.equal((await run(f)).httpStatus, 403);
});
test('target with effective berechtigungen.verwalten cannot be deleted', async () => {
  const f = fixture({ effectiveRoleManager: true });
  assert.equal((await run(f)).httpStatus, 403);
});
test('legacy admin role alone with no V2 privilege does not block dry run', async () => {
  const f = fixture({ role: 'admin', revocations: 512 });
  assert.equal((await run(f, { dryRun: true })).outcome, 'eligible');
  assert.deepEqual(f.s.writes, []);
});
test('protected identity is blocked independently of legacy flags', async () => {
  const f = fixture({ protectedIdentity: true });
  assert.equal((await run(f)).httpStatus, 403);
  assert.deepEqual(f.s.writes, []);
});
test('app_user mismatched member is blocked', async () => {
  const f = fixture({ badMemberId: true });
  assert.equal((await run(f)).httpStatus, 409);
});
test('already pending deletion is blocked before a second attempt', async () => {
  const f = fixture({ pending: true });
  assert.equal((await run(f)).httpStatus, 409);
  assert.deepEqual(f.s.writes, []);
});
test('successful deletion reserves, deletes Auth, finalizes', async () => {
  const f = fixture();
  assert.equal((await run(f)).outcome, 'deleted');
  assert.deepEqual(f.s.writes, ['begin', 'auth-delete', 'finish']);
  assert.equal(f.s.authExists, false);
  assert.equal(f.s.member.auth_user_id, null);
  assert.equal(f.s.lock, null);
});
test('failed database reservation does not call Auth delete', async () => {
  const f = fixture({ beginReject: true });
  assert.equal((await run(f)).httpStatus, 409);
  assert.equal(f.s.authExists, true);
  assert.deepEqual(f.s.writes, []);
});
test('ambiguous SQL response after commit restores this reservation', async () => {
  const f = fixture({ beginTimeout: true });
  assert.equal((await run(f)).outcome, 'unlink_outcome_restored');
  assert.deepEqual(f.s.writes, ['begin', 'restore']);
  assert.equal(f.s.member.auth_user_id, TARGET);
});
test('failed Auth delete restores member, privileges, and clears lock', async () => {
  const f = fixture({ authFail: true, grants: 8 });
  assert.equal((await run(f)).outcome, 'delete_failed_restored');
  assert.deepEqual(f.s.writes, ['begin', 'auth-delete', 'restore']);
  assert.equal(f.s.app.permission_grants, 8);
  assert.equal(f.s.lock, null);
});
test('unknown Auth status does not release deletion reservation', async () => {
  const f = fixture({ authFail: true, authUnknown: true });
  assert.equal((await run(f)).outcome, 'manual_review_required');
  assert.equal(f.s.member.auth_user_id, null);
  assert.ok(f.s.lock);
});
test('failed restore holds reservation for manual recovery', async () => {
  const f = fixture({ authFail: true, restoreFail: true });
  assert.equal((await run(f)).outcome, 'manual_recovery_required');
  assert.ok(f.s.lock);
});
test('failed finalize does not claim complete success', async () => {
  const f = fixture({ finishFail: true });
  assert.equal((await run(f)).outcome, 'manual_review_required');
  assert.equal(f.s.authExists, false);
  assert.ok(f.s.lock);
});
test('invalid member ID is rejected without data access', async () => {
  const f = fixture();
  assert.equal((await run(f, { memberId: -1 })).httpStatus, 400);
  assert.deepEqual(f.s.reads, []);
});
test('invalid caller is rejected', async () => {
  const f = fixture();
  assert.equal((await run(f, { callerId: 'garbage' })).httpStatus, 401);
});
test('SQL migration guards role, member relation, re-link, and legacy RPC', () => {
  const root = fileURLToPath(new URL('../../', import.meta.url));
  const migrations = join(root, 'supabase', 'migrations');
  const name = readdirSync(migrations).find(file => /^\d+_reparaturblock03_safe_account_deletion\.sql$/.test(file));
  assert.ok(name, 'new migration missing');
  const sql = readFileSync(join(migrations, name), 'utf8');
  assert.match(sql, /CREATE FUNCTION public\.begin_auth_user_deletion/);
  assert.match(sql, /FOR UPDATE/);
  assert.match(sql, /v_link IS DISTINCT FROM p_user_id/);
  assert.match(sql, /pg_advisory_xact_lock/);
  assert.match(sql, /auth_user_delete_lock/);
  assert.match(sql, /guard_pending_auth_user_deletion/);
  assert.match(sql, /CREATE FUNCTION public\.restore_auth_user_deletion/);
  assert.match(sql, /CREATE FUNCTION public\.finish_auth_user_deletion/);
  assert.match(sql, /REVOKE ALL ON FUNCTION public\.unlink_app_user\(uuid\) FROM PUBLIC, anon, authenticated/);
});
test('Edge entrypoint validates expected Auth user independently from member ID', () => {
  const index = readFileSync(new URL('../functions/kgv-delete-user/index.ts', import.meta.url), 'utf8');
  assert.match(index, /expectedAuthUserId/);
  assert.match(index, /callerClient\.auth\.getUser\(\)/);
  assert.match(index, /performAccountDeletion/);
});
