import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const migration = new URL("../../supabase/migrations/20261023140000_vd3_contract_dispatch.sql", import.meta.url);
const edge = new URL("../../supabase/functions/kgv-send-contract-documents/index.ts", import.meta.url);
const panel = new URL("../features/communication/ContractDispatchPanel.tsx", import.meta.url);
const repository = new URL("../repositories/contracts/contract-dispatch-repository.ts", import.meta.url);

test("contract dispatch distinguishes frozen digital and paper attachments", async () => {
  const source = await readFile(migration, "utf8");
  for (const expected of ["versandinstanz", "versandinstanz_empfaenger_snapshot", "versandinstanz_dokument_snapshot", "email_info_einwilligung", "email_rechnung_einwilligung", "vertragsversand", "wartungsvertrag_verlaengerung", "satzung", "kleingartenordnung", "beitragsordnung", "arbeitsordnung", "digital_gebunden", "papier_ausgehaendigt", "weder digital gebunden noch als Papierübergabe dokumentiert", "bereitstellungsart'='papier'"]) assert.match(source, new RegExp(expected));
});
test("edge function sends only digital snapshots and never fabricates a paper PDF", async () => {
  const source = await readFile(edge, "utf8");
  for (const expected of ["claim_contract_dispatch", "versandinstanz_versuch", "SMTP_TEST_RECIPIENT", "GOOGLE_DRIVE_REFRESH_TOKEN", "teilweise_fehlerhaft", "stableMessageId", "idempotenzschluessel"]) assert.ok(source.includes(expected));
  assert.match(source, /eq\("bindungsstatus",\s*"digital_gebunden"\)/);
  assert.doesNotMatch(source, /papier.*placeholder|placeholder.*papier/i);
});
test("redispatch is a separate successful-dispatch path with new current recipients", async () => {
  const source = await readFile(migration, "utf8");
  for (const expected of ["create_contract_redispatch", "z.status='versendet'", "erneuter_versand_von_id", "from public.mitglied m", "email_info_einwilligung", "email_rechnung_einwilligung", "e.status in ('bereit','fehler')", "insert into public.versandinstanz_dokument_snapshot select x"]) assert.ok(source.includes(expected));
});
test("web dispatch UI is server-driven, displays paper evidence and keeps retry distinct", async () => {
  const [ui, repo] = await Promise.all([readFile(panel, "utf8"), readFile(repository, "utf8")]);
  for (const expected of ["Unterlagen per E-Mail versenden", "E-Mail senden", "Fehlgeschlagene E-Mail erneut senden", "Unterlagen erneut senden", "bereits in Papierform ausgehändigt", "Mailanhang", "Mailtext anzeigen", "window.confirm", "canDispatchContracts", "createContractRedispatch", "sendContractDispatch"]) assert.ok(ui.includes(expected));
  for (const expected of ["kann_vertragsversand_ausfuehren", "prepare_contract_dispatch", "create_contract_redispatch", "kgv-send-contract-documents", "versandinstanz_empfaenger_snapshot", "versandinstanz_dokument_snapshot"]) assert.ok(repo.includes(expected));
  assert.doesNotMatch(ui, /input[^>]+(?:email|empfänger)|freie Empfängeradresse/i);
  assert.doesNotMatch(ui, /Nebenmitglied.*find|list.*Nebenmitglied/i);
});
