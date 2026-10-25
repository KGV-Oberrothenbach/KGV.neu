import { readSupabaseRest } from "../../lib/supabase/rest-client";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import { invokeEdgeFunction } from "../../lib/supabase/edge-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";

const context = (club: ClubContext, session: BrowserSession) => ({ club, session });
export type Dispatch = { id: number; status: "bereit" | "wird_versendet" | "teilweise_fehlerhaft" | "versendet" | "fehler"; betreff_snapshot: string; text_snapshot: string; erneuterversand_von_id: number | null; erstellt_am: string; versendet_am: string | null; letzter_fehler: string | null };
export type DispatchRecipient = { id: number; rolle: string; name_snapshot: string; email_snapshot: string | null; versand_erlaubt: boolean; nicht_versandfaehig_grund: string | null; status: string; versendet_am: string | null; letzter_fehler: string | null };
export type DispatchDocument = { sortierung: number; bezeichnung_snapshot: string; bindungsstatus: "digital_gebunden" | "papier_ausgehaendigt"; dateiname_snapshot: string | null; dokumenttyp_code: string | null; bereitgestellt_am_snapshot: string | null; bemerkung_snapshot: string | null };
export const canDispatchContracts = (club: ClubContext, session: BrowserSession) => callSupabaseRpc<boolean>(context(club, session), "kann_vertragsversand_ausfuehren", {});
export const prepareContractDispatch = (club: ClubContext, session: BrowserSession, communicationId: number) => callSupabaseRpc<number>(context(club, session), "prepare_contract_dispatch", { p_kommunikationsvorgang_id: communicationId, p_erneuter_versand_von_id: null });
export const createContractRedispatch = (club: ClubContext, session: BrowserSession, dispatchId: number) => callSupabaseRpc<number>(context(club, session), "create_contract_redispatch", { p_versandinstanz_id: dispatchId });
export const sendContractDispatch = (club: ClubContext, session: BrowserSession, dispatchId: number) => invokeEdgeFunction<{ success: boolean; message?: string }>(context(club, session), "kgv-send-contract-documents", { versandinstanz_id: dispatchId });
export const listContractDispatches = (club: ClubContext, session: BrowserSession, communicationId: number) => readSupabaseRest<Dispatch>(context(club, session), "versandinstanz", { select: "id,status,betreff_snapshot,text_snapshot,erneuter_versand_von_id,erstellt_am,versendet_am,letzter_fehler", kommunikationsvorgang_id: `eq.${communicationId}`, order: "erstellt_am.desc" });
export const listDispatchRecipients = (club: ClubContext, session: BrowserSession, dispatchId: number) => readSupabaseRest<DispatchRecipient>(context(club, session), "versandinstanz_empfaenger_snapshot", { select: "id,rolle,name_snapshot,email_snapshot,versand_erlaubt,nicht_versandfaehig_grund,status,versendet_am,letzter_fehler", versandinstanz_id: `eq.${dispatchId}`, order: "id.asc" });
export const listDispatchDocuments = (club: ClubContext, session: BrowserSession, dispatchId: number) => readSupabaseRest<DispatchDocument>(context(club, session), "versandinstanz_dokument_snapshot", { select: "sortierung,bezeichnung_snapshot,bindungsstatus,dateiname_snapshot,dokumenttyp_code,bereitgestellt_am_snapshot,bemerkung_snapshot", versandinstanz_id: `eq.${dispatchId}`, order: "sortierung.asc" });
