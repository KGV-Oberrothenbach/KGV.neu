import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";

export type MeterChangeRpcInput = {
  oldMeterId: number; parcelId: number; medium: "strom" | "wasser"; removalDate: string; finalValue: number;
  removalPhotoPath?: string | null; newMeterNumber: string; calibrationDate: string; installationDate: string;
  initialValue: number; installationPhotoPath?: string | null;
};
export type MeterChangeRpcResult = { alter_zaehler_id: number; neuer_zaehler_id: number };

export const executeMeterChange = (club: ClubContext, session: BrowserSession, input: MeterChangeRpcInput) =>
  callSupabaseRpc<MeterChangeRpcResult[]>({ club, session }, "execute_meter_change", {
    p_alter_zaehler_id: input.oldMeterId, p_parzelle_id: input.parcelId, p_medium: input.medium,
    p_ausbaudatum: input.removalDate, p_endstand: input.finalValue, p_ausbau_foto_pfad: input.removalPhotoPath ?? null,
    p_neue_zaehlernummer: input.newMeterNumber, p_eichdatum: input.calibrationDate, p_einbaudatum: input.installationDate,
    p_anfangsstand: input.initialValue, p_einbau_foto_pfad: input.installationPhotoPath ?? null,
  });
