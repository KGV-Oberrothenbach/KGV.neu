import { type BrowserSession } from "../../lib/supabase-auth";
import {
  createAdministrativeWorkHour,
  createOwnOpenWorkHour,
  getWorkHoursSummary,
  listMemberWorkHoursForSeason,
  listWorkHourHistory,
  listWorkHoursForReview,
  updateOwnOpenWorkHour,
} from "../../repositories/work-hours/work-hours-repository";
import { type WorkHour } from "../../features/work-hours/work-hours-types";

export type OwnWorkHourInput = { date: string; hours: number; workType: string };

const validateOwnWorkHourInput = (input: OwnWorkHourInput) => {
  if (!input.date.trim()) throw new Error("Ein Datum ist erforderlich.");
  if (!Number.isFinite(input.hours) || input.hours <= 0) throw new Error("Stunden müssen als Zahl größer als 0 eingegeben werden.");
  if (!input.workType.trim()) throw new Error("Die Art der Arbeit ist erforderlich.");
};

export const isOwnOpenWorkHourEditable = (workHour: WorkHour, memberId: number) =>
  workHour.mitglied_id === memberId
  && workHour.status === "offen"
  && workHour.freigegeben === false;

export async function createOwnWorkHour(session: BrowserSession, memberId: number, saisonId: number | null, input: OwnWorkHourInput) {
  if (!Number.isInteger(memberId) || memberId <= 0) throw new Error("Für die Arbeitsstunde fehlt der Mitgliedskontext.");
  if (saisonId === null || !Number.isInteger(saisonId) || saisonId <= 0) throw new Error("Für die Arbeitsstunde fehlt die Saison.");
  validateOwnWorkHourInput(input);
  const created = await createOwnOpenWorkHour(session, {
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

export async function createAdministrativeWorkHourEntry(session: BrowserSession, canManageWorkHours: boolean, memberId: number, saisonId: number | null, input: OwnWorkHourInput) {
  if (!canManageWorkHours) throw new Error("Für die administrative Arbeitsstundenerfassung fehlt ManageWorkHours.");
  if (!Number.isInteger(memberId) || memberId <= 0) throw new Error("Für die Arbeitsstunde fehlt der Mitgliedskontext.");
  if (saisonId === null || !Number.isInteger(saisonId) || saisonId <= 0) throw new Error("Für die Arbeitsstunde fehlt die Saison.");
  validateOwnWorkHourInput(input);
  const created = await createAdministrativeWorkHour(session, {
    mitglied_id: memberId,
    saison_id: saisonId,
    datum: input.date,
    stunden: input.hours,
    art_der_arbeit: input.workType.trim(),
    status: "genehmigt",
    freigegeben: true,
  });
  if (!created[0]) throw new Error("Arbeitsstunde konnte nicht gespeichert werden.");
  return created[0];
}

export async function updateOwnWorkHour(session: BrowserSession, workHour: WorkHour | undefined, memberId: number, input: OwnWorkHourInput) {
  if (!workHour || !isOwnOpenWorkHourEditable(workHour, memberId)) throw new Error("Nur eigene offene Arbeitsstunden können bearbeitet werden.");
  validateOwnWorkHourInput(input);
  const updated = await updateOwnOpenWorkHour(session, workHour.id, {
    datum: input.date,
    stunden: input.hours,
    art_der_arbeit: input.workType.trim(),
  });
  if (!updated[0]) throw new Error("Arbeitsstunde konnte nicht gespeichert werden.");
  return updated[0];
}

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
