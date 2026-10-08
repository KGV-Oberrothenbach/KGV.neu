import { type BrowserSession, readSupabase, writeSupabase } from "../../lib/supabase-auth";
import { type Meter } from "../meters/meter-repository";
import { type ParcelOverviewAssignment, type ParcelOverviewMember } from "../parcels/parcel-repository";

export type ReviewReadingRecord = {
  id: number;
  zaehler_id: number;
  stand: number;
  ablesedatum: string;
  art: string;
  freigegeben: boolean;
  pruefstatus: string | null;
  pruefkommentar: string | null;
  geprueft_von: number | null;
  geprueft_am: string | null;
  foto_pfad: string | null;
  foto_drive_file_id: string | null;
  foto_dateiname: string | null;
};

export type ReviewParcel = { id: number; garten_nr: string; Anlage: string };
export type ReviewMutation = Pick<ReviewReadingRecord, "ablesedatum" | "stand" | "freigegeben" | "pruefstatus" | "pruefkommentar" | "geprueft_von" | "geprueft_am">;

export const listReviewReadings = (session: BrowserSession) => readSupabase<ReviewReadingRecord>(session, "zaehler_ablesung", {
  select: "id,zaehler_id,stand,ablesedatum,art,freigegeben,pruefstatus,pruefkommentar,geprueft_von,geprueft_am,foto_pfad,foto_drive_file_id,foto_dateiname",
  order: "ablesedatum.desc",
  limit: "3000",
});

export const getReviewReading = (session: BrowserSession, readingId: number) => readSupabase<ReviewReadingRecord>(session, "zaehler_ablesung", {
  select: "id,zaehler_id,stand,ablesedatum,art,freigegeben,pruefstatus,pruefkommentar,geprueft_von,geprueft_am,foto_pfad,foto_drive_file_id,foto_dateiname",
  id: `eq.${readingId}`,
  limit: "1",
}).then((rows) => rows[0] ?? null);

export const listReviewAssignments = (session: BrowserSession) => readSupabase<ParcelOverviewAssignment>(session, "parzellen_belegung", {
  select: "id,parzelle_id,mitglied_id,von_datum,bis_datum,beendigungsgrund",
  order: "von_datum.desc",
  limit: "3000",
});

export const listReviewMembers = (session: BrowserSession) => readSupabase<ParcelOverviewMember>(session, "mitglied", {
  select: "id,vorname,name,email,aktiv,hauptmitglied_id,role",
  limit: "2000",
});

export const conditionalUpdateReviewReading = (session: BrowserSession, readingId: number, mutation: ReviewMutation) => writeSupabase<ReviewReadingRecord>(session, "zaehler_ablesung", "PATCH", mutation, {
  id: `eq.${readingId}`,
  freigegeben: "eq.false",
  pruefstatus: "eq.eingereicht",
});

export type ReviewReferenceData = { meters: Meter[]; parcels: ReviewParcel[]; assignments: ParcelOverviewAssignment[]; members: ParcelOverviewMember[] };
