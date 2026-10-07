import { type BrowserSession, readSupabase } from "../../lib/supabase-auth";

export type ParcelGarden = { id: number; garten_nr: string };
export type ParcelAssignment = { parzelle_id: number; mitglied_id: number; von_datum: string | null; bis_datum: string | null };
export type LeaseParcel = { id: number; garten_nr: string | null; Anlage: string | null; flaeche_qm: number | null };

export const listParcelGardens = (session: BrowserSession) => readSupabase<ParcelGarden>(session, "parzelle", {
  select: "id,garten_nr",
  order: "garten_nr.asc",
  limit: "1000",
});

export const listParcelAssignments = (session: BrowserSession, memberIds: number[]) => memberIds.length
  ? readSupabase<ParcelAssignment>(session, "parzellen_belegung", {
      select: "parzelle_id,mitglied_id,von_datum,bis_datum",
      mitglied_id: `in.(${memberIds.join(",")})`,
      order: "von_datum.desc",
      limit: "3000",
    })
  : Promise.resolve([] as ParcelAssignment[]);

export const listLeaseParcelsForMember = (session: BrowserSession, memberId: number) => readSupabase<ParcelAssignment>(session, "parzellen_belegung", {
  select: "parzelle_id,mitglied_id,von_datum,bis_datum",
  mitglied_id: `eq.${memberId}`,
  order: "von_datum.desc",
  limit: "3000",
});

export const getLeaseParcel = (session: BrowserSession, parcelId: number) => readSupabase<LeaseParcel>(session, "parzelle", {
  select: "id,garten_nr,Anlage,flaeche_qm",
  id: `eq.${parcelId}`,
  limit: "1",
}).then((parcels) => parcels[0] ?? null);

export const listLeaseParcels = (session: BrowserSession, parcelIds: number[]) => parcelIds.length ? readSupabase<LeaseParcel>(session, "parzelle", {
  select: "id,garten_nr,Anlage,flaeche_qm",
  id: `in.(${parcelIds.join(",")})`,
  order: "garten_nr.asc",
}) : Promise.resolve([] as LeaseParcel[]);
