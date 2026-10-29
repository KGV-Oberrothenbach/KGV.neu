import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const vm = readFileSync(new URL("../../../KGV.Maui/ViewModels/ExportViewModel.cs", import.meta.url), "utf8");
const page = readFileSync(new URL("../../../KGV.Maui/Pages/ExportPage.cs", import.meta.url), "utf8");
const migration = readFileSync(new URL("../../../supabase/migrations/20261029110000_align_rfid_pdf_capability.sql", import.meta.url), "utf8");

test("MAUI materializes export parameters entirely from metadata", () => {
  assert.match(vm, /GetDefaultFilterValue/);
  assert.match(vm, /\$currentSeason/);
  assert.match(vm, /using Newtonsoft\.Json\.Linq;/);
  assert.match(vm, /JTokenType\.Float => token\.ToObject<decimal>\(\)/);
  assert.match(vm, /type is "number" or "decimal" or "float"/);
  assert.match(vm, /CoerceFilterValue\(f, val\)/);
  assert.doesNotMatch(vm, /isArbeitsstundenUebersicht/);
  assert.doesNotMatch(vm, /GetRfidScanContextRecordsAsync/);
});

test("RPC errors win over an empty-result message and PDF visibility follows metadata", () => {
  assert.match(page, /if \(!string\.IsNullOrWhiteSpace\(_vm\.LastRpcError\)\)[\s\S]*?if \(_vm\.ProcessedResults\.Count == 0\)/);
  assert.match(page, /_exportPdfButton\.IsVisible = hasDef && def\.ErlaubtPdf/);
  assert.match(page, /_exportCsvButton\.IsVisible = hasDef && def\.ErlaubtCsv/);
  assert.match(migration, /erlaubt_pdf = false/);
});
