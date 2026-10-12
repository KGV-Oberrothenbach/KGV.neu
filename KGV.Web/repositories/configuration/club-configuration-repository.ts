import { type BrowserSession, readSupabase, writeSupabase } from "../../lib/supabase-auth";
import { type ClubConfigurationRecord } from "../../models/configuration/club-configuration";

const select = "id,vereinsname,kurzname,registerangabe,strasse,plz,ort,standard_email,standard_telefon,website,kontoinhaber,bankname,iban,bic,verwendungszweck_mitgliedsantrag,verwendungszweck_pachtvertrag,dokument_ort,standard_hinweistext,datenschutz_text,datenschutz_version,datenschutz_stand,aktiv";

export async function getActiveClubConfiguration(session: BrowserSession): Promise<ClubConfigurationRecord | null> {
  const rows = await readSupabase<ClubConfigurationRecord>(session, "vereinskonfiguration", { select, aktiv: "eq.true", order: "updated_at.desc", limit: "1" });
  return rows[0] ?? null;
}

export const createClubConfiguration = (session: BrowserSession, payload: Omit<ClubConfigurationRecord, "id">) => writeSupabase<ClubConfigurationRecord>(session, "vereinskonfiguration", "POST", payload);
export const updateClubConfiguration = (session: BrowserSession, id: number, payload: Omit<ClubConfigurationRecord, "id">) => writeSupabase<ClubConfigurationRecord>(session, "vereinskonfiguration", "PATCH", payload, { id: `eq.${id}` });
