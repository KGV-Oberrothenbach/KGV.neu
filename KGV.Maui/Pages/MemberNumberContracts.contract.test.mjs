import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const pageFiles = [
  "MemberGardenAssignPage.cs",
  "MemberGardensPage.cs",
  "MemberParzellenDetailPage.cs",
  "PachtvertragDialogPage.cs",
  "WartungsvertragDetailPage.cs",
  "MemberWartungsvertraegePage.cs",
  "WartungsvertragAssignMembersPage.cs",
];

test("M4 displays canonical membership numbers across parcel and contract pages", async () => {
  const sources = await Promise.all([
    ...pageFiles.map(file => readFile(new URL(`./${file}`, import.meta.url), "utf8")),
    readFile(new URL("../../KGV.Infrastructure/Services/SupabaseService.cs", import.meta.url), "utf8"),
  ]);
  const [assign, gardens, parcelDetail, lease, maintenanceDetail, maintenance, assignment, service] = sources;

  for (const source of sources) {
    assert.doesNotMatch(source, /Mitglied #/);
    assert.doesNotMatch(source, /· #\{/);
  }
  assert.match(assign, /Parzelle zuordnen für \{BuildMemberDisplayName\(_memberRecord\)\}/);
  assert.match(gardens, /Gärten von \{BuildMemberDisplayName\(member\)\}/);
  assert.match(gardens, /Mitgliedsnummer = rec\.Mitgliedsnummer/);
  assert.doesNotMatch(parcelDetail, /Mitglieds-ID/);
  assert.match(lease, /Mgl\.-Nr\. \{member\.Mitgliedsnummer\}/);
  assert.match(maintenanceDetail, /nameof\(WartungsvertragAssignedMemberItem\.DisplayName\)/);
  assert.match(maintenance, /Wartungsverträge von \{displayName\}/);
  assert.match(maintenance, /Mgl\.-Nr\. \{member\.Mitgliedsnummer\}/);
  assert.match(assignment, /Mgl\.-Nr\. \{member\.Mitgliedsnummer\}/);
  assert.match(service, /private static string BuildWartungsvertragMemberDisplayName\(MitgliedRecord\? member\)\s*\{\s*return member == null \? "Mitglied unbekannt" : FormatMemberReferenceLabel\(member\);/);
});

test("M4 keeps technical identifiers for contracts and selection values", async () => {
  const [assign, parcelDetail, lease, assignment] = await Promise.all([
    readFile(new URL("./MemberGardenAssignPage.cs", import.meta.url), "utf8"),
    readFile(new URL("./MemberParzellenDetailPage.cs", import.meta.url), "utf8"),
    readFile(new URL("./PachtvertragDialogPage.cs", import.meta.url), "utf8"),
    readFile(new URL("./WartungsvertragAssignMembersPage.cs", import.meta.url), "utf8"),
  ]);

  assert.match(assign, /RunAsync\([\s\S]*?mitgliedId, parzelleId, vertragsbeginn\)/);
  assert.match(parcelDetail, /ContextMitgliedId is not > 0/);
  assert.match(lease, /MitgliedId = _member\.Id/);
  assert.match(lease, /SecondaryMemberId = .*_nebenmitglied\.Id/);
  assert.match(assignment, /public int MitgliedId \{ get; \}/);
});
