import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const read = (path) => readFile(new URL(path, import.meta.url), "utf8");

test("meter-reading approval projects canonical membership numbers without per-reading lookups", async () => {
  const [model, service, page] = await Promise.all([
    read("../../KGV.Core/Models/AblesungReviewItem.cs"),
    read("../../KGV.Infrastructure/Services/SupabaseService.cs"),
    read("AblesungenFreigabePage.cs"),
  ]);

  const projection = service.match(/GetOffeneAblesungenZurFreigabeAsync\(\)[\s\S]*?new List<AblesungReviewItem>\(\)\);/)?.[0];
  assert.ok(projection);
  assert.equal((projection.match(/GetMitgliederAsync\(\)/g) ?? []).length, 1);
  assert.doesNotMatch(projection, /GetMitgliedByIdAsync/);
  assert.match(projection, /MitgliedId = activeBelegung\?\.MitgliedId/);
  assert.match(projection, /Mitgliedsnummer = member\?\.Mitgliedsnummer \?\? 0/);

  assert.match(model, /public int\? MitgliedId/);
  assert.match(model, /public long Mitgliedsnummer/);
  assert.match(model, /\? \$"Mgl\.-Nr\. \{Mitgliedsnummer\}"/);
  assert.match(model, /\$"\{MitgliedName\} · Mgl\.-Nr\. \{Mitgliedsnummer\}"/);
  assert.doesNotMatch(model, /Mitglied \{MitgliedId\}|Id \+ 1000/);
  assert.match(page, /nameof\(AblesungReviewItem\.MitgliedDisplay\)/);
});

test("remaining member references preserve identifiers while rendering canonical labels", async () => {
  const [home, parcels, workHours, maintenance] = await Promise.all([
    read("../ViewModels/HomeViewModel.cs"),
    read("../ViewModels/ParzellenViewModel.cs"),
    read("ArbeitsstundenEditorPage.cs"),
    read("AuslaufendeWartungsvertraegePage.cs"),
  ]);

  for (const source of [home, parcels, workHours, maintenance]) {
    assert.match(source, /Mgl\.-Nr\. \{[^}]*Mitgliedsnummer\}/);
    assert.doesNotMatch(source, /Id \+ 1000/);
  }
  assert.match(home, /FormatMemberReference\(selectedMember\)/);
  assert.match(parcels, /MitgliedId = belegung\?\.MitgliedId/);
  assert.match(workHours, /new MemberOption\(nebenmitglied\.Id,/);
  assert.match(workHours, /entry\.Mitgliedsnummer/);
  assert.match(maintenance, /GetMitgliederAsync\(\)/);
  assert.match(maintenance, /TryGetValue\(item\.HauptmitgliedId, out var member\)/);
});
