import { readSupabaseRest, writeSupabaseRest } from "../../lib/supabase/rest-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { CommunicationItem } from "../../models/communication/communication-item";

const select = "id,vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,revision,signatur_revision,signatur_gestartet_am,empfaenger_email,betreff,mailtext,versendet_am,fehlertext,gedruckt_am,archiviert_am,zugewiesen_an,erstellt_am,erstellt_von,bearbeitet_am,bearbeitet_von,mitglied:mitglied_id(id,vorname,name),parzelle:parzelle_id(id,garten_nr)";
const contractSelect = `${select},entwurf_daten`;

export function listCommunicationItems(club: ClubContext, session: BrowserSession, query: Record<string, string>) {
  return readSupabaseRest<CommunicationItem>({ club, session }, "kommunikationsvorgang", { select, order: "bearbeitet_am.desc,erstellt_am.desc,id.desc", limit: "500", ...query });
}
export const getCommunicationContractDraft = (club: ClubContext, session: BrowserSession, id: number) => readSupabaseRest<CommunicationItem & { entwurf_daten: unknown }>({ club, session }, "kommunikationsvorgang", { select: contractSelect, id: `eq.${id}`, limit: "1" }).then((rows) => rows[0] ?? null);
export const findActivePreparedContract = (club: ClubContext, session: BrowserSession, documentType: "mitgliedsantrag" | "pachtvertrag", memberId: number, parcelId: number | null) => readSupabaseRest<CommunicationItem & { entwurf_daten: unknown }>({ club, session }, "kommunikationsvorgang", { select: contractSelect, vorgangstyp: "eq.vertrag_signatur", dokumenttyp: `eq.${documentType}`, mitglied_id: `eq.${memberId}`, parzelle_id: parcelId === null ? "is.null" : `eq.${parcelId}`, status: "in.(vorbereitet,zur_signatur,letzte_pruefung)", limit: "2" }).then((rows) => { if (rows.length > 1) throw new Error("Es wurden mehrere aktive Vertragsvorgänge gefunden."); return rows[0] ?? null; });
export const createPreparedContract = (club: ClubContext, session: BrowserSession, payload: Record<string, unknown>) => writeSupabaseRest<CommunicationItem>({ club, session }, "kommunikationsvorgang", "POST", payload);
export async function updatePreparedContractDraft(club: ClubContext, session: BrowserSession, id: number, revision: number, userId: string, entwurf_daten: unknown) { const rows = await writeSupabaseRest<CommunicationItem>({ club, session }, "kommunikationsvorgang", "PATCH", { entwurf_daten, revision: revision + 1 }, { id: `eq.${id}`, revision: `eq.${revision}`, status: "eq.vorbereitet", signatur_gestartet_am: "is.null", signatur_revision: "is.null", zugewiesen_an: `eq.${userId}` }); if (rows.length !== 1) throw new Error("Der Vertragsentwurf wurde inzwischen geändert oder ist nicht mehr zur Bearbeitung freigegeben. Bitte laden Sie den Vorgang erneut."); return rows[0]; }
export async function startPreparedContractSignature(club: ClubContext, session: BrowserSession, id: number, revision: number, userId: string) { const rows = await writeSupabaseRest<CommunicationItem>({ club, session }, "kommunikationsvorgang", "PATCH", { status: "zur_signatur", signatur_gestartet_am: new Date().toISOString() }, { id: `eq.${id}`, revision: `eq.${revision}`, status: "eq.vorbereitet", signatur_gestartet_am: "is.null", signatur_revision: "is.null", zugewiesen_an: `eq.${userId}` }); if (rows.length !== 1) throw new Error("Der Vertragsvorgang konnte nicht zur Signatur gestartet werden. Er wurde möglicherweise inzwischen geändert oder ist nicht mehr freigegeben. Bitte laden Sie den Vorgang erneut."); return rows[0]; }
export async function completePreparedContract(club: ClubContext, session: BrowserSession, id: number, revision: number, userId: string) { const rows = await writeSupabaseRest<CommunicationItem>({ club, session }, "kommunikationsvorgang", "PATCH", { status: "erledigt" }, { id: `eq.${id}`, status: "eq.zur_signatur", revision: `eq.${revision}`, signatur_revision: `eq.${revision}`, zugewiesen_an: `eq.${userId}` }); if (rows.length !== 1) throw new Error("Der Kommunikationsvorgang konnte nicht abgeschlossen werden."); return rows[0]; }

export async function claimCommunicationItem(club: ClubContext, session: BrowserSession, id: number, userId: string) {
  const rows = await writeSupabaseRest<CommunicationItem>({ club, session }, "kommunikationsvorgang", "PATCH", { zugewiesen_an: userId }, { id: `eq.${id}`, zugewiesen_an: "is.null" });
  if (rows.length !== 1) throw new Error("Dieser Vorgang wurde zwischenzeitlich von einer anderen Person übernommen.");
  return rows[0];
}

export async function releaseCommunicationItem(club: ClubContext, session: BrowserSession, id: number, userId: string) {
  const rows = await writeSupabaseRest<CommunicationItem>({ club, session }, "kommunikationsvorgang", "PATCH", { zugewiesen_an: null }, { id: `eq.${id}`, zugewiesen_an: `eq.${userId}` });
  if (rows.length !== 1) throw new Error("Der Vorgang ist nicht mehr dir zugewiesen.");
  return rows[0];
}
