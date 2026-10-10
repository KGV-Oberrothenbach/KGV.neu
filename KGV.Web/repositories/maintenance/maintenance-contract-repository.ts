import { type BrowserSession, readSupabase, writeSupabase } from "../../lib/supabase-auth";

export type MaintenanceContractRow = {
  id: number;
  titel: string;
  beschreibung: string | null;
  bereich: string | null;
  max_aktive_zuordnungen: number;
  befreit_von_pflichtstunden: boolean;
  arbeitsstunden_gutschrift: number;
  aktiv: boolean;
  bemerkung: string | null;
  is_demo: boolean;
};

export type MaintenanceContractWriteRow = Omit<MaintenanceContractRow, "id">;

const select = "id,titel,beschreibung,bereich,max_aktive_zuordnungen,befreit_von_pflichtstunden,arbeitsstunden_gutschrift,aktiv,bemerkung,is_demo";

export const listMaintenanceContracts = (session: BrowserSession) =>
  readSupabase<MaintenanceContractRow>(session, "wartungsvertraege", { select, is_demo: "eq.false", order: "titel.asc", limit: "500" });

export const getMaintenanceContract = async (session: BrowserSession, id: number) =>
  (await readSupabase<MaintenanceContractRow>(session, "wartungsvertraege", { select, id: `eq.${id}`, is_demo: "eq.false", limit: "1" }))[0] ?? null;

export const createMaintenanceContract = (session: BrowserSession, payload: MaintenanceContractWriteRow) =>
  writeSupabase<MaintenanceContractRow>(session, "wartungsvertraege", "POST", payload);

export const updateMaintenanceContract = (session: BrowserSession, id: number, payload: MaintenanceContractWriteRow) =>
  writeSupabase<MaintenanceContractRow>(session, "wartungsvertraege", "PATCH", payload, { id: `eq.${id}`, is_demo: "eq.false" });
