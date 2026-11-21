import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import ts from 'typescript';

const targetSource = await readFile(new URL('../services/workspace/dashboard-management-targets.ts', import.meta.url), 'utf8');
const transpiled = ts.transpileModule(targetSource, {
  compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 },
}).outputText;
const { getDashboardManagementTargets } = await import(`data:text/javascript;base64,${Buffer.from(transpiled).toString('base64')}`);
const ids = (access) => getDashboardManagementTargets(access).map(({ id }) => id);
const empty = { canManageWorkAssignments: false, canManageAppointments: false, canManageAnnouncements: false };

test('Dashboard-only management targets: no entitlement yields no route', () => {
  assert.deepEqual(ids(empty), []);
});

test('Dashboard-only management targets: granular entitlements restrict routes', () => {
  assert.deepEqual(ids({ ...empty, canManageWorkAssignments: true }), ['arbeitseinsaetze']);
  assert.deepEqual(ids({ ...empty, canManageAppointments: true }), ['termine']);
  assert.deepEqual(ids({ ...empty, canManageAnnouncements: true }), ['bekanntmachungen']);
});

test('Dashboard-only management targets: administrator can open all three pages', () => {
  const routes = getDashboardManagementTargets({
    canManageWorkAssignments: true, canManageAppointments: true, canManageAnnouncements: true,
  });
  assert.deepEqual(routes.map(({ id }) => id), ['arbeitseinsaetze', 'termine', 'bekanntmachungen']);
  for (const route of routes) {
    assert.ok(route.label && route.detail, `${route.id} must provide a display heading`);
  }
});

test('Workspace resolves dashboard targets in addition to sidebar items and gates them by effective rights', async () => {
  const page = await readFile(new URL('../app/page.tsx', import.meta.url), 'utf8');
  assert.match(page, /getDashboardManagementTargets\(\{\s*canManageWorkAssignments:\s*has\(Permission\.manageWorkAssignments\),\s*canManageAppointments:\s*has\(Permission\.manageAppointments\),\s*canManageAnnouncements:\s*has\(Permission\.manageAnnouncements\)/);
  assert.match(page, /const permittedTargets = \[\.\.\.allNavigationItems, \.\.\.dashboardManagementTargets\]/);
  assert.match(page, /new Set\(permittedTargets\.map\(\(item\) => item\.id\)\)/);
  assert.match(page, /const active = permittedTargets\.find\(\(item\) => item\.id === effectiveActiveId\)/);
  for (const id of ['arbeitseinsaetze', 'termine', 'bekanntmachungen']) {
    assert.match(page, new RegExp(`effectiveActiveId === "${id}" && <`));
  }
});

test('Home page offers dashboard-only links only with corresponding capabilities', async () => {
  const homepage = await readFile(new URL('../features/home/HomeDashboard.tsx', import.meta.url), 'utf8');
  for (const [right, destination] of [
    ['canManageWorkAssignments', 'arbeitseinsaetze'],
    ['canManageAppointments', 'termine'],
    ['canManageAnnouncements', 'bekanntmachungen'],
  ]) {
    assert.match(homepage, new RegExp(`${right} && <button[^>]+onClick=\\{\\(\\) => onNavigate\\("${destination}"\\)\\}`));
  }
});
