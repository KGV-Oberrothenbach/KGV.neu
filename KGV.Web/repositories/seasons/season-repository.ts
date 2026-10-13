import { readSupabaseRest } from "../../lib/supabase/rest-client";
import { writeWithBrowserEditLock } from "../../services/concurrency/edit-lock-service";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import type { Season } from "../../models/seasons/season";

const seasonSelect = "id,jahr,pflichtstunden_soll,euro_pro_fehlstunde,bemerkung,pacht_pro_qm,mitgliedsbeitrag,mitgliedsbeitrag_nebenmitglied,aufnahmegebuehr,gebuehr_bauantrag";

export function listSeasons(club: ClubContext, session: BrowserSession) {
  return readSupabaseRest<Season>({ club, session }, "saison", {
    select: seasonSelect,
    order: "jahr.desc",
  });
}

export async function getSeason(club: ClubContext, session: BrowserSession, id: number) {
  const seasons = await readSupabaseRest<Season>({ club, session }, "saison", { select: seasonSelect, id: `eq.${id}`, limit: "1" });
  return seasons[0] ?? null;
}

export async function createSeason(club: ClubContext, session: BrowserSession, season: Season) {
  const seasons = await writeWithBrowserEditLock<Season>({ club, session }, "saison", "POST", season);
  return seasons[0] ?? season;
}

export async function updateSeason(club: ClubContext, session: BrowserSession, id: number, season: Season) {
  const seasons = await writeWithBrowserEditLock<Season>({ club, session }, "saison", "PATCH", season, { id: `eq.${id}` });
  return seasons[0] ?? season;
}

export type { Season } from "../../models/seasons/season";
