import { BrowserSession, readSupabase } from "../../lib/supabase-auth";

export type SeasonRecord = { id: number; jahr: number };

export async function listSeasons(session: BrowserSession): Promise<SeasonRecord[]> {
  // Liefert Saisons absteigend nach Jahr wie bisher.
  return readSupabase<SeasonRecord>(session, "saison", { select: "id,jahr", order: "jahr.desc" });
}
