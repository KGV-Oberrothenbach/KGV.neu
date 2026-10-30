import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";

export type HistoricalContractRegistrationInput = { documentId: number; documentTypeCode: "historischer_ma_pv_erstvertrag" | "historischer_pv_folgevertrag"; contractDate: string; signed: boolean; leaseRelationshipIds: number[]; previousEndDates: Record<string, string>; membershipSinceCorrection?: { memberId: number; date: string } | null; partyMemberIds: number[]; membershipEnds: { mitglied_id: number; mitglied_ende: string }[]; activityStatusChanges: { mitglied_id: number; aktiv: boolean }[] };
export type HistoricalContractRegistrationRow = { dokument_id: number; dokumenttyp_id: number; pachtverhaeltnis_id: number; pachtvertragsfassung_id: number; historische_vertragsnummer: number };

// Deliberately calls the G6a multi-relation RPC, never register_historisches_dokument.
export function registerHistoricalContract(club: ClubContext, session: BrowserSession, input: HistoricalContractRegistrationInput) {
  return callSupabaseRpc<HistoricalContractRegistrationRow[]>({ club, session }, "register_historischen_pachtvertrag", { p_dokument_id: input.documentId, p_dokumenttyp_code: input.documentTypeCode, p_vertragsdatum: input.contractDate, p_unterzeichnet: input.signed, p_pachtverhaeltnis_ids: input.leaseRelationshipIds, p_vorherige_enddaten: input.previousEndDates, p_mitglied_seit_korrektur_mitglied_id: input.membershipSinceCorrection?.memberId ?? null, p_mitglied_seit_korrektur_datum: input.membershipSinceCorrection?.date ?? null, p_vertragspartei_mitglied_ids: input.partyMemberIds, p_mitgliedschaftsenden: input.membershipEnds, p_aktiv_status_aenderungen: input.activityStatusChanges });
}
