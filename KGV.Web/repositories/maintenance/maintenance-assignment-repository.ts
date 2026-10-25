import { readSupabaseRest } from "../../lib/supabase/rest-client";
import { writeWithBrowserEditLock } from "../../services/concurrency/edit-lock-service";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";

export type MaintenanceAssignmentRow = {
  id: number;
  wartungsvertrag_id: number;
  hauptmitglied_id: number;
  vertragspartei_mitglied_id: number | null;
  gueltig_ab: string;
  gueltig_bis: string | null;
  bemerkung: string | null;
  vorgaenger_zuordnung_id?: number | null;
};

export type MaintenanceAssignmentWriteRow = Omit<MaintenanceAssignmentRow, "id">;
export type MaintenanceAssignmentMemberRow = { id: number; vorname: string | null; name: string | null; aktiv: boolean; is_demo: boolean; hauptmitglied_id: number | null };

const select = "id,wartungsvertrag_id,hauptmitglied_id,vertragspartei_mitglied_id,gueltig_ab,gueltig_bis,bemerkung,vorgaenger_zuordnung_id";

export const listMaintenanceAssignments = (club: ClubContext, session: BrowserSession, principalMemberId?: number) =>
  readSupabaseRest<MaintenanceAssignmentRow>({ club, session }, "wartungsvertrag_zuordnungen", { select, ...(principalMemberId ? { hauptmitglied_id: `eq.${principalMemberId}` } : {}), order: "gueltig_ab.desc", limit: "2000" });

export const listMaintenanceAssignmentsForContract = (club: ClubContext, session: BrowserSession, contractId: number) =>
  readSupabaseRest<MaintenanceAssignmentRow>({ club, session }, "wartungsvertrag_zuordnungen", { select, wartungsvertrag_id: `eq.${contractId}`, order: "gueltig_ab.asc", limit: "2000" });

export const resolveMaintenanceAssignmentPrincipalMember = async (club: ClubContext, session: BrowserSession, selectedMemberId: number): Promise<number> => {
  const rows = await readSupabaseRest<{ id: number; hauptmitglied_id: number | null }>({ club, session }, "mitglied", { select: "id,hauptmitglied_id", id: `eq.${selectedMemberId}`, limit: "1" });
  const member = rows[0];
  if (!member) throw new Error("Das ausgewählte Mitglied wurde nicht gefunden.");
  return member.hauptmitglied_id ?? member.id;
};

export const listMaintenanceAssignmentMembers = (club: ClubContext, session: BrowserSession) =>
  readSupabaseRest<MaintenanceAssignmentMemberRow>({ club, session }, "mitglied", { select: "id,vorname,name,aktiv,is_demo,hauptmitglied_id", aktiv: "eq.true", aufnahme_status: "eq.aufgenommen", is_demo: "eq.false", order: "name.asc,vorname.asc", limit: "1000" });

export const resolveMaintenanceAssignmentMembers = (club: ClubContext, session: BrowserSession, selectedMemberIds: number[]) =>
  selectedMemberIds.length
    ? readSupabaseRest<MaintenanceAssignmentMemberRow>({ club, session }, "mitglied", { select: "id,vorname,name,aktiv,is_demo,hauptmitglied_id", id: `in.(${selectedMemberIds.join(",")})`, limit: "1000" })
    : Promise.resolve([] as MaintenanceAssignmentMemberRow[]);

export const createMaintenanceAssignment = (club: ClubContext, session: BrowserSession, payload: MaintenanceAssignmentWriteRow) =>
  writeWithBrowserEditLock<MaintenanceAssignmentRow>({ club, session }, "wartungsvertrag_zuordnungen", "POST", payload);

// Assignments are ended by setting gueltig_bis; they are never hard-deleted.
export const endMaintenanceAssignment = (club: ClubContext, session: BrowserSession, id: number, validUntil: string) =>
  writeWithBrowserEditLock<MaintenanceAssignmentRow>({ club, session }, "wartungsvertrag_zuordnungen", "PATCH", { gueltig_bis: validUntil }, { id: `eq.${id}` });
