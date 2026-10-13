import { readSupabaseRest } from "../../lib/supabase/rest-client";
import { writeWithBrowserEditLock } from "../../services/concurrency/edit-lock-service";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { listMeters, listMeterOverviewParcels, type Meter, type MeterOverviewParcel } from "../meters/meter-repository";
import { listMemberParcelAssignments, type ParcelOverviewAssignment } from "../parcels/parcel-repository";

export type MeterReading = {
  id: number;
  zaehler_id: number;
  stand: number;
  ablesedatum: string;
  art: string;
  freigegeben: boolean;
  pruefstatus: string;
};

export type MeterReadingInsert = Pick<MeterReading, "zaehler_id" | "stand" | "ablesedatum" | "art" | "freigegeben" | "pruefstatus">;

export const listReadingMeters = (club: ClubContext, session: BrowserSession): Promise<Meter[]> => listMeters(club, session);
export const listReadingParcels = (club: ClubContext, session: BrowserSession): Promise<MeterOverviewParcel[]> => listMeterOverviewParcels(club, session);
export const listReadingMetersForParcels = (club: ClubContext, session: BrowserSession, parcelIds: number[]) => parcelIds.length ? readSupabaseRest<Meter>({ club, session }, "zaehler", {
  select: "id,parzelle_id,medium,zaehlernummer,eingebaut_am,ausgebaut_am,eichfaellig_am,status",
  parzelle_id: `in.(${parcelIds.join(",")})`,
  order: "medium.asc,zaehlernummer.asc",
  limit: "1000",
}) : Promise.resolve([] as Meter[]);
export const listReadingParcelsByIds = (club: ClubContext, session: BrowserSession, parcelIds: number[]) => parcelIds.length ? readSupabaseRest<MeterOverviewParcel>({ club, session }, "parzelle", {
  select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv",
  id: `in.(${parcelIds.join(",")})`,
  limit: "1000",
}) : Promise.resolve([] as MeterOverviewParcel[]);
export const listReadingsForMeter = (club: ClubContext, session: BrowserSession, meterId: number) => readSupabaseRest<MeterReading>({ club, session }, "zaehler_ablesung", {
  select: "id,zaehler_id,stand,ablesedatum,art,freigegeben,pruefstatus",
  zaehler_id: `eq.${meterId}`,
  limit: "3000",
});
export const listMemberReadingAssignments = (club: ClubContext, session: BrowserSession, memberId: number): Promise<ParcelOverviewAssignment[]> => listMemberParcelAssignments(club, session, memberId);
export const getBooleanSetting = (club: ClubContext, session: BrowserSession, settingKey: string, fallback: boolean) => readSupabaseRest<{ bool_value: boolean }>({ club, session }, "app_setting", {
  select: "bool_value",
  setting_key: `eq.${settingKey}`,
  limit: "1",
}).then((rows) => rows[0]?.bool_value ?? fallback);
export const createMeterReading = (club: ClubContext, session: BrowserSession, input: MeterReadingInsert) => writeWithBrowserEditLock<MeterReading>({ club, session }, "zaehler_ablesung", "POST", input);
