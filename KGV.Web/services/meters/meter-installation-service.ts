import { type BrowserSession } from "../../lib/supabase-auth";
import { listActiveMeterMedia, listRfidParcels } from "../../repositories/rfid/rfid-repository";
import { createMeterInstallation, type MeterInstallationInsert } from "../../repositories/meters/meter-installation-repository";
import { resolveRfidScanContext, type RfidScanResolution } from "../rfid/rfid-service";

export type MeterInstallationPermissions = { canManageMeterChanges: boolean };
export type MeterInstallationContext = { uid: string; parcelId: number; medium: "strom" | "wasser"; gardenNr: string; anlage: string };

const normalizeMedium = (value: string | null | undefined): "strom" | "wasser" | null => value?.trim().toLowerCase() === "strom" ? "strom" : value?.trim().toLowerCase() === "wasser" ? "wasser" : null;
const validDate = (value: string) => {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const [year, month, day] = value.split("-").map(Number);
  const date = new Date(year, month - 1, day, 12);
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
};

export function installationContextFromResolution(resolution: RfidScanResolution): MeterInstallationContext | null {
  const medium = normalizeMedium(resolution.context?.medium);
  if (resolution.state !== "KnownWithoutActiveMeter" || !resolution.context || !medium) return null;
  return { uid: resolution.normalizedUid, parcelId: resolution.context.parzelle_id, medium, gardenNr: resolution.context.garten_nr ?? "–", anlage: resolution.context.anlage ?? "" };
}

export async function createMeterInstallationForRfid(session: BrowserSession, permissions: MeterInstallationPermissions, context: MeterInstallationContext, input: { meterNumber: string; installationDate: string; calibrationYear: string }) {
  if (!permissions.canManageMeterChanges) throw new Error("Für den Zählereinbau besteht keine Berechtigung.");
  const meterNumber = input.meterNumber.trim();
  if (!meterNumber) throw new Error("Bitte eine Zählernummer eingeben.");
  if (!validDate(input.installationDate)) throw new Error("Bitte ein gültiges Einbau-Datum eingeben.");
  const calibrationYear = Number(input.calibrationYear.trim());
  if (!Number.isInteger(calibrationYear) || calibrationYear < 1900 || calibrationYear > 9999) throw new Error("Bitte ein gültiges Eichjahr eingeben.");

  const fresh = await resolveRfidScanContext(session, context.uid);
  const freshContext = installationContextFromResolution(fresh);
  if (!freshContext || freshContext.parcelId !== context.parcelId || freshContext.medium !== context.medium) throw new Error("Der RFID-Kontext hat sich geändert oder besitzt bereits einen aktiven Zähler.");
  const parcel = (await listRfidParcels(session)).find((item) => item.id === context.parcelId);
  if (!parcel) throw new Error("Die Parzelle konnte nicht geladen werden.");
  const enabled = context.medium === "strom" ? parcel.hat_strom && Boolean(parcel.rfid_strom?.trim()) : parcel.hat_wasser && Boolean(parcel.rfid_wasser?.trim());
  if (!enabled) throw new Error(`Für ${context.medium === "strom" ? "Strom" : "Wasser"} müssen Medium und RFID an der Parzelle freigeschaltet sein.`);
  if ((await listActiveMeterMedia(session, context.parcelId)).some((item) => normalizeMedium(item.medium) === context.medium)) throw new Error("Für diese Parzelle und dieses Medium existiert bereits ein aktiver Zähler.");

  const created = await createMeterInstallation(session, {
    parcelId: context.parcelId, medium: context.medium, meterNumber, installationDate: input.installationDate,
    calibrationDate: `${String(calibrationYear).padStart(4, "0")}-01-01`,
  } satisfies MeterInstallationInsert);
  if (!created?.id || created.parzelle_id !== context.parcelId || normalizeMedium(created.medium) !== context.medium || created.ausgebaut_am || created.status?.toLowerCase() !== "aktiv") throw new Error("Der neu angelegte Zähler konnte nicht bestätigt werden.");
  const verified = await resolveRfidScanContext(session, context.uid);
  if (verified.state !== "KnownWithActiveMeter" || verified.context?.aktiver_zaehler_id !== created.id) throw new Error("Der RFID-Kontext bestätigt den neu angelegten aktiven Zähler nicht.");
  return created;
}
