import assert from "node:assert/strict";
import test from "node:test";
import { createJaeWorkbook } from "./jae-xlsx-service";

const base = { jahr: 2026, parzelle_id: 12, parzelle: "12", mitglied_id: 7, pachtbezug: "Pächter Neu", zaehler_id: 3, zaehlernummer: "W-3", von_datum: "2026-01-01", bis_datum: "2026-12-31", start_ablesung_id: 1, end_ablesung_id: 2, startstand: 10, endstand: 16, verbrauch: 6, grund: "normal" as const, berechenbar: true, fehlercode: null, fehlertext: null };
test("JAE workbook preserves the legacy sheet contract and writes separate G12 segments", () => {
  const workbook = createJaeWorkbook({ year: 2026, parcelAreas: new Map([[12, 285.34]]), segments: [{ ...base, medium: "Wasser" }, { ...base, medium: "Strom", zaehler_id: 4, zaehlernummer: "S-4", verbrauch: 9 }, { ...base, medium: "Wasser", pachtbezug: "Leerstand", von_datum: "2026-06-01", berechenbar: false, verbrauch: null, fehlercode: "WECHSELABLESUNG_FEHLT", fehlertext: "Übergangsablesung fehlt." }] });
  const legacy = workbook.getWorksheet("Tabelle1")!; const extended = workbook.getWorksheet("JAE erweitert")!;
  assert.equal(legacy.getCell("B2").value, "Ablesedaten 2026"); assert.equal(legacy.getCell("B4").value, "Garten"); assert.equal(legacy.getCell("I4").value, "Gartengröße"); assert.equal(extended.rowCount, 7); assert.equal(extended.getCell("E4").value, "Leerstand"); assert.equal(extended.getCell("O4").value, "WECHSELABLESUNG_FEHLT"); assert.equal(extended.getCell("A6").value, "Wasser gesamt umlagefähig"); assert.equal(extended.getCell("B6").value, 6);
});
