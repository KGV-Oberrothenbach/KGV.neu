import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import ts from "typescript";

const source = await readFile(new URL("./permissions.ts", import.meta.url), "utf8");
const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText;
const permissionModule = { exports: {} };
new Function("exports", "module", compiled)(permissionModule.exports, permissionModule);

const { Permission, getRolePermissions, getEffectivePermissions, hasPermission, preserveUncontrolledPermissions } = permissionModule.exports;
const expectedBits = [
  ["CanSearchMembers", 0], ["CanViewMembers", 1], ["CanEditAllMembers", 2], ["CanSeeOwnDataOnly", 3],
  ["CanManageDocuments", 4], ["CanReadMeters", 5], ["CanManageMeterChanges", 6], ["CanApproveMeterReadings", 7],
  ["CanManageWorkHours", 8], ["CanManageRoles", 9], ["CanShowStammdaten", 10], ["CanReadStammdaten", 11],
  ["CanWriteStammdaten", 12], ["CanShowParzellen", 13], ["CanReadParzellen", 14], ["CanWriteParzellen", 15],
  ["CanReadDocuments", 16], ["CanReadWorkHours", 17], ["CanReadRoles", 18], ["CanCreateMitglied", 19],
  ["CanManageWorkAssignments", 20], ["CanManageAppointments", 21], ["CanManageAnnouncements", 22], ["CanPerformYearEndClosing", 23],
];

for (const [name, bit] of expectedBits) assert.equal(Permission[name], 1 << bit, `${name} must match Core bit ${bit}`);
assert.equal(Permission.CanPerformYearEndClosing, 8388608);
assert.equal(getRolePermissions("user"), 10);
assert.equal(getRolePermissions("vorstand"), 7855607);
assert.equal(getRolePermissions("admin"), 16769015);
assert.equal(hasPermission(getEffectivePermissions("vorstand", Permission.CanPerformYearEndClosing, 0), Permission.CanPerformYearEndClosing), true);
assert.equal(hasPermission(getEffectivePermissions("admin", 0, Permission.CanPerformYearEndClosing), Permission.CanPerformYearEndClosing), false);
assert.equal(hasPermission(getEffectivePermissions("user", Permission.CanPerformYearEndClosing, Permission.CanPerformYearEndClosing), Permission.CanPerformYearEndClosing), false);

const unknownFutureBit = 1 << 24;
const preservedGrants = preserveUncontrolledPermissions(unknownFutureBit, Permission.CanManageRoles);
const preservedRevocations = preserveUncontrolledPermissions(unknownFutureBit, Permission.CanManageRoles);
assert.equal(preservedGrants, unknownFutureBit | Permission.CanManageRoles, "unknown grant bits must survive an editor save");
assert.equal(preservedRevocations, unknownFutureBit | Permission.CanManageRoles, "unknown revocation bits must survive an editor save");
assert.equal(preserveUncontrolledPermissions(0, unknownFutureBit), 0, "a stale editor must not introduce unknown bits");

const pageSource = await readFile(new URL("../app/page.tsx", import.meta.url), "utf8");
assert.doesNotMatch(pageSource, /const Permission\s*=/);
assert.doesNotMatch(pageSource, /function rolePermissions\s*\(/);
assert.doesNotMatch(pageSource, /const (Vorstand|Admin|User)Permissions\s*=/);
