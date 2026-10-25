import assert from "node:assert/strict";
import { assignmentState, explainClubFunctionError, functionPurposes, memberLabel } from "./club-function-service.ts";

assert.deepEqual(functionPurposes, ["impressum", "vertragsunterzeichnung", "vertragsversand", "mitgliedsaufnahme_abstimmung", "mitgliedsaufnahme_entscheidung", "wartungsvertrag_entscheidung"]);
assert.equal(assignmentState({ gueltig_ab: "2026-01-01", gueltig_bis: null }, "2026-10-21"), "aktuell");
assert.equal(assignmentState({ gueltig_ab: "2025-01-01", gueltig_bis: "2025-12-31" }, "2026-10-21"), "historisch");
assert.equal(assignmentState({ gueltig_ab: "2027-01-01", gueltig_bis: null }, "2026-10-21"), "zukünftig");
assert.equal(memberLabel({ id: 7, mitgliedsnummer: 1001, vorname: "Ada", name: "Lovelace", email: "ada@example.test" }), "Ada Lovelace · Mgl.-Nr. 1001 · ada@example.test");
assert.equal(memberLabel({ id: 7, mitgliedsnummer: 1002, vorname: null, name: null, email: null }), "Mgl.-Nr. 1002");
assert.match(explainClubFunctionError(new Error("Eine Person kann nicht ihre eigene Besetzung vertreten")), /nicht ihre eigene Besetzung/);
assert.match(explainClubFunctionError(new Error("Der Vertretungszeitraum muss innerhalb beider zugrunde liegenden Besetzungszeiträume liegen")), /innerhalb beider/);
console.log("club-function service contract passed");
