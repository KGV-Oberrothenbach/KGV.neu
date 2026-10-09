import { type BrowserSession } from "../../lib/supabase-auth";
import {
  listActiveWorkAssignmentMembers,
  listManagementAssignmentRegistrations,
  listManagementWorkAssignments,
} from "../../repositories/work-assignments/work-assignment-repository";

export async function loadWorkAssignmentsManagement(session: BrowserSession) {
  const assignments = await listManagementWorkAssignments(session);
  return assignments.sort((left, right) => assignmentSortKey(left).localeCompare(assignmentSortKey(right), "de"));
}

export async function loadWorkAssignmentParticipants(session: BrowserSession, assignmentId: number) {
  const [registrations, members] = await Promise.all([
    listManagementAssignmentRegistrations(session, assignmentId),
    listActiveWorkAssignmentMembers(session),
  ]);
  return { registrations, members };
}

function assignmentSortKey(assignment: { datum: string; start_uhrzeit: string | null; end_uhrzeit: string | null; titel: string | null }) {
  return `${assignment.datum}|${assignment.start_uhrzeit ?? "99:99"}|${assignment.end_uhrzeit ?? "99:99"}|${assignment.titel ?? ""}`;
}
