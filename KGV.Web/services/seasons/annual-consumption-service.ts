import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { listAnnualConsumptionSegments, type AnnualConsumptionSegment } from "../../repositories/readings/annual-consumption-repository";

export type AnnualConsumption = {
  year: number;
  segments: AnnualConsumptionSegment[];
  calculableSegments: AnnualConsumptionSegment[];
  issues: AnnualConsumptionSegment[];
  segmentsByMedium: Record<string, AnnualConsumptionSegment[]>;
};

export async function getAnnualConsumption(club: ClubContext, session: BrowserSession, seasonYear: number): Promise<AnnualConsumption> {
  const segments = await listAnnualConsumptionSegments(club, session, seasonYear);
  const calculableSegments = segments.filter((segment) => segment.berechenbar);
  const segmentsByMedium = calculableSegments.reduce<Record<string, AnnualConsumptionSegment[]>>((grouped, segment) => {
    (grouped[segment.medium] ??= []).push(segment);
    return grouped;
  }, {});

  return {
    year: seasonYear,
    segments,
    calculableSegments,
    issues: segments.filter((segment) => !segment.berechenbar),
    segmentsByMedium,
  };
}
