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
