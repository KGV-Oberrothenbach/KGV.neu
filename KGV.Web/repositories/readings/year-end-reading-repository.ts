import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";

export type YearEndReadingStatusRow = {
  jahr: number;
  parzelle: string;
  medium: string;
  zaehler_id: number;
  zaehlernummer: string;
  eingebaut_am: string;
  jae_ablesedatum: string | null;
  jae_stand: number | null;
  pruefstatus: string | null;
  status: string;
  pachtbezug: string;
  pachtwechsel_im_jahr: boolean;
};

export function listYearEndReadingStatus(club: ClubContext, session: BrowserSession, seasonYear: number) {
  return callSupabaseRpc<YearEndReadingStatusRow[]>({ club, session }, "rpc_export_jahresablesung_status", { p_jahr: seasonYear });
}
