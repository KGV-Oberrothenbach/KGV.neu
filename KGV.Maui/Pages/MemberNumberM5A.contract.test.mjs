import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const read = (path) => readFile(new URL(path, import.meta.url), "utf8");

test("member search labels use membership numbers and retain technical selection ids", async () => {
  const source = await read("../ViewModels/MemberSearchViewModel.cs");

  assert.match(source, /FormatMemberReferenceLabel\(name, member\.Mitgliedsnummer\)/);
  assert.match(source, /\?\s*\$"Mgl\.-Nr\. \{membershipNumber\}"/);
  assert.match(source, /\$"\{name\} · Mgl\.-Nr\. \{membershipNumber\}"/);
  assert.match(source, /Subtitle:\s*member\.Email|member\.Email \?\? string\.Empty/);
  assert.match(source, /new MemberSearchResultItem\(\s*member\.Id,/);
  assert.match(source, /if \(result\.MemberId\.HasValue\)\s*return await ResolveMemberAsync\(result\.MemberId\.Value\)/);
  assert.match(source, /Mitgliedsnummer = member\.Mitgliedsnummer/);
  assert.doesNotMatch(source, /Mitglied #|Mitglied \{member\.Id\}|Mitglied \{id\}/);
});

test("work-assignment participant and picker labels use the existing bulk member projection", async () => {
  const [page, service] = await Promise.all([
    read("ArbeitseinsatzTeilnehmerPage.cs"),
    read("../../KGV.Infrastructure/Services/SupabaseService.cs"),
  ]);

  const projection = service.match(/GetArbeitseinsatzManagementParticipantsAsync\(int arbeitseinsatzId\)[\s\S]*?new List<WorkAssignmentManagementParticipantItem>\(\)\);/)?.[0];
  assert.ok(projection);
  assert.equal((projection.match(/GetMitgliederAsync\(\)/g) ?? []).length, 1);
  assert.match(projection, /DisplayName = membersById\.TryGetValue\(x\.MitgliedId, out var member\)\s*\?\s*FormatMemberReferenceLabel\(member\)\s*:\s*"Mitglied unbekannt"/);
  assert.match(page, /_memberPicker\.ItemsSource = _members\.Select\(FormatMemberReferenceLabel\)/);
  assert.match(page, /_members\[_memberPicker\.SelectedIndex\]\.Id/);
  assert.match(page, /ActionButton\([^;]+item\.MitgliedId/);
  for (const status of ["angemeldet", "teilgenommen", "nicht_erschienen", "abgesagt"]) {
    assert.ok(page.includes(`"${status}"`));
  }
  assert.doesNotMatch(projection, /GetMitgliedByIdAsync|Mitglied #\{x\.MitgliedId\}/);
});

test("home work-assignment participant details use membership labels from one bulk lookup", async () => {
  const [page, service] = await Promise.all([
    read("HomeSectionDetailPage.cs"),
    read("../../KGV.Infrastructure/Services/SupabaseService.cs"),
  ]);

  const projection = service.match(/GetArbeitseinsatzParticipantsAsync\(int arbeitseinsatzId\)[\s\S]*?new List<WorkAssignmentParticipantItem>\(\)\);/)?.[0];
  assert.ok(projection);
  assert.equal((projection.match(/GetMitgliederAsync\(\)/g) ?? []).length, 1);
  assert.match(projection, /DisplayName = memberById\.TryGetValue\(x\.MitgliedId, out var member\)\s*\?\s*FormatMemberReferenceLabel\(member\)\s*:\s*"Mitglied unbekannt"/);
  assert.match(projection, /MitgliedId = x\.MitgliedId/);
  assert.doesNotMatch(projection, /GetMitgliedByIdAsync|Mitglied #\{x\.MitgliedId\}/);
  assert.match(page, /GetArbeitseinsatzParticipantsAsync\(arbeitseinsatzId\)/);
  assert.match(page, /nameof\(WorkAssignmentParticipantItem\.DisplayName\)/);
});

test("club-function assignments and representations use member labels without changing ids", async () => {
  const source = await read("VereinsfunktionenPage.cs");

  assert.match(source, /\? \$"Mgl\.-Nr\. \{member\.Mitgliedsnummer\}"/);
  assert.match(source, /\$"\{name\} · Mgl\.-Nr\. \{member\.Mitgliedsnummer\}"/);
  assert.match(source, /record\.MitgliedId = selected\.Id/);
  assert.match(source, /new Choice\(x\.Id, MemberName\(x\.Id\)\)/);
  assert.match(source, /AssignmentName\(x\.VertreteneBesetzungId\).*AssignmentName\(x\.VertreterBesetzungId\)/);
  assert.match(source, /record\.VertreteneBesetzungId = one\.Id;\s*record\.VertreterBesetzungId = two\.Id/);
  assert.match(source, /SaveVereinsfunktionBesetzungAsync|SaveVereinsfunktionVertretungAsync/);
});

test("communication member references use numbers while signatory text remains function-oriented", async () => {
  const source = await read("KommunikationPage.cs");

  assert.match(source, /\$"Mgl\.-Nr\. \{member\.Mitgliedsnummer\}"/);
  assert.match(source, /\$"\{name\} · Mgl\.-Nr\. \{member\.Mitgliedsnummer\}"/);
  assert.match(source, /MemberName\(contract\.MitgliedId, members\)/);
  assert.match(source, /option\.Anzeigename\} – \{option\.VereinsfunktionBezeichnung/);
  assert.match(source, /signatory\?\.MitgliedId \?\? position\.MitgliedId/);
});

test("individual invoice targets display the member number and persist the selected member id", async () => {
  const source = await read("JahresabschlussRechnungEditorPage.cs");

  assert.match(source, /Label = FormatMemberReferenceLabel\(member\)/);
  assert.match(source, /_ziel\.Text = \$"Ziel: \{FormatMemberReferenceLabel\(selected\)\}"/);
  assert.match(source, /MitgliedId = _zielMitglied\?\.Id/);
  assert.match(source, /_zielMitglied = selected/);
  assert.match(source, /DisplayActionSheetAsync\("Mitglied auswählen"/);
});

test("member-document send confirmation uses the canonical member label", async () => {
  const source = await read("MemberDetailPage.cs");

  assert.match(source, /Mitglied: \{FormatMemberDisplayName\(_memberRecord\)\}\\nE-Mail:/);
  assert.match(source, /private static string FormatMemberDisplayName\(MitgliedRecord\? member\)/);
  assert.match(source, /if \(member\.Mitgliedsnummer <= 0\)\s*return "Mitgliedsnummer nicht verfügbar"/);
  assert.match(source, /SendMitgliedVereinsunterlagenDigitalAsync\(mitgliedId,/);
});

test("public imprint contact and function display is not changed to member labels", async () => {
  const source = await read("ImpressumPage.cs");

  assert.match(source, /x\.DisplayName/);
  assert.match(source, /Funktion/);
  assert.doesNotMatch(source, /Mgl\.-Nr\./);
});
