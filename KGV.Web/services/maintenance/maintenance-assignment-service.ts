import { type BrowserSession } from "../../lib/supabase-auth";
import { type MaintenanceAssignment } from "../../models/maintenance/maintenance-assignment";
import { type MaintenanceContract } from "../../models/maintenance/maintenance-contract";
import { getMember } from "../members/member-service";
import { listCurrentGardenNumbersByMemberId } from "../parcels/parcel-service";
import { loadMaintenanceContract, loadMaintenanceContracts } from "./maintenance-contract-service";
import { createMaintenanceAssignment, endMaintenanceAssignment, listMaintenanceAssignmentMembers, listMaintenanceAssignments, listMaintenanceAssignmentsForContract, resolveMaintenanceAssignmentMembers, resolveMaintenanceAssignmentPrincipalMember, type MaintenanceAssignmentMemberRow, type MaintenanceAssignmentRow } from "../../repositories/maintenance/maintenance-assignment-repository";

function fromRow(row: MaintenanceAssignmentRow): MaintenanceAssignment {
  return { id: row.id, maintenanceContractId: row.wartungsvertrag_id, principalMemberId: row.hauptmitglied_id, validFrom: row.gueltig_ab, validUntil: row.gueltig_bis, note: row.bemerkung };
}

export type ClassifiedMaintenanceAssignments = { active: MaintenanceAssignment[]; future: MaintenanceAssignment[]; history: MaintenanceAssignment[] };
export type MaintenanceAssignmentCreateInput = { maintenanceContractId: number; selectedMemberId: number; validFrom: string; validUntil: string | null; note: string | null };
export type MaintenanceAssignmentBatchInput = { maintenanceContractId: number; selectedMemberIds: number[]; validFrom: string; validUntil: null; note: string | null };
export type MaintenanceMemberCandidate = { selectedMemberId: number; principalMemberId: number; displayName: string; memberContext: "Hauptmitglied" | "Nebenmitglied"; gardenNumbers: string[]; alreadyAssignedInPeriod: boolean; selectable: boolean; statusText: string | null };
export type MaintenanceContractCandidate = { contract: MaintenanceContract; principalMemberId: number; alreadyAssignedInPeriod: boolean; availableInPeriod: boolean; selectable: boolean; statusText: string | null };
export type MaintenanceContractBatchInput = { selectedMemberId: number; maintenanceContractIds: number[]; validFrom: string; validUntil: null; note: string | null };

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

function displayName(member: Pick<MaintenanceAssignmentMemberRow, "id" | "name" | "vorname">) {
  return [member.vorname, member.name].filter(Boolean).join(" ") || `Mitglied #${member.id}`;
}

export async function loadMaintenanceMemberCandidates(session: BrowserSession, maintenanceContractId: number, validFrom: string): Promise<{ candidates: MaintenanceMemberCandidate[]; availableCapacity: number }> {
  const [contract, rows, members] = await Promise.all([loadMaintenanceContract(session, maintenanceContractId), listMaintenanceAssignmentsForContract(session, maintenanceContractId), listMaintenanceAssignmentMembers(session)]);
  if (!contract) throw new Error("Der Wartungsvertrag wurde nicht gefunden.");
  const assignments = rows.map(fromRow);
  const gardenNumbersByMemberId = await listCurrentGardenNumbersByMemberId(session, members.map((member) => member.id));
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
export function classifyMaintenanceAssignments(assignments: MaintenanceAssignment[], referenceDate: string): ClassifiedMaintenanceAssignments {
  return assignments.reduce<ClassifiedMaintenanceAssignments>((result, assignment) => {
    if (assignment.validFrom > referenceDate) result.future.push(assignment);
    else if (assignment.validUntil && assignment.validUntil < referenceDate) result.history.push(assignment);
    else result.active.push(assignment);
    return result;
  }, { active: [], future: [], history: [] });
}

export async function loadMaintenanceAssignments(session: BrowserSession, principalMemberId?: number): Promise<MaintenanceAssignment[]> {
  return (await listMaintenanceAssignments(session, principalMemberId)).map(fromRow);
}

export const resolveMaintenancePrincipalMember = (session: BrowserSession, selectedMemberId: number) => resolveMaintenanceAssignmentPrincipalMember(session, selectedMemberId);

export async function loadMemberMaintenanceContracts(session: BrowserSession, selectedMemberId: number) {
  const member = await getMember(session, selectedMemberId);
  if (!member) throw new Error("Das Mitglied wurde nicht gefunden.");
  const principalMemberId = await resolveMaintenancePrincipalMember(session, selectedMemberId);
  const [principalMember, assignments, contracts] = await Promise.all([principalMemberId === selectedMemberId ? Promise.resolve(member) : getMember(session, principalMemberId), loadMaintenanceAssignments(session, principalMemberId), loadMaintenanceContracts(session)]);
  return { member, principalMember, principalMemberId, assignments, contracts };
}

export async function loadAssignableMaintenanceContractsForMember(session: BrowserSession, selectedMemberId: number, validFrom: string): Promise<MaintenanceContractCandidate[]> {
  const principalMemberId = await resolveMaintenancePrincipalMember(session, selectedMemberId);
  const [contracts, assignments] = await Promise.all([loadMaintenanceContracts(session), loadMaintenanceAssignments(session)]);
  return contracts.filter((contract) => contract.active).map((contract) => {
    const contractAssignments = assignments.filter((assignment) => assignment.maintenanceContractId === contract.id);
    const alreadyAssignedInPeriod = contractAssignments.some((assignment) => assignment.principalMemberId === principalMemberId && maintenanceAssignmentIntervalsOverlap(assignment.validFrom, assignment.validUntil, validFrom, null));
    const availableInPeriod = calculatePeakMaintenanceOccupancy(contractAssignments, { validFrom, validUntil: null }) <= contract.maxActiveAssignments;
    return { contract, principalMemberId, alreadyAssignedInPeriod, availableInPeriod, selectable: contract.active && !alreadyAssignedInPeriod && availableInPeriod, statusText: alreadyAssignedInPeriod ? "Bereits zugeordnet" : !availableInPeriod ? "Im gewählten Zeitraum ist das Kontingent ausgeschöpft" : null };
  });
}

export async function createMaintenanceAssignmentForContract(session: BrowserSession, input: MaintenanceAssignmentCreateInput): Promise<MaintenanceAssignment> {
  if (!input.maintenanceContractId || !input.selectedMemberId || !input.validFrom) throw new Error("Vertrag, Mitglied und Gültigkeitsbeginn sind erforderlich.");
  if (input.validUntil && input.validUntil < input.validFrom) throw new Error("Das Enddatum darf nicht vor dem Beginn liegen.");
  const [contract, principalMemberId, rows] = await Promise.all([loadMaintenanceContract(session, input.maintenanceContractId), resolveMaintenanceAssignmentPrincipalMember(session, input.selectedMemberId), listMaintenanceAssignmentsForContract(session, input.maintenanceContractId)]);
  if (!contract) throw new Error("Der Wartungsvertrag wurde nicht gefunden.");
  if (!contract.active) throw new Error("Dieser Wartungsvertrag ist nicht aktiv.");
  const assignments = rows.map(fromRow);
  if (assignments.some((item) => item.principalMemberId === principalMemberId && maintenanceAssignmentIntervalsOverlap(item.validFrom, item.validUntil, input.validFrom, input.validUntil))) throw new Error("Das Mitglied besitzt diesen Wartungsvertrag im gewählten Zeitraum bereits.");
  if (calculatePeakMaintenanceOccupancy(assignments, { validFrom: input.validFrom, validUntil: input.validUntil }) > contract.maxActiveAssignments) throw new Error("Im gewählten Zeitraum ist das Kontingent des Wartungsvertrags ausgeschöpft.");
  const created = await createMaintenanceAssignment(session, { wartungsvertrag_id: input.maintenanceContractId, hauptmitglied_id: principalMemberId, gueltig_ab: input.validFrom, gueltig_bis: input.validUntil, bemerkung: input.note?.trim() || null });
  const row = created[0];
  if (!row) throw new Error("Wartungsvertragszuordnung konnte nicht gespeichert werden.");
  return fromRow(row);
}

export async function assignMembersToMaintenanceContract(session: BrowserSession, input: MaintenanceAssignmentBatchInput): Promise<MaintenanceAssignment[]> {
  if (!input.maintenanceContractId || !input.validFrom || input.validUntil !== null || input.selectedMemberIds.length === 0) throw new Error("Vertrag, mindestens ein Mitglied und Gültigkeitsbeginn sind erforderlich.");
  const selectedMemberIds = [...new Set(input.selectedMemberIds)];
  if (selectedMemberIds.length !== input.selectedMemberIds.length || selectedMemberIds.some((id) => !Number.isInteger(id) || id <= 0)) throw new Error("Die Mitgliedsauswahl ist ungültig.");
  const [contract, rows, members] = await Promise.all([loadMaintenanceContract(session, input.maintenanceContractId), listMaintenanceAssignmentsForContract(session, input.maintenanceContractId), resolveMaintenanceAssignmentMembers(session, selectedMemberIds)]);
  if (!contract) throw new Error("Der Wartungsvertrag wurde nicht gefunden.");
  if (!contract.active) throw new Error("Dieser Wartungsvertrag ist nicht aktiv.");
  if (members.length !== selectedMemberIds.length || members.some((member) => !member.aktiv || member.is_demo)) throw new Error("Mindestens ein ausgewähltes Mitglied ist nicht für die produktive Zuordnung verfügbar.");
  const principalMemberIds = members.map(principalMemberId);
  if (new Set(principalMemberIds).size !== principalMemberIds.length) throw new Error("Pro Hauptmitglied-Kontext kann nur ein Mitglied ausgewählt werden.");
  const assignments = rows.map(fromRow);
  if (principalMemberIds.some((principal) => assignments.some((assignment) => assignment.principalMemberId === principal && maintenanceAssignmentIntervalsOverlap(assignment.validFrom, assignment.validUntil, input.validFrom, input.validUntil)))) throw new Error("Mindestens ein Mitglied besitzt diesen Wartungsvertrag im gewählten Zeitraum bereits.");
  const peakWithOneCandidate = calculatePeakMaintenanceOccupancy(assignments, { validFrom: input.validFrom, validUntil: input.validUntil });
  if (peakWithOneCandidate + principalMemberIds.length - 1 > contract.maxActiveAssignments) throw new Error("Im gewählten Zeitraum ist das Kontingent des Wartungsvertrags ausgeschöpft.");
  const created: MaintenanceAssignment[] = [];
  try {
    for (const principal of principalMemberIds) {
      const inserted = await createMaintenanceAssignment(session, { wartungsvertrag_id: input.maintenanceContractId, hauptmitglied_id: principal, gueltig_ab: input.validFrom, gueltig_bis: null, bemerkung: input.note?.trim() || null });
      if (!inserted[0]) throw new Error("Wartungsvertragszuordnung konnte nicht gespeichert werden.");
      created.push(fromRow(inserted[0]));
    }
  } catch (cause) {
    throw new Error(created.length ? `${created.length} Zuordnung(en) wurden gespeichert. Der Zustand wurde aktualisiert; weitere Zuordnungen konnten nicht gespeichert werden.` : cause instanceof Error ? cause.message : "Zuordnungen konnten nicht gespeichert werden.");
  }
  return created;
}

export async function assignMaintenanceContractsToMember(session: BrowserSession, input: MaintenanceContractBatchInput): Promise<MaintenanceAssignment[]> {
  if (!input.selectedMemberId || !input.validFrom || input.validUntil !== null || input.maintenanceContractIds.length === 0) throw new Error("Mitglied, mindestens ein Wartungsvertrag und Gültigkeitsbeginn sind erforderlich.");
  const contractIds = [...new Set(input.maintenanceContractIds)];
  if (contractIds.length !== input.maintenanceContractIds.length || contractIds.some((id) => !Number.isInteger(id) || id <= 0)) throw new Error("Die Vertragsauswahl ist ungültig.");
  const principalMemberId = await resolveMaintenancePrincipalMember(session, input.selectedMemberId);
  const [contracts, assignments] = await Promise.all([Promise.all(contractIds.map((id) => loadMaintenanceContract(session, id))), loadMaintenanceAssignments(session)]);
  if (contracts.some((contract) => !contract || !contract.active)) throw new Error("Mindestens ein ausgewählter Wartungsvertrag ist nicht aktiv oder wurde nicht gefunden.");
  for (const contract of contracts as MaintenanceContract[]) {
    const contractAssignments = assignments.filter((assignment) => assignment.maintenanceContractId === contract.id);
    if (contractAssignments.some((assignment) => assignment.principalMemberId === principalMemberId && maintenanceAssignmentIntervalsOverlap(assignment.validFrom, assignment.validUntil, input.validFrom, null))) throw new Error("Das Mitglied besitzt mindestens einen Wartungsvertrag im gewählten Zeitraum bereits.");
    if (calculatePeakMaintenanceOccupancy(contractAssignments, { validFrom: input.validFrom, validUntil: null }) > contract.maxActiveAssignments) throw new Error("Im gewählten Zeitraum ist das Kontingent mindestens eines Wartungsvertrags ausgeschöpft.");
  }
  const created: MaintenanceAssignment[] = [];
  try {
    for (const contract of contracts as MaintenanceContract[]) {
      const rows = await createMaintenanceAssignment(session, { wartungsvertrag_id: contract.id, hauptmitglied_id: principalMemberId, gueltig_ab: input.validFrom, gueltig_bis: null, bemerkung: input.note?.trim() || null });
      if (!rows[0]) throw new Error("Wartungsvertragszuordnung konnte nicht gespeichert werden.");
      created.push(fromRow(rows[0]));
    }
  } catch (cause) {
    throw new Error(created.length ? `${created.length} Zuordnung(en) wurden gespeichert. Der Zustand wurde aktualisiert; weitere Zuordnungen konnten nicht gespeichert werden.` : cause instanceof Error ? cause.message : "Zuordnungen konnten nicht gespeichert werden.");
  }
  return created;
}

export async function endMaintenanceAssignmentForContract(session: BrowserSession, assignment: Pick<MaintenanceAssignment, "id" | "validFrom">, validUntil: string): Promise<MaintenanceAssignment> {
  if (!validUntil || validUntil < assignment.validFrom) throw new Error("Das Enddatum darf nicht vor dem Beginn liegen.");
  const rows = await endMaintenanceAssignment(session, assignment.id, validUntil);
  const row = rows[0];
  if (!row) throw new Error("Wartungsvertragszuordnung konnte nicht beendet werden.");
  return fromRow(row);
}
