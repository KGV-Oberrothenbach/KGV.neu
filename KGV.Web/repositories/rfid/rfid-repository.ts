import { readSupabaseRest } from "../../lib/supabase/rest-client";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";

export type RfidScanContext = {
  parzelle_id: number;
  anlage: string | null;
  garten_nr: string | null;
  medium: string | null;
  rfid_tag_uid: string | null;
  aktiver_zaehler_id: number | null;
  zaehlernummer: string | null;
  eichdatum: string | null;
  eichfaellig_am: string | null;
  eingebaut_am: string | null;
  ausgebaut_am: string | null;
  status: string | null;
};

export type RfidParcel = {
  id: number;
  garten_nr: string;
  Anlage: string;
  hat_strom: boolean;
  hat_wasser: boolean;
  rfid_strom: string | null;
  rfid_wasser: string | null;
  aktiv: boolean;
};

export type ActiveMeterMedium = { medium: string };

export const listRfidParcels = (club: ClubContext, session: BrowserSession) => readSupabaseRest<RfidParcel>({ club, session }, "parzelle", {
  select: "id,garten_nr,Anlage,hat_strom,hat_wasser,rfid_strom,rfid_wasser,aktiv",
  aktiv: "eq.true",
  limit: "1000",
});

export const listRfidScanContexts = (club: ClubContext, session: BrowserSession) => readSupabaseRest<RfidScanContext>({ club, session }, "v_rfid_scan_context", {
  select: "parzelle_id,anlage,garten_nr,medium,rfid_tag_uid,aktiver_zaehler_id,zaehlernummer,eichdatum,eichfaellig_am,eingebaut_am,ausgebaut_am,status",
  limit: "3000",
});

export const listActiveMeterMedia = (club: ClubContext, session: BrowserSession, parcelId: number) => readSupabaseRest<ActiveMeterMedium>({ club, session }, "zaehler", {
  select: "medium",
  parzelle_id: `eq.${parcelId}`,
  status: "eq.aktiv",
  ausgebaut_am: "is.null",
  limit: "10",
});

export const assignParcelRfid = (club: ClubContext, session: BrowserSession, parcelId: number, medium: "strom" | "wasser", uid: string) =>
  callSupabaseRpc<RfidParcel>({ club, session }, "assign_parzelle_rfid", { p_parzelle_id: parcelId, p_medium: medium, p_rfid_tag_uid: uid });
