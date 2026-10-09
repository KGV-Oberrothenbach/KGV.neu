import { type BrowserSession, readSupabase, writeSupabase } from "../../lib/supabase-auth";
import { type WorkHour, type WorkHourHistory, type WorkHoursSummary } from "../../features/work-hours/work-hours-types";

export type HomeWorkHoursSummary = WorkHoursSummary;

const workHourSelect = "id,mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben,genehmigt_von,genehmigt_am";
const workHourHistorySelect = "id,arbeitsstunde_id,aktion,begruendung,geprueft_von,geprueft_am,vorher_snapshot,nachher_snapshot";
const summarySelect = "mitglied_id,hauptmitglied_id,saison_id,saison_jahr,regelgrund,ist_befreit,hat_wartungsvertrag,altersbefreit,eintritt_im_saisonjahr,eintritt_zweites_halbjahr,pflichtstunden_soll,geleistete_stunden,offene_stunden,euro_pro_fehlstunde,fehlbetrag";

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

export const updateOwnOpenWorkHour = (session: BrowserSession, workHourId: number, payload: OwnOpenWorkHourUpdate) => writeSupabase<WorkHour>(session, "arbeitsstunde", "PATCH", payload, {
  id: `eq.${workHourId}`,
});

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
