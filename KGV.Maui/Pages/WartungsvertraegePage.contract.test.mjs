import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

test("W2 MAUI overview and detail use the shared current-state contract model", async () => {
  const [overview, detail, model, service] = await Promise.all([
    readFile(new URL("./WartungsvertraegePage.cs", import.meta.url), "utf8"),
    readFile(new URL("./WartungsvertragDetailPage.cs", import.meta.url), "utf8"),
    readFile(new URL("../../KGV.Core/Models/WartungsvertragOverviewItem.cs", import.meta.url), "utf8"),
    readFile(new URL("../../KGV.Infrastructure/Services/SupabaseService.cs", import.meta.url), "utf8"),
  ]);

  assert.match(overview, /GetWartungsvertraegeOverviewAsync/);
  assert.match(overview, /ArbeitsstundenwirkungText/);
  assert.match(overview, /AktivText/);
  assert.match(detail, /GetWartungsvertragDetailAsync/);
  for (const label of ["Bereich", "Arbeitsstundenwirkung", "Status", "Bemerkung"]) assert.ok(detail.includes(`CreateInfoSection("${label}"`));
  assert.match(detail, /Text = "Aktive Zuordnungen"/);
  assert.match(model, /ArbeitsstundenwirkungText/);
  assert.match(service, /IsWartungsvertragZuordnungAktivOn\(x, activeDate\)/);
  assert.doesNotMatch(overview, /validFrom|GueltigAb|future|history/i);
});

test("W2 MAUI editor persists all maintenance contract fields without local effect duplication", async () => {
  const [editor, mapping, service] = await Promise.all([
    readFile(new URL("./WartungsvertragEditorPage.cs", import.meta.url), "utf8"),
    readFile(new URL("../../KGV.Core/Models/InsertRecordMappingExtensions.cs", import.meta.url), "utf8"),
    readFile(new URL("../../KGV.Infrastructure/Services/SupabaseService.cs", import.meta.url), "utf8"),
  ]);

  for (const field of ["Bereich", "Bemerkung", "BefreitVonPflichtstunden", "ArbeitsstundenGutschrift"]) assert.match(editor, new RegExp(field));
  assert.match(mapping, /ArbeitsstundenGutschrift = record\.ArbeitsstundenGutschrift/);
  assert.match(service, /\.Set\(x => x\.Bereich/);
  assert.match(service, /\.Set\(x => x\.ArbeitsstundenGutschrift/);
});
