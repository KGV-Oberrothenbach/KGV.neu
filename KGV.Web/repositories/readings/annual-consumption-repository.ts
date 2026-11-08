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

export type MainSubMeterDifference = {
  jahr: number; medium: string; anlagenbereich: string; einheit: string;
  hauptzaehler_verbrauch: number | null; unterzaehler_verbrauch: number | null; differenz: number | null;
  berechenbar: boolean; pruefbeduerftig: boolean; fehlercode: string | null; fehlertext: string | null;
};

export function listAnnualConsumptionSegments(club: ClubContext, session: BrowserSession, seasonYear: number) {
  return callSupabaseRpc<AnnualConsumptionSegment[]>({ club, session }, "rpc_jahresverbrauch_segmente", { p_jahr: seasonYear });
}

export function listMainSubMeterDifferences(club: ClubContext, session: BrowserSession, seasonYear: number) {
  return callSupabaseRpc<MainSubMeterDifference[]>({ club, session }, "rpc_jahresverbrauch_haupt_unter_differenz", { p_jahr: seasonYear });
}
