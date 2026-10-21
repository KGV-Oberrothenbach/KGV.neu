import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const root = new URL("../../", import.meta.url);
const [service, repository, page] = await Promise.all([
  readFile(new URL("services/imprint/imprint-service.ts", root), "utf8"),
  readFile(new URL("repositories/imprint/imprint-repository.ts", root), "utf8"),
  readFile(new URL("features/imprint/ImprintPage.tsx", root), "utf8"),
]);

assert.match(service, /loadClubConfiguration/);
assert.match(repository, /impressum_kontakte/);
assert.match(service, /groupImprintContacts/);
assert.doesNotMatch(service, /isImprintChairSlot|isConstructionSlot|vorsitz|bauausschuss/i);
assert.doesNotMatch(repository, /impressum_funktion_slot|readSupabaseRest/);
assert.match(page, /loadImprintData/);
assert.match(page, /imprint\.responsible/);
assert.match(page, /imprint\.groups/);
assert.doesNotMatch(page, /Kleingartenverein Oberrothenbach/);
