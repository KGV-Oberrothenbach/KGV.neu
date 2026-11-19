import assert from 'node:assert/strict';
import { readFile, readdir } from 'node:fs/promises';
import { resolve } from 'node:path';
import test from 'node:test';
import { PGlite } from '@electric-sql/pglite';

const root = resolve(import.meta.dirname, '../..');
const read = (path) => readFile(resolve(root, path), 'utf8');

async function actualFunction(file, name) {
  const source = await read(`supabase/migrations/${file}`);
  const start = source.indexOf(`create or replace function public.${name}(`);
  assert.ok(start >= 0, `function ${name} not found in ${file}`);
  const end = source.indexOf('$$;', start);
  assert.ok(end > start, `function ${name} has no terminator in ${file}`);
  return source.slice(start, end + 3);
}

async function asAuthenticated(db, userId, sql, args = []) {
  await db.query("select set_config('test.jwt_sub', $1, false)", [userId]);
  await db.query("select set_config('test.jwt_role', 'authenticated', false)");
  await db.exec('begin; set local role authenticated;');
  try {
    const result = await db.query(sql, args);
    await db.exec('commit;');
    return result.rows;
  } catch (error) {
    await db.exec('rollback;');
    throw error;
  }
}

async function rejected(db, userId, dispatchId, expectedCode) {
  await assert.rejects(
    asAuthenticated(db, userId, 'select * from public.claim_contract_dispatch($1)', [dispatchId]),
    (error) => error.code === expectedCode,
  );
}

test('contract dispatch claim rejects once without PostgREST serialization retries', async (t) => {
  const migrations = await readdir(resolve(root, 'supabase/migrations'));
  const repair = migrations.find((name) => name.endsWith('_fix_contract_dispatch_retry_timeout.sql'));
  assert.ok(repair, 'contract dispatch timeout repair migration is missing');

  const db = new PGlite();
  try {
    await db.exec(`
      create role anon;
      create role authenticated;
      create role service_role;
      create schema auth;
      create function auth.uid() returns uuid language sql stable as $$
        select nullif(current_setting('test.jwt_sub', true), '')::uuid
      $$;
      create function auth.role() returns text language sql stable as $$
        select coalesce(nullif(current_setting('test.jwt_role', true), ''), 'anon')
      $$;
      create table public.app_user (
        user_id uuid primary key,
        role text not null,
        permission_grants bigint not null default 0,
        permission_revocations bigint not null default 0
      );
      create table public.kommunikationsvorgang (
        id bigint primary key,
        zugewiesen_an uuid
      );
      create table public.versandinstanz (
        id bigint primary key,
        kommunikationsvorgang_id bigint not null references public.kommunikationsvorgang(id),
        status text not null,
        beansprucht_am timestamptz,
        beansprucht_von uuid,
        letzter_fehler text,
        betreff_snapshot text not null,
        text_snapshot text not null
      );
      create table public.versandinstanz_empfaenger_snapshot (
        id bigint primary key,
        versandinstanz_id bigint not null references public.versandinstanz(id),
        status text not null
      );
      grant usage on schema public, auth to authenticated, service_role;
    `);

    for (const name of [
      'permission_role_base',
      'known_permission_mask',
      'current_effective_permissions',
      'has_effective_permission',
    ]) {
      await db.exec(await actualFunction('20261108120000_p11_ap02a_effective_permissions.sql', name));
    }
    await db.exec(await actualFunction('20261111120000_p11_ap02a_a34_contract_dispatch.sql', 'kann_vertragsversand_ausfuehren'));
    await db.exec(await read(`supabase/migrations/${repair}`));

    const users = {
      admin: '00000000-0000-0000-0000-000000000101',
      denied: '00000000-0000-0000-0000-000000000102',
      revoked: '00000000-0000-0000-0000-000000000103',
      granted: '00000000-0000-0000-0000-000000000104',
      foreign: '00000000-0000-0000-0000-000000000105',
    };
    await db.exec(`
      insert into public.app_user(user_id, role, permission_grants, permission_revocations) values
        ('${users.admin}', 'admin', 0, 0),
        ('${users.denied}', 'user', 0, 0),
        ('${users.revoked}', 'admin', 0, 1073741824),
        ('${users.granted}', 'user', 1073741824, 0),
        ('${users.foreign}', 'user', 1073741824, 0);
      insert into public.kommunikationsvorgang(id, zugewiesen_an) values
        (1, '${users.admin}'),
        (2, '${users.granted}');
      insert into public.versandinstanz(id, kommunikationsvorgang_id, status, beansprucht_am, betreff_snapshot, text_snapshot) values
        (10, 1, 'bereit', null, 'Admin subject', 'Admin body'),
        (11, 2, 'bereit', null, 'Grant subject', 'Grant body'),
        (12, 2, 'wird_versendet', now(), 'Claimed subject', 'Claimed body'),
        (13, 2, 'versendet', now(), 'Sent subject', 'Sent body');
      insert into public.versandinstanz_empfaenger_snapshot(id, versandinstanz_id, status) values
        (100, 10, 'bereit'),
        (101, 11, 'bereit'),
        (102, 12, 'bereit'),
        (103, 13, 'versendet');
    `);

    await t.test('authorized administrator rejects a missing dispatch with PT403', async () => {
      const started = performance.now();
      await rejected(db, users.admin, 2147483647, 'PT403');
      assert.ok(performance.now() - started < 3000, 'missing dispatch rejection was not prompt');
    });

    await t.test('missing permission and individual revocation fail before claim', async () => {
      await rejected(db, users.denied, 2147483647, '42501');
      await rejected(db, users.revoked, 2147483647, '42501');
    });

    await t.test('an individual grant is honored but assignment remains mandatory', async () => {
      await rejected(db, users.foreign, 11, 'PT403');
      const rows = await asAuthenticated(
        db,
        users.granted,
        'select * from public.claim_contract_dispatch($1)',
        [11],
      );
      assert.deepEqual(rows, [{ id: 11, betreff: 'Grant subject', mailtext: 'Grant body' }]);
      const state = await db.query('select status, beansprucht_von from public.versandinstanz where id=11');
      assert.equal(state.rows[0].status, 'wird_versendet');
      assert.equal(state.rows[0].beansprucht_von, users.granted);
    });

    await t.test('claimed and completed dispatches cannot be sent again', async () => {
      await rejected(db, users.granted, 11, 'PT403');
      await rejected(db, users.granted, 12, 'PT403');
      await rejected(db, users.granted, 13, 'PT403');
    });

    await t.test('the existing valid administrator claim path stays functional', async () => {
      const rows = await asAuthenticated(
        db,
        users.admin,
        'select * from public.claim_contract_dispatch($1)',
        [10],
      );
      assert.deepEqual(rows, [{ id: 10, betreff: 'Admin subject', mailtext: 'Admin body' }]);
    });

    await t.test('RPC execution remains restricted to authenticated and service roles', async () => {
      const privileges = await db.query(`
        select
          has_function_privilege('anon', 'public.claim_contract_dispatch(bigint)', 'execute') as anon,
          has_function_privilege('authenticated', 'public.claim_contract_dispatch(bigint)', 'execute') as authenticated,
          has_function_privilege('service_role', 'public.claim_contract_dispatch(bigint)', 'execute') as service_role
      `);
      assert.deepEqual(privileges.rows[0], { anon: false, authenticated: true, service_role: true });
    });
  } finally {
    await db.close();
  }
});
