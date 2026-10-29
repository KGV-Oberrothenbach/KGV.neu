import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import test from "node:test";
import { isolateExportRows } from "./export-data-contract.ts";

const hashFile = (path) => createHash("sha256").update(readFileSync(new URL(path, import.meta.url))).digest("hex");

test("export-allgemein v2 replaces the meter report embedded in v1", () => {
  const oldTemplateHash = hashFile("../../../KGV.Core/Templates/export-allgemein/v1/template.pdf");
  const newTemplateHash = hashFile("../../../KGV.Core/Templates/export-allgemein/v2/template.pdf");
  const migration = readFileSync(new URL("../../../supabase/migrations/20261023123000_publish_export_allgemein_v2.sql", import.meta.url), "utf8");

  assert.notEqual(newTemplateHash, oldTemplateHash);
  assert.match(migration, new RegExp(newTemplateHash));
  assert.match(migration, /set aktiv = false\s+where template_key = 'export-allgemein'/);
  assert.match(migration, /'export-allgemein', 'v2'/);
});

const workHoursRow = (extra = {}) => ({
  mitglied_id: 12, mitgliedsnummer: 1012, garten_nr: "019", nachname: "Müller", vorname: "Erika", jahr: 2026,
  pflichtstunden_soll: 9, geleistete_stunden: 3, wartungsvertrag_gutschrift_stunden: 0, offene_stunden: 6, status: "Stunden offen",
  ...extra,
});

test("Arbeitsstunden uses its source contract while projecting additional RPC fields away", () => {
  const rows = isolateExportRows(
    "arbeitsstunden_uebersicht",
    "rpc_export_arbeitsstunden_uebersicht_json",
    [{ column_key: "garten_nr" }, { column_key: "pflichtstunden_soll" }],
    [workHoursRow({ technische_diagnose: "ignored" })],
  );

  assert.deepEqual(rows, [{ garten_nr: "019", pflichtstunden_soll: 9 }]);
  assert.throws(
    () => isolateExportRows(
      "arbeitsstunden_uebersicht",
      "rpc_export_zaehler_eichfaelligkeit_json",
      [{ column_key: "garten_nr" }],
      [],
    ),
    /Exportquelle passt nicht/,
  );
  assert.throws(
    () => isolateExportRows(
      "arbeitsstunden_uebersicht",
      "rpc_export_arbeitsstunden_uebersicht_json",
      [{ column_key: "zaehlernummer" }],
      [],
    ),
    /Spalten passen nicht/,
  );
  assert.throws(
    () => isolateExportRows(
      "arbeitsstunden_uebersicht",
      "rpc_export_arbeitsstunden_uebersicht_json",
      [{ column_key: "garten_nr" }],
      [{ garten_nr: "019" }],
    ),
    /erforderliche Felder nicht/,
  );
});

test("member list accepts the current 13 visible metadata columns and projects internal fields away", () => {
  const currentVisibleColumns = ["mitgliedsnr", "mitglied", "adresse_block", "kontakt_block", "email", "geburtsdatum", "mitglied_seit", "gartennummern", "wa", "re", "info", "app", "aktiv_export"];
  const rows = isolateExportRows("mitgliederliste", "rpc_export_mitgliederliste_json", currentVisibleColumns.map((column_key) => ({ column_key })), [{ mitgliedsnummer: "1001", mitgliedsnr: "1001", mitglied: "Müller, Erika", adresse_block: "Straße 1\n12345 Ort", kontakt_block: "Tel\nMobil", email: "e@example.test", geburtsdatum: "1980-01-01", mitglied_seit: "2020-01-01", gartennummern: "1", wa: true, re: false, info: true, app: true, aktiv_export: true, internal_only: true }]);
  assert.deepEqual(Object.keys(rows[0]), currentVisibleColumns);
  assert.throws(() => isolateExportRows("mitgliederliste", "rpc_export_arbeitsstunden_uebersicht_json", [{ column_key: "mitglied" }], []), /Exportquelle passt nicht/);
});

test("Zähler Eichfälligkeit remains bound to its own source and fields", () => {
  const rows = isolateExportRows(
    "zaehler_eichfaelligkeit",
    "rpc_export_zaehler_eichfaelligkeit_json",
    [{ column_key: "parzelle" }, { column_key: "eichdatum" }],
    [{ parzelle: "Garten 019", eichdatum: "2026-10-22", zaehler_id: 21 }],
  );

  assert.deepEqual(rows, [{ parzelle: "Garten 019", eichdatum: "2026-10-22" }]);
});

test("other exports also pass only selected columns into the PDF model", () => {
  const rows = isolateExportRows(
    "another_export",
    "rpc_export_another_export_json",
    [{ column_key: "name" }],
    [{ name: "Beispiel", zaehlernummer: "Z-19" }],
  );

  assert.deepEqual(rows, [{ name: "Beispiel" }]);
});
