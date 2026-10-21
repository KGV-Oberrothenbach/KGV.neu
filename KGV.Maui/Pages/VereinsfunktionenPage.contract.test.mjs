import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const page = await readFile(new URL("./VereinsfunktionenPage.cs", import.meta.url), "utf8");
const service = await readFile(new URL("../../KGV.Infrastructure/Services/SupabaseService.cs", import.meta.url), "utf8");
assert.match(page, /UserRole\.Admin or UserRole\.Vorstand/);
assert.match(page, /SaveVereinsfunktionBesetzungAsync/);
assert.match(page, /SaveVereinsfunktionVertretungAsync/);
assert.match(page, /SaveVereinsfunktionVerwendungAsync/);
assert.match(page, /eigene Besetzung/);
assert.match(page, /Vertretungszeitraum/);
assert.doesNotMatch(page, /Vorsitz|Bauausschuss|app_user\.role/);
assert.match(service, /GetVereinsfunktionBesetzungenAsync/);
console.log("MAUI club-function management contract passed");
