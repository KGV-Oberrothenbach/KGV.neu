import { type BrowserSession, writeSupabase } from "../../lib/supabase-auth";
import { type Meter } from "./meter-repository";

export const setMeterRemovedAt = (session: BrowserSession, meterId: number, removedAt: string) =>
  writeSupabase<Meter>(session, "zaehler", "PATCH", { ausgebaut_am: removedAt }, { id: `eq.${meterId}`, status: "eq.aktiv", ausgebaut_am: "is.null" });
