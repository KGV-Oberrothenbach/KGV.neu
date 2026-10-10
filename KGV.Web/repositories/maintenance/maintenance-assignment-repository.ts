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

export const createMaintenanceAssignment = (session: BrowserSession, payload: MaintenanceAssignmentWriteRow) =>
  writeSupabase<MaintenanceAssignmentRow>(session, "wartungsvertrag_zuordnungen", "POST", payload);

// Assignments are ended by setting gueltig_bis; they are never hard-deleted.
export const endMaintenanceAssignment = (session: BrowserSession, id: number, validUntil: string) =>
  writeSupabase<MaintenanceAssignmentRow>(session, "wartungsvertrag_zuordnungen", "PATCH", { gueltig_bis: validUntil }, { id: `eq.${id}` });
