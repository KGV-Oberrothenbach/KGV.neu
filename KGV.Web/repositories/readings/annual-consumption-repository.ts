import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";

export type AnnualConsumptionSegment = {
  jahr: number;
  parzelle_id: number;
  parzelle: string;
  mitglied_id: number | null;
  pachtbezug: string;
  medium: string;
  zaehler_id: number;
  zaehlernummer: string;
  von_datum: string;
  bis_datum: string;
  start_ablesung_id: number | null;
  end_ablesung_id: number | null;
  startstand: number | null;
  endstand: number | null;
  verbrauch: number | null;
  grund: "normal" | "zaehlerwechsel" | "paechterwechsel" | "zaehlerwechsel_und_paechterwechsel";
  berechenbar: boolean;
  fehlercode: string | null;
  fehlertext: string | null;
  bezug_typ: "paechter" | "leerstand" | "verein";
  ist_vereinsgarten: boolean;
  umlagefaehig: boolean;
};

export function listAnnualConsumptionSegments(club: ClubContext, session: BrowserSession, seasonYear: number) {
  return callSupabaseRpc<AnnualConsumptionSegment[]>({ club, session }, "rpc_jahresverbrauch_segmente", { p_jahr: seasonYear });
}
