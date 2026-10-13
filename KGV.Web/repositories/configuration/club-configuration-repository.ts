import { readSupabaseRest } from "../../lib/supabase/rest-client";
import { writeWithBrowserEditLock } from "../../services/concurrency/edit-lock-service";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type ClubConfigurationRecord } from "../../models/configuration/club-configuration";

const select = "id,vereinsname,kurzname,registerangabe,strasse,plz,ort,standard_email,standard_telefon,website,kontoinhaber,bankname,iban,bic,verwendungszweck_mitgliedsantrag,verwendungszweck_pachtvertrag,dokument_ort,standard_hinweistext,datenschutz_text,datenschutz_version,datenschutz_stand,aktiv";

export async function getActiveClubConfiguration(club: ClubContext, session: BrowserSession): Promise<ClubConfigurationRecord | null> {
  const rows = await readSupabaseRest<ClubConfigurationRecord>({ club, session }, "vereinskonfiguration", { select, aktiv: "eq.true", order: "updated_at.desc", limit: "1" });
  return rows[0] ?? null;
}

export const createClubConfiguration = (club: ClubContext, session: BrowserSession, payload: Omit<ClubConfigurationRecord, "id">) => writeWithBrowserEditLock<ClubConfigurationRecord>({ club, session }, "vereinskonfiguration", "POST", payload);
export const updateClubConfiguration = (club: ClubContext, session: BrowserSession, id: number, payload: Omit<ClubConfigurationRecord, "id">) => writeWithBrowserEditLock<ClubConfigurationRecord>({ club, session }, "vereinskonfiguration", "PATCH", payload, { id: `eq.${id}` });
