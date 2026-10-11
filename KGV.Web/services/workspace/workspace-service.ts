export type WorkspaceSeason = { id: number; jahr: number };

export function selectInitialSeasonFromList(seasons: WorkspaceSeason[], persistedSeasonId: number | null): WorkspaceSeason | null {
  return seasons.find((season) => season.jahr === new Date().getFullYear())
    ?? seasons.find((season) => season.id === persistedSeasonId)
    ?? seasons[0]
    ?? null;
}
