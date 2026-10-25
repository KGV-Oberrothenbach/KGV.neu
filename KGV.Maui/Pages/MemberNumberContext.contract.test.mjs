import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("M1 preserves the canonical membership number through the member context", async () => {
  const [dto, context, adminMenu, userManagement] = await Promise.all([
    readFile(new URL("../../KGV.Core/Models/MemberDTO.cs", import.meta.url), "utf8"),
    readFile(new URL("../State/MemberContextState.cs", import.meta.url), "utf8"),
    readFile(new URL("./AdminMenuPage.cs", import.meta.url), "utf8"),
    readFile(new URL("../ViewModels/UserManagementViewModel.cs", import.meta.url), "utf8"),
  ]);

  assert.match(dto, /public long Mitgliedsnummer/);
  assert.match(dto, /Mitgliedsnummer = other\.Mitgliedsnummer/);
  assert.match(dto, /Mitgliedsnummer == other\.Mitgliedsnummer/);
  assert.match(context, /SelectedMember = member\?\.Clone\(\)/);
  assert.match(adminMenu, /Mgl\.-Nr\. \{member\.Mitgliedsnummer\}/);
  assert.doesNotMatch(adminMenu, /\(ID: \{selectedMember\.Id\}\)/);
  assert.match(userManagement, /Mitgliedsnummer = rec\.Mitgliedsnummer/);
  assert.doesNotMatch(userManagement, /Mitglied #\{BoundMember\.Id\}/);
});

test("M1 resolves app users from one member list without exposing technical IDs", async () => {
  const [viewModel, page] = await Promise.all([
    readFile(new URL("../ViewModels/UserManagementViewModel.cs", import.meta.url), "utf8"),
    readFile(new URL("./UserManagementPage.cs", import.meta.url), "utf8"),
  ]);

  assert.match(viewModel, /var membersTask = _supabaseService\.GetMitgliederAsync\(\)/);
  assert.match(viewModel, /await Task\.WhenAll\(usersTask, membersTask\)/);
  assert.match(viewModel, /_membersById\[member\.Id\] = member/);
  assert.match(viewModel, /SelectedUserMemberDisplay/);
  assert.doesNotMatch(viewModel, /GetMitgliedByIdAsync\(user\.MitgliedId/);
  assert.match(page, /nameof\(UserManagementViewModel\.SelectedUserMemberDisplay\)/);
  assert.doesNotMatch(page, /"SelectedUser\.MitgliedId"/);
});
