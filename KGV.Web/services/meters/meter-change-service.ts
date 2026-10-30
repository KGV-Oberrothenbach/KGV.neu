import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { executeMeterChange, type MeterChangeRpcInput } from "../../repositories/meters/meter-change-repository";

const isDate = (value: string) => /^\d{4}-\d{2}-\d{2}$/.test(value) && !Number.isNaN(Date.parse(`${value}T12:00:00`));

/** The only direct technical meter-change path.  The database performs all four writes atomically. */
export async function performMeterChange(club: ClubContext, session: BrowserSession, permitted: boolean, input: MeterChangeRpcInput) {
  if (!permitted) throw new Error("Für den Zählerwechsel besteht keine Berechtigung.");
  if (!isDate(input.removalDate) || !isDate(input.installationDate) || !isDate(input.calibrationDate)) throw new Error("Bitte gültige Einbau-, Ausbau- und Eichdaten angeben.");
  if (input.installationDate < input.removalDate) throw new Error("Das Einbaudatum darf nicht vor dem Ausbaudatum liegen.");
  if (!input.newMeterNumber.trim() || !Number.isFinite(input.finalValue) || input.finalValue < 0 || !Number.isFinite(input.initialValue) || input.initialValue < 0) throw new Error("Zählernummer sowie Anfangs- und Endstand müssen gültig sein.");
  const result = await executeMeterChange(club, session, input);
  if (!result[0]?.neuer_zaehler_id) throw new Error("Der Zählerwechsel wurde nicht bestätigt.");
  return result[0];
}
