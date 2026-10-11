import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import ts from "typescript";

function compileModule(source, require) {
  const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText;
  const module = { exports: {} };
  new Function("exports", "module", "require", compiled)(module.exports, module, require);
  return module.exports;
}

const permissionsSource = await readFile(new URL("./permissions.ts", import.meta.url), "utf8");
const permissions = compileModule(permissionsSource, () => { throw new Error("permissions.ts has no runtime imports"); });
const capabilitiesSource = await readFile(new URL("./capabilities.ts", import.meta.url), "utf8");
const capabilities = compileModule(capabilitiesSource, (path) => path === "./permissions" ? permissions : undefined);

const { Permission } = permissions;
const {
  createWebCapabilityContext,
  canReadDocuments,
  canManageDocuments,
  canReadRoleManagement,
  canManageRoleManagement,
  canPerformYearEndClosing,
  canArchiveDocuments,
  canManageClubSeasonConfiguration,
  canConfigureUserMeterReadingSubmissions,
} = capabilities;

const admin = createWebCapabilityContext("admin");
const revokedAdmin = createWebCapabilityContext("admin", 0, Permission.CanPerformYearEndClosing | Permission.CanManageRoles);
const vorstand = createWebCapabilityContext("vorstand");
const grantedVorstand = createWebCapabilityContext("vorstand", Permission.CanPerformYearEndClosing);
const user = createWebCapabilityContext("user");
const documentReader = createWebCapabilityContext("user", Permission.CanReadDocuments);
const documentManager = createWebCapabilityContext("user", Permission.CanManageDocuments);
const memberEditor = createWebCapabilityContext("user", Permission.CanEditAllMembers);

assert.equal(canPerformYearEndClosing(admin), true);
assert.equal(canPerformYearEndClosing(revokedAdmin), false);
assert.equal(canPerformYearEndClosing(vorstand), false);
assert.equal(canPerformYearEndClosing(grantedVorstand), true);
assert.equal(canPerformYearEndClosing(user), false);
assert.equal(canReadDocuments(documentReader), true);
assert.equal(canManageDocuments(documentReader), false);
assert.equal(canReadDocuments(documentManager), true);
assert.equal(canManageDocuments(documentManager), true);
assert.equal(canManageDocuments(memberEditor), false);
assert.equal(canArchiveDocuments(admin), true);
assert.equal(canArchiveDocuments(vorstand), false);
assert.equal(canReadRoleManagement(vorstand), true);
assert.equal(canManageRoleManagement(vorstand), false);
assert.equal(canManageRoleManagement(admin), true);
assert.equal(canManageRoleManagement(revokedAdmin), false);
assert.equal(canManageClubSeasonConfiguration(admin), true);
assert.equal(canManageClubSeasonConfiguration(vorstand), false);
assert.equal(canConfigureUserMeterReadingSubmissions(admin), true);
assert.equal(canConfigureUserMeterReadingSubmissions(vorstand), true);
assert.equal(canConfigureUserMeterReadingSubmissions(user), false);

for (const check of [canReadDocuments, canManageDocuments, canReadRoleManagement, canManageRoleManagement, canPerformYearEndClosing, canArchiveDocuments, canManageClubSeasonConfiguration, canConfigureUserMeterReadingSubmissions]) {
  assert.equal(check(null), false, "missing capability context must fail closed");
}

assert.match(capabilitiesSource, /from "\.\/permissions"/);
assert.doesNotMatch(capabilitiesSource, /8380407|AdminPermissions|VorstandPermissions|UserPermissions/);
