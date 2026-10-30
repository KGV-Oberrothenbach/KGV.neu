import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";

export type HistoricalContractRegistrationInput = { documentId: number; documentTypeCode: "historischer_ma_pv_erstvertrag" | "historischer_pv_folgevertrag"; contractDate: string; signed: boolean; leaseRelationshipIds: number[]; previousEndDates: Record<string, string>; membershipSinceCorrection?: { memberId: number; date: string } | null; partyMemberIds: number[]; membershipEnds: { mitglied_id: number; mitglied_ende: string }[]; activityStatusChanges: { mitglied_id: number; aktiv: boolean }[] };
export type HistoricalContractRegistrationRow = { dokument_id: number; dokumenttyp_id: number; pachtverhaeltnis_id: number; pachtvertragsfassung_id: number; historische_vertragsnummer: number };
export type HistoricalContractHistoryEntry = { pachtverhaeltnis_id: number; pachtvertragsfassung_id: number; wirksam_ab: string; gueltig_bis: string | null; historische_vertragsnummer: number | null; vertragspartei_mitglied_ids: number[]; ist_vorgaenger: boolean; ist_nachfolger: boolean; vorgeschlagenes_vorgaengerende: string | null };

export function loadHistoricalContractPreview(club: ClubContext, session: BrowserSession, leaseRelationshipIds: number[], contractDate: string) {
  return callSupabaseRpc<HistoricalContractHistoryEntry[]>({ club, session }, "get_historische_pachtvertragsvorschau", { p_pachtverhaeltnis_ids: leaseRelationshipIds, p_vertragsdatum: contractDate });
}

// Deliberately calls the G6a multi-relation RPC, never register_historisches_dokument.
export function registerHistoricalContract(club: ClubContext, session: BrowserSession, input: HistoricalContractRegistrationInput) {
  return callSupabaseRpc<HistoricalContractRegistrationRow[]>({ club, session }, "register_historischen_pachtvertrag", { p_dokument_id: input.documentId, p_dokumenttyp_code: input.documentTypeCode, p_vertragsdatum: input.contractDate, p_unterzeichnet: input.signed, p_pachtverhaeltnis_ids: input.leaseRelationshipIds, p_vorherige_enddaten: input.previousEndDates, p_mitglied_seit_korrektur_mitglied_id: input.membershipSinceCorrection?.memberId ?? null, p_mitglied_seit_korrektur_datum: input.membershipSinceCorrection?.date ?? null, p_vertragspartei_mitglied_ids: input.partyMemberIds, p_mitgliedschaftsenden: input.membershipEnds, p_aktiv_status_aenderungen: input.activityStatusChanges });
}

export type HistoricalContractLookupMember = { id: number; mitgliedsnummer?: number; vorname: string | null; name: string | null; mitglied_seit: string | null; aktiv: boolean };
export type HistoricalContractLookupLease = import("../../models/documents/document-type").HistoricalLeaseRelationship;
export function loadHistoricalContractMembers(club: ClubContext, session: BrowserSession) {
  return callSupabaseRpc<HistoricalContractLookupMember[]>({ club, session }, "get_historical_contract_members", {});
}
export function loadHistoricalContractLeaseRelationships(club: ClubContext, session: BrowserSession, memberId: number) {
  return callSupabaseRpc<HistoricalContractLookupLease[]>({ club, session }, "get_historical_contract_lease_relationships", { p_mitglied_id: memberId });
}

export type HistoricalContractDocumentLink = { documentId: number; title: string | null; number: number | null; date: string | null; mimeType: string | null };
export function loadHistoricalContractDocuments(club: ClubContext, session: BrowserSession, memberId: number) {
  return callSupabaseRpc<HistoricalContractDocumentLink[]>({ club, session }, "get_historical_contract_documents", { p_mitglied_id: memberId });
}
