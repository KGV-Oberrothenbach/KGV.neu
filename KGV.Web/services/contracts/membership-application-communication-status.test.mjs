import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import ts from "typescript";

async function loadStatus() {
  const source = await readFile(new URL("./membership-application-communication-status.ts", import.meta.url), "utf8");
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 } });
  return import(`data:text/javascript;base64,${Buffer.from(outputText).toString("base64")}`);
}

test("active membership application statuses are displayed while completed statuses are not active", async () => {
  const { membershipApplicationCommunicationLabel } = await loadStatus();
  assert.equal(membershipApplicationCommunicationLabel("vorbereitet"), "Mitgliedsantrag: Zur Signatur vorbereitet");
  assert.equal(membershipApplicationCommunicationLabel("zur_signatur"), "Mitgliedsantrag: Signatur läuft");
  assert.equal(membershipApplicationCommunicationLabel("letzte_pruefung"), "Mitgliedsantrag: Letzte Prüfung");
  assert.equal(membershipApplicationCommunicationLabel("erledigt"), null);
  assert.equal(membershipApplicationCommunicationLabel("abgebrochen"), null);
});

test("member master data loads active server contracts and opens communication without starting signatures", async () => {
  const source = await readFile(new URL("../../features/contracts/MembershipApplicationAction.tsx", import.meta.url), "utf8");
  assert.match(source, /findActivePreparedContract/);
  assert.match(source, /Mitgliedsantrag erstellen/);
  assert.match(source, /In Kommunikation öffnen/);
  assert.match(source, /kgv:open-communication/);
  const openCommunication = source.match(/const openCommunication[\s\S]*?;/)?.[0] ?? "";
  assert.doesNotMatch(openCommunication, /startPreparedContractSigning|Signatur/);
});
