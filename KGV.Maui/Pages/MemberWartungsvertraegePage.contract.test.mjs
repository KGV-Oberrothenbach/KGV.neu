import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("member page renders central time lists and contract work-hour effects", async () => {
  const [page, service, model] = await Promise.all([
    readFile(new URL("./MemberWartungsvertraegePage.cs", import.meta.url), "utf8"),
    readFile(new URL("../../KGV.Infrastructure/Services/SupabaseService.cs", import.meta.url), "utf8"),
    readFile(new URL("../../KGV.Core/Models/MemberWartungsvertragItem.cs", import.meta.url), "utf8"),
  ]);
  for (const status of ["Aktuell", "Zukünftig", "Historisch"])
    assert.match(page, new RegExp(`CreateAssignmentSection\\("${status}"`));
  assert.match(page, /GetWartungsvertraegeForMitgliedAsync\(selectedMember\.Id\)/);
  assert.match(page, /source\.ArbeitsstundenwirkungText/);
  assert.doesNotMatch(page, /ArbeitsstundenGutschrift\s*[>*+]|BefreitVonPflichtstunden\s*\?/);
  assert.doesNotMatch(page, /hauptmitglied_id|HauptmitgliedId/);
  assert.match(service, /WartungsvertragZeitmodell\.Klassifiziere\(zuordnung\.GueltigAb, zuordnung\.GueltigBis, stichtag\)/);
  assert.match(service, /bundle\.Assignments[\s\S]*?ResolveHomeMitgliedIdAsync\(mitgliedId\)/);
  assert.match(service, /OrderBy\(x => x\.Titel, StringComparer\.CurrentCultureIgnoreCase\)/);
  assert.match(service, /WartungsvertragZeitstatus\.Zukuenftig\)[\s\S]*?OrderBy\(x => x\.GueltigAb\)/);
  assert.match(service, /WartungsvertragZeitstatus\.Historisch\)[\s\S]*?OrderByDescending\(x => x\.GueltigBis\)/);
  assert.match(model, /WartungsvertragZeitmodell\.Bezeichnung\(Zeitstatus\)/);
  assert.match(model, /Gültig ab .* bis offen/);
});

test("member assignment completion refreshes lists and preserves W3 batch item results", async () => {
  const [page, coreResult, service] = await Promise.all([
    readFile(new URL("./MemberWartungsvertraegePage.cs", import.meta.url), "utf8"),
    readFile(new URL("../../KGV.Core/Models/WartungsvertragAssignmentSaveResult.cs", import.meta.url), "utf8"),
    readFile(new URL("../../KGV.Infrastructure/Services/SupabaseService.cs", import.meta.url), "utf8"),
  ]);
  assert.match(coreResult, /List<WartungsvertragAssignmentSaveItem> Items/);
  assert.match(page, /result\.Items\.Select\(x => x\.AnzeigeText\)/);
  assert.match(page, /if \(!result\.Success \|\| result\.FailedCount > 0\)[\s\S]*?_isBusy = false;[\s\S]*?await LoadAsync\(\)/);
  assert.match(service, /return CreateWartungsvertragAssignmentSaveResult\(items, contractIds\.Count\)/);
  assert.doesNotMatch(page, /HttpClient|RestRequest|Supabase\.Postgrest/);
});
