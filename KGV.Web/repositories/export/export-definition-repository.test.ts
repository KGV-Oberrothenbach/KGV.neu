import assert from "node:assert/strict";
import test from "node:test";
import { withOptionalColumnSerialLetterMetadata, withOptionalDefinitionSerialLetterMetadata } from "./export-serial-letter-metadata.ts";

const definitionCore: any = [{ export_key: "zaehler_eichfaelligkeit", titel: "Zähler", beschreibung: null, quelle_typ: "rpc", quelle_name: "rpc_export", aktiv: true, standard_sortierung: null, standard_ausgabe: "csv", erlaubt_csv: true, erlaubt_pdf: true }];
const columnCore: any = [{ export_key: "zaehler_eichfaelligkeit", column_key: "zaehlernummer", label_kurz: "Nr.", label_lang: "Zählernummer", sortierung: 1, standard_sichtbar: true, ist_sortierspalte: false }];

test("keeps core export definitions usable when optional serial-letter metadata is unavailable", () => {
  const definitions = withOptionalDefinitionSerialLetterMetadata(definitionCore, null);
  assert.equal(definitions[0].export_key, "zaehler_eichfaelligkeit");
  assert.equal(definitions[0].erlaubt_csv, true);
  assert.equal(definitions[0].erlaubt_pdf, true);
  assert.equal(definitions[0].serienbrief_erlaubt, false);
  assert.equal(definitions[0].serienbrief_mitglied_id_feld, null);
});
test("retains serial-letter availability when optional definition metadata is present", () => {
  const definitions = withOptionalDefinitionSerialLetterMetadata(definitionCore, [{ export_key: "zaehler_eichfaelligkeit", serienbrief_erlaubt: true, serienbrief_mitglied_id_feld: "mitglied_id", serienbrief_parzelle_id_feld: "parzelle_id" }]);
  assert.deepEqual(definitions[0], { ...definitionCore[0], serienbrief_erlaubt: true, serienbrief_mitglied_id_feld: "mitglied_id", serienbrief_parzelle_id_feld: "parzelle_id" });
});

test("keeps normal columns when optional serial-letter fields are unavailable", () => {
  const columns = withOptionalColumnSerialLetterMetadata(columnCore, null);
  assert.equal(columns[0].column_key, "zaehlernummer");
  assert.equal(columns[0].standard_sichtbar, true);
  assert.equal(columns[0].serienbrief_freigegeben, false);
  assert.equal(columns[0].serienbrief_gruppe, null);
});

test("retains serial-letter column metadata when it is present", () => {
  const columns = withOptionalColumnSerialLetterMetadata(columnCore, [{ export_key: "zaehler_eichfaelligkeit", column_key: "zaehlernummer", serienbrief_freigegeben: true, serienbrief_gruppe: "Zähler" }]);
  assert.equal(columns[0].serienbrief_freigegeben, true);
  assert.equal(columns[0].serienbrief_gruppe, "Zähler");
});
