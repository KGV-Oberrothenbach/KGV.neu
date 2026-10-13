import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { Season } from "../../models/seasons/season";
import { createSeason, getSeason, listSeasons, updateSeason } from "../../repositories/seasons/season-repository";

export type SeasonSaveOptions = { existingSeasonId?: number };

function seasonYear(season: Pick<Season, "id" | "jahr">) {
  return season.jahr > 0 ? season.jahr : season.id;
}

export function canEditSeason(season: Pick<Season, "id" | "jahr"> | null | undefined, today = new Date()) {
  return Boolean(season) && seasonYear(season) >= today.getFullYear();
}

export function createNextSeasonProposal(seasons: Season[] | null | undefined, today = new Date()): Season {
  const currentYear = today.getFullYear();
  const previous = [...(seasons ?? [])].sort((left, right) => seasonYear(right) - seasonYear(left))[0];
  const nextYear = previous ? Math.max(seasonYear(previous) + 1, currentYear) : currentYear;

  return {
    id: nextYear,
    jahr: nextYear,
    pflichtstunden_soll: previous?.pflichtstunden_soll ?? 0,
    euro_pro_fehlstunde: previous?.euro_pro_fehlstunde ?? 25,
    bemerkung: previous?.bemerkung ?? null,
    pacht_pro_qm: previous?.pacht_pro_qm ?? null,
    mitgliedsbeitrag: previous?.mitgliedsbeitrag ?? null,
    mitgliedsbeitrag_nebenmitglied: previous?.mitgliedsbeitrag_nebenmitglied ?? null,
    aufnahmegebuehr: previous?.aufnahmegebuehr ?? null,
    gebuehr_bauantrag: previous?.gebuehr_bauantrag ?? null,
  };
}

export function normalizeSeasonForSave(season: Season): Season {
  const jahr = seasonYear(season);
  if (jahr < 1900 || jahr > 3000) throw new Error("Bitte ein gültiges Saisonjahr angeben.");

  return {
    ...season,
    id: jahr,
    jahr,
    bemerkung: season.bemerkung?.trim() || null,
  };
}

export function getSeasons(club: ClubContext, session: BrowserSession) {
  return listSeasons(club, session);
}

export async function saveSeason(club: ClubContext, session: BrowserSession, season: Season, options: SeasonSaveOptions = {}) {
  const normalized = normalizeSeasonForSave(season);
  if (options.existingSeasonId !== undefined) {
    if (options.existingSeasonId !== normalized.id) throw new Error("Eine bestehende Saison kann nicht in ein anderes Jahr umnummeriert werden.");
    if (!await getSeason(club, session, options.existingSeasonId)) throw new Error("Die zu aktualisierende Saison wurde nicht gefunden.");
    return updateSeason(club, session, options.existingSeasonId, normalized);
  }

  return createSeason(club, session, normalized);
}

export type { Season } from "../../models/seasons/season";
