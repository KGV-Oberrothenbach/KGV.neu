import { type BrowserSession } from "../../lib/supabase-auth";
import {
  getHomeWorkHours,
  listHomeAnnouncements,
  listHomeAppointments,
  listHomeRegistrations,
  listHomeWorkAssignments,
  signOffFromHomeWorkAssignment,
  signUpForHomeWorkAssignment,
  type HomeAnnouncement,
  type HomeAppointment,
  type HomeWorkAssignment,
  type HomeWorkHoursSummary,
  type WorkAssignmentRegistration,
} from "../../repositories/home/home-repository";

export type { HomeAnnouncement, HomeAppointment, HomeWorkAssignment, HomeWorkHoursSummary, WorkAssignmentRegistration };
export type HomeDetailSelection = { kind: "assignment" | "appointment" | "announcement"; id: number };
export type HomeDashboardData = { assignments: HomeWorkAssignment[]; appointments: HomeAppointment[]; announcements: HomeAnnouncement[]; workHours: HomeWorkHoursSummary | null; registrations: WorkAssignmentRegistration[] };

export async function loadHomeDashboard(session: BrowserSession, memberId: number | null, saisonId: number | null, season: number): Promise<HomeDashboardData> {
  const today = new Date().toISOString().slice(0, 10);
  const [assignments, appointments, announcements, workHours, registrations] = await Promise.all([
    listHomeWorkAssignments(session, today),
    listHomeAppointments(session, today),
    listHomeAnnouncements(session),
    memberId === null ? Promise.resolve([] as HomeWorkHoursSummary[]) : getHomeWorkHours(session, memberId, saisonId, season),
    memberId === null ? Promise.resolve([] as WorkAssignmentRegistration[]) : listHomeRegistrations(session, memberId),
  ]);
  return { assignments, appointments, announcements, workHours: workHours[0] ?? null, registrations };
}

export function registerForHomeWorkAssignment(session: BrowserSession, assignmentId: number, memberId: number) {
  return signUpForHomeWorkAssignment(session, assignmentId, memberId);
}

export function signOffFromHomeAssignment(session: BrowserSession, assignmentId: number, memberId: number) {
  return signOffFromHomeWorkAssignment(session, assignmentId, memberId);
}
