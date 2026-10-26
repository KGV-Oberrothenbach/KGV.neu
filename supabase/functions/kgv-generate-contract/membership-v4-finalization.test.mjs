import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const source = await readFile(new URL("./index.ts", import.meta.url), "utf8");

test("membership application renderer accepts both v2 and v4 contracts", () => {
  assert.match(source, /membershipApplicationFieldSpecV2/);
  assert.match(source, /membershipApplicationFieldSpecV4/);
  assert.match(source, /expectedFieldSpecVersions: \[membershipApplicationFieldSpecV2, membershipApplicationFieldSpecV4, membershipApplicationFieldSpecV5\]/);
});

test("v4 maps every persistent signature key explicitly", () => {
  for (const key of ["member.application", "member.privacy", "gesetzliche_vertretung.antrag", "gesetzliche_vertretung.datenschutz", "nebenmitglied.antrag", "nebenmitglied.datenschutz"]) assert.match(source, new RegExp(`"${key}":`));
  for (const key of ["vorstand_1", "vorstand_2"]) assert.match(source, new RegExp(`\\b${key}:`));
  for (const field of ["unterschrift_antragsteller_antrag", "unterschrift_antragsteller_datenschutz", "unterschrift_zweite_partei_antrag", "unterschrift_zweite_partei_datenschutz", "unterschrift_vorstand_1", "unterschrift_vorstand_2"]) assert.match(source, new RegExp(`"${field}"`));
  assert.doesNotMatch(source, /sort_order[^\n]{0,80}membershipV4SignatureFields/);
});

test("v4 finalization uses completeness RPC, private storage and SHA-256", () => {
  assert.match(source, /prepared_contract_signaturen_vollstaendig/);
  assert.match(source, /prepared-contract-signatures/);
  assert.match(source, /auth\.admin\.storage\.from\(position\.storage_bucket!\)\.download/);
  assert.match(source, /Integritätsfehler: Die Signaturdatei/);
});

test("representative and secondary alternatives are complete and mutually exclusive", () => {
  assert.match(source, /hasRepresentative && hasSecondary/);
  assert.match(source, /Signaturen der gesetzlichen Vertretung sind unvollständig/);
  assert.match(source, /Signaturen des Nebenmitglieds sind unvollständig/);
});

test("board signatures and snapshots stay separate, including representation text", () => {
  assert.match(source, /board1Snapshot/);
  assert.match(source, /board2Snapshot/);
  assert.match(source, /vereinsunterzeichner_1 = persistent\.board1Snapshot/);
  assert.match(source, /vereinsunterzeichner_2 = persistent\.board2Snapshot/);
  assert.match(source, /in Vertretung für/);
});

test("v4 ignores request signature fields and preserves positions on failures", () => {
  const v4Match = /template\.fieldSpecVersion === membershipApplicationFieldSpecV4\) \{([\s\S]*?)\r?\n      \} else \{/.exec(source);
  assert.ok(v4Match);
  const v4Block = v4Match[1];
  assert.match(v4Block, /loadPersistentMembershipV4Signatures/);
  assert.doesNotMatch(v4Block, /body\.signature_/);
  assert.doesNotMatch(source, /prepared_contract_signatur"\)\.delete|prepared_contract_signatur"\)\.update/);
});
