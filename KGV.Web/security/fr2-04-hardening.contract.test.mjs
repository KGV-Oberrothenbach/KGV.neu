import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
const root = new URL('../../', import.meta.url);
const sql = readFileSync(new URL('supabase/migrations/20261122095500_fr2_04_parzelle_belegung_rls_write_split.sql', root), 'utf8');

test('FR2-04 hardening: SQL revokes unsafe table-wide privileges', () => {
  assert.match(sql, /REVOKE DELETE, TRUNCATE, REFERENCES, TRIGGER/);
  assert.match(sql, /ON TABLE public\.parzelle, public\.parzellen_belegung/);
  assert.match(sql, /FROM PUBLIC, anon, authenticated/);
  assert.match(sql, /has_table_privilege\('authenticated','public\.parzellen_belegung','TRUNCATE'\)/);
});

test('FR2-04 hardening: client changes to contract-linked occupancy are blocked', () => {
  assert.match(sql, /SECURITY INVOKER/);
  assert.match(sql, /current_user <> 'authenticated'/);
  assert.match(sql, /OLD\.pachtverhaeltnis_id IS NOT NULL OR NEW\.pachtverhaeltnis_id IS NOT NULL/);
  assert.match(sql, /NEW\.pachtverhaeltnis_id IS NOT NULL/);
  assert.match(sql, /CREATE TRIGGER trg_fr2_04_guard_direct_belegung_mutation/);
});

test('FR2-04 hardening: unrestricted changes to assignment identities are blocked', () => {
  assert.match(sql, /NEW\.parzelle_id IS DISTINCT FROM OLD\.parzelle_id/);
  assert.match(sql, /NEW\.mitglied_id IS DISTINCT FROM OLD\.mitglied_id/);
  assert.match(sql, /NEW\.von_datum IS DISTINCT FROM OLD\.von_datum/);
  assert.match(sql, /NEW\.bis_datum < NEW\.von_datum/);
});
