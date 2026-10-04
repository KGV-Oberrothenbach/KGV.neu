import { type BrowserSession, readSupabase } from "../../lib/supabase-auth";
export type HomeAnnouncement = { id: number; titel: string | null; inhalt_html: string | null; sichtbar_ab: string | null; sichtbar_bis: string | null; created_at: string | null };
export const listHomeAnnouncements = (session: BrowserSession) => readSupabase<HomeAnnouncement>(session, "v_startseite_bekanntmachungen", { select: "id,titel,inhalt_html,sichtbar_ab,sichtbar_bis,created_at", order: "created_at.desc", limit: "30" });
