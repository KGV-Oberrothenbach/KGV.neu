import { readSupabaseRest } from "../../lib/supabase/rest-client";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { MembershipBoardAuthorizationSource, MembershipBoardReview, MembershipBoardVote } from "../../models/communication/membership-board-review";

type VoteHistoryRow = {
  stimme: "ja" | "nein";
  nein_begruendung: string | null;
  abgegeben_am: string | null;
  version: number;
};

type SnapshotPersonRow = {
  id: number;
  mitglied_id: number;
  name_snapshot: string;
  stimme: VoteHistoryRow[] | null;
};

const context = (club: ClubContext, session: BrowserSession) => ({ club, session });
export const listMembershipBoardReviews = (club: ClubContext, session: BrowserSession) => readSupabaseRest<MembershipBoardReview>(context(club, session), "mitgliedsaufnahme_vorstand_liste", { select: "*", order: "gestartet_am.desc", limit: "200" });
export const listMembershipBoardVotes = async (club: ClubContext, session: BrowserSession, reviewId: number): Promise<MembershipBoardVote[]> => {
  const rows = await readSupabaseRest<SnapshotPersonRow>(context(club, session), "mitgliedsaufnahme_abstimmung_snapshot_person", { select: "id,mitglied_id,name_snapshot,stimme:mitgliedsaufnahme_abstimmung_stimme(stimme,nein_begruendung,abgegeben_am,version)", vorstandsbearbeitung_id: `eq.${reviewId}`, order: "name_snapshot.asc" });
  const ids = rows.map(row => row.id).filter(Number.isInteger);
  const sources = ids.length ? await readSupabaseRest<MembershipBoardAuthorizationSource>(context(club, session), "mitgliedsaufnahme_abstimmung_snapshot_berechtigung", { select: "snapshot_person_id,berechtigungsart,funktionsbezeichnung_snapshot,vertretene_funktionsbezeichnung_snapshot", snapshot_person_id: `in.(${ids.join(",")})`, order: "id.asc" }) : [];
  return rows.map(row => { const vote = [...(row.stimme ?? [])].sort((a, b) => b.version - a.version)[0]; return { person_id: row.id, mitglied_id: row.mitglied_id, name_snapshot: row.name_snapshot, stimme: vote?.stimme ?? null, nein_begruendung: vote?.nein_begruendung ?? null, abgegeben_am: vote?.abgegeben_am ?? null, berechtigungsquellen: sources.filter(source => source.snapshot_person_id === row.id) }; });
};
export const voteMembershipBoardReview = (club: ClubContext, session: BrowserSession, id: number, vote: "ja" | "nein", reason: string | null) => callSupabaseRpc<number>(context(club, session), "stimme_mitgliedsaufnahme_ab", { p_bearbeitung_id: id, p_stimme: vote, p_nein_begruendung: reason });
export const decideMembershipBoardReview = (club: ClubContext, session: BrowserSession, id: number, result: "annehmen" | "ablehnen", path: "einstimmig" | "vorstandssitzung", meetingDate: string | null, reason: string | null, admissionDate: string | null) => callSupabaseRpc<void>(context(club, session), "entscheide_mitgliedsaufnahme", { p_bearbeitung_id: id, p_ergebnis: result, p_weg: path, p_sitzungsdatum: meetingDate, p_begruendung: reason, p_aufnahmedatum: admissionDate });
export const canDecideMembershipBoardReview = (club: ClubContext, session: BrowserSession) => callSupabaseRpc<boolean>(context(club, session), "kann_mitgliedsaufnahme_entscheiden", {});
