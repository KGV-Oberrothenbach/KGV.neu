import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { HistoricalDocumentRegistrationInput } from "../../models/documents/document-type";

export type HistoricalDocumentRegistrationRow = {
  dokument_id: number;
  dokumenttyp_id: number;
  dokumentdatum: string;
  unterzeichnet: boolean;
  pachtvertragsfassung_id: number | null;
  wartungsvertrag_zuordnung_id: number | null;
};

export function registerHistoricalDocument(club: ClubContext, session: BrowserSession, input: HistoricalDocumentRegistrationInput) {
  return callSupabaseRpc<HistoricalDocumentRegistrationRow[]>({ club, session }, "register_historisches_dokument", {
    p_dokument_id: input.documentId,
    p_dokumenttyp_id: input.dokumenttypId,
    p_dokumentdatum: input.dokumentdatum,
    p_unterzeichnet: input.unterzeichnet,
    p_pachtverhaeltnis_id: input.pachtverhaeltnisId,
    p_wartungsvertrag_zuordnung_id: input.wartungsvertragZuordnungId,
  });
}
