import assert from "node:assert/strict";
import test from "node:test";
import { type ExportDefinition } from "../../models/export/export-definition";
import { type ExportFilter } from "../../models/export/export-filter";
import { executeExportWithDataAccess } from "./export-service";

const columns = [{ export_key: "x", column_key: "garten_nr", label_kurz: null, label_lang: null, sortierung: 10, standard_sichtbar: true, ist_sortierspalte: true }];
const filter = (overrides: Partial<ExportFilter> = {}): ExportFilter => ({ export_key: "x", filter_key: "jahr", label: "Jahr", typ: "select", optionen_json: null, parameter_name: "p_jahr", wert_typ: "integer", standardwert_json: null, pflicht: false, sortierung: 10, ...overrides });
const definition = (overrides: Partial<ExportDefinition> = {}): ExportDefinition => ({ export_key: "x", titel: "Test", beschreibung: null, quelle_typ: "rpc", quelle_name: "rpc_export_test", aktiv: true, standard_sortierung: "garten_nr", standard_ausgabe: "csv", erlaubt_csv: true, erlaubt_pdf: true, ...overrides });

test("executes all normal RPC export definitions through metadata without export-key branches", async () => {
  const called: string[] = [];
  const data = { executeRpcExport: async (source: string, parameters: Record<string, unknown>) => { called.push(`${source}:${parameters.p_jahr ?? ""}`); return [{ garten_nr: "10" }, { garten_nr: "2" }]; }, readTableOrViewExport: async () => [] };
  for (const source of ["rpc_export_arbeitsstunden_uebersicht", "rpc_export_jahresablesung_status", "rpc_export_datenschutz_aktive_mitglieder", "rpc_export_zaehler_eichfaelligkeit"]) await executeExportWithDataAccess(data, { definition: definition({ quelle_name: source }), filters: [filter()], columns, values: { jahr: "2026" } });
  assert.equal(called.length, 4);
  assert.equal(called[0], "rpc_export_arbeitsstunden_uebersicht:2026");
});

test("executes RFID as a metadata-driven view and keeps all result rows", async () => {
  const data = { executeRpcExport: async () => [], readTableOrViewExport: async (source: string) => { assert.equal(source, "v_export_rfid_status"); return Array.from({ length: 501 }, (_, index) => ({ garten_nr: String(index + 1) })); } };
  const result = await executeExportWithDataAccess(data, { definition: definition({ export_key: "rfid_status", quelle_typ: "view", quelle_name: "v_export_rfid_status" }), filters: [], columns, values: {} });
  assert.equal(result.recordCount, 501);
  assert.equal(result.rows.length, 501);
  assert.equal(result.standardSortierung, "garten_nr");
});

test("uses the server-side member list RPC and validates required typed filters", async () => {
  const data = { executeRpcExport: async (source: string) => { assert.equal(source, "rpc_export_mitgliederliste"); return [{ gaerten: "2, 10" }]; }, readTableOrViewExport: async () => [] };
  const result = await executeExportWithDataAccess(data, { definition: definition({ export_key: "mitgliederliste", quelle_name: "rpc_export_mitgliederliste", standard_sortierung: "name" }), filters: [], columns, values: {} });
  assert.equal(result.rows[0].gaerten, "2, 10");
  await assert.rejects(() => executeExportWithDataAccess(data, { definition: definition(), filters: [filter({ filter_key: "ansicht", label: "Ansicht", parameter_name: "p_ansicht", wert_typ: "string", pflicht: true })], columns, values: { ansicht: null } }), /Ansicht.*erforderlich/);
});

test("normalizes boolean, integer, string and null RPC values", async () => {
  let payload: Record<string, unknown> = {};
  const data = { executeRpcExport: async (_source: string, parameters: Record<string, unknown>) => { payload = parameters; return []; }, readTableOrViewExport: async () => [] };
  await executeExportWithDataAccess(data, { definition: definition(), filters: [filter({ filter_key: "jahr", parameter_name: "p_jahr", wert_typ: "integer" }), filter({ filter_key: "offen", parameter_name: "p_offen", wert_typ: "boolean" }), filter({ filter_key: "text", parameter_name: "p_text", wert_typ: "string" }), filter({ filter_key: "leer", parameter_name: "p_leer", wert_typ: "string" })], columns, values: { jahr: "2026", offen: "false", text: "abc", leer: "" } });
  assert.deepEqual(payload, { p_jahr: 2026, p_offen: false, p_text: "abc", p_leer: null });
});
