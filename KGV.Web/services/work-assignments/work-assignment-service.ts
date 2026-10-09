import { type BrowserSession } from "../../lib/supabase-auth";
import { type WorkAssignment, type WorkAssignmentMember, type WorkAssignmentRegistration } from "../../models/work-assignments/work-assignment";
import {
  listActiveWorkAssignmentMembers,
  listManagementAssignmentRegistrations,
  listManagementWorkAssignments,
  createWorkAssignment,
  updateWorkAssignment,
  deleteWorkAssignment,
} from "../../repositories/work-assignments/work-assignment-repository";

export type WorkAssignmentDraft = Partial<WorkAssignment>;

export function berlinNow(): string {
  const parts = new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Berlin", year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", hourCycle: "h23" }).formatToParts(new Date()).reduce<Record<string, string>>((result, part) => { result[part.type] = part.value; return result; }, {});
  return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}:${parts.minute}`;
}

export function workAssignmentDateDefaults(date: string): Pick<WorkAssignment, "sichtbar_bis" | "anmeldung_bis"> {
  const assignmentDate = new Date(`${date}T12:00`);
  const signUpDeadline = new Date(assignmentDate); signUpDeadline.setDate(signUpDeadline.getDate() - 2);
  const visibleUntil = new Date(assignmentDate); visibleUntil.setDate(visibleUntil.getDate() + 14);
  const localDate = (value: Date) => `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, "0")}-${String(value.getDate()).padStart(2, "0")}`;
  return { sichtbar_bis: `${localDate(visibleUntil)}T23:59`, anmeldung_bis: `${localDate(signUpDeadline)}T00:00` };
}

export function updateWorkAssignmentDateDefaults(draft: WorkAssignmentDraft, date: string): WorkAssignmentDraft {
  if (!date) return { ...draft, datum: date };
  const previousDefaults = draft.datum ? workAssignmentDateDefaults(draft.datum) : undefined;
  const nextDefaults = workAssignmentDateDefaults(date);
  return {
    ...draft,
    datum: date,
    anmeldung_bis: previousDefaults && draft.anmeldung_bis === previousDefaults.anmeldung_bis ? nextDefaults.anmeldung_bis : draft.anmeldung_bis,
    sichtbar_bis: previousDefaults && draft.sichtbar_bis === previousDefaults.sichtbar_bis ? nextDefaults.sichtbar_bis : draft.sichtbar_bis,
  };
}

export function workAssignmentHasStarted(assignment: Pick<WorkAssignment, "datum" | "start_uhrzeit">): boolean {
  const start = `${assignment.datum}T${assignment.start_uhrzeit ?? "23:59"}`;
  return berlinNow() >= start;
}

export function workAssignmentDefaults(date = berlinNow().slice(0, 10)): WorkAssignmentDraft {
  return { titel: "", beschreibung: "", datum: date, start_uhrzeit: "10:00", end_uhrzeit: "13:00", treffpunkt: "", max_teilnehmer: null, stunden_wert: 0, sichtbar_ab: berlinNow(), ...workAssignmentDateDefaults(date), aktiv: true };
}

export function normalizeAndValidateWorkAssignment(draft: WorkAssignmentDraft): { payload?: Omit<WorkAssignment, "id">; error?: string } {
  const title = draft.titel?.trim(); const hours = Number(draft.stunden_wert ?? 0); const capacity = draft.max_teilnehmer === null || draft.max_teilnehmer === undefined || draft.max_teilnehmer === "" ? null : Number(draft.max_teilnehmer);
  if (!title || !draft.datum) return { error: "Titel und Datum sind erforderlich." };
  if (draft.start_uhrzeit && draft.end_uhrzeit && draft.end_uhrzeit < draft.start_uhrzeit) return { error: "Das Ende darf nicht vor dem Beginn liegen." };
  if (!Number.isFinite(hours) || hours < 0 || (capacity !== null && (!Number.isInteger(capacity) || capacity < 1))) return { error: "Stundenwert und Teilnehmerbegrenzung sind ungültig." };
  if (draft.sichtbar_ab && draft.sichtbar_bis && draft.sichtbar_bis < draft.sichtbar_ab) return { error: "Sichtbar bis darf nicht vor Sichtbar ab liegen." };
  const assignmentStart = draft.start_uhrzeit ? `${draft.datum}T${draft.start_uhrzeit}` : `${draft.datum}T23:59`;
  if (draft.anmeldung_bis && draft.anmeldung_bis > assignmentStart) return { error: "Der Anmeldeschluss darf nicht nach Einsatzbeginn liegen." };
  return { payload: { titel: title, beschreibung: draft.beschreibung?.trim() || null, datum: draft.datum, start_uhrzeit: draft.start_uhrzeit || null, end_uhrzeit: draft.end_uhrzeit || null, treffpunkt: draft.treffpunkt?.trim() || null, max_teilnehmer: capacity, stunden_wert: hours, sichtbar_ab: draft.sichtbar_ab || null, sichtbar_bis: draft.sichtbar_bis || null, anmeldung_bis: draft.anmeldung_bis || null, aktiv: draft.aktiv !== false } };
}

export const saveWorkAssignment = (session: BrowserSession, id: number | null, payload: Omit<WorkAssignment, "id">) => id === null ? createWorkAssignment(session, payload) : updateWorkAssignment(session, id, payload);
export const deactivateWorkAssignment = (session: BrowserSession, id: number) => updateWorkAssignment(session, id, { aktiv: false });
export const removeWorkAssignment = (session: BrowserSession, id: number) => deleteWorkAssignment(session, id);

export function prepareNextWorkAssignment(source: Omit<WorkAssignment, "id">): WorkAssignmentDraft {
  const defaults = workAssignmentDefaults(source.datum); const start = source.start_uhrzeit ?? "10:00"; const end = source.end_uhrzeit ?? "13:00";
  const toMinutes = (value: string) => { const [hours, minutes] = value.split(":").map(Number); return hours * 60 + minutes; }; const toTime = (value: number) => `${String(Math.floor(value / 60)).padStart(2, "0")}:${String(value % 60).padStart(2, "0")}`;
  const startMinutes = toMinutes(start); const endMinutes = toMinutes(end); const duration = endMinutes > startMinutes ? endMinutes - startMinutes : 180; const nextStart = Math.min(endMinutes, 1439);
  return { ...defaults, titel: source.titel, beschreibung: source.beschreibung, treffpunkt: source.treffpunkt, max_teilnehmer: source.max_teilnehmer, stunden_wert: source.stunden_wert, aktiv: source.aktiv, start_uhrzeit: toTime(nextStart), end_uhrzeit: toTime(Math.min(nextStart + duration, 1439)) };
}

export async function loadWorkAssignmentsManagement(session: BrowserSession): Promise<WorkAssignment[]> {
  const assignments = await listManagementWorkAssignments(session);
  return assignments.sort((left, right) => assignmentSortKey(left).localeCompare(assignmentSortKey(right), "de"));
}

export async function loadWorkAssignmentParticipants(session: BrowserSession, assignmentId: number): Promise<{ registrations: WorkAssignmentRegistration[]; members: WorkAssignmentMember[] }> {
  const [registrations, members] = await Promise.all([
    listManagementAssignmentRegistrations(session, assignmentId),
    listActiveWorkAssignmentMembers(session),
  ]);
  return { registrations, members };
}

function assignmentSortKey(assignment: { datum: string; start_uhrzeit: string | null; end_uhrzeit: string | null; titel: string | null }) {
  return `${assignment.datum}|${assignment.start_uhrzeit ?? "99:99"}|${assignment.end_uhrzeit ?? "99:99"}|${assignment.titel ?? ""}`;
}
