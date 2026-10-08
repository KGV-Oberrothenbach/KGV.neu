import { type BrowserSession, readSupabase, writeSupabase } from "../../lib/supabase-auth";
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

export const listReadingMeters = (session: BrowserSession): Promise<Meter[]> => listMeters(session);
export const listReadingParcels = (session: BrowserSession): Promise<MeterOverviewParcel[]> => listMeterOverviewParcels(session);
export const listReadingMetersForParcels = (session: BrowserSession, parcelIds: number[]) => parcelIds.length ? readSupabase<Meter>(session, "zaehler", {
  select: "id,parzelle_id,medium,zaehlernummer,eingebaut_am,ausgebaut_am,eichfaellig_am,status",
  parzelle_id: `in.(${parcelIds.join(",")})`,
  order: "medium.asc,zaehlernummer.asc",
  limit: "1000",
}) : Promise.resolve([] as Meter[]);
export const listReadingParcelsByIds = (session: BrowserSession, parcelIds: number[]) => parcelIds.length ? readSupabase<MeterOverviewParcel>(session, "parzelle", {
  select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv",
  id: `in.(${parcelIds.join(",")})`,
  limit: "1000",
}) : Promise.resolve([] as MeterOverviewParcel[]);
export const listReadingsForMeter = (session: BrowserSession, meterId: number) => readSupabase<MeterReading>(session, "zaehler_ablesung", {
  select: "id,zaehler_id,stand,ablesedatum,art,freigegeben,pruefstatus",
  zaehler_id: `eq.${meterId}`,
  limit: "3000",
});
export const listMemberReadingAssignments = (session: BrowserSession, memberId: number): Promise<ParcelOverviewAssignment[]> => listMemberParcelAssignments(session, memberId);
export const getBooleanSetting = (session: BrowserSession, settingKey: string, fallback: boolean) => readSupabase<{ bool_value: boolean }>(session, "app_setting", {
  select: "bool_value",
  setting_key: `eq.${settingKey}`,
  limit: "1",
}).then((rows) => rows[0]?.bool_value ?? fallback);
export const createMeterReading = (session: BrowserSession, input: MeterReadingInsert) => writeSupabase<MeterReading>(session, "zaehler_ablesung", "POST", input);
