import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const root = new URL("../../", import.meta.url);
const [service, repository, page] = await Promise.all([
  readFile(new URL("services/imprint/imprint-service.ts", root), "utf8"),
  readFile(new URL("repositories/imprint/imprint-repository.ts", root), "utf8"),
  readFile(new URL("features/imprint/ImprintPage.tsx", root), "utf8"),
]);

assert.match(service, /loadClubConfiguration/);
assert.match(service, /isImprintChairSlot/);
assert.match(service, /responsible \?\?= contact/);
assert.match(service, /session\.isDemoAccount \|\| !member\.is_demo/);
assert.doesNotMatch(service, /demo\|test\|play/);
assert.match(repository, /is_demo/);
assert.match(page, /loadImprintData/);
assert.match(page, /imprint\.responsible/);
assert.match(page, /imprint\.board/);
assert.match(page, /imprint\.construction/);
assert.doesNotMatch(page, /Kleingartenverein Oberrothenbach/);
