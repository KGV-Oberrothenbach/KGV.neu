import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const page = await readFile(new URL("./VereinsfunktionenPage.cs", import.meta.url), "utf8");
const service = await readFile(new URL("../../KGV.Infrastructure/Services/SupabaseService.cs", import.meta.url), "utf8");
assert.match(page, /UserRole\.Admin or UserRole\.Vorstand/);
assert.match(page, /SaveVereinsfunktionBesetzungAsync/);
assert.match(page, /SaveVereinsfunktionVertretungAsync/);
assert.match(page, /SaveVereinsfunktionVerwendungAsync/);
assert.match(page, /DeleteVereinsfunktionVerwendungAsync/);
assert.match(page, /Verwendungszweck entfernen/);
assert.match(page, /IsRepresentationForSelected/);
assert.match(page, /eigene Besetzung/);
assert.match(page, /Vertretungszeitraum/);
assert.doesNotMatch(page, /Vorsitz|Bauausschuss|app_user\.role/);
for (const purpose of ["impressum", "vertragsunterzeichnung", "mitgliedsaufnahme_abstimmung", "mitgliedsaufnahme_entscheidung", "wartungsvertrag_entscheidung"]) assert.match(page, new RegExp(`"${purpose}"`));
assert.match(service, /GetVereinsfunktionBesetzungenAsync/);
console.log("MAUI club-function management contract passed");
