import { BrowserSession } from "../../lib/supabase-auth";
import { listSeasons, type SeasonRecord } from "../../repositories/seasons/season-repository";

export function selectInitialSeasonFromList(seasons: SeasonRecord[], persistedId?: number | null): SeasonRecord | null {
  if (!seasons || seasons.length === 0) return null;
  const restored = typeof persistedId === "number" ? seasons.find((s) => s.id === persistedId) ?? null : null;
  if (restored) return restored;
  const currentYear = new Date().getFullYear();
  const byYear = seasons.find((s) => s.jahr === currentYear) ?? null;
  if (byYear) return byYear;
  return seasons[0] ?? null;
}

export async function loadAndSelectInitialSeason(session: BrowserSession, persistedId?: number | null): Promise<SeasonRecord | null> {
  const seasons = await listSeasons(session);
  return selectInitialSeasonFromList(seasons, persistedId);
}
