import { writeWithBrowserEditLock } from "../../services/concurrency/edit-lock-service";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type Meter } from "./meter-repository";

export const setMeterRemovedAt = (club: ClubContext, session: BrowserSession, meterId: number, removedAt: string) =>
  writeWithBrowserEditLock<Meter>({ club, session }, "zaehler", "PATCH", { ausgebaut_am: removedAt }, { id: `eq.${meterId}`, status: "eq.aktiv", ausgebaut_am: "is.null" });
