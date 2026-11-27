import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { canUseExport, authorizedExportDefinitions, chooseAuthorizedExportKey } from "../services/export/export-access.ts";

const sql = readFileSync(new URL("../../supabase/migrations/20261124215220_fr211_export_security.sql", import.meta.url), "utf8");
const source = sql.split("AS $fr211_rights$")[1].split("$fr211_rights$;")[0];
const sourceRights = Object.fromEntries([...source.matchAll(/WHEN\s+((?:'[^']+'(?:,\s*)?)+)\s+THEN v_required := ARRAY\[([^\]]+)\]/g)].flatMap((match) =>
  [...match[1].matchAll(/'([^']+)'/g)].map((key) => [key[1], [...match[2].matchAll(/'([^']+)'/g)].map((right) => right[1])])));
const common = ["auswertung.lesen", "export.ausfuehren"];
const relevant = [...new Set([...common, ...Object.values(sourceRights).flat()])];
const rows = (granted) => relevant.map((code) => ({ code, erlaubt: granted.includes(code) }));
const definitions = ["arbeitsstunden_uebersicht", "datenschutz_aktive_mitglieder", "jahresablesung_status", "mitgliederliste", "parzellenliste", "zaehler_eichfaelligkeit", "jae_xlsx", "unknown"].map((export_key) => ({ export_key, aktiv: true }));

test("all 64 relevant rights combinations match FR211 source authorization", () => {
  assert.equal(Object.keys(sourceRights).length, 7);
  assert.equal(relevant.length, 6);
  for (let mask = 0; mask < 2 ** relevant.length; mask++) {
    const granted = relevant.filter((_, index) => mask & (1 << index));
    for (const [key, required] of Object.entries(sourceRights)) {
      assert.equal(canUseExport(key, rows(granted)), [...common, ...required].every((code) => granted.includes(code)), `${mask}/${key}`);
    }
    assert.equal(canUseExport("unknown", rows(granted)), false);
    assert.equal(canUseExport("__proto__", rows(granted)), false);
  }
});

test("D does not request the unauthorized default hours export", () => {
  const allowed = authorizedExportDefinitions(definitions, rows([...common, "mitglied.lesen", "parzelle.lesen", "zaehler.lesen"]));
  assert.deepEqual(allowed.map((d) => d.export_key), ["datenschutz_aktive_mitglieder", "jahresablesung_status", "mitgliederliste", "parzellenliste", "zaehler_eichfaelligkeit"]);
  assert.equal(chooseAuthorizedExportKey(allowed, ""), "datenschutz_aktive_mitglieder");
  assert.equal(chooseAuthorizedExportKey(allowed, "arbeitsstunden_uebersicht"), "datenschutz_aktive_mitglieder");
  assert.equal(chooseAuthorizedExportKey(allowed, "zaehler_eichfaelligkeit"), "zaehler_eichfaelligkeit");
});

test("full authorized catalog preserves ordering; inactive, unknown and dedicated JAE are excluded", () => {
  const input = [...definitions, { export_key: "mitgliederliste", aktiv: false }];
  assert.deepEqual(authorizedExportDefinitions(input, rows(relevant)).map((d) => d.export_key), definitions.slice(0, 6).map((d) => d.export_key));
});

test("missing common or source rights and unavailable context fail closed", () => {
  for (const rights of [null, [], rows(common), rows(relevant.filter((code) => code !== "export.ausfuehren"))]) {
    assert.deepEqual(authorizedExportDefinitions(definitions, rights), []);
  }
  assert.equal(chooseAuthorizedExportKey([], "mitgliederliste"), "");
});

test("component gates metadata and output by currently authorized selection", () => {
  const ui = readFileSync(new URL("../features/export/ExportCenter.tsx", import.meta.url), "utf8");
  assert.match(ui, /authorizedExportDefinitions\(catalog\.items, fachrechteV2\)/);
  assert.match(ui, /chooseAuthorizedExportKey\(definitions, selectedKey\)/);
  assert.match(ui, /if \(!canExport \|\| !activeKey\) return;[\s\S]*loadExportMetadata\(club, session, activeKey, season\)/);
  assert.doesNotMatch(ui, /loadExportMetadata\(club, session, selectedKey/);
  assert.match(ui, /if \(cancelled\) return;[\s\S]*setMetadataOwner\(owner\)/);
  assert.match(ui, /if \(!canExport \|\| !selected \|\| !metadataReady \|\| !metadataOwner\) return/);
  assert.match(ui, /runResult\.owner === metadataOwner/);
  assert.match(ui, /if \(!selected \|\| !metadataReady\) return;[\s\S]*generateExportPdf/);
  assert.match(ui, /disabled=\{busy \|\| !canExportJae\}/);
});
