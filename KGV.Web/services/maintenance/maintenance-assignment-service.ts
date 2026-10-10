import { type BrowserSession } from "../../lib/supabase-auth";
import { type MaintenanceAssignment, type MaintenanceAssignmentDraft } from "../../models/maintenance/maintenance-assignment";
import { createMaintenanceAssignment, endMaintenanceAssignment, listMaintenanceAssignments, type MaintenanceAssignmentRow } from "../../repositories/maintenance/maintenance-assignment-repository";

function fromRow(row: MaintenanceAssignmentRow): MaintenanceAssignment {
  return { id: row.id, maintenanceContractId: row.wartungsvertrag_id, principalMemberId: row.hauptmitglied_id, validFrom: row.gueltig_ab, validUntil: row.gueltig_bis, note: row.bemerkung };
}

export async function loadMaintenanceAssignments(session: BrowserSession, principalMemberId?: number): Promise<MaintenanceAssignment[]> {
  return (await listMaintenanceAssignments(session, principalMemberId)).map(fromRow);
}

export async function createMaintenanceAssignmentForContract(session: BrowserSession, draft: MaintenanceAssignmentDraft): Promise<MaintenanceAssignment> {
  if (!draft.maintenanceContractId || !draft.principalMemberId || !draft.validFrom) throw new Error("Vertrag, Hauptmitglied und Gültigkeitsbeginn sind erforderlich.");
  const rows = await createMaintenanceAssignment(session, { wartungsvertrag_id: draft.maintenanceContractId, hauptmitglied_id: draft.principalMemberId, gueltig_ab: draft.validFrom, gueltig_bis: draft.validUntil, bemerkung: draft.note?.trim() || null });
  const row = rows[0];
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
