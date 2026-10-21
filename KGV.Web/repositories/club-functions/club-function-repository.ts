import type { BrowserSession } from "../../lib/supabase-auth";
import { deleteSupabaseRest, readSupabaseRest, writeSupabaseRest } from "../../lib/supabase/rest-client";
import type { ClubContext } from "../../models/auth/club";

export type ClubFunction = { id: number; schluessel: string; bezeichnung: string; aktiv: boolean; erlaubt_mehrfachbesetzung: boolean };
export type ClubFunctionAssignment = { id: number; vereinsfunktion_id: number; mitglied_id: number; gueltig_ab: string; gueltig_bis: string | null; sort_order: number };
export type ClubFunctionUse = { id: number; vereinsfunktion_id: number; verwendungszweck: string; sort_order: number; impressum_gruppe: string; impressum_gruppe_bezeichnung: string; impressum_gruppen_sort_order: number; impressum_ist_hauptkontakt: boolean; impressum_zeige_ladungsfaehige_anschrift: boolean };
export type ClubFunctionRepresentation = { id: number; vertretene_besetzung_id: number; vertreter_besetzung_id: number; gueltig_ab: string; gueltig_bis: string | null };
export type ClubFunctionMember = { id: number; vorname: string | null; name: string | null; email: string | null };
const context = (club: ClubContext, session: BrowserSession) => ({ club, session });

export const listClubFunctions = (club: ClubContext, session: BrowserSession) => readSupabaseRest<ClubFunction>(context(club, session), "vereinsfunktion", { select: "id,schluessel,bezeichnung,aktiv,erlaubt_mehrfachbesetzung", order: "aktiv.desc,bezeichnung.asc", limit: "2000" });
export const listClubFunctionAssignments = (club: ClubContext, session: BrowserSession) => readSupabaseRest<ClubFunctionAssignment>(context(club, session), "vereinsfunktion_besetzung", { select: "id,vereinsfunktion_id,mitglied_id,gueltig_ab,gueltig_bis,sort_order", order: "gueltig_ab.desc,sort_order.asc", limit: "5000" });
export const listClubFunctionUses = (club: ClubContext, session: BrowserSession) => readSupabaseRest<ClubFunctionUse>(context(club, session), "vereinsfunktion_verwendung", { select: "id,vereinsfunktion_id,verwendungszweck,sort_order,impressum_gruppe,impressum_gruppe_bezeichnung,impressum_gruppen_sort_order,impressum_ist_hauptkontakt,impressum_zeige_ladungsfaehige_anschrift", order: "verwendungszweck.asc,sort_order.asc", limit: "5000" });
export const listClubFunctionRepresentations = (club: ClubContext, session: BrowserSession) => readSupabaseRest<ClubFunctionRepresentation>(context(club, session), "vereinsfunktion_vertretung", { select: "id,vertretene_besetzung_id,vertreter_besetzung_id,gueltig_ab,gueltig_bis", order: "gueltig_ab.desc", limit: "5000" });
export const listClubFunctionMembers = (club: ClubContext, session: BrowserSession) => readSupabaseRest<ClubFunctionMember>(context(club, session), "mitglied", { select: "id,vorname,name,email", aktiv: "eq.true", is_demo: "eq.false", order: "name.asc,vorname.asc", limit: "5000" });
export const saveClubFunction = (club: ClubContext, session: BrowserSession, value: Omit<ClubFunction, "id">, id?: number) => id
  ? writeSupabaseRest<ClubFunction>(context(club, session), "vereinsfunktion", "PATCH", value, { id: `eq.${id}` })
  : writeSupabaseRest<ClubFunction>(context(club, session), "vereinsfunktion", "POST", value);
export const saveClubFunctionAssignment = (club: ClubContext, session: BrowserSession, value: Omit<ClubFunctionAssignment, "id">, id?: number) => id
  ? writeSupabaseRest<ClubFunctionAssignment>(context(club, session), "vereinsfunktion_besetzung", "PATCH", value, { id: `eq.${id}` })
  : writeSupabaseRest<ClubFunctionAssignment>(context(club, session), "vereinsfunktion_besetzung", "POST", value);
export const saveClubFunctionUse = (club: ClubContext, session: BrowserSession, value: Omit<ClubFunctionUse, "id">, id?: number) => id
  ? writeSupabaseRest<ClubFunctionUse>(context(club, session), "vereinsfunktion_verwendung", "PATCH", value, { id: `eq.${id}` })
  : writeSupabaseRest<ClubFunctionUse>(context(club, session), "vereinsfunktion_verwendung", "POST", value);
export const saveClubFunctionRepresentation = (club: ClubContext, session: BrowserSession, value: Omit<ClubFunctionRepresentation, "id">, id?: number) => id
  ? writeSupabaseRest<ClubFunctionRepresentation>(context(club, session), "vereinsfunktion_vertretung", "PATCH", value, { id: `eq.${id}` })
  : writeSupabaseRest<ClubFunctionRepresentation>(context(club, session), "vereinsfunktion_vertretung", "POST", value);
export const deleteClubFunctionUse = (club: ClubContext, session: BrowserSession, id: number) => deleteSupabaseRest(context(club, session), "vereinsfunktion_verwendung", { id: `eq.${id}` });
