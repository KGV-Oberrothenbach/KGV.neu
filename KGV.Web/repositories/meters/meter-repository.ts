import { type BrowserSession, readSupabase } from "../../lib/supabase-auth";

export type Meter = {
  id: number;
  parzelle_id: number;
  medium: string;
  zaehlernummer: string;
  eingebaut_am: string;
  ausgebaut_am: string | null;
  eichfaellig_am: string | null;
  status: string | null;
};

export type MeterOverviewParcel = {
  id: number;
  garten_nr: string;
  Anlage: string;
  flaeche_qm: number | null;
  hat_strom: boolean;
  hat_wasser: boolean;
  aktiv: boolean;
};

export type MeterDueStatus = {
  id: number;
  parzelle_id: number;
  anlage: string | null;
  garten_nr: string | null;
  medium: string | null;
  zaehlernummer: string | null;
  eichdatum: string | null;
  eichfaellig_am: string | null;
  eingebaut_am: string | null;
  status: string | null;
  tage_bis_faellig: number | null;
  eichstatus: string | null;
};

export const listMeters = (session: BrowserSession) => readSupabase<Meter>(session, "zaehler", {
  select: "id,parzelle_id,medium,zaehlernummer,eingebaut_am,ausgebaut_am,eichfaellig_am,status",
  order: "medium.asc,zaehlernummer.asc",
  limit: "1000",
});

export const listMeterOverviewParcels = (session: BrowserSession) => readSupabase<MeterOverviewParcel>(session, "parzelle", {
  select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv",
  limit: "500",
});

export const listMeterDueStatuses = (session: BrowserSession) => readSupabase<MeterDueStatus>(session, "v_zaehler_eichstatus", {
  select: "id,parzelle_id,anlage,garten_nr,medium,zaehlernummer,eichdatum,eichfaellig_am,eingebaut_am,status,tage_bis_faellig,eichstatus",
  limit: "1000",
});
