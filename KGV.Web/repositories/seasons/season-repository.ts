import { readSupabase, type BrowserSession } from "../../lib/supabase-auth";

export type Season = {
  id: number;
  jahr: number;
  mitgliedsbeitrag: number | null;
  mitgliedsbeitrag_nebenmitglied: number | null;
  aufnahmegebuehr: number | null;
};

export function listSeasons(session: BrowserSession) {
  return readSupabase<Season>(session, "saison", {
    select: "id,jahr,mitgliedsbeitrag,mitgliedsbeitrag_nebenmitglied,aufnahmegebuehr",
    order: "jahr.desc",
  });
}
