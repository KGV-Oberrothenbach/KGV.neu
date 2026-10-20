import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
const root = new URL("../../", import.meta.url);
const [navigation, page, service, repository, feature] = await Promise.all([
  readFile(new URL("services/workspace/navigation-service.ts", root), "utf8"),
  readFile(new URL("app/page.tsx", root), "utf8"),
  readFile(new URL("services/communication/communication-service.ts", root), "utf8"),
  readFile(new URL("repositories/communication/communication-repository.ts", root), "utf8"),
  readFile(new URL("features/communication/CommunicationCenter.tsx", root), "utf8"),
]);
assert.match(navigation, /canAccessCommunication/);
assert.match(navigation, /openProcessCount/);
assert.match(navigation, /groups\.communication/);
assert.match(page, /context\.role === "admin" \|\| context\.role === "vorstand"/);
assert.match(page, /refreshOpenProcessCount/);
assert.match(page, /isOpenCommunicationWorkItem/);
assert.doesNotMatch(page, /context\.role === "user"[^\n]*CommunicationCenter/);
assert.match(service, /vertrag_signatur[\s\S]*entwurf,vorbereitet,zur_signatur/);
assert.match(service, /vorgangstyp: "eq\.mail"[\s\S]*mail_bereit,wird_versendet,fehler/);
assert.match(service, /vorgangstyp: "eq\.druck"[\s\S]*druck_bereit,fehler/);
assert.match(service, /status: "in\.\(erledigt,abgebrochen,verworfen\)"/);
assert.match(service, /isOpenCommunicationWorkItem/);
assert.match(service, /isHistoryCommunicationItem/);
assert.match(service, /zugewiesen_an: `eq\.\$\{userId\}`/);
assert.match(service, /zugewiesen_an: "is\.null"/);
assert.match(repository, /zugewiesen_an: "is\.null"/);
assert.match(repository, /zugewiesen_an: `eq\.\$\{userId\}`/);
assert.match(repository, /zugewiesen_an: null/);
assert.match(service, /wartungsvertrag_verlaengerung/);
assert.match(feature, /fehlertext/);
assert.match(feature, /Druck erledigt/);
assert.match(feature, /showAllHistory/);
assert.match(repository, /gedruckt_am/);
assert.match(repository, /status: "eq\.druck_bereit"/);
assert.doesNotMatch(feature, /createSignedUrl|publicUrl|storage\.from/);
