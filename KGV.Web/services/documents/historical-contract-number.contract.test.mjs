import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import test from "node:test";

const root = resolve(import.meta.dirname, "../../..");
const read = (path) => readFileSync(resolve(root, path), "utf8");

test("B2 reservation is source-locked, idempotent and rejects a wrong number type", () => {
  const sql = read("supabase/migrations/20261027120000_nb2b_b2_historische_vertragsnummern.sql");
  assert.match(sql, /pg_advisory_xact_lock\(hashtextextended/);
  assert.match(sql, /quelle_art = p_quelle_art[\s\S]*quelle_id = p_quelle_id/);
  assert.match(sql, /v_bestehende_art <> p_art/);
  assert.match(sql, /historischer_kombivertrag/);
  assert.match(sql, /between 80000 and 89999/);
});

test("B2 derives demo scope from the selected document member and keeps 80xxx metadata", () => {
  const sql = read("supabase/migrations/20261027120000_nb2b_b2_historische_vertragsnummern.sql");
  assert.match(sql, /from public\.mitglied where id = v_dokument\.mitglied_id/);
  assert.match(sql, /v_dokument\.mitglied_id is null/);
  assert.match(sql, /historische_vertragsnummer/);
  assert.match(sql, /'historisches_dokument', p_dokument_id/);
});

test("Web and MAUI use the shared explicit assignment endpoint", () => {
  const web = read("KGV.Web/features/documents/HistoricalDocumentUploadDialog.tsx");
  const maui = read("KGV.Maui/Pages/HistoricalDocumentImportPage.cs");
  const edge = read("supabase/functions/kgv-upload-document/index.ts");
  assert.match(web, /Nummer zuweisen/);
  assert.match(web, /reserveHistoricalContractNumber\(/);
  assert.match(maui, /ReserveHistorischeVertragsnummerAsync/);
  assert.match(edge, /reserve_historical_contract_number/);
  assert.match(edge, /stampHistoricalContractPdf/);
});

test("B2 explicitly leaves image scans unmodified and exposes their number in metadata", () => {
  const edge = read("supabase/functions/kgv-upload-document/index.ts");
  const web = read("KGV.Web/features/documents/HistoricalDocumentUploadDialog.tsx");
  assert.match(edge, /if \(isPdf && !reservation\.pdf_bereits_gestempelt\)/);
  assert.match(web, /Der Bildscan bleibt unverändert; Titel und Metadaten führen die Nummer/);
});
