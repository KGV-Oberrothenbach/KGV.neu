import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const read = (relativePath) => readFile(new URL(relativePath, import.meta.url), "utf8");

test("each saved assignment has one idempotent database workflow bound to its assignment and principal member", async () => {
  const [migration, core] = await Promise.all([
    read("../../../supabase/migrations/20261021150000_wartungsvertrag_document_workflow.sql"),
    read("../../../KGV.Core/Models/PreparedContractModels.cs"),
  ]);
  assert.match(migration, /UNIQUE \(wartungsvertrag_zuordnung_id\)/);
  assert.match(migration, /CREATE TRIGGER trg_create_wartungsvertrag_dokument_workflow\s+AFTER INSERT ON public\.wartungsvertrag_zuordnungen/s);
  assert.match(migration, /ON CONFLICT \(wartungsvertrag_zuordnung_id\) DO NOTHING/);
  assert.match(migration, /v_communication\.mitglied_id IS DISTINCT FROM v_assignment\.hauptmitglied_id/);
  assert.match(migration, /v_document\.mitglied_id IS DISTINCT FROM v_assignment\.hauptmitglied_id/);
  assert.match(core, /MaintenanceContract = "wartungsvertrag"/);
  assert.match(core, /wartungsvertrag_zuordnung_id/);
});

test("web batch saves prepare document workflows only for successfully created assignments and preserve per-item failures", async () => {
  const [service, repository, memberAssignment, contractAssignment] = await Promise.all([
    read("./maintenance-assignment-service.ts"),
    read("../../repositories/maintenance/maintenance-document-workflow-repository.ts"),
    read("../../features/maintenance/assignments/AssignContractsToMember.tsx"),
    read("../../features/maintenance/assignments/AssignMembersToContract.tsx"),
  ]);
  assert.match(service, /ensureDocumentWorkflows\(club, session, created\)/);
  assert.match(service, /ensureMaintenanceDocumentWorkflow\(club, session, row\.id\)/);
  assert.match(service, /documentWorkflows: MaintenanceDocumentWorkflowRow\[\]/);
  assert.match(service, /documentFailures: \{ assignmentId: number; message: string \}\[\]/);
  assert.match(service, /console\.error\("Wartungsvertrags-Dokumentworkflow konnte nicht vorbereitet werden\."/);
  assert.match(repository, /"ensure_wartungsvertrag_dokument_workflow"/);
  assert.match(repository, /workflow\.wartungsvertrag_zuordnung_id !== assignmentId/);
  assert.match(memberAssignment, /result\.documentWorkflows/);
  assert.match(contractAssignment, /await onSaved\(result\)/);
  assert.match(contractAssignment, /result\.documentFailures/);
});

test("web member recovery is explicit and consumes the shared main-member assignment data", async () => {
  const [service, component, documentService, repository] = await Promise.all([
    read("./maintenance-assignment-service.ts"),
    read("../../features/maintenance/MemberMaintenanceContracts.tsx"),
    read("./maintenance-document-workflow-service.ts"),
    read("../../repositories/maintenance/maintenance-assignment-repository.ts"),
  ]);
  assert.match(service, /resolveMaintenancePrincipalMember\(club, session, selectedMemberId\)/);
  assert.match(service, /hauptmitglied_id: principalMemberId/);
  assert.match(component, /Dokumentworkflow prüfen \/ vorbereiten/);
  assert.match(component, /prepareDocumentWorkflow\(assignment\.id\)/);
  assert.match(component, /prepareMaintenanceDocumentWorkflow\(club, session, assignmentId\)/);
  assert.match(documentService, /ensureMaintenanceDocumentWorkflow\(club, session, assignmentId\)/);
  assert.match(repository, /resolveMaintenanceAssignmentPrincipalMember/);
});

test("MAUI uses the shared Infrastructure workflow and W3 batch result, not page-level REST or PDF generation", async () => {
  const [service, memberPage, batchPage, coreResult] = await Promise.all([
    read("../../../KGV.Infrastructure/Services/SupabaseService.cs"),
    read("../../../KGV.Maui/Pages/MemberWartungsvertraegePage.cs"),
    read("../../../KGV.Maui/Pages/WartungsvertragAssignMembersPage.cs"),
    read("../../../KGV.Core/Models/WartungsvertragAssignmentSaveResult.cs"),
  ]);
  assert.match(service, /AssignMitgliederToWartungsvertragAsync/);
  assert.match(service, /AttachWartungsvertragDokumentWorkflowAsync\(item, assignmentId\.Value\)/);
  assert.match(service, /AssignWartungsvertraegeToMitgliedAsync/);
  assert.match(memberPage, /EnsureWartungsvertragDokumentWorkflowAsync\(item\.ZuordnungId\)/);
  assert.match(memberPage, /Dokumentworkflow prüfen \/ vorbereiten/);
  assert.match(memberPage, /result\.Items\.Select\(x => x\.AnzeigeText\)/);
  assert.match(batchPage, /DisplayAlertAsync\("Zuordnung gespeichert", resultText/);
  assert.match(coreResult, /DokumentStatus/);
  assert.match(coreResult, /DokumentMessage/);
  assert.doesNotMatch(memberPage, /HttpClient|RestClient|Generate.*Pdf/i);
});

test("missing approved template remains a transparent pending state rather than fabricated contract content", async () => {
  const [migration, documentation] = await Promise.all([
    read("../../../supabase/migrations/20261021150000_wartungsvertrag_document_workflow.sql"),
    read("../../../Documentation/WARTUNGSVERTRAEGE_W1_BESTAND.md"),
  ]);
  assert.match(migration, /Zuordnung gespeichert, Wartungsvertragsdokument noch nicht erzeugt/);
  assert.match(migration, /Es fehlt eine freigegebene Wartungsvertragsvorlage/);
  assert.match(documentation, /PDF-Erzeugung und\s+Signaturfreigabe bleiben\s+deshalb ausdrücklich offen/);
  assert.match(documentation, /Platzhalter|Vorlagenfelder/);
  assert.match(documentation, /Unterzeichnerrollen|Signaturparteien/);
  assert.match(documentation, /Prepared-Contract/);
});
