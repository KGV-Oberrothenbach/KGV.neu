import { type BrowserSession } from "../../lib/supabase-auth";
import { setMeterRemovedAt } from "../../repositories/meters/meter-removal-repository";
import { resolveRfidScanContext } from "../rfid/rfid-service";

const validDate = (value: string) => { if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false; const [year, month, day] = value.split("-").map(Number); const date = new Date(year, month - 1, day, 12); return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day; };

export async function completeMeterRemoval(session: BrowserSession, permissions: { canManageMeterChanges: boolean }, input: { uid: string; meterId: number; parcelId: number; medium: string; removedAt: string }) {
  if (!permissions.canManageMeterChanges) throw new Error("Für den Zählerausbau besteht keine Berechtigung.");
  if (!validDate(input.removedAt)) throw new Error("Bitte ein gültiges Ausbaudatum eingeben.");
  const fresh = await resolveRfidScanContext(session, input.uid);
  if (fresh.state !== "KnownWithActiveMeter" || fresh.context?.aktiver_zaehler_id !== input.meterId || fresh.context.parzelle_id !== input.parcelId || fresh.context.medium?.toLowerCase() !== input.medium.toLowerCase()) throw new Error("Der RFID-Kontext hat sich geändert. Der Zähler wird nicht ausgebaut.");
  if (fresh.context.eingebaut_am && input.removedAt < fresh.context.eingebaut_am.slice(0, 10)) throw new Error("Das Ausbaudatum darf nicht vor dem Einbaudatum liegen.");
  const updated = await setMeterRemovedAt(session, input.meterId, input.removedAt);
  if (!updated[0]) throw new Error("Der Zähler wurde inzwischen bereits beendet.");
  return updated[0];
}
