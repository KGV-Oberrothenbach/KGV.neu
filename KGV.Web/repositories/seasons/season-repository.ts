import { readSupabase, writeSupabase, type BrowserSession } from "../../lib/supabase-auth";
import type { Season } from "../../models/seasons/season";

const seasonSelect = "id,jahr,pflichtstunden_soll,euro_pro_fehlstunde,bemerkung,pacht_pro_qm,mitgliedsbeitrag,mitgliedsbeitrag_nebenmitglied,aufnahmegebuehr,gebuehr_bauantrag";

export function listSeasons(session: BrowserSession) {
  return readSupabase<Season>(session, "saison", {
    select: seasonSelect,
    order: "jahr.desc",
  });
}

export async function getSeason(session: BrowserSession, id: number) {
  const seasons = await readSupabase<Season>(session, "saison", { select: seasonSelect, id: `eq.${id}`, limit: "1" });
  return seasons[0] ?? null;
}

export async function createSeason(session: BrowserSession, season: Season) {
  const seasons = await writeSupabase<Season>(session, "saison", "POST", season);
  return seasons[0] ?? season;
}

export async function updateSeason(session: BrowserSession, id: number, season: Season) {
  const seasons = await writeSupabase<Season>(session, "saison", "PATCH", season, { id: `eq.${id}` });
  return seasons[0] ?? season;
}

export type { Season } from "../../models/seasons/season";
