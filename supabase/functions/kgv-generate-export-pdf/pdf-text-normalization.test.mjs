import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { normalizePdfText } from "./pdf-text-normalization.mjs";

test("PDF text normalization preserves German printable text", () => {
  assert.equal(normalizePdfText("Müllerstraße ß", true), "Müllerstraße ß");
});

test("PDF text normalization keeps intentional multiline blocks as separate lines", () => {
  assert.equal(normalizePdfText("Musterstraße 1\r\n12345 Ort", true), "Musterstraße 1\n12345 Ort");
  assert.equal(normalizePdfText("Telefon: 1\rMobil: 2", true), "Telefon: 1\nMobil: 2");
  assert.equal(normalizePdfText("Hinweis\r\nzweite Zeile", true), "Hinweis\nzweite Zeile");
});

test("PDF text normalization makes single-line fields safe and removes other controls", () => {
  assert.equal(normalizePdfText("Erste\r\nzweite", false), "Erste zweite");
  assert.equal(normalizePdfText("\tA\u0000B\u000bC", true), " A B C");
  assert.equal(normalizePdfText("", true), "");
});

test("central renderer wraps multiline values and records a safe rendering context", () => {
  const source = readFileSync(new URL("./index.ts", import.meta.url), "utf8");
  assert.match(source, /wrapTableText\(value/);
  assert.match(source, /new PdfRenderError\(fieldName, rowIndex/);
  assert.match(source, /export_key: exportKey, field_name: error\.fieldName, row_index: error\.rowIndex, row_reference: error\.rowReference, exception_type: error\.causeType/);
  assert.match(source, /genericValue\(row\[column\.column_key\]\)/);
});
