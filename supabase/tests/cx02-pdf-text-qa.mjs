import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const [textPath, resultPath] = process.argv.slice(2);
if (!textPath || !resultPath) throw new Error("Usage: node cx02-pdf-text-qa.mjs <pdftotext.txt> <cx02-results.json>");

const text = (await readFile(textPath, "utf8")).replace(/\s+/g, " ").trim();
const result = JSON.parse(await readFile(resultPath, "utf8"));
const required = [
  `Vertrags-Nr. ${result.contract_number}`,
  "Tessa CX01 Pächter",
  "Bisheriger Vertragsstand: Fläche 100 m²; Strom nein; Wasser nein.",
  "Vertragsänderung (verbindlich): Fläche 120 m²; Strom ja; Wasser nein.",
  "Begründung: Anpassung der Gartenfläche und Aufnahme des Stromanschlusses",
  "CX01 Vorstand Eins",
  "CX01 Vorstand Zwei",
];

for (const expected of required) assert.ok(text.includes(expected), `PDF text is missing: ${expected}`);
assert.equal(result.signature_count, 3);
console.log(`ok - extracted PDF text contains all ${required.length} required facts and the report confirms three signatures`);
