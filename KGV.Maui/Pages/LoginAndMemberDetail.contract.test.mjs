import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const login = readFileSync(new URL("LoginPage.xaml.cs", import.meta.url), "utf8");
const member = readFileSync(new URL("MemberDetailPage.cs", import.meta.url), "utf8");

test("biometric login starts automatically once and leaves normal login available on cancel", () => {
  assert.match(login, /Loaded \+= async \(_, _\) => await StartBiometricLoginIfAvailableAsync\(\)/);
  assert.match(login, /_biometricAutoAttempted \|\| _biometricPromptStarted/);
  assert.match(login, /AuthenticateAsync\("Biometrische Anmeldung"\)/);
  assert.match(login, /_biometricSessionStore\.Clear\(\)/);
  assert.doesNotMatch(login, /Mit Biometrie anmelden/);
});

test("member master data is mapped before optional integrations", () => {
  assert.match(member, /var memberDto = MapMember\(member\);[\s\S]*?LoadOptionalMemberDetailsAsync\(member, memberDto\)/);
  assert.match(member, /Optionale Mitgliedsdetaildaten konnten nicht geladen werden/);
  assert.match(member, /catch \(Exception ex\)[\s\S]*?Die Stammdaten konnten nicht geladen werden/);
});
