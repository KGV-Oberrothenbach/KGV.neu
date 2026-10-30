import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const repository = await readFile(new URL("../../repositories/documents/historical-contract-registration-repository.ts", import.meta.url), "utf8");
const workflow = await readFile(new URL("../../features/documents/HistoricalContractRegistrationWorkflow.tsx", import.meta.url), "utf8");
const navigation = await readFile(new URL("../workspace/navigation-service.ts", import.meta.url), "utf8");

assert.match(repository, /register_historischen_pachtvertrag/);
assert.doesNotMatch(repository, /"register_historisches_dokument"/);
assert.match(repository, /p_pachtverhaeltnis_ids/);
assert.match(repository, /p_mitglied_seit_korrektur/);
assert.match(repository, /p_vertragspartei_mitglied_ids/);
assert.match(repository, /p_mitgliedschaftsenden/);
assert.match(repository, /p_aktiv_status_aenderungen/);
assert.match(workflow, /historischer_ma_pv_erstvertrag/);
assert.match(workflow, /historischer_pv_folgevertrag/);
assert.match(workflow, /Zweiter vorhandener Pächter/);
assert.match(workflow, /Pachtbeziehungen \/ Parzellen/);
assert.match(workflow, /Altvertrag übernehmen/);
assert.match(workflow, /reserveHistoricalContractNumber/);
assert.match(workflow, /partyMemberIds/);
assert.match(navigation, /canRegisterHistoricalContracts/);
