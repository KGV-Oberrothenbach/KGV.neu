import type { ClubContext } from "../../models/auth/club";
import { resolveClubRecord } from "../../repositories/auth/club-repository";

const clubStorageKey = "kgv.browser.club.v1";

export async function resolveClub(code: string): Promise<ClubContext> {
  const entries = await resolveClubRecord(code.trim());
  const entry = entries.length === 1 ? entries[0] : null;
  if (!entry?.verein_id || !entry.vereins_code || !entry.vereinsname || !entry.supabase_url || !entry.supabase_publishable_key) {
    throw new Error("Die Vereins-ID ist ungültig, inaktiv oder unvollständig eingerichtet.");
  }

  const club: ClubContext = {
    vereinId: entry.verein_id,
    vereinsCode: entry.vereins_code,
    vereinsname: entry.vereinsname,
    kurzname: entry.kurzname,
    supabaseUrl: entry.supabase_url,
    supabasePublishableKey: entry.supabase_publishable_key,
  };
  window.localStorage.setItem(clubStorageKey, JSON.stringify(club));
  return club;
}
