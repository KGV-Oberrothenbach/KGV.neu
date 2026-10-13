import { readSupabaseRest } from "../../lib/supabase/rest-client";
import { writeWithBrowserEditLock } from "../../services/concurrency/edit-lock-service";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";

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

export const listMaintenanceContracts = (club: ClubContext, session: BrowserSession) =>
  readSupabaseRest<MaintenanceContractRow>({ club, session }, "wartungsvertraege", { select, is_demo: "eq.false", order: "titel.asc", limit: "500" });

export const getMaintenanceContract = async (club: ClubContext, session: BrowserSession, id: number) =>
  (await readSupabaseRest<MaintenanceContractRow>({ club, session }, "wartungsvertraege", { select, id: `eq.${id}`, is_demo: "eq.false", limit: "1" }))[0] ?? null;

export const createMaintenanceContract = (club: ClubContext, session: BrowserSession, payload: MaintenanceContractWriteRow) =>
  writeWithBrowserEditLock<MaintenanceContractRow>({ club, session }, "wartungsvertraege", "POST", payload);

export const updateMaintenanceContract = (club: ClubContext, session: BrowserSession, id: number, payload: MaintenanceContractWriteRow) =>
  writeWithBrowserEditLock<MaintenanceContractRow>({ club, session }, "wartungsvertraege", "PATCH", payload, { id: `eq.${id}`, is_demo: "eq.false" });
