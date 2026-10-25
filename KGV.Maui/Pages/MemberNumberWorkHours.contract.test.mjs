import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("M3 projects canonical membership numbers into both work-hour DTO paths", async () => {
  const [dto, service] = await Promise.all([
    readFile(new URL("../../KGV.Core/Models/ArbeitsstundeDTO.cs", import.meta.url), "utf8"),
    readFile(new URL("../../KGV.Infrastructure/Services/SupabaseService.cs", import.meta.url), "utf8"),
  ]);

  assert.match(dto, /public long Mitgliedsnummer/);
  assert.match(service, /Mitgliedsnummer = mitglied\?\.Mitgliedsnummer \?\? 0/);
  assert.equal((service.match(/Mitgliedsnummer = mitglied\?\.Mitgliedsnummer \?\? 0/g) ?? []).length, 2);
  assert.match(service, /GetArbeitsstundenAsync[\s\S]*?var mitglieder = await GetMitgliederAsync\(\)/);
  assert.match(service, /GetOffeneArbeitsstundenZurFreigabeAsync[\s\S]*?var membersResponse = await client\.From<MitgliedRecord>\(\)\.Get\(\)/);
});

test("M3 renders member numbers without changing technical work-hour actions", async () => {
  const [review, ownWorkHours] = await Promise.all([
    readFile(new URL("./ArbeitsstundenReviewDetailPage.cs", import.meta.url), "utf8"),
    readFile(new URL("./MyArbeitsstundenPage.cs", import.meta.url), "utf8"),
  ]);

  assert.match(review, /Mgl\.-Nr\. \{entry\.Mitgliedsnummer\}/);
  assert.doesNotMatch(review, /Mitglied \{entry\.MitgliedId\}/);
  assert.match(review, /Genehmiger-Kontext fehlt\./);
  assert.doesNotMatch(review, /Genehmiger-MitgliedId fehlt\./);
  assert.match(review, /ReviewArbeitsstundeAsync\(_reviewState\.CurrentEntry!\.Id, "freigeben", kommentar\)/);
  assert.match(review, /_userContextState\.CurrentMitgliedId/);
  assert.match(ownWorkHours, /Mitgliedskontext fehlt\./);
  assert.doesNotMatch(ownWorkHours, /MitgliedId fehlt\./);
  assert.match(ownWorkHours, /GetArbeitsstundenAsync\(ids\)/);
});
