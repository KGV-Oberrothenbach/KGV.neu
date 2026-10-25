import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const root = new URL("../../", import.meta.url);
const migration = await readFile(new URL("supabase/migrations/20261023130000_vf2_vereinsfunktionen_integrity.sql", root), "utf8");
const webPage = await readFile(new URL("../features/club-functions/ClubFunctionManagement.tsx", import.meta.url), "utf8");
const imprintPage = await readFile(new URL("../features/imprint/ImprintPage.tsx", import.meta.url), "utf8");
const webService = await readFile(new URL("../services/club-functions/club-function-service.ts", import.meta.url), "utf8");
const mauiImprintPage = await readFile(new URL("../../KGV.Maui/Pages/ImpressumPage.cs", import.meta.url), "utf8");

assert.match(migration, /unique \(vereinsfunktion_id, verwendungszweck\)/i);
assert.match(migration, /prevent_vereinsfunktion_besetzung_identity_change/);
assert.match(migration, /new\.mitglied_id is distinct from old\.mitglied_id/i);
assert.match(migration, /new\.vereinsfunktion_id is distinct from old\.vereinsfunktion_id/i);
assert.match(migration, /new\.gueltig_ab is distinct from old\.gueltig_ab/i);
assert.match(migration, /prevent_vereinsfunktion_schluessel_change/);
assert.match(migration, /prevent_legacy_impressum_funktion_slot_write/);
assert.match(webPage, /readOnly aria-readonly="true"/);
assert.match(webPage, /functionKey\(bezeichnung\)/);
for (const purpose of ["impressum", "vertragsunterzeichnung", "mitgliedsaufnahme_abstimmung", "mitgliedsaufnahme_entscheidung", "wartungsvertrag_entscheidung"]) assert.match(webService, new RegExp(`"${purpose}"`));
assert.match(imprintPage, /item\.functionName/);
assert.match(imprintPage, /item\.mobile && <a/);
assert.match(mauiImprintPage, /x\.Funktion/);
assert.match(mauiImprintPage, /x\.HasHandy/);
console.log("VF-2 club-function integrity contract passed");
