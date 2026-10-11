import { callSupabaseRpc, type BrowserSession } from "../../lib/supabase-auth";

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
};

export function listAnnualConsumptionSegments(session: BrowserSession, seasonYear: number) {
  return callSupabaseRpc<AnnualConsumptionSegment[]>(session, "rpc_jahresverbrauch_segmente", { p_jahr: seasonYear });
}
