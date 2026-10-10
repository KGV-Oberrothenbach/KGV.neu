import { callSupabaseRpc, type BrowserSession, readSupabase, writeSupabase } from "../../lib/supabase-auth";
import { type WorkHour, type WorkHourHistory, type WorkHoursSummary } from "../../features/work-hours/work-hours-types";

export type HomeWorkHoursSummary = WorkHoursSummary;

const workHourSelect = "id,mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben,genehmigt_von,genehmigt_am,arbeitseinsatz_anmeldung_id";
const workHourHistorySelect = "id,arbeitsstunde_id,aktion,begruendung,geprueft_von,geprueft_am,vorher_snapshot,nachher_snapshot";
const summarySelect = "mitglied_id,hauptmitglied_id,saison_id,jahr,saison_jahr,regelgrund,ist_befreit,hat_wartungsvertrag,wartungsvertrag_gutschrift_stunden,altersbefreit,eintritt_im_saisonjahr,eintritt_zweites_halbjahr,pflichtstunden_soll,geleistete_stunden,offene_stunden,euro_pro_fehlstunde,fehlbetrag";

export type OwnOpenWorkHourCreate = {
  mitglied_id: number;
  saison_id: number;
  datum: string;
  stunden: number;
  art_der_arbeit: string;
  status: "offen";
  freigegeben: false;
  genehmigt_von: null;
  genehmigt_am: null;
};

export type OwnOpenWorkHourUpdate = Pick<OwnOpenWorkHourCreate, "datum" | "stunden" | "art_der_arbeit">;

export type AdministrativeWorkHourCreate = {
  mitglied_id: number;
  saison_id: number;
  datum: string;
  stunden: number;
  art_der_arbeit: string;
  status: "genehmigt";
  freigegeben: true;
};

export type WorkHourReviewAction = "freigeben" | "ablehnen" | "korrigieren" | "loeschen";
export type WorkHourReviewPayload = {
  workHourId: number;
  action: WorkHourReviewAction;
  comment: string;
  date?: string;
  hours?: number;
  workType?: string;
};

export const listMemberWorkHoursForSeason = (session: BrowserSession, memberId: number, saisonId: number) => readSupabase<WorkHour>(session, "arbeitsstunde", {
  select: workHourSelect,
  mitglied_id: `eq.${memberId}`,
  saison_id: `eq.${saisonId}`,
  order: "datum.desc,id.desc",
  limit: "500",
});

export const listWorkHoursForReview = (session: BrowserSession) => readSupabase<WorkHour>(session, "arbeitsstunde", {
  select: workHourSelect,
  order: "datum.desc,id.desc",
  limit: "1000",
});

export const createOwnOpenWorkHour = (session: BrowserSession, payload: OwnOpenWorkHourCreate) => writeSupabase<WorkHour>(session, "arbeitsstunde", "POST", payload);

export const createAdministrativeWorkHour = (session: BrowserSession, payload: AdministrativeWorkHourCreate) => writeSupabase<WorkHour>(session, "arbeitsstunde", "POST", payload);

export const updateOwnOpenWorkHour = (session: BrowserSession, workHourId: number, payload: OwnOpenWorkHourUpdate) => writeSupabase<WorkHour>(session, "arbeitsstunde", "PATCH", payload, {
  id: `eq.${workHourId}`,
});

export const reviewWorkHour = (session: BrowserSession, payload: WorkHourReviewPayload) => callSupabaseRpc<unknown>(session, "review_arbeitsstunde", {
  p_arbeitsstunde_id: payload.workHourId,
  p_aktion: payload.action,
  p_begruendung: payload.comment,
  p_datum: payload.date ?? null,
  p_stunden: payload.hours ?? null,
  p_art_der_arbeit: payload.workType ?? null,
});

export const getLinkedWorkHour = (session: BrowserSession, registrationId: number) => readSupabase<WorkHour>(session, "arbeitsstunde", { select: workHourSelect, arbeitseinsatz_anmeldung_id: `eq.${registrationId}`, limit: "1" }).then((rows) => rows[0] ?? null);
export const submitWorkAssignmentWorkHour = (session: BrowserSession, registrationId: number, hours: number, workType: string) => callSupabaseRpc<WorkHour>(session, "submit_arbeitseinsatz_arbeitsstunde", { p_arbeitseinsatz_anmeldung_id: registrationId, p_stunden: hours, p_art_der_arbeit: workType });
export const confirmWorkAssignmentWorkHour = (session: BrowserSession, registrationId: number, hours: number, workType: string) => callSupabaseRpc<WorkHour>(session, "confirm_arbeitseinsatz_arbeitsstunde", { p_arbeitseinsatz_anmeldung_id: registrationId, p_stunden: hours, p_art_der_arbeit: workType });

export const listWorkHourHistory = (session: BrowserSession, workHourIds: number[]) => workHourIds.length
  ? readSupabase<WorkHourHistory>(session, "arbeitsstunde_pruefverlauf", {
    select: workHourHistorySelect,
    arbeitsstunde_id: `in.(${workHourIds.join(",")})`,
    order: "geprueft_am.desc,id.desc",
    limit: "1000",
  })
  : Promise.resolve([] as WorkHourHistory[]);

export const getWorkHoursSummary = (session: BrowserSession, memberId: number, saisonId: number) => readSupabase<WorkHoursSummary>(session, "v_pflichtstunden_uebersicht", {
  select: summarySelect,
  mitglied_id: `eq.${memberId}`,
  saison_id: `eq.${saisonId}`,
  limit: "1",
}).then((rows) => rows[0] ?? null);

export const getHomeWorkHours = (session: BrowserSession, memberId: number, saisonId: number | null, season: number) => readSupabase<HomeWorkHoursSummary>(session, "v_pflichtstunden_uebersicht", {
  select: summarySelect,
  hauptmitglied_id: `eq.${memberId}`,
  ...(saisonId ? { saison_id: `eq.${saisonId}` } : { saison_jahr: `eq.${season}` }),
  limit: "1",
});
