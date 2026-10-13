import { readSupabaseRest } from "../../lib/supabase/rest-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";

export type ImprintSlot = { id: number; slot_key: string; funktion: string; sort_order: number; mitglied_id: number | null };
export type ImprintMember = { id: number; vorname: string | null; name: string | null; email: string | null; aktiv: boolean; hauptmitglied_id: number | null; auth_user_id?: string | null; geburtsdatum: string | null; adresse: string | null; plz: string | null; ort: string | null; telefon: string | null; handy: string | null; whatsapp_einwilligung: boolean; mitglied_seit: string | null; mitglied_ende: string | null; bemerkung: string | null };

export async function listImprintSlots(club: ClubContext, session: BrowserSession): Promise<ImprintSlot[]> {
  return readSupabaseRest<ImprintSlot>({ club, session }, "impressum_funktion_slot", { select: "id,slot_key,funktion,sort_order,mitglied_id", order: "sort_order.asc", limit: "100" });
}

export async function listImprintMembers(club: ClubContext, session: BrowserSession): Promise<ImprintMember[]> {
  return readSupabaseRest<ImprintMember>({ club, session }, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,geburtsdatum,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,mitglied_seit,mitglied_ende,bemerkung", limit: "1000" });
}
