import { type BrowserSession } from "../../lib/supabase-auth";
import { type Appointment, type AppointmentDraft } from "../../models/appointments/appointment";
import { createAppointment, listManagementAppointments, updateAppointment } from "../../repositories/appointments/appointment-repository";

export function appointmentDefaults(): AppointmentDraft {
  const now = berlinNow(); const [hours, minutes] = now.slice(11, 16).split(":").map(Number); const endMinutes = Math.min(hours * 60 + minutes + 60, 1439);
  return { titel: "", beschreibung: "", datum: now.slice(0, 10), start_uhrzeit: now.slice(11, 16), end_uhrzeit: `${String(Math.floor(endMinutes / 60)).padStart(2, "0")}:${String(endMinutes % 60).padStart(2, "0")}`, sichtbar_ab: now, sichtbar_bis: `${now.slice(0, 10)}T23:59`, aktiv: true };
}
export async function loadAppointmentsManagement(session: BrowserSession): Promise<Appointment[]> {
  const rows = await listManagementAppointments(session);
  return rows.sort((left, right) => `${left.datum}|${left.start_uhrzeit ?? "99:99"}|${left.end_uhrzeit ?? "99:99"}|${left.titel ?? ""}`.localeCompare(`${right.datum}|${right.start_uhrzeit ?? "99:99"}|${right.end_uhrzeit ?? "99:99"}|${right.titel ?? ""}`, "de"));
}
export function normalizeAndValidateAppointment(draft: AppointmentDraft, existingActive?: boolean): { payload?: Omit<Appointment, "id">; error?: string } {
  const title = draft.titel?.trim() ?? "";
  if (!title || !draft.datum) return { error: "Titel und Datum sind erforderlich." };
  if (draft.start_uhrzeit && draft.end_uhrzeit && draft.end_uhrzeit < draft.start_uhrzeit) return { error: "Das Terminende darf nicht vor dem Beginn liegen." };
  if (draft.sichtbar_ab && draft.sichtbar_bis && draft.sichtbar_bis < draft.sichtbar_ab) return { error: "Das Sichtbarkeitsende darf nicht vor dem Beginn liegen." };
  return { payload: { titel: title, beschreibung: draft.beschreibung?.trim() || null, datum: draft.datum, start_uhrzeit: draft.start_uhrzeit || null, end_uhrzeit: draft.end_uhrzeit || null, sichtbar_ab: draft.sichtbar_ab || null, sichtbar_bis: draft.sichtbar_bis || null, aktiv: existingActive ?? true } };
}
export const saveAppointment = (session: BrowserSession, id: number | null, payload: Omit<Appointment, "id">) => id === null ? createAppointment(session, payload) : updateAppointment(session, id, payload);
export const setAppointmentActive = (session: BrowserSession, id: number, active: boolean) => updateAppointment(session, id, { aktiv: active });
function berlinNow() { const parts = new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Berlin", year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", hourCycle: "h23" }).formatToParts(new Date()).reduce<Record<string, string>>((result, part) => { result[part.type] = part.value; return result; }, {}); return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}:${parts.minute}`; }
