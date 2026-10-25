import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";

const read = (path) => readFile(new URL(path, import.meta.url), "utf8");
const [rpc, web, webRepo, maui, webEditor, webService, mauiEditor] = await Promise.all([
  read("../../supabase/migrations/20261023170000_wv6_echte_verlaengerung.sql"),
  read("../features/maintenance/MaintenanceContinuationPanel.tsx"),
  read("../repositories/maintenance/maintenance-continuation-repository.ts"),
  read("../../KGV.Maui/Pages/WartungsvertragFortfuehrungPage.cs"),
  read("../features/maintenance/MaintenanceContractEditor.tsx"),
  read("../services/maintenance/maintenance-contract-service.ts"),
  read("../../KGV.Maui/Pages/WartungsvertragEditorPage.cs"),
]);

test("nur dieselbe WV-ID kann serverseitig verlängert werden", () => {
  assert.match(rpc, /drop function if exists public\.verlaengere_wartungsvertrag\(bigint,bigint,date,date,text\)/);
  assert.doesNotMatch(rpc, /p_wartungsvertrag_id/);
  assert.match(rpc, /values\(v_alt\.wartungsvertrag_id,v_alt\.hauptmitglied_id,v_alt\.vertragspartei_mitglied_id/);
  assert.match(rpc, /v_neu\.wartungsvertrag_id=v_alt\.wartungsvertrag_id/);
  assert.match(rpc, /'wartungsvertrag_verlaengerung'/);
});

test("Web und MAUI bieten bei einer Verlängerung keinen Vertragswechsel", () => {
  assert.doesNotMatch(web, /Ziel-Wartungsvertrag<select/);
  assert.doesNotMatch(web, /targetId/);
  assert.doesNotMatch(webRepo, /p_wartungsvertrag_id/);
  assert.doesNotMatch(maui, /GetWartungsvertraegeOverviewAsync/);
  assert.doesNotMatch(maui, /Zielvertrag/);
});

test("grundlegende Änderung erzeugt eine neue Definition statt die alte zu überschreiben", () => {
  assert.match(webEditor, /grundlegende Vertragsänderungen/);
  assert.match(webEditor, /fundamentalChange/);
  assert.match(webService, /id === null \|\| fundamentalChange \? await createMaintenanceContract/);
  assert.match(mauiEditor, /grundlegende Vertragsänderungen/);
  assert.match(mauiEditor, /Ja – neue Vertragsfassung anlegen/);
  assert.match(mauiEditor, /CreateWartungsvertragAsync/);
});

test("neue WV-ID folgt dem Erstvertragsweg; historische Ketten bleiben unverändert", () => {
  assert.match(webService, /former definition remains intact/);
  assert.match(webEditor, /neue Vertrags-ID angelegt/);
  assert.match(rpc, /v_alt\.wartungsvertrag_id/);
  assert.match(rpc, /vertragspartei_mitglied_id/);
});
