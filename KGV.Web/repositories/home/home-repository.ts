import { type BrowserSession, callSupabaseRpc, readSupabase } from "../../lib/supabase-auth";

export type WorkAssignmentRegistration = { id: number; arbeitseinsatz_id: number; mitglied_id: number; status: "angemeldet" | "abgesagt" | "teilgenommen" | "nicht_erschienen"; bemerkung: string | null; angemeldet_am: string; updated_at: string };
export type HomeWorkAssignment = { id: number; titel: string | null; beschreibung: string | null; datum: string; start_uhrzeit: string | null; end_uhrzeit: string | null; treffpunkt: string | null; max_teilnehmer: number | null; stunden_wert: number; sichtbar_ab: string | null; sichtbar_bis: string | null; anmeldung_bis: string | null; angemeldet_count: number; freie_plaetze: number | null };
export type HomeAppointment = { id: number; titel: string | null; beschreibung: string | null; datum: string; start_uhrzeit: string | null; end_uhrzeit: string | null; sichtbar_ab: string | null; sichtbar_bis: string | null };
export type HomeAnnouncement = { id: number; titel: string | null; inhalt_html: string | null; sichtbar_ab: string | null; sichtbar_bis: string | null; created_at: string | null };
export type HomeWorkHoursSummary = { hauptmitglied_id: number; saison_id: number; saison_jahr: number; pflichtstunden_soll: number | null; geleistete_stunden: number | null; offene_stunden: number | null; hat_wartungsvertrag: boolean; altersbefreit: boolean; ist_befreit: boolean; regelgrund: string | null };

export function listHomeWorkAssignments(session: BrowserSession, today: string) {
  return readSupabase<HomeWorkAssignment>(session, "v_startseite_arbeitseinsatz", { select: "id,titel,beschreibung,datum,start_uhrzeit,end_uhrzeit,treffpunkt,max_teilnehmer,stunden_wert,sichtbar_ab,sichtbar_bis,anmeldung_bis,angemeldet_count,freie_plaetze", datum: `gte.${today}`, order: "datum.asc,start_uhrzeit.asc", limit: "30" });
}

export function listHomeAppointments(session: BrowserSession, today: string) {
  return readSupabase<HomeAppointment>(session, "v_startseite_termine", { select: "id,titel,beschreibung,datum,start_uhrzeit,end_uhrzeit,sichtbar_ab,sichtbar_bis", datum: `gte.${today}`, order: "datum.asc,start_uhrzeit.asc", limit: "30" });
}

export function listHomeAnnouncements(session: BrowserSession) {
  return readSupabase<HomeAnnouncement>(session, "v_startseite_bekanntmachungen", { select: "id,titel,inhalt_html,sichtbar_ab,sichtbar_bis,created_at", order: "created_at.desc", limit: "30" });
}

export function getHomeWorkHours(session: BrowserSession, memberId: number, saisonId: number | null, season: number) {
  return readSupabase<HomeWorkHoursSummary>(session, "v_pflichtstunden_uebersicht", { select: "hauptmitglied_id,saison_id,saison_jahr,pflichtstunden_soll,geleistete_stunden,offene_stunden,hat_wartungsvertrag,altersbefreit,ist_befreit,regelgrund", hauptmitglied_id: `eq.${memberId}`, ...(saisonId ? { saison_id: `eq.${saisonId}` } : { saison_jahr: `eq.${season}` }), limit: "1" });
}

export function listHomeRegistrations(session: BrowserSession, memberId: number) {
  return readSupabase<WorkAssignmentRegistration>(session, "arbeitseinsatz_anmeldung", { select: "id,arbeitseinsatz_id,mitglied_id,status,bemerkung,angemeldet_am,updated_at", mitglied_id: `eq.${memberId}`, order: "angemeldet_am.desc", limit: "200" });
}

export function signUpForHomeWorkAssignment(session: BrowserSession, assignmentId: number, memberId: number) {
  return callSupabaseRpc(session, "sign_up_for_arbeitseinsatz", { p_arbeitseinsatz_id: assignmentId, p_mitglied_id: memberId });
}

export function signOffFromHomeWorkAssignment(session: BrowserSession, assignmentId: number, memberId: number) {
  return callSupabaseRpc(session, "sign_off_from_arbeitseinsatz", { p_arbeitseinsatz_id: assignmentId, p_mitglied_id: memberId });
}
