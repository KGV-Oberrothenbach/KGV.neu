import { type BrowserSession, deleteSupabase, readSupabase, writeSupabase } from "../../lib/supabase-auth";
import { type Announcement } from "../../models/announcements/announcement";
export type HomeAnnouncement = { id: number; titel: string | null; inhalt_html: string | null; sichtbar_ab: string | null; sichtbar_bis: string | null; created_at: string | null };
export const listHomeAnnouncements = (session: BrowserSession) => readSupabase<HomeAnnouncement>(session, "v_startseite_bekanntmachungen", { select: "id,titel,inhalt_html,sichtbar_ab,sichtbar_bis,created_at", order: "created_at.desc", limit: "30" });
export const listManagementAnnouncements = (session: BrowserSession) => readSupabase<Announcement>(session, "bekanntmachung", { select: "id,titel,inhalt_html,sichtbar_ab,sichtbar_bis,sort_order,aktiv", order: "sort_order.asc", limit: "500" });
export const createAnnouncement = (session: BrowserSession, payload: Omit<Announcement, "id">) => writeSupabase<Announcement>(session, "bekanntmachung", "POST", payload);
export const updateAnnouncement = (session: BrowserSession, id: number, payload: Omit<Announcement, "id"> | Partial<Pick<Announcement, "aktiv">>) => writeSupabase<Announcement>(session, "bekanntmachung", "PATCH", payload, { id: `eq.${id}` });
export const deleteAnnouncement = (session: BrowserSession, id: number) => deleteSupabase(session, "bekanntmachung", { id: `eq.${id}` });
