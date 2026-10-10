import { type BrowserSession } from "../../lib/supabase-auth";
import { type MaintenanceAssignment } from "../../models/maintenance/maintenance-assignment";
import { loadMaintenanceContract } from "./maintenance-contract-service";
import { createMaintenanceAssignment, endMaintenanceAssignment, listMaintenanceAssignments, listMaintenanceAssignmentsForContract, resolveMaintenanceAssignmentPrincipalMember, type MaintenanceAssignmentRow } from "../../repositories/maintenance/maintenance-assignment-repository";

function fromRow(row: MaintenanceAssignmentRow): MaintenanceAssignment {
  return { id: row.id, maintenanceContractId: row.wartungsvertrag_id, principalMemberId: row.hauptmitglied_id, validFrom: row.gueltig_ab, validUntil: row.gueltig_bis, note: row.bemerkung };
}

export type ClassifiedMaintenanceAssignments = { active: MaintenanceAssignment[]; future: MaintenanceAssignment[]; history: MaintenanceAssignment[] };
export type MaintenanceAssignmentCreateInput = { maintenanceContractId: number; selectedMemberId: number; validFrom: string; validUntil: string | null; note: string | null };

export function maintenanceAssignmentIntervalsOverlap(aFrom: string, aUntil: string | null, bFrom: string, bUntil: string | null): boolean {
  return aFrom <= (bUntil ?? "9999-12-31") && bFrom <= (aUntil ?? "9999-12-31");
}

export function calculatePeakMaintenanceOccupancy(existing: MaintenanceAssignment[], candidate: Pick<MaintenanceAssignment, "validFrom" | "validUntil">): number {
  const overlapping = existing.filter((item) => maintenanceAssignmentIntervalsOverlap(item.validFrom, item.validUntil, candidate.validFrom, candidate.validUntil));
  const dates = new Set<string>([candidate.validFrom, ...overlapping.map((item) => item.validFrom > candidate.validFrom ? item.validFrom : candidate.validFrom)]);
  return Math.max(1, ...[...dates].map((date) => 1 + overlapping.filter((item) => item.validFrom <= date && (!item.validUntil || item.validUntil >= date)).length));
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

export async function endMaintenanceAssignmentForContract(session: BrowserSession, assignment: Pick<MaintenanceAssignment, "id" | "validFrom">, validUntil: string): Promise<MaintenanceAssignment> {
  if (!validUntil || validUntil < assignment.validFrom) throw new Error("Das Enddatum darf nicht vor dem Beginn liegen.");
  const rows = await endMaintenanceAssignment(session, assignment.id, validUntil);
  const row = rows[0];
  if (!row) throw new Error("Wartungsvertragszuordnung konnte nicht beendet werden.");
  return fromRow(row);
}
