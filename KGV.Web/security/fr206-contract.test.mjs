import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';

// Stable even when launched from C:\Users\... or another working directory.
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const source = (relative) => readFileSync(path.join(root,relative),'utf8');
const sql = source('supabase/migrations/20261122235600_fr2_06_finance_rights_cutover.sql');
const checks = source('KGV.Core/Security/PermissionChecks.cs');
const page = source('KGV.Maui/Pages/JahresabschlussPage.cs');
const shell = source('KGV.Maui/AdminShell.cs');
const supabase = source('KGV.Infrastructure/Services/SupabaseService.cs');
const rights = ['finanzen.lesen','finanzen.rechnungen','finanzen.kostenarten',
  'jahresabschluss.vorbereiten','jahresabschluss.finalisieren','jahresabschluss.korrigieren'];

test('alle sechs Finanzrechte als getrennte Autorisierung',() => {
  for(const right of rights) assert.ok(sql.includes(right), `${right} fehlt`);
  for(const method of ['CanReadFinance','CanManageFinanceInvoices','CanManageFinanceCostTypes',
    'CanPrepareYearEndClosing','CanPerformYearEndClosing','CanCorrectYearEndClosing']) {
    assert.ok(checks.includes(`bool ${method}(`),`${method} fehlt`);
  }
  assert.ok(!checks.includes('HasPermission(context, PermissionFlags.CanPerformYearEndClosing)'),
    'Alte Bitberechtigung darf Finalisierung nicht legitimieren');
});

test('deaktivierte Rechte werden nicht aktiviert',()=>{
  assert.match(sql,/code\s*=\s*'jahresabschluss\.finalisieren'[\s\S]*?aktiv IS FALSE/);
  assert.match(sql,/code\s*=\s*'jahresabschluss\.korrigieren'[\s\S]*?aktiv IS FALSE/);
  assert.doesNotMatch(sql,/UPDATE\s+public\.fachrecht|SET\s+aktiv\s*=\s*true/i);
});

test('Abschluss-Uebersicht nicht mehr von finalisieren abhaengig',()=>{
  assert.ok(shell.includes('HasAnyFinanceAccess(managementContext)'));
  assert.ok(page.includes('CanPrepareYearEndClosing'));
  assert.ok(page.includes('CanManageFinanceInvoices'));
  assert.ok(page.includes('CanManageFinanceCostTypes'));
  assert.ok(!page.includes('CanPerformYearEndClosing'));
});

test('Positionen nur noch ueber den atomaren und idempotenten RPC',()=>{
  const first = supabase.indexOf('public Task<bool> ReplaceJahresabschlussPositionenAsync');
  const next = supabase.indexOf('public Task<JahresabschlussRecord?> FinalizeJahresabschlussAsync',first);
  assert.ok(first>=0&&next>first);
  const block=supabase.slice(first,next);
  assert.ok(block.includes('Rpc<int>("fr206_replace_jahresabschluss_positionen"'));
  assert.ok(!block.includes('.Delete()'));
  assert.ok(sql.includes('IF v_current = v_wanted THEN'));
  assert.ok(sql.includes('FOR UPDATE'));
});

test('keine pauschalen ALL-Policies mehr im FR2-06 Finanzbereich',()=>{
  assert.doesNotMatch(sql,/CREATE POLICY[^;]*FOR ALL\b/i);
  assert.match(sql,/DROP POLICY IF EXISTS jahresabschluss_admin_full/);
  assert.match(sql,/fr206_guard_consumption/);
  assert.match(sql,/fr206_guard_finalized_source/);
});
