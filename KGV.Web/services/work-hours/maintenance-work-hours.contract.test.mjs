import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const migrationUrl = new URL("../../../supabase/migrations/20261010140000_g11_apply_maintenance_work_hours.sql", import.meta.url);

async function readWorkHoursSources() {
  const [
    migration,
    exportMigration,
    exportGateway,
    webRepository,
    webSummary,
    webHome,
    webMemberWorkHours,
    mauiService,
    mauiSummary,
    mauiMemberDetail,
    mauiExport,
    exportPdf,
  ] = await Promise.all([
    readFile(migrationUrl, "utf8"),
    readFile(new URL("../../../supabase/migrations/20261010141000_g11_fix_work_hours_export_views.sql", import.meta.url), "utf8"),
    readFile(new URL("../../../supabase/migrations/20261015210000_generic_export_gateway.sql", import.meta.url), "utf8"),
    readFile(new URL("../../repositories/work-hours/work-hours-repository.ts", import.meta.url), "utf8"),
    readFile(new URL("../../features/work-hours/RequiredHoursSummary.tsx", import.meta.url), "utf8"),
    readFile(new URL("../../features/home/HomeDashboard.tsx", import.meta.url), "utf8"),
    readFile(new URL("../../features/work-hours/MemberWorkHours.tsx", import.meta.url), "utf8"),
    readFile(new URL("../../../KGV.Infrastructure/Services/SupabaseService.cs", import.meta.url), "utf8"),
    readFile(new URL("../../../KGV.Maui/Pages/MyArbeitsstundenPage.cs", import.meta.url), "utf8"),
    readFile(new URL("../../../KGV.Maui/Pages/MeineDatenPage.xaml.cs", import.meta.url), "utf8"),
    readFile(new URL("../../../KGV.Maui/ViewModels/ExportViewModel.cs", import.meta.url), "utf8"),
    readFile(new URL("../../../KGV.Core/Utilities/ExportPdfBuilder.cs", import.meta.url), "utf8"),
  ]);
  const fnStart = migration.indexOf("CREATE FUNCTION public.fn_berechne_pflichtstunden_status");
  const fnEnd = migration.indexOf("\n$$;", fnStart);
  assert.ok(fnStart >= 0 && fnEnd > fnStart, "central SQL function should be present");
  return { functionSql: migration.slice(fnStart, fnEnd), migration, exportMigration, exportGateway, webRepository, webSummary, webHome, webMemberWorkHours, mauiService, mauiSummary, mauiMemberDetail, mauiExport, exportPdf };
}

function applyDatabaseContractEffect(contracts, requiredHours, seasonStart = "2026-01-01", seasonEnd = "2026-12-31") {
  const applicable = contracts.filter((contract) =>
    contract.active
    && contract.validFrom <= seasonEnd
    && (contract.validUntil === null || contract.validUntil >= seasonStart));
  const exempt = applicable.some((contract) => contract.exempts);
  const creditsByContractId = new Map(applicable.map((contract) => [contract.id, contract.credit]));
  const credit = exempt || requiredHours <= 0
    ? 0
    : Math.min(requiredHours, [...creditsByContractId.values()].reduce((sum, value) => sum + value, 0));
  return { hasContract: applicable.length > 0, exempt, requiredHours: exempt ? 0 : requiredHours, credit };
}

test("central SQL seasonal overlap includes the interval endpoints and rejects outside-season assignments", async () => {
  const { functionSql } = await readWorkHoursSources();
  assert.match(functionSql, /z\.gueltig_ab <= v_saison_ende and \(z\.gueltig_bis is null or z\.gueltig_bis >= v_saison_start\)/);
  assert.match(functionSql, /where z\.hauptmitglied_id = v_hauptmitglied_id and w\.aktiv = true/);

  const overlapsSeason = (from, until, seasonStart = "2026-01-01", seasonEnd = "2026-12-31") =>
    from <= seasonEnd && (until === null || until >= seasonStart);
  assert.equal(overlapsSeason("2026-03-01", null), true, "active assignment overlaps the season");
  assert.equal(overlapsSeason("2025-11-01", "2025-12-31"), false, "historical assignment outside the season is excluded");
  assert.equal(overlapsSeason("2027-01-01", null), false, "future assignment outside the season is excluded");
  assert.equal(overlapsSeason("2026-12-01", null), true, "the existing full-season model includes a future start within this season");
  assert.equal(overlapsSeason("2026-10-01", "2027-02-01"), true, "partial-season overlap counts for the full season");
  assert.equal(overlapsSeason("2025-12-31", "2026-01-01"), true, "season start boundary is inclusive");
  assert.equal(overlapsSeason("2026-12-31", "2026-12-31"), true, "season end boundary is inclusive");
  assert.equal(overlapsSeason("2025-12-30", "2025-12-31"), false, "an assignment ending before season start is excluded");
});

test("central SQL applies exemption and deduplicated bounded contract credits", async () => {
  const { functionSql } = await readWorkHoursSources();
  assert.match(functionSql, /v_hat_befreienden_wartungsvertrag boolean := false/);
  assert.match(functionSql, /w\.aktiv = true and w\.befreit_von_pflichtstunden = true/);
  assert.match(functionSql, /if v_hat_befreienden_wartungsvertrag then\s+v_pflichtstunden_soll := 0; v_ist_befreit := true; v_regelgrund := 'wartungsvertrag'/);
  assert.match(functionSql, /select distinct w\.id, w\.arbeitsstunden_gutschrift/);
  assert.match(functionSql, /if v_ist_befreit or v_pflichtstunden_soll <= 0 then v_wartungsvertrag_gutschrift_stunden := 0/);
  assert.match(functionSql, /least\(greatest\(v_wartungsvertrag_gutschrift_stunden, 0\), v_pflichtstunden_soll\)/);
  assert.match(functionSql, /greatest\(v_pflichtstunden_soll - v_geleistete_stunden - v_wartungsvertrag_gutschrift_stunden, 0\)/);

  const credit = (overrides = {}) => ({ id: 1, active: true, exempts: false, credit: 2, validFrom: "2026-02-01", validUntil: null, ...overrides });
  assert.deepEqual(applyDatabaseContractEffect([], 10), { hasContract: false, exempt: false, requiredHours: 10, credit: 0 });
  assert.deepEqual(applyDatabaseContractEffect([credit({ validFrom: "2027-01-01" })], 10), { hasContract: false, exempt: false, requiredHours: 10, credit: 0 });
  assert.deepEqual(applyDatabaseContractEffect([credit({ validFrom: "2026-12-01" })], 10), { hasContract: true, exempt: false, requiredHours: 10, credit: 2 });
  assert.deepEqual(applyDatabaseContractEffect([credit({ validFrom: "2025-01-01", validUntil: "2025-12-31" })], 10), { hasContract: false, exempt: false, requiredHours: 10, credit: 0 });
  assert.deepEqual(applyDatabaseContractEffect([credit()], 10), { hasContract: true, exempt: false, requiredHours: 10, credit: 2 });
  assert.deepEqual(applyDatabaseContractEffect([credit({ exempts: true })], 10), { hasContract: true, exempt: true, requiredHours: 0, credit: 0 });
  assert.deepEqual(applyDatabaseContractEffect([credit({ id: 11 }), credit({ id: 11 }), credit({ id: 12, credit: 3 })], 4), { hasContract: true, exempt: false, requiredHours: 4, credit: 4 });
  assert.deepEqual(applyDatabaseContractEffect([credit({ id: 11 }), credit({ id: 12, credit: 3 })], 10), { hasContract: true, exempt: false, requiredHours: 10, credit: 5 });
  assert.deepEqual(applyDatabaseContractEffect([credit({ validFrom: "2026-10-01", validUntil: "2027-02-01", credit: 4 })], 10), { hasContract: true, exempt: false, requiredHours: 10, credit: 4 });
  assert.deepEqual(applyDatabaseContractEffect([credit({ validFrom: "2026-01-01", validUntil: "2026-01-01" })], 10), { hasContract: true, exempt: false, requiredHours: 10, credit: 2 });
  assert.deepEqual(applyDatabaseContractEffect([credit({ validFrom: "2025-12-01", validUntil: "2025-12-31" })], 10), { hasContract: false, exempt: false, requiredHours: 10, credit: 0 });
});

test("Web and MAUI read and only display the central duty-hours view values", async () => {
  const { functionSql, migration, webRepository, webSummary, webHome, webMemberWorkHours, mauiService, mauiSummary, mauiMemberDetail } = await readWorkHoursSources();
  assert.match(migration, /CREATE VIEW public\.v_pflichtstunden_uebersicht AS/);
  assert.match(migration, /x\.wartungsvertrag_gutschrift_stunden/);
  assert.match(webRepository, /"v_pflichtstunden_uebersicht"/);
  assert.match(webRepository, /ist_befreit,hat_wartungsvertrag,wartungsvertrag_gutschrift_stunden/);
  assert.match(webSummary, /summary\?\.pflichtstunden_soll/);
  assert.match(webSummary, /summary\.ist_befreit/);
  assert.match(webSummary, /summary\.wartungsvertrag_gutschrift_stunden/);
  assert.match(webHome, /workHours\?\.pflichtstunden_soll/);
  assert.match(webMemberWorkHours, /getWorkHoursSummary|RequiredHoursSummary/);
  assert.match(mauiService, /MaintenanceWorkHoursCredit = record\.WartungsvertragGutschriftStunden/);
  assert.match(mauiService, /IsExempt = record\.IstBefreit/);
  assert.match(mauiSummary, /summary\?\.PflichtstundenSoll/);
  assert.match(mauiSummary, /summary\?\.IstBefreit == true \? "–" : FormatHours\(summary\?\.WartungsvertragGutschriftStunden\)/);
  assert.match(mauiMemberDetail, /summary\.IstBefreit/);
  assert.doesNotMatch(webSummary, /pflichtstunden_soll\s*=\s*0|wartungsvertrag_gutschrift_stunden\s*[=+*/-]/);
  assert.doesNotMatch(webHome, /pflichtstunden_soll\s*=\s*0|wartungsvertrag_gutschrift_stunden\s*[=+*/-]/);
  assert.doesNotMatch(mauiSummary, /PflichtstundenSoll\s*=\s*0|WartungsvertragGutschriftStunden\s*[=+*/-]/);
  assert.match(functionSql, /return query select v_hauptmitglied_id,[\s\S]*?v_pflichtstunden_soll,[\s\S]*?v_wartungsvertrag_gutschrift_stunden/);
});

test("work-hours exports reuse central view measures and do not calculate maintenance effects", async () => {
  const { exportMigration, exportGateway, mauiExport, exportPdf } = await readWorkHoursSources();
  assert.match(exportMigration, /from public\.v_pflichtstunden_uebersicht v/);
  assert.match(exportMigration, /coalesce\(v\.wartungsvertrag_gutschrift_stunden, 0\)/);
  assert.match(exportMigration, /coalesce\(v\.hat_wartungsvertrag, false\)/);
  assert.match(exportMigration, /coalesce\(v\.pflichtstunden_soll/);
  assert.match(exportMigration, /coalesce\(v\.offene_stunden, 0\)/);
  assert.match(exportGateway, /rpc_export_arbeitsstunden_uebersicht_json/);
  assert.match(exportGateway, /from public\.rpc_export_arbeitsstunden_uebersicht\(/);
  assert.match(mauiExport, /"arbeitsstunden_uebersicht"/);
  assert.match(mauiExport, /RunProtectedExportRpcAsync\(SelectedDefinition\?\.ExportKey/);
  assert.match(exportPdf, /BuildArbeitsstundenPdf\(rows\)/);
  assert.doesNotMatch(exportPdf, /WartungsvertragGutschrift|BefreitVonPflichtstunden|pflichtstunden_soll\s*[=+*/-]/);
});
