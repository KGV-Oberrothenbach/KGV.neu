import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { listYearEndReadingStatus, type YearEndReadingStatusRow } from "../../repositories/readings/year-end-reading-repository";

export type JeaReadinessIssue = {
  row: YearEndReadingStatusRow;
  status: string;
};

export type JeaReadinessWarning = {
  row: YearEndReadingStatusRow;
  message: "Pächterwechsel im Saisonjahr";
};

export type JeaReadiness = {
  year: number;
  ready: boolean;
  totalRequiredMeters: number;
  approvedMeters: number;
  blockingIssues: JeaReadinessIssue[];
  warnings: JeaReadinessWarning[];
  rows: YearEndReadingStatusRow[];
};

const approvedStatus = "JEA freigegeben";

export async function getJeaReadiness(club: ClubContext, session: BrowserSession, seasonYear: number): Promise<JeaReadiness> {
  const rows = await listYearEndReadingStatus(club, session, seasonYear);
  const blockingIssues = rows
    .filter((row) => row.status !== approvedStatus)
    .map((row) => ({ row, status: row.status }));
  const warnings = rows
    .filter((row) => row.pachtwechsel_im_jahr)
    .map((row) => ({ row, message: "Pächterwechsel im Saisonjahr" as const }));

  return {
    year: seasonYear,
    ready: blockingIssues.length === 0,
    totalRequiredMeters: rows.length,
    approvedMeters: rows.length - blockingIssues.length,
    blockingIssues,
    warnings,
    rows,
  };
}
