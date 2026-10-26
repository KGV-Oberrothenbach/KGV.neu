import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const edgeSource = await readFile(new URL("./index.ts", import.meta.url), "utf8");
const localContractSource = await readFile(new URL("../../../KGV.Core/Utilities/MitgliedsvertragDokumentFactory.cs", import.meta.url), "utf8");
const signatureSource = await readFile(new URL("../../../KGV.Core/Utilities/SignedVertragsdokumentPdfBuilder.cs", import.meta.url), "utf8");

test("membership contracts render the stored membership number instead of the technical id", () => {
  assert.match(localContractSource, /Mitgliedsnummer: \{FormatMembershipNumber\(member\)\}/);
  assert.match(localContractSource, /member\.Mitgliedsnummer > 0/);
  assert.doesNotMatch(localContractSource, /Mitglied-ID:/);
  assert.doesNotMatch(localContractSource, /Hauptmitglied #\{member\.HauptmitgliedId\.Value\}/);
  assert.doesNotMatch(localContractSource, /member\.Id \+ 1000|1000 \+ member\.Id/);

  const edgeContract = /async function buildSimpleContract\([\s\S]*?\n\}/.exec(edgeSource)?.[0];
  assert.ok(edgeContract, "membership contract renderer is missing");
  assert.match(edgeContract, /member\.mitgliedsnummer/);
  assert.match(edgeContract, /Mitgliedsnummer: \$\{memberNumberDisplay\}/);
  assert.doesNotMatch(edgeContract, /Mitglied-ID:|member\.id/);
  assert.doesNotMatch(edgeContract, /\+ 1000|1000 \+/);
});

test("signature metadata keeps names and uses the membership number only as fallback", () => {
  assert.match(signatureSource, /: signerName;/);
  assert.match(signatureSource, /Mgl\.-Nr\. \{member\.Mitgliedsnummer\}/);
  assert.match(signatureSource, /: "Mitglied"/);
  assert.doesNotMatch(signatureSource, /Mitglied #\{member\.Id\}/);
  assert.doesNotMatch(signatureSource, /member\.Id \+ 1000|1000 \+ member\.Id/);

  const signaturePage = /async function appendLeaseSignaturePage\([\s\S]*?\n\}/.exec(edgeSource)?.[0];
  assert.ok(signaturePage, "lease signature page renderer is missing");
  assert.doesNotMatch(signaturePage, /#\$\{member\.id\}/);
});
