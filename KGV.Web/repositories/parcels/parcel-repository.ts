import { type BrowserSession, readSupabase, writeSupabase } from "../../lib/supabase-auth";

export type ParcelGarden = { id: number; garten_nr: string };
export type ParcelAssignment = { parzelle_id: number; mitglied_id: number; von_datum: string | null; bis_datum: string | null };
export type LeaseParcel = { id: number; garten_nr: string | null; Anlage: string | null; flaeche_qm: number | null };
export type ParcelOverviewParcel = {
  id: number;
  garten_nr: string;
  Anlage: string;
  flaeche_qm: number | null;
  hat_strom: boolean;
  hat_wasser: boolean;
  rfid_strom: string | null;
  rfid_wasser: string | null;
  aktiv: boolean;
};
export type ParcelMasterDataUpdate = Pick<ParcelOverviewParcel, "flaeche_qm" | "hat_strom" | "hat_wasser">;
export type ParcelAssignmentCreate = { parzelle_id: number; mitglied_id: number; von_datum: string; bis_datum: null };
export type ParcelOverviewAssignment = ParcelAssignment & { id: number; beendigungsgrund: string | null };
export type ParcelOverviewMember = {
  id: number;
  vorname: string | null;
  name: string | null;
  email: string | null;
  aktiv: boolean;
  hauptmitglied_id: number | null;
  role: string | null;
};

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

export const listParcelOverviewParcels = (session: BrowserSession) => readSupabase<ParcelOverviewParcel>(session, "parzelle", {
  select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,rfid_strom,rfid_wasser,aktiv",
  limit: "1000",
});

export const listParcelOverviewParcelsByIds = (session: BrowserSession, parcelIds: number[]) => parcelIds.length
  ? readSupabase<ParcelOverviewParcel>(session, "parzelle", {
      select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,rfid_strom,rfid_wasser,aktiv",
      id: `in.(${parcelIds.join(",")})`,
      limit: "1000",
    })
  : Promise.resolve([] as ParcelOverviewParcel[]);

export const listMemberParcelAssignments = (session: BrowserSession, memberId: number) => readSupabase<ParcelOverviewAssignment>(session, "parzellen_belegung", {
  select: "id,parzelle_id,mitglied_id,von_datum,bis_datum,beendigungsgrund",
  mitglied_id: `eq.${memberId}`,
  order: "von_datum.desc",
  limit: "3000",
});

export const updateParcelMasterData = (session: BrowserSession, parcelId: number, data: ParcelMasterDataUpdate) =>
  writeSupabase<ParcelOverviewParcel>(session, "parzelle", "PATCH", data, { id: `eq.${parcelId}` });

export const createParcelAssignment = (session: BrowserSession, data: ParcelAssignmentCreate) =>
  writeSupabase<ParcelOverviewAssignment>(session, "parzellen_belegung", "POST", data);

export const listParcelOverviewAssignments = (session: BrowserSession) => readSupabase<ParcelOverviewAssignment>(session, "parzellen_belegung", {
  select: "id,parzelle_id,mitglied_id,von_datum,bis_datum,beendigungsgrund",
  order: "von_datum.desc",
  limit: "3000",
});

export const listParcelAssignmentsForParcel = (session: BrowserSession, parcelId: number) => readSupabase<ParcelOverviewAssignment>(session, "parzellen_belegung", {
  select: "id,parzelle_id,mitglied_id,von_datum,bis_datum,beendigungsgrund",
  parzelle_id: `eq.${parcelId}`,
  order: "von_datum.desc",
  limit: "3000",
});

export const getParcelAssignment = (session: BrowserSession, assignmentId: number) => readSupabase<ParcelOverviewAssignment>(session, "parzellen_belegung", {
  select: "id,parzelle_id,mitglied_id,von_datum,bis_datum,beendigungsgrund",
  id: `eq.${assignmentId}`,
  limit: "1",
}).then((assignments) => assignments[0] ?? null);

export const updateParcelAssignmentEnd = (session: BrowserSession, assignmentId: number, data: Pick<ParcelOverviewAssignment, "bis_datum" | "beendigungsgrund">) =>
  writeSupabase<ParcelOverviewAssignment>(session, "parzellen_belegung", "PATCH", data, { id: `eq.${assignmentId}` });

export const listParcelOverviewMembers = (session: BrowserSession) => readSupabase<ParcelOverviewMember>(session, "mitglied", {
  select: "id,vorname,name,email,aktiv,hauptmitglied_id,role",
  order: "name.asc,vorname.asc",
  limit: "2000",
});
