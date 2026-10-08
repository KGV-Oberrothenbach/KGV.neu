import { callSupabaseRpc, type BrowserSession } from "../../lib/supabase-auth";
import { type Meter } from "./meter-repository";

export type MeterInstallationInsert = { parcelId: number; medium: "strom" | "wasser"; meterNumber: string; calibrationDate: string; installationDate: string };

export const createMeterInstallation = (session: BrowserSession, input: MeterInstallationInsert) => callSupabaseRpc<Meter>(session, "create_meter_installation", {
  p_parzelle_id: input.parcelId,
  p_medium: input.medium,
  p_zaehlernummer: input.meterNumber,
  p_eichdatum: input.calibrationDate,
  p_eingebaut_am: input.installationDate,
});
