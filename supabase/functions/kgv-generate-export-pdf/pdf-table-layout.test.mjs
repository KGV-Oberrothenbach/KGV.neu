import assert from "node:assert/strict";
import test from "node:test";
import { paginateTableRows, wrapTableText } from "./pdf-table-layout.mjs";

const measure = (value) => value.length;

test("member address and contact blocks remain multiline and control-safe", () => {
  assert.deepEqual(wrapTableText("Müllerstraße 4\r\n12345 Röthenbach", measure, 80), ["Müllerstraße 4", "12345 Röthenbach"]);
  assert.deepEqual(wrapTableText("Tel.\t123\rMobil\u0000456", measure, 80), ["Tel. 123", "Mobil 456"]);
});

test("63 work-hour rows produce deterministic multiple table pages", () => {
  const pages = paginateTableRows(Array.from({ length: 63 }, () => 14), 140);
  assert.equal(pages.length, 7);
  assert.equal(pages.flat().length, 63);
});

test("a row that cannot fit on an empty page is rejected", () => {
  assert.throws(() => paginateTableRows([141], 140), /passt nicht/);
});
