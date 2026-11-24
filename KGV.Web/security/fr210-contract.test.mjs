import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const read = (p) => readFileSync(path.join(repo, p), 'utf8');
const core = read('KGV.Core/Security/PermissionChecks.cs');
const page = read('KGV.Web/app/page.tsx');
const compatibility = read('KGV.Web/security/fachrechte-v2-compatibility.ts');
const capabilities = read('KGV.Web/security/capabilities.ts');
const shell = read('KGV.Maui/AdminShell.cs');
const files = readdirSync(path.join(repo, 'supabase/migrations'))
  .filter((p) => /^\d{14}_fr2_10_vereinsverwaltung\.sql$/.test(p));
assert.equal(files.length, 1, 'Genau eine FR2-10-Migration erwartet');
const sql = read('supabase/migrations/' + files[0]);
const method = (name) => {
  const i = core.indexOf(`bool ${name}(`);
  assert.ok(i >= 0, `${name} fehlt`);
  return core.slice(i, core.indexOf(';', i) + 1);
};

test('Fünf eigenständige Core-Fachrechte, keine Legacy-Flags', () => {
  for (const [name, right] of [
    ['CanManageAppointments','TerminVerwalten'],
    ['CanManageAnnouncements','BekanntmachungVerwalten'],
    ['CanManageClubFunctions','VereinsfunktionVerwalten'],
    ['CanManageClubConfiguration','VereinKonfiguration'],
    ['CanManageClubSeasonConfiguration','SaisonVerwalten'],
  ]) {
    assert.ok(method(name).includes(`FachrechteV2.${right}`));
    assert.doesNotMatch(method(name), /HasPermission\(context,\s*PermissionFlags/);
  }
});

test('Web-Saison und Vereinskonfiguration getrennt', () => {
  assert.match(capabilities, /canManageClubConfiguration\(context: WebCapabilityContext, hasClubConfigurationRight = false\)/);
  assert.match(page, /canManageClubConfiguration\(capabilityContext, hasRight\("verein\.konfiguration"\)\)/);
  assert.match(page, /canManageSeasonConfiguration: canManageClubSeasonConfiguration\(capabilityContext\)/);
  assert.match(compatibility, /Permission\.manageClubSeasonConfiguration, \["saison\.verwalten"\]/);
  assert.doesNotMatch(capabilities, /return canManageClubSeasonConfiguration\(context\);/);
});

test('Web-/MAUI-Navigation zeigt nicht bloß Adminrollen', () => {
  assert.match(page, /canEdit=\{hasRight\("termin\.verwalten"\)\}/);
  assert.match(page, /canEdit=\{hasRight\("bekanntmachung\.verwalten"\)\}/);
  assert.match(shell, /CanManageClubConfiguration\(managementContext\)/);
  assert.match(shell, /CanManageClubConfiguration\(context\)/);
  assert.match(read('KGV.Maui/Pages/VereinskonfigurationPage.cs'), /PermissionChecks\.CanManageClubConfiguration/);
});

test('RLS und ACL schließen Lösch- und TRUNCATE-Umgehungen', () => {
  assert.match(sql, /REVOKE TRUNCATE, TRIGGER, REFERENCES ON TABLE/);
  assert.match(sql, /REVOKE DELETE ON TABLE/);
  assert.match(sql, /DROP POLICY IF EXISTS vereinskonfiguration_delete_admin/);
  assert.match(sql, /fr210_vf_verwendung_delete/);
  assert.match(sql, /fr210_saison_insert/);
  assert.match(sql, /fr210_saison_update/);
  assert.match(sql, /fr210_guard_saison_history/);
  assert.match(sql, /trg_fr210_vf_besetzung_block_delete/);
});

test('Bisherige Leser-Policies bleiben erhalten', () => {
  assert.doesNotMatch(sql, /DROP POLICY IF EXISTS termin_select_visible_authenticated/);
  assert.doesNotMatch(sql, /DROP POLICY IF EXISTS bekanntmachung_select_visible_authenticated/);
  assert.doesNotMatch(sql, /DROP POLICY IF EXISTS saison_select_authenticated/);
  assert.doesNotMatch(sql, /DROP POLICY IF EXISTS vereinsfunktion_impressum_select_authenticated/);
});
