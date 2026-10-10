import { type BrowserSession, readSupabase, writeSupabase } from "../../lib/supabase-auth";
import { type Appointment } from "../../models/appointments/appointment";
export type HomeAppointment = { id: number; titel: string | null; beschreibung: string | null; datum: string; start_uhrzeit: string | null; end_uhrzeit: string | null; sichtbar_ab: string | null; sichtbar_bis: string | null };
export const listHomeAppointments = (session: BrowserSession, today: string) => readSupabase<HomeAppointment>(session, "v_startseite_termine", { select: "id,titel,beschreibung,datum,start_uhrzeit,end_uhrzeit,sichtbar_ab,sichtbar_bis", datum: `gte.${today}`, order: "datum.asc,start_uhrzeit.asc", limit: "30" });
export const listManagementAppointments = (session: BrowserSession) => readSupabase<Appointment>(session, "termin", { select: "id,titel,beschreibung,datum,start_uhrzeit,end_uhrzeit,sichtbar_ab,sichtbar_bis,aktiv", order: "datum.asc", limit: "500" });
export const createAppointment = (session: BrowserSession, payload: Omit<Appointment, "id">) => writeSupabase<Appointment>(session, "termin", "POST", { ...payload, is_demo: session.isDemoAccount });
export const updateAppointment = (session: BrowserSession, id: number, payload: Omit<Appointment, "id"> | Partial<Pick<Appointment, "aktiv">>) => writeSupabase<Appointment>(session, "termin", "PATCH", payload, { id: `eq.${id}` });
