import assert from "node:assert/strict";
import test from "node:test";
import { type ExportDefinition } from "../../models/export/export-definition";
import { type ExportFilter } from "../../models/export/export-filter";
import { buildExportRpcPayload, CURRENT_SEASON_DEFAULT, getExportDefaultValue, getExportOptionsSource, getExportParameterName, prepareExportDefinitions, sortExportColumns, sortExportFilters } from "./export-metadata-service";

const definition: ExportDefinition = { export_key: "arbeitsstunden_uebersicht", titel: "Arbeitsstunden", beschreibung: null, quelle_typ: "rpc", quelle_name: "rpc_export_arbeitsstunden_uebersicht", aktiv: true, standard_sortierung: "garten_nr", standard_ausgabe: "csv", erlaubt_csv: true, erlaubt_pdf: true };
const filter = (overrides: Partial<ExportFilter> = {}): ExportFilter => ({ export_key: definition.export_key, filter_key: "jahr", label: "Jahr", typ: "select", optionen_json: null, parameter_name: null, standardwert_json: null, pflicht: false, sortierung: 20, ...overrides });

test("metadata retains definition sorting and orders filters and columns", () => {
  assert.equal(prepareExportDefinitions([definition])[0].standard_sortierung, "garten_nr");
  assert.deepEqual(sortExportFilters([filter({ filter_key: "zweiter", sortierung: 20 }), filter({ filter_key: "erster", sortierung: 10 })]).map((item) => item.filter_key), ["erster", "zweiter"]);
  const columns = sortExportColumns([{ export_key: "x", column_key: "b", label_kurz: null, label_lang: null, sortierung: 20, standard_sichtbar: false, ist_sortierspalte: false }, { export_key: "x", column_key: "a", label_kurz: null, label_lang: null, sortierung: 10, standard_sichtbar: true, ist_sortierspalte: true }]);
  assert.deepEqual(columns.map((item) => item.column_key), ["a", "b"]);
  assert.equal(columns[0].standard_sichtbar, true);
  assert.equal(columns[0].ist_sortierspalte, true);
});

test("options support static JSON and recognised RPC sources", () => {
  assert.deepEqual(getExportOptionsSource([{ label: "Strom", value: "strom" }]), { kind: "static", options: [{ label: "Strom", value: "strom" }] });
  assert.deepEqual(getExportOptionsSource("rpc_export_arbeitsstunden_jahre"), { kind: "rpc", rpcName: "rpc_export_arbeitsstunden_jahre" });
});

test("parameter names and defaults are entirely metadata-driven", () => {
  assert.equal(getExportParameterName(filter({ parameter_name: "p_jahr" })), "p_jahr");
  assert.equal(getExportParameterName(filter({ filter_key: "p_medium" })), "p_medium");
  assert.equal(getExportDefaultValue(filter({ standardwert_json: CURRENT_SEASON_DEFAULT }), 2026), 2026);
  assert.equal(getExportDefaultValue(filter({ standardwert_json: false }), 2026), false);
  assert.equal(getExportDefaultValue(filter({ standardwert_json: "zusammenfassung" }), 2026), "zusammenfassung");
});

test("work-hour payload has no export-key special case", () => {
  const filters = [filter({ filter_key: "jahr", parameter_name: "p_jahr", standardwert_json: CURRENT_SEASON_DEFAULT }), filter({ filter_key: "stunden_offen", parameter_name: "p_stunden_offen", standardwert_json: false }), filter({ filter_key: "ansicht", parameter_name: "p_ansicht", standardwert_json: "zusammenfassung", pflicht: true })];
  assert.deepEqual(buildExportRpcPayload(filters, { jahr: "2026", stunden_offen: false, ansicht: "zusammenfassung" }), { p_jahr: 2026, p_stunden_offen: false, p_ansicht: "zusammenfassung" });
});
