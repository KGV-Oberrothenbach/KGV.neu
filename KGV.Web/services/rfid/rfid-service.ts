import { type BrowserSession } from "../../lib/supabase-auth";
import {
  assignParcelRfid,
  listActiveMeterMedia,
  listRfidParcels,
  listRfidScanContexts,
  type RfidParcel,
  type RfidScanContext,
} from "../../repositories/rfid/rfid-repository";

export type RfidScanState = "Unknown" | "KnownWithoutActiveMeter" | "KnownWithActiveMeter";
export type RfidMediumOption = { key: "strom" | "wasser"; displayName: "Strom" | "Wasser" };
export type RfidAssignmentCheck = {
  isValid: boolean;
  requiresOverwriteConfirmation: boolean;
  alreadyAssignedToTarget: boolean;
  message: string;
  normalizedUid: string;
  currentTargetRfid: string;
  conflict: RfidScanContext | null;
};
export type RfidAssignmentResult = { success: boolean; requiresOverwriteConfirmation: boolean; message: string; normalizedUid: string };
export type RfidScanResolution = { normalizedUid: string; state: RfidScanState; context: RfidScanContext | null; message: string };

const normalizeUid = (uid: string) => uid.trim().toUpperCase();
const normalizeMedium = (medium: string | null) => medium?.trim().toLowerCase() === "wasser" ? "wasser" : medium?.trim().toLowerCase() === "strom" ? "strom" : null;
const mediumName = (medium: string | null) => normalizeMedium(medium) === "wasser" ? "Wasser" : "Strom";
const parcelName = (parcel: Pick<RfidParcel, "garten_nr" | "Anlage">) => `${parcel.garten_nr} – ${parcel.Anlage}`;
const contextParcelName = (context: RfidScanContext) => `${context.garten_nr ?? "–"} – ${context.anlage ?? "–"}`;

export async function listRfidSetupParcels(session: BrowserSession) {
  return (await listRfidParcels(session)).sort((left, right) => left.garten_nr.localeCompare(right.garten_nr, "de", { numeric: true }) || left.Anlage.localeCompare(right.Anlage, "de"));
}

export async function resolveRfidScanContext(session: BrowserSession, uid: string): Promise<RfidScanResolution> {
  const normalizedUid = normalizeUid(uid);
  if (!normalizedUid) return { normalizedUid: "", state: "Unknown", context: null, message: "Bitte eine RFID-UID eingeben." };
  const contexts = await listRfidScanContexts(session);
  const context = contexts.filter((item) => normalizeUid(item.rfid_tag_uid ?? "") === normalizedUid).sort((left, right) => Number(Boolean(right.aktiver_zaehler_id)) - Number(Boolean(left.aktiver_zaehler_id)))[0] ?? null;
  if (!context) return { normalizedUid, state: "Unknown", context: null, message: `Für die UID ${normalizedUid} wurde kein RFID-Kontext gefunden.` };
  const state: RfidScanState = context.aktiver_zaehler_id ? "KnownWithActiveMeter" : "KnownWithoutActiveMeter";
  return { normalizedUid, state, context, message: state === "KnownWithActiveMeter" ? `RFID-Kontext für ${contextParcelName(context)} mit aktivem ${mediumName(context.medium).toLowerCase()}zähler geladen.` : `RFID-Kontext für ${contextParcelName(context)} geladen, aktuell jedoch ohne aktiven Zähler.` };
}

export async function listAvailableRfidMediumOptions(session: BrowserSession, parcelId: number): Promise<RfidMediumOption[]> {
  const parcel = (await listRfidParcels(session)).find((item) => item.id === parcelId);
  if (!parcel) return [];
  const activeMeterMedia = new Set((await listActiveMeterMedia(session, parcelId)).map((item) => normalizeMedium(item.medium)));
  const hasStrom = parcel.hat_strom || Boolean(normalizeUid(parcel.rfid_strom ?? "")) || activeMeterMedia.has("strom");
  const hasWasser = parcel.hat_wasser || Boolean(normalizeUid(parcel.rfid_wasser ?? "")) || activeMeterMedia.has("wasser");
  const options: RfidMediumOption[] = [];
  if (hasStrom) options.push({ key: "strom", displayName: "Strom" });
  if (hasWasser) options.push({ key: "wasser", displayName: "Wasser" });
  return options.length ? options : [{ key: "strom", displayName: "Strom" }, { key: "wasser", displayName: "Wasser" }];
}

export async function checkRfidAssignment(session: BrowserSession, parcelId: number, medium: string, uid: string): Promise<RfidAssignmentCheck> {
  const normalizedUid = normalizeUid(uid);
  const normalizedMedium = normalizeMedium(medium);
  if (!parcelId) return { isValid: false, requiresOverwriteConfirmation: false, alreadyAssignedToTarget: false, message: "Bitte zuerst eine Parzelle auswählen.", normalizedUid, currentTargetRfid: "", conflict: null };
  if (!normalizedMedium) return { isValid: false, requiresOverwriteConfirmation: false, alreadyAssignedToTarget: false, message: "Bitte ein gültiges Medium auswählen.", normalizedUid, currentTargetRfid: "", conflict: null };
  if (!normalizedUid) return { isValid: false, requiresOverwriteConfirmation: false, alreadyAssignedToTarget: false, message: "Bitte eine RFID-UID eingeben.", normalizedUid, currentTargetRfid: "", conflict: null };
  const [parcels, contexts] = await Promise.all([listRfidParcels(session), listRfidScanContexts(session)]);
  const parcel = parcels.find((item) => item.id === parcelId);
  if (!parcel) return { isValid: false, requiresOverwriteConfirmation: false, alreadyAssignedToTarget: false, message: "Die gewählte Parzelle konnte nicht geladen werden.", normalizedUid, currentTargetRfid: "", conflict: null };
  const options = await listAvailableRfidMediumOptions(session, parcelId);
  if (!options.some((item) => item.key === normalizedMedium)) return { isValid: false, requiresOverwriteConfirmation: false, alreadyAssignedToTarget: false, message: "Das gewählte Medium ist für diese Parzelle aktuell nicht auswählbar.", normalizedUid, currentTargetRfid: "", conflict: null };
  const conflict = contexts.find((item) => normalizeUid(item.rfid_tag_uid ?? "") === normalizedUid && (item.parzelle_id !== parcelId || normalizeMedium(item.medium) !== normalizedMedium)) ?? null;
  if (conflict) return { isValid: false, requiresOverwriteConfirmation: false, alreadyAssignedToTarget: false, message: `Die UID ${normalizedUid} ist bereits bei ${contextParcelName(conflict)} für ${mediumName(conflict.medium)} hinterlegt.`, normalizedUid, currentTargetRfid: "", conflict };
  const currentTargetRfid = normalizeUid(normalizedMedium === "strom" ? parcel.rfid_strom ?? "" : parcel.rfid_wasser ?? "");
  if (currentTargetRfid === normalizedUid) return { isValid: true, requiresOverwriteConfirmation: false, alreadyAssignedToTarget: true, message: `Die UID ${normalizedUid} ist für ${mediumName(normalizedMedium)} bei ${parcelName(parcel)} bereits hinterlegt.`, normalizedUid, currentTargetRfid, conflict: null };
  if (currentTargetRfid) return { isValid: true, requiresOverwriteConfirmation: true, alreadyAssignedToTarget: false, message: `Für ${mediumName(normalizedMedium)} ist bei ${parcelName(parcel)} bereits die RFID ${currentTargetRfid} hinterlegt. Bitte das Überschreiben ausdrücklich bestätigen.`, normalizedUid, currentTargetRfid, conflict: null };
  return { isValid: true, requiresOverwriteConfirmation: false, alreadyAssignedToTarget: false, message: `Prüfung erfolgreich. Die UID ${normalizedUid} kann für ${mediumName(normalizedMedium)} bei ${parcelName(parcel)} gespeichert werden.`, normalizedUid, currentTargetRfid: "", conflict: null };
}

export async function assignRfid(session: BrowserSession, parcelId: number, medium: string, uid: string, overwriteExisting = false): Promise<RfidAssignmentResult> {
  const check = await checkRfidAssignment(session, parcelId, medium, uid);
  if (!check.isValid) return { success: false, requiresOverwriteConfirmation: false, message: check.message, normalizedUid: check.normalizedUid };
  if (check.requiresOverwriteConfirmation && !overwriteExisting) return { success: false, requiresOverwriteConfirmation: true, message: check.message, normalizedUid: check.normalizedUid };
  if (check.alreadyAssignedToTarget) return { success: true, requiresOverwriteConfirmation: false, message: check.message, normalizedUid: check.normalizedUid };
  await assignParcelRfid(session, parcelId, normalizeMedium(medium)!, check.normalizedUid);
  return { success: true, requiresOverwriteConfirmation: false, message: `Die RFID ${check.normalizedUid} wurde für ${mediumName(medium)} gespeichert.`, normalizedUid: check.normalizedUid };
}
