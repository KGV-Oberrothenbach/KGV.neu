import assert from "node:assert/strict";
import { communicationMemberLabel } from "./communication-service.ts";

const item = (mitglied) => ({ mitglied });

assert.equal(communicationMemberLabel(item({ id: 7, mitgliedsnummer: 1001, vorname: "Ada", name: "Lovelace" })), "Ada Lovelace · Mgl.-Nr. 1001");
assert.equal(communicationMemberLabel(item({ id: 7, mitgliedsnummer: 1002, vorname: null, name: null })), "Mgl.-Nr. 1002");
assert.doesNotMatch(communicationMemberLabel(item({ id: 7, mitgliedsnummer: 1002, vorname: null, name: null })), /Mitglied #|#7/);
console.log("communication service member labels passed");
