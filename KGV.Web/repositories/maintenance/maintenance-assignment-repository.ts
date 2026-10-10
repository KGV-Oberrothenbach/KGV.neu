import { type BrowserSession, readSupabase, writeSupabase } from "../../lib/supabase-auth";

export type MaintenanceAssignmentRow = {
  id: number;
  wartungsvertrag_id: number;
  hauptmitglied_id: number;
  gueltig_ab: string;
  gueltig_bis: string | null;
  bemerkung: string | null;
};

export type MaintenanceAssignmentWriteRow = Omit<MaintenanceAssignmentRow, "id">;

const select = "id,wartungsvertrag_id,hauptmitglied_id,gueltig_ab,gueltig_bis,bemerkung";

export const listMaintenanceAssignments = (session: BrowserSession, principalMemberId?: number) =>
  readSupabase<MaintenanceAssignmentRow>(session, "wartungsvertrag_zuordnungen", { select, ...(principalMemberId ? { hauptmitglied_id: `eq.${principalMemberId}` } : {}), order: "gueltig_ab.desc", limit: "2000" });

export const listMaintenanceAssignmentsForContract = (session: BrowserSession, contractId: number) =>
  readSupabase<MaintenanceAssignmentRow>(session, "wartungsvertrag_zuordnungen", { select, wartungsvertrag_id: `eq.${contractId}`, order: "gueltig_ab.asc", limit: "2000" });

export const resolveMaintenanceAssignmentPrincipalMember = async (session: BrowserSession, selectedMemberId: number): Promise<number> => {
  const rows = await readSupabase<{ id: number; hauptmitglied_id: number | null }>(session, "mitglied", { select: "id,hauptmitglied_id", id: `eq.${selectedMemberId}`, limit: "1" });
  const member = rows[0];
  if (!member) throw new Error("Das ausgewählte Mitglied wurde nicht gefunden.");
  return member.hauptmitglied_id ?? member.id;
};

export const createMaintenanceAssignment = (session: BrowserSession, payload: MaintenanceAssignmentWriteRow) =>
  writeSupabase<MaintenanceAssignmentRow>(session, "wartungsvertrag_zuordnungen", "POST", payload);

// Assignments are ended by setting gueltig_bis; they are never hard-deleted.
export const endMaintenanceAssignment = (session: BrowserSession, id: number, validUntil: string) =>
  writeSupabase<MaintenanceAssignmentRow>(session, "wartungsvertrag_zuordnungen", "PATCH", { gueltig_bis: validUntil }, { id: `eq.${id}` });
