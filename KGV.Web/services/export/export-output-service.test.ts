import assert from "node:assert/strict";
import test from "node:test";
import { createExportCsv } from "./export-csv-service";
import { defaultExportColumnKeys, selectedExportColumns } from "./export-output-service";
import { exportPdfDocumentDefinition } from "./export-pdf-service";

const columns = [{ export_key: "x", column_key: "hidden", label_kurz: "Versteckt", label_lang: null, sortierung: 20, standard_sichtbar: false, ist_sortierspalte: false }, { export_key: "x", column_key: "name", label_kurz: null, label_lang: "Name", sortierung: 10, standard_sichtbar: true, ist_sortierspalte: false }];
const result = { definition: { export_key: "x", titel: "Test", beschreibung: null, quelle_typ: "rpc", quelle_name: "x", aktiv: true, standard_sortierung: null, standard_ausgabe: "csv", erlaubt_csv: true, erlaubt_pdf: true }, columns, rows: Array.from({ length: 501 }, (_, index) => ({ name: index === 0 ? 'A; "B"' : null, hidden: index % 2 === 0 })), recordCount: 501, filters: {}, standardSortierung: null };

test("column selection starts with standard columns, restores metadata order and permits changes", () => {
  assert.deepEqual(defaultExportColumnKeys(columns), ["name"]);
  assert.deepEqual(selectedExportColumns(columns, ["hidden", "name"]).map((column) => column.column_key), ["name", "hidden"]);
});

test("CSV uses selected columns, BOM, CRLF, semicolon quoting and every result row", () => {
  const csv = createExportCsv(result, [columns[1]]);
  assert.ok(csv.startsWith("\ufeffName\r\n\"A; \"\"B\"\"\"\r\n"));
  assert.equal(csv.split("\r\n").length, 502);
  assert.equal(createExportCsv(result, [columns[0]]).split("\r\n")[1], "Ja");
});

test("CSV output service rejects definitions without CSV permission", () => {
  assert.throws(() => createExportCsv({ ...result, definition: { ...result.definition, erlaubt_csv: false } }, columns), /nicht erlaubt/);
});

test("PDF output uses the binding MAUI/Web layout definition", () => {
  assert.deepEqual(exportPdfDocumentDefinition, { page: "A4-landscape", margin: 28, clubFont: 12, metaFont: 8, titleFont: 14, headerFont: 8, cellFont: 8, accent: "#2e7d32" });
});
