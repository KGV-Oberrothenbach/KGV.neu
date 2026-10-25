import { readSupabaseRest } from "../../lib/supabase/rest-client";
import { writeWithBrowserEditLock } from "../../services/concurrency/edit-lock-service";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";

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
export type ParcelOccupancy = ParcelAssignment & { id: number };
export type ParcelOverviewMember = {
  id: number;
  mitgliedsnummer: number | null;
  vorname: string | null;
  name: string | null;
  email: string | null;
  aktiv: boolean;
  hauptmitglied_id: number | null;
  role: string | null;
};

export const listParcelGardens = (club: ClubContext, session: BrowserSession) => readSupabaseRest<ParcelGarden>({ club, session }, "parzelle", {
  select: "id,garten_nr",
  order: "garten_nr.asc",
  limit: "1000",
});

export const listParcelAssignments = (club: ClubContext, session: BrowserSession, memberIds: number[]) => memberIds.length
  ? readSupabaseRest<ParcelAssignment>({ club, session }, "parzellen_belegung", {
      select: "parzelle_id,mitglied_id,von_datum,bis_datum",
      mitglied_id: `in.(${memberIds.join(",")})`,
      order: "von_datum.desc",
      limit: "3000",
    })
  : Promise.resolve([] as ParcelAssignment[]);

export const listLeaseParcelsForMember = (club: ClubContext, session: BrowserSession, memberId: number) => readSupabaseRest<ParcelAssignment>({ club, session }, "parzellen_belegung", {
  select: "parzelle_id,mitglied_id,von_datum,bis_datum",
  mitglied_id: `eq.${memberId}`,
  order: "von_datum.desc",
  limit: "3000",
});

export const getLeaseParcel = (club: ClubContext, session: BrowserSession, parcelId: number) => readSupabaseRest<LeaseParcel>({ club, session }, "parzelle", {
  select: "id,garten_nr,Anlage,flaeche_qm",
  id: `eq.${parcelId}`,
  limit: "1",
}).then((parcels) => parcels[0] ?? null);

export const listLeaseParcels = (club: ClubContext, session: BrowserSession, parcelIds: number[]) => parcelIds.length ? readSupabaseRest<LeaseParcel>({ club, session }, "parzelle", {
  select: "id,garten_nr,Anlage,flaeche_qm",
  id: `in.(${parcelIds.join(",")})`,
  order: "garten_nr.asc",
}) : Promise.resolve([] as LeaseParcel[]);

export const listParcelOverviewParcels = (club: ClubContext, session: BrowserSession) => readSupabaseRest<ParcelOverviewParcel>({ club, session }, "parzelle", {
  select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,rfid_strom,rfid_wasser,aktiv",
  limit: "1000",
});

export const listParcelOverviewParcelsByIds = (club: ClubContext, session: BrowserSession, parcelIds: number[]) => parcelIds.length
  ? readSupabaseRest<ParcelOverviewParcel>({ club, session }, "parzelle", {
      select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,rfid_strom,rfid_wasser,aktiv",
      id: `in.(${parcelIds.join(",")})`,
      limit: "1000",
    })
  : Promise.resolve([] as ParcelOverviewParcel[]);

export const listMemberParcelAssignments = (club: ClubContext, session: BrowserSession, memberId: number) => readSupabaseRest<ParcelOverviewAssignment>({ club, session }, "parzellen_belegung", {
  select: "id,parzelle_id,mitglied_id,von_datum,bis_datum,beendigungsgrund",
  mitglied_id: `eq.${memberId}`,
  order: "von_datum.desc",
  limit: "3000",
});

export const listMemberParcelOccupancies = (club: ClubContext, session: BrowserSession, memberId: number) => readSupabaseRest<ParcelOccupancy>({ club, session }, "parzellen_belegung", {
  select: "id,parzelle_id,mitglied_id,von_datum,bis_datum",
  mitglied_id: `eq.${memberId}`,
  order: "von_datum.desc",
  limit: "3000",
});

export const listParcelOccupancies = (club: ClubContext, session: BrowserSession) => readSupabaseRest<ParcelOccupancy>({ club, session }, "parzellen_belegung", {
  select: "id,parzelle_id,mitglied_id,von_datum,bis_datum",
  order: "von_datum.desc",
  limit: "3000",
});

export const updateParcelMasterData = (club: ClubContext, session: BrowserSession, parcelId: number, data: ParcelMasterDataUpdate) =>
  writeWithBrowserEditLock<ParcelOverviewParcel>({ club, session }, "parzelle", "PATCH", data, { id: `eq.${parcelId}` });

export const createParcelAssignment = (club: ClubContext, session: BrowserSession, data: ParcelAssignmentCreate) =>
  writeWithBrowserEditLock<ParcelOverviewAssignment>({ club, session }, "parzellen_belegung", "POST", data);

export const listParcelOverviewAssignments = (club: ClubContext, session: BrowserSession) => readSupabaseRest<ParcelOverviewAssignment>({ club, session }, "parzellen_belegung", {
  select: "id,parzelle_id,mitglied_id,von_datum,bis_datum,beendigungsgrund",
  order: "von_datum.desc",
  limit: "3000",
});

export const listParcelAssignmentsForParcel = (club: ClubContext, session: BrowserSession, parcelId: number) => readSupabaseRest<ParcelOverviewAssignment>({ club, session }, "parzellen_belegung", {
  select: "id,parzelle_id,mitglied_id,von_datum,bis_datum,beendigungsgrund",
  parzelle_id: `eq.${parcelId}`,
  order: "von_datum.desc",
  limit: "3000",
});

export const getParcelAssignment = (club: ClubContext, session: BrowserSession, assignmentId: number) => readSupabaseRest<ParcelOverviewAssignment>({ club, session }, "parzellen_belegung", {
  select: "id,parzelle_id,mitglied_id,von_datum,bis_datum,beendigungsgrund",
  id: `eq.${assignmentId}`,
  limit: "1",
}).then((assignments) => assignments[0] ?? null);

export const updateParcelAssignmentEnd = (club: ClubContext, session: BrowserSession, assignmentId: number, data: Pick<ParcelOverviewAssignment, "bis_datum" | "beendigungsgrund">) =>
  writeWithBrowserEditLock<ParcelOverviewAssignment>({ club, session }, "parzellen_belegung", "PATCH", data, { id: `eq.${assignmentId}` });

export const listParcelOverviewMembers = (club: ClubContext, session: BrowserSession) => readSupabaseRest<ParcelOverviewMember>({ club, session }, "mitglied", {
  select: "id,mitgliedsnummer,vorname,name,email,aktiv,hauptmitglied_id,role",
  order: "name.asc,vorname.asc",
  limit: "2000",
});
