import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { ExpiringMaintenanceAssignment, MaintenanceContinuationDetail, MaintenanceDecisionInitiator } from "../../models/maintenance/maintenance-continuation";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";

export const listExpiringMaintenanceAssignments = (club: ClubContext, session: BrowserSession) => callSupabaseRpc<ExpiringMaintenanceAssignment[]>({ club, session }, "get_auslaufende_wartungsvertrag_zuordnungen", {});
export async function getMaintenanceContinuationDetail(club: ClubContext, session: BrowserSession, assignmentId: number) {
  const rows = await callSupabaseRpc<MaintenanceContinuationDetail[]>({ club, session }, "get_wartungsvertrag_folgeentscheidung_detail", { p_vorgaenger_zuordnung_id: assignmentId });
  return rows[0] ?? null;
}
export const extendMaintenanceAssignment = (club: ClubContext, session: BrowserSession, value: { assignmentId: number; validUntil: string; boardDecisionDate: string; note?: string }) => callSupabaseRpc<{ folgeentscheidung_id: number; folgezuordnung_id: number; kommunikationsvorgang_id: number; workflow_id: number; vertragspartei_mitglied_id: number }[]>({ club, session }, "verlaengere_wartungsvertrag", { p_vorgaenger_zuordnung_id: value.assignmentId, p_neues_gueltig_bis: value.validUntil, p_vorstandsbeschlussdatum: value.boardDecisionDate, p_bemerkung: value.note || null });
export const decideMaintenanceNonExtension = (club: ClubContext, session: BrowserSession, value: { assignmentId: number; initiator: MaintenanceDecisionInitiator; decisionDate: string; boardDecisionDate?: string; reason?: string }) => callSupabaseRpc<number>({ club, session }, "entscheide_wartungsvertrag_nichtverlaengerung", { p_vorgaenger_zuordnung_id: value.assignmentId, p_initiator: value.initiator, p_entscheidungsdatum: value.decisionDate, p_vorstandsbeschlussdatum: value.boardDecisionDate || null, p_begruendung: value.reason || null });
export const withdrawMaintenanceNonExtension = (club: ClubContext, session: BrowserSession, decisionId: number, reason?: string) => callSupabaseRpc<number>({ club, session }, "nehme_wartungsvertrag_nichtverlaengerung_zurueck", { p_folgeentscheidung_id: decisionId, p_ruecknahmebegruendung: reason || null });
