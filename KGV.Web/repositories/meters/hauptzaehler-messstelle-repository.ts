import { readSupabaseRest } from "../../lib/supabase/rest-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";

export type HauptzaehlerMessstelle = { id: number; medium: string; anlagenbereich: string; rfid_tag_uid: string | null };

export const listHauptzaehlerMessstellen = (club: ClubContext, session: BrowserSession) =>
  readSupabaseRest<HauptzaehlerMessstelle>({ club, session }, "hauptzaehler_messstelle", {
    select: "id,medium,anlagenbereich,rfid_tag_uid", order: "medium.asc,anlagenbereich.asc", limit: "10",
  });

export async function findHauptzaehlerMessstelleByRfid(club: ClubContext, session: BrowserSession, uid: string) {
  const normalized = uid.trim().toUpperCase();
  if (!normalized) return null;
  return (await listHauptzaehlerMessstellen(club, session)).find((item) => item.rfid_tag_uid?.trim().toUpperCase() === normalized) ?? null;
}
