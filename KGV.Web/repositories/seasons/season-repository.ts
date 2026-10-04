import { readSupabase, type BrowserSession } from "../../lib/supabase-auth";

export type Season = { id: number; jahr: number };

export function listSeasons(session: BrowserSession) {
  return readSupabase<Season>(session, "saison", { select: "id,jahr", order: "jahr.desc" });
}
