import { type BrowserSession } from "../../lib/supabase-auth";
import {
  getWorkHoursSummary,
  listMemberWorkHoursForSeason,
  listWorkHourHistory,
  listWorkHoursForReview,
} from "../../repositories/work-hours/work-hours-repository";

export async function loadMemberWorkHoursWorkspace(session: BrowserSession, memberId: number, saisonId: number) {
  const [workHours, summary] = await Promise.all([
    listMemberWorkHoursForSeason(session, memberId, saisonId),
    getWorkHoursSummary(session, memberId, saisonId),
  ]);
  const history = await listWorkHourHistory(session, workHours.map((item) => item.id));
  return { workHours, summary, history };
}

export const loadWorkHoursReview = (session: BrowserSession) => listWorkHoursForReview(session);

export async function loadWorkHourHistory(session: BrowserSession, workHourId: number) {
  return listWorkHourHistory(session, [workHourId]);
}
