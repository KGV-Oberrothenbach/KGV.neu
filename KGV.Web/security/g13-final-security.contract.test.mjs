import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const [flags, web, sql] = await Promise.all([
  readFile(new URL("../../KGV.Core/Security/PermissionFlags.cs", import.meta.url), "utf8"),
  readFile(new URL("./permissions.ts", import.meta.url), "utf8"),
  readFile(new URL("../../supabase/migrations/20261011130000_g13_permission_contract.sql", import.meta.url), "utf8"),
]);
for (let bit = 0; bit <= 24; bit += 1) assert.match(flags, new RegExp(`1 << ${bit}`));
assert.match(flags, /CanPerformYearEndClosing\s*=\s*1 << 23/);
assert.match(sql, /when 'admin' then 16769015::bigint/);
assert.match(sql, /when 'vorstand' then 7855607::bigint/);
assert.match(sql, /else 10::bigint/);
assert.match(sql, /select 16777215::bigint/);
const bits = Object.fromEntries([...web.matchAll(/^\s*(Can\w+):\s*1 << (\d+),/gm)].map(([, name, bit]) => [name, 1 << Number(bit)]));
assert.equal(bits.CanPerformYearEndClosing, 8388608);
assert.equal(bits.CanRegisterHistoricalContracts, 16777216);
assert.equal(Object.values(bits).reduce((mask, bit) => mask | bit, 0), 33554431);
assert.match(web, /const UserPermissions = Permission\.CanViewMembers \| Permission\.CanSeeOwnDataOnly/);
assert.match(web, /const AdminPermissions = VorstandPermissions \| Permission\.CanManageRoles \| Permission\.CanCreateMitglied \| Permission\.CanPerformYearEndClosing/);

const mauiAuthorizationFiles = [
  "../../KGV.Maui/Pages/ExportPage.cs",
  "../../KGV.Maui/Pages/WartungsvertraegePage.cs",
  "../../KGV.Maui/Pages/MemberWartungsvertraegePage.cs",
  "../../KGV.Maui/Pages/WartungsvertragDetailPage.cs",
  "../../KGV.Maui/Pages/WartungsvertragAssignMembersPage.cs",
  "../../KGV.Maui/Pages/WartungsvertragEditorPage.cs",
];
for (const path of mauiAuthorizationFiles) {
  const source = await readFile(new URL(path, import.meta.url), "utf8");
  assert.doesNotMatch(source, /\.Role\s+is\s+.*UserRole\.(Admin|Vorstand)/);
  assert.match(source, /PermissionChecks\.(CanSearchMembers|CanEditAllMembers)/);
}
