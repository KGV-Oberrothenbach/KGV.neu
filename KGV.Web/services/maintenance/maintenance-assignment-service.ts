import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type MaintenanceAssignment } from "../../models/maintenance/maintenance-assignment";
import type { HistoricalMaintenanceAssignment } from "../../models/documents/document-type";
import { type MaintenanceContract } from "../../models/maintenance/maintenance-contract";
import { getMember } from "../members/member-service";
import { listCurrentGardenNumbersByMemberId } from "../parcels/parcel-service";
import { loadMaintenanceContract, loadMaintenanceContracts } from "./maintenance-contract-service";
import { createMaintenanceAssignment, endMaintenanceAssignment, listMaintenanceAssignmentMembers, listMaintenanceAssignments, listMaintenanceAssignmentsForContract, resolveMaintenanceAssignmentMembers, resolveMaintenanceAssignmentPrincipalMember, type MaintenanceAssignmentMemberRow, type MaintenanceAssignmentRow } from "../../repositories/maintenance/maintenance-assignment-repository";
import { classifyMaintenanceAssignments, maintenanceReferenceDate } from "./maintenance-assignment-time-model.js";
import { ensureMaintenanceDocumentWorkflow, type MaintenanceDocumentWorkflowRow } from "../../repositories/maintenance/maintenance-document-workflow-repository";

export { classifyMaintenanceAssignments } from "./maintenance-assignment-time-model.js";

function fromRow(row: MaintenanceAssignmentRow): MaintenanceAssignment {
  return { id: row.id, maintenanceContractId: row.wartungsvertrag_id, principalMemberId: row.hauptmitglied_id, contractPartyMemberId: row.vertragspartei_mitglied_id, validFrom: row.gueltig_ab, validUntil: row.gueltig_bis, note: row.bemerkung, predecessorAssignmentId: row.vorgaenger_zuordnung_id ?? null };
}

export type ClassifiedMaintenanceAssignments = { active: MaintenanceAssignment[]; future: MaintenanceAssignment[]; history: MaintenanceAssignment[] };
export type MaintenanceAssignmentCreateInput = { maintenanceContractId: number; selectedMemberId: number; validFrom: string; validUntil: string | null; note: string | null };
export type MaintenanceAssignmentBatchInput = { maintenanceContractId: number; selectedMemberIds: number[]; validFrom: string; validUntil: null; note: string | null };
export type MaintenanceMemberCandidate = { selectedMemberId: number; principalMemberId: number; displayName: string; memberContext: "Hauptmitglied" | "Nebenmitglied"; gardenNumbers: string[]; alreadyAssignedInPeriod: boolean; selectable: boolean; statusText: string | null };
export type MaintenanceContractCandidate = { contract: MaintenanceContract; principalMemberId: number; alreadyAssignedInPeriod: boolean; availableInPeriod: boolean; selectable: boolean; statusText: string | null };
export type MaintenanceContractBatchInput = { selectedMemberId: number; maintenanceContractIds: number[]; validFrom: string; validUntil: null; note: string | null };
export type MaintenanceBatchSaveResult = {
  created: MaintenanceAssignment[];
  failures: { targetId: number; message: string }[];
  documentWorkflows: MaintenanceDocumentWorkflowRow[];
  documentFailures: { assignmentId: number; message: string }[];
};

export function maintenanceAssignmentIntervalsOverlap(aFrom: string, aUntil: string | null, bFrom: string, bUntil: string | null): boolean {
  return aFrom <= (bUntil ?? "9999-12-31") && bFrom <= (aUntil ?? "9999-12-31");
}

export function calculatePeakMaintenanceOccupancy(existing: MaintenanceAssignment[], candidate: Pick<MaintenanceAssignment, "validFrom" | "validUntil">): number {
  const overlapping = existing.filter((item) => maintenanceAssignmentIntervalsOverlap(item.validFrom, item.validUntil, candidate.validFrom, candidate.validUntil));
  const dates = new Set<string>([candidate.validFrom, ...overlapping.map((item) => item.validFrom > candidate.validFrom ? item.validFrom : candidate.validFrom)]);
  return Math.max(1, ...[...dates].map((date) => 1 + overlapping.filter((item) => item.validFrom <= date && (!item.validUntil || item.validUntil >= date)).length));
}

function principalMemberId(member: Pick<MaintenanceAssignmentMemberRow, "id" | "hauptmitglied_id">) {
  return member.hauptmitglied_id ?? member.id;
}

function displayName(member: Pick<MaintenanceAssignmentMemberRow, "mitgliedsnummer" | "name" | "vorname">) {
  const name = [member.vorname, member.name].filter(Boolean).join(" ");
  const number = `Mgl.-Nr. ${member.mitgliedsnummer ?? "–"}`;
  return name ? `${name} · ${number}` : number;
}

export async function loadMaintenanceMemberCandidates(club: ClubContext, session: BrowserSession, maintenanceContractId: number, validFrom: string): Promise<{ candidates: MaintenanceMemberCandidate[]; availableCapacity: number }> {
  const [contract, rows, members] = await Promise.all([loadMaintenanceContract(club, session, maintenanceContractId), listMaintenanceAssignmentsForContract(club, session, maintenanceContractId), listMaintenanceAssignmentMembers(club, session)]);
  if (!contract) throw new Error("Der Wartungsvertrag wurde nicht gefunden.");
  const assignments = rows.map(fromRow);
  const gardenNumbersByMemberId = await listCurrentGardenNumbersByMemberId(club, session, members.map((member) => member.id));
  const availableCapacity = Math.max(0, contract.maxActiveAssignments - (calculatePeakMaintenanceOccupancy(assignments, { validFrom, validUntil: null }) - 1));
  return {
    availableCapacity,
    candidates: members.map((member) => {
      const principal = principalMemberId(member);
      const alreadyAssignedInPeriod = assignments.some((assignment) => assignment.principalMemberId === principal && maintenanceAssignmentIntervalsOverlap(assignment.validFrom, assignment.validUntil, validFrom, null));
      return { selectedMemberId: member.id, principalMemberId: principal, displayName: displayName(member), memberContext: member.hauptmitglied_id ? "Nebenmitglied" : "Hauptmitglied", gardenNumbers: gardenNumbersByMemberId.get(member.id) ?? [], alreadyAssignedInPeriod, selectable: contract.active && !alreadyAssignedInPeriod, statusText: alreadyAssignedInPeriod ? "Bereits über Hauptmitglied-Kontext zugeordnet" : null };
    }),
  };
}
export async function loadMaintenanceAssignments(club: ClubContext, session: BrowserSession, principalMemberId?: number): Promise<MaintenanceAssignment[]> {
  return (await listMaintenanceAssignments(club, session, principalMemberId)).map(fromRow);
}

export const resolveMaintenancePrincipalMember = (club: ClubContext, session: BrowserSession, selectedMemberId: number) => resolveMaintenanceAssignmentPrincipalMember(club, session, selectedMemberId);

export async function loadMemberMaintenanceContracts(club: ClubContext, session: BrowserSession, selectedMemberId: number) {
  const member = await getMember(club, session, selectedMemberId);
  if (!member) throw new Error("Das Mitglied wurde nicht gefunden.");
  const principalMemberId = await resolveMaintenancePrincipalMember(club, session, selectedMemberId);
  const [principalMember, assignments, contracts] = await Promise.all([principalMemberId === selectedMemberId ? Promise.resolve(member) : getMember(club, session, principalMemberId), loadMaintenanceAssignments(club, session, principalMemberId), loadMaintenanceContracts(club, session)]);
  return { member, principalMember, principalMemberId, assignments: classifyMaintenanceAssignments(assignments, maintenanceReferenceDate(), contracts), contracts, documentWorkflows: [] as MaintenanceDocumentWorkflowRow[] };
}

export async function loadHistoricalDocumentMaintenanceAssignments(club: ClubContext, session: BrowserSession, selectedMemberId: number): Promise<{ principalMemberId: number; assignments: HistoricalMaintenanceAssignment[] }> {
  const principalMemberId = await resolveMaintenancePrincipalMember(club, session, selectedMemberId);
  const [assignments, contracts] = await Promise.all([loadMaintenanceAssignments(club, session, principalMemberId), loadMaintenanceContracts(club, session)]);
  const contractById = new Map(contracts.map((contract) => [contract.id, contract]));
  return {
    principalMemberId,
    assignments: assignments.map((assignment) => ({
      id: assignment.id,
      bezeichnung: [contractById.get(assignment.maintenanceContractId)?.title ?? "Wartungsvertrag", contractById.get(assignment.maintenanceContractId)?.contractNumber ? `Nr. ${contractById.get(assignment.maintenanceContractId)?.contractNumber}` : null].filter(Boolean).join(" · "),
      gueltigAb: assignment.validFrom,
      gueltigBis: assignment.validUntil,
      hauptmitgliedId: assignment.principalMemberId,
    })).sort((left, right) => right.gueltigAb.localeCompare(left.gueltigAb) || right.id - left.id),
  };
}

export async function loadAssignableMaintenanceContractsForMember(club: ClubContext, session: BrowserSession, selectedMemberId: number, validFrom: string): Promise<MaintenanceContractCandidate[]> {
  const principalMemberId = await resolveMaintenancePrincipalMember(club, session, selectedMemberId);
  const [contracts, assignments] = await Promise.all([loadMaintenanceContracts(club, session), loadMaintenanceAssignments(club, session)]);
  return contracts.filter((contract) => contract.active).map((contract) => {
    const contractAssignments = assignments.filter((assignment) => assignment.maintenanceContractId === contract.id);
    const alreadyAssignedInPeriod = contractAssignments.some((assignment) => assignment.principalMemberId === principalMemberId && maintenanceAssignmentIntervalsOverlap(assignment.validFrom, assignment.validUntil, validFrom, null));
    const availableInPeriod = calculatePeakMaintenanceOccupancy(contractAssignments, { validFrom, validUntil: null }) <= contract.maxActiveAssignments;
    return { contract, principalMemberId, alreadyAssignedInPeriod, availableInPeriod, selectable: contract.active && !alreadyAssignedInPeriod && availableInPeriod, statusText: alreadyAssignedInPeriod ? "Bereits zugeordnet" : !availableInPeriod ? "Im gewählten Zeitraum ist das Kontingent ausgeschöpft" : null };
  });
}

export async function createMaintenanceAssignmentForContract(club: ClubContext, session: BrowserSession, input: MaintenanceAssignmentCreateInput): Promise<MaintenanceAssignment> {
  if (!input.maintenanceContractId || !input.selectedMemberId || !input.validFrom) throw new Error("Vertrag, Mitglied und Gültigkeitsbeginn sind erforderlich.");
  if (input.validUntil && input.validUntil < input.validFrom) throw new Error("Das Enddatum darf nicht vor dem Beginn liegen.");
  const [contract, principalMemberId, rows] = await Promise.all([loadMaintenanceContract(club, session, input.maintenanceContractId), resolveMaintenanceAssignmentPrincipalMember(club, session, input.selectedMemberId), listMaintenanceAssignmentsForContract(club, session, input.maintenanceContractId)]);
  if (!contract) throw new Error("Der Wartungsvertrag wurde nicht gefunden.");
  if (!contract.active) throw new Error("Dieser Wartungsvertrag ist nicht aktiv.");
  const assignments = rows.map(fromRow);
  if (assignments.some((item) => item.principalMemberId === principalMemberId && maintenanceAssignmentIntervalsOverlap(item.validFrom, item.validUntil, input.validFrom, input.validUntil))) throw new Error("Das Mitglied besitzt diesen Wartungsvertrag im gewählten Zeitraum bereits.");
  if (calculatePeakMaintenanceOccupancy(assignments, { validFrom: input.validFrom, validUntil: input.validUntil }) > contract.maxActiveAssignments) throw new Error("Im gewählten Zeitraum ist das Kontingent des Wartungsvertrags ausgeschöpft.");
  const created = await createMaintenanceAssignment(club, session, { wartungsvertrag_id: input.maintenanceContractId, hauptmitglied_id: principalMemberId, vertragspartei_mitglied_id: input.selectedMemberId, gueltig_ab: input.validFrom, gueltig_bis: input.validUntil, bemerkung: input.note?.trim() || null });
  const row = created[0];
  if (!row) throw new Error("Wartungsvertragszuordnung konnte nicht gespeichert werden.");
  try {
    await ensureMaintenanceDocumentWorkflow(club, session, row.id);
  } catch (cause) {
    const message = cause instanceof Error ? cause.message : "Unbekannter Fehler";
    console.error("Wartungsvertragszuordnung gespeichert, Dokumentworkflow konnte nicht vorbereitet werden.", { assignmentId: row.id, message });
    throw new Error("Zuordnung wurde gespeichert, das Wartungsvertragsdokument konnte jedoch nicht erstellt werden.", { cause });
  }
  return fromRow(row);
}

async function ensureDocumentWorkflows(club: ClubContext, session: BrowserSession, assignments: MaintenanceAssignment[]) {
  const workflows: MaintenanceDocumentWorkflowRow[] = [];
  const failures: { assignmentId: number; message: string }[] = [];
  for (const assignment of assignments) {
    try {
      workflows.push(await ensureMaintenanceDocumentWorkflow(club, session, assignment.id));
    } catch (cause) {
      const message = cause instanceof Error ? cause.message : "Dokumentworkflow konnte nicht angelegt werden.";
      console.error("Wartungsvertrags-Dokumentworkflow konnte nicht vorbereitet werden.", { assignmentId: assignment.id, message });
      failures.push({ assignmentId: assignment.id, message });
    }
  }
  return { workflows, failures };
}

export async function assignMembersToMaintenanceContract(club: ClubContext, session: BrowserSession, input: MaintenanceAssignmentBatchInput): Promise<MaintenanceBatchSaveResult> {
  if (!input.maintenanceContractId || !input.validFrom || input.validUntil !== null || input.selectedMemberIds.length === 0) throw new Error("Vertrag, mindestens ein Mitglied und Gültigkeitsbeginn sind erforderlich.");
  const selectedMemberIds = [...new Set(input.selectedMemberIds)];
  if (selectedMemberIds.length !== input.selectedMemberIds.length || selectedMemberIds.some((id) => !Number.isInteger(id) || id <= 0)) throw new Error("Die Mitgliedsauswahl ist ungültig.");
  const [contract, rows, members] = await Promise.all([loadMaintenanceContract(club, session, input.maintenanceContractId), listMaintenanceAssignmentsForContract(club, session, input.maintenanceContractId), resolveMaintenanceAssignmentMembers(club, session, selectedMemberIds)]);
  if (!contract) throw new Error("Der Wartungsvertrag wurde nicht gefunden.");
  if (!contract.active) throw new Error("Dieser Wartungsvertrag ist nicht aktiv.");
  if (members.length !== selectedMemberIds.length || members.some((member) => !member.aktiv || member.is_demo)) throw new Error("Mindestens ein ausgewähltes Mitglied ist nicht für die produktive Zuordnung verfügbar.");
  const principalMemberIds = members.map(principalMemberId);
  if (new Set(principalMemberIds).size !== principalMemberIds.length) throw new Error("Pro Hauptmitglied-Kontext kann nur ein Mitglied ausgewählt werden.");
  const assignments = rows.map(fromRow);
  if (principalMemberIds.some((principal) => assignments.some((assignment) => assignment.principalMemberId === principal && maintenanceAssignmentIntervalsOverlap(assignment.validFrom, assignment.validUntil, input.validFrom, input.validUntil)))) throw new Error("Mindestens ein Mitglied besitzt diesen Wartungsvertrag im gewählten Zeitraum bereits.");
  const peakWithOneCandidate = calculatePeakMaintenanceOccupancy(assignments, { validFrom: input.validFrom, validUntil: input.validUntil });
  if (peakWithOneCandidate + principalMemberIds.length - 1 > contract.maxActiveAssignments) throw new Error("Im gewählten Zeitraum ist das Kontingent des Wartungsvertrags ausgeschöpft.");
  const created: MaintenanceAssignment[] = []; const failures: { targetId: number; message: string }[] = [];
  for (let index = 0; index < principalMemberIds.length; index += 1) {
    const principal = principalMemberIds[index]; const targetId = selectedMemberIds[index];
    try {
      const inserted = await createMaintenanceAssignment(club, session, { wartungsvertrag_id: input.maintenanceContractId, hauptmitglied_id: principal, vertragspartei_mitglied_id: targetId, gueltig_ab: input.validFrom, gueltig_bis: null, bemerkung: input.note?.trim() || null });
      if (!inserted[0]) throw new Error("Wartungsvertragszuordnung konnte nicht gespeichert werden.");
      created.push(fromRow(inserted[0]));
    } catch (cause) {
      failures.push({ targetId, message: cause instanceof Error ? cause.message : "Zuordnung konnte nicht gespeichert werden." });
    }
  }
  const documentResult = await ensureDocumentWorkflows(club, session, created);
  return { created, failures, documentWorkflows: documentResult.workflows, documentFailures: documentResult.failures };
}

export async function assignMaintenanceContractsToMember(club: ClubContext, session: BrowserSession, input: MaintenanceContractBatchInput): Promise<MaintenanceBatchSaveResult> {
  if (!input.selectedMemberId || !input.validFrom || input.validUntil !== null || input.maintenanceContractIds.length === 0) throw new Error("Mitglied, mindestens ein Wartungsvertrag und Gültigkeitsbeginn sind erforderlich.");
  const contractIds = [...new Set(input.maintenanceContractIds)];
  if (contractIds.length !== input.maintenanceContractIds.length || contractIds.some((id) => !Number.isInteger(id) || id <= 0)) throw new Error("Die Vertragsauswahl ist ungültig.");
  const principalMemberId = await resolveMaintenancePrincipalMember(club, session, input.selectedMemberId);
  const [contracts, assignments] = await Promise.all([Promise.all(contractIds.map((id) => loadMaintenanceContract(club, session, id))), loadMaintenanceAssignments(club, session)]);
  if (contracts.some((contract) => !contract || !contract.active)) throw new Error("Mindestens ein ausgewählter Wartungsvertrag ist nicht aktiv oder wurde nicht gefunden.");
  for (const contract of contracts as MaintenanceContract[]) {
    const contractAssignments = assignments.filter((assignment) => assignment.maintenanceContractId === contract.id);
    if (contractAssignments.some((assignment) => assignment.principalMemberId === principalMemberId && maintenanceAssignmentIntervalsOverlap(assignment.validFrom, assignment.validUntil, input.validFrom, null))) throw new Error("Das Mitglied besitzt mindestens einen Wartungsvertrag im gewählten Zeitraum bereits.");
    if (calculatePeakMaintenanceOccupancy(contractAssignments, { validFrom: input.validFrom, validUntil: null }) > contract.maxActiveAssignments) throw new Error("Im gewählten Zeitraum ist das Kontingent mindestens eines Wartungsvertrags ausgeschöpft.");
  }
  const created: MaintenanceAssignment[] = []; const failures: { targetId: number; message: string }[] = [];
  for (const contract of contracts as MaintenanceContract[]) {
    try {
      const rows = await createMaintenanceAssignment(club, session, { wartungsvertrag_id: contract.id, hauptmitglied_id: principalMemberId, vertragspartei_mitglied_id: input.selectedMemberId, gueltig_ab: input.validFrom, gueltig_bis: null, bemerkung: input.note?.trim() || null });
      if (!rows[0]) throw new Error("Wartungsvertragszuordnung konnte nicht gespeichert werden.");
      created.push(fromRow(rows[0]));
    } catch (cause) {
      failures.push({ targetId: contract.id, message: cause instanceof Error ? cause.message : "Zuordnung konnte nicht gespeichert werden." });
    }
  }
  const documentResult = await ensureDocumentWorkflows(club, session, created);
  return { created, failures, documentWorkflows: documentResult.workflows, documentFailures: documentResult.failures };
}

export async function endMaintenanceAssignmentForContract(club: ClubContext, session: BrowserSession, assignment: Pick<MaintenanceAssignment, "id" | "validFrom">, validUntil: string): Promise<MaintenanceAssignment> {
  if (!validUntil || validUntil < assignment.validFrom) throw new Error("Das Enddatum darf nicht vor dem Beginn liegen.");
  const rows = await endMaintenanceAssignment(club, session, assignment.id, validUntil);
  const row = rows[0];
  if (!row) throw new Error("Wartungsvertragszuordnung konnte nicht beendet werden.");
  return fromRow(row);
}
