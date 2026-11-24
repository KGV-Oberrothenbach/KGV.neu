import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import {
  createAdministrativeWorkHour,
  createOwnOpenWorkHour,
  getWorkHoursSummary,
  listMemberWorkHoursForSeason,
  listWorkHourHistory,
  listWorkHoursForReview,
  reviewWorkHour,
  getLinkedWorkHour,
  submitWorkAssignmentWorkHour,
  confirmWorkAssignmentWorkHour,
  type WorkHourReviewAction,
  updateOwnOpenWorkHour,
} from "../../repositories/work-hours/work-hours-repository";
import { type WorkHour } from "../../features/work-hours/work-hours-types";

export type OwnWorkHourInput = { date: string; hours: number; workType: string };
export type WorkHourReviewInput = OwnWorkHourInput & { action: WorkHourReviewAction; comment: string };

const validateOwnWorkHourInput = (input: OwnWorkHourInput) => {
  if (!input.date.trim()) throw new Error("Ein Datum ist erforderlich.");
  if (!Number.isFinite(input.hours) || input.hours <= 0) throw new Error("Stunden müssen als Zahl größer als 0 eingegeben werden.");
  if (!input.workType.trim()) throw new Error("Die Art der Arbeit ist erforderlich.");
};

export const isOwnOpenWorkHourEditable = (workHour: WorkHour, memberId: number) =>
  workHour.mitglied_id === memberId
  && workHour.status === "offen"
  && workHour.freigegeben === false;

export async function createOwnWorkHour(club: ClubContext, session: BrowserSession, memberId: number, saisonId: number | null, input: OwnWorkHourInput) {
  if (!Number.isInteger(memberId) || memberId <= 0) throw new Error("Für die Arbeitsstunde fehlt der Mitgliedskontext.");
  if (saisonId === null || !Number.isInteger(saisonId) || saisonId <= 0) throw new Error("Für die Arbeitsstunde fehlt die Saison.");
  validateOwnWorkHourInput(input);
  const created = await createOwnOpenWorkHour(club, session, {
    mitglied_id: memberId,
    saison_id: saisonId,
    datum: input.date,
    stunden: input.hours,
    art_der_arbeit: input.workType.trim(),
    status: "offen",
    freigegeben: false,
    genehmigt_von: null,
    genehmigt_am: null,
  });
  if (!created[0]) throw new Error("Arbeitsstunde konnte nicht gespeichert werden.");
  return created[0];
}

export async function createAdministrativeWorkHourEntry(club: ClubContext, session: BrowserSession, canEditWorkHours: boolean, canApproveWorkHours: boolean, memberId: number, saisonId: number | null, input: OwnWorkHourInput) {
  if (!canEditWorkHours) throw new Error("Fachrecht arbeitsstunden.bearbeiten fehlt.");
  if (!Number.isInteger(memberId) || memberId <= 0) throw new Error("Für die Arbeitsstunde fehlt der Mitgliedskontext.");
  if (saisonId === null || !Number.isInteger(saisonId) || saisonId <= 0) throw new Error("Für die Arbeitsstunde fehlt die Saison.");
  validateOwnWorkHourInput(input);
  const created = await createAdministrativeWorkHour(club, session, {
    mitglied_id: memberId,
    saison_id: saisonId,
    datum: input.date,
    stunden: input.hours,
    art_der_arbeit: input.workType.trim(),
    status: canApproveWorkHours ? "genehmigt" : "offen",
    freigegeben: canApproveWorkHours,
  });
  if (!created[0]) throw new Error("Arbeitsstunde konnte nicht gespeichert werden.");
  return created[0];
}

export async function updateOwnWorkHour(club: ClubContext, session: BrowserSession, workHour: WorkHour | undefined, memberId: number, input: OwnWorkHourInput, canEditWorkHours = false) {
  if (!workHour || !isOwnOpenWorkHourEditable(workHour, memberId) || (!canEditWorkHours && workHour.mitglied_id !== memberId))
    throw new Error("Nur berechtigt ausgewaehlte, offene Arbeitsstunden koennen bearbeitet werden.");
  validateOwnWorkHourInput(input);
  const updated = await updateOwnOpenWorkHour(club, session, workHour.id, {
    datum: input.date,
    stunden: input.hours,
    art_der_arbeit: input.workType.trim(),
  });
  if (!updated[0]) throw new Error("Arbeitsstunde konnte nicht gespeichert werden.");
  return updated[0];
}

export async function reviewOpenWorkHour(club: ClubContext, session: BrowserSession, canApproveWorkHours: boolean, canEditWorkHours: boolean, workHour: WorkHour | undefined, input: WorkHourReviewInput) {
  if (!canApproveWorkHours) throw new Error("Fachrecht arbeitsstunden.freigeben fehlt.");
  if ((input.action === "korrigieren" || input.action === "loeschen") && !canEditWorkHours)
    throw new Error("Fuer Korrektur oder Loeschen ist auch arbeitsstunden.bearbeiten erforderlich.");
  if (!workHour || workHour.status !== "offen" || workHour.freigegeben !== false) throw new Error("Diese Arbeitsstunde ist nicht mehr prüfbar.");
  if (!input.comment.trim()) throw new Error("Für jede Prüfaktion ist eine Begründung erforderlich.");
  if (input.action === "korrigieren") validateOwnWorkHourInput(input);
  await reviewWorkHour(club, session, {
    workHourId: workHour.id,
    action: input.action,
    comment: input.comment.trim(),
    ...(input.action === "korrigieren" ? { date: input.date, hours: input.hours, workType: input.workType.trim() } : {}),
  });
}

export async function loadMemberWorkHoursWorkspace(club: ClubContext, session: BrowserSession, memberId: number, saisonId: number) {
  const [workHours, summary] = await Promise.all([
    listMemberWorkHoursForSeason(club, session, memberId, saisonId),
    getWorkHoursSummary(club, session, memberId, saisonId),
  ]);
  const history = await listWorkHourHistory(club, session, workHours.map((item) => item.id));
  return { workHours, summary, history };
}

export const loadWorkHoursReview = (club: ClubContext, session: BrowserSession, saisonId: number) => listWorkHoursForReview(club, session, saisonId);
export const loadLinkedWorkAssignmentWorkHour = (club: ClubContext, session: BrowserSession, registrationId: number) => getLinkedWorkHour(club, session, registrationId);
export const submitLinkedWorkAssignmentWorkHour = (club: ClubContext, session: BrowserSession, registrationId: number, hours: number, workType: string) => submitWorkAssignmentWorkHour(club, session, registrationId, hours, workType);
export const confirmLinkedWorkAssignmentWorkHour = (club: ClubContext, session: BrowserSession, registrationId: number, hours: number, workType: string) => confirmWorkAssignmentWorkHour(club, session, registrationId, hours, workType);

export async function loadWorkHourHistory(club: ClubContext, session: BrowserSession, workHourId: number) {
  return listWorkHourHistory(club, session, [workHourId]);
}
