import assert from "node:assert/strict";
import { emptyClubConfiguration } from "../../models/configuration/club-configuration";
import { isValidBic, isValidIban, normalizeAndValidateClubConfiguration, normalizeBic, normalizeIban } from "./club-configuration-service";

function validDraft() {
  return { ...emptyClubConfiguration(), vereinsname: "KGV Beispiel", registerangabe: "VR 123", strasse: "Gartenweg 1", plz: "12345", ort: "Beispielstadt", standard_email: "vorstand@example.org", kontoinhaber: "KGV Beispiel", bankname: "Beispielbank", iban: "DE89 3704 0044 0532 0130 00", bic: "COBADEFFXXX", dokument_ort: "Beispielstadt" };
}

assert.equal(isValidIban("DE89370400440532013000"), true);
assert.equal(isValidIban("DE89370400440532013001"), false);
assert.equal(normalizeIban("de89 3704 0044 0532 0130 00"), "DE89370400440532013000");
assert.equal(isValidBic("COBADEFFXXX"), true);
assert.equal(isValidBic("INVALID"), false);
assert.equal(normalizeBic(" coba deff xxx "), "COBADEFFXXX");

const requiredCases: Array<[keyof ReturnType<typeof validDraft>, string]> = [["vereinsname", "Vereinsname"], ["registerangabe", "Registerangabe"], ["strasse", "Straße"], ["plz", "PLZ"], ["ort", "Ort"], ["standard_email", "Standard-E-Mail"], ["kontoinhaber", "Kontoinhaber"], ["bankname", "Bankname"], ["dokument_ort", "Dokumentort"]];
for (const [field, label] of requiredCases) assert.equal(normalizeAndValidateClubConfiguration({ ...validDraft(), [field]: "" }).error, `${label} ist erforderlich.`);
assert.equal(normalizeAndValidateClubConfiguration({ ...validDraft(), iban: "" }).error, "IBAN ist erforderlich.");
assert.equal(normalizeAndValidateClubConfiguration({ ...validDraft(), bic: "" }).error, "BIC ist erforderlich.");
assert.equal(normalizeAndValidateClubConfiguration({ ...validDraft(), datenschutz_stand: "" }).payload?.datenschutz_stand, null);
assert.equal(normalizeAndValidateClubConfiguration({ ...validDraft(), datenschutz_stand: "2026-10-12" }).payload?.datenschutz_stand, "2026-10-12");
