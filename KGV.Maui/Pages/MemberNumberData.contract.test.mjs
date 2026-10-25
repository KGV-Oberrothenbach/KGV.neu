import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("M2 renders membership references from their own canonical membership numbers", async () => {
  const [detail, profile, secondary] = await Promise.all([
    readFile(new URL("./MemberDetailPage.cs", import.meta.url), "utf8"),
    readFile(new URL("./MeineDatenPage.xaml.cs", import.meta.url), "utf8"),
    readFile(new URL("./NebenmitgliedPage.cs", import.meta.url), "utf8"),
  ]);

  for (const source of [detail, profile, secondary]) {
    assert.match(source, /Mgl\.-Nr\. \{member\.Mitgliedsnummer\}/);
    assert.doesNotMatch(source, /Mitglied #/);
  }
  assert.match(detail, /var hauptmitgliedName = FormatMemberDisplayName\(hauptmitglied\)/);
  assert.match(detail, /var nebenmitgliedName = FormatMemberDisplayName\(nebenmitglied\)/);
  assert.match(detail, /Mitgliedsnummer = rec\.Mitgliedsnummer/);
  assert.match(profile, /Mitgliedsnummer = rec\.Mitgliedsnummer/);
  assert.match(secondary, /Nebenmitglied für \{BuildMemberDisplayName\(hauptmitglied\)\}/);
});

test("M2 uses numbers only for existing member records and retains technical action IDs", async () => {
  const [application, detail] = await Promise.all([
    readFile(new URL("./MitgliedsantragDialogPage.cs", import.meta.url), "utf8"),
    readFile(new URL("./MemberDetailPage.cs", import.meta.url), "utf8"),
  ]);

  assert.match(application, /DisplayName = FormatMemberDisplayName\(member\)/);
  assert.match(application, /private static string FormatMemberDisplayName\(MitgliedRecord member\)/);
  assert.match(application, /if \(member\.Mitgliedsnummer > 0\)/);
  assert.doesNotMatch(application, /Mitglied #/);
  assert.match(application, /VertreterMitgliedId = _member\.Id/);
  assert.match(detail, /PromoteNebenmitgliedToHauptmitgliedAsync\(nebenmitglied\.Id\)/);
  assert.match(detail, /MitgliedId = member\.Id/);
});
