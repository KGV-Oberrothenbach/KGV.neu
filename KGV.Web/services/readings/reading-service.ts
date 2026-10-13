import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { isParcelAssignmentActiveOn } from "../parcels/parcel-service";
import {
  createMeterReading,
  getBooleanSetting,
  listMemberReadingAssignments,
  listReadingMeters,
  listReadingMetersForParcels,
  listReadingParcels,
  listReadingParcelsByIds,
  listReadingsForMeter,
  type MeterReading,
} from "../../repositories/readings/reading-repository";
import type { ParcelOverviewAssignment } from "../../repositories/parcels/parcel-repository";
import { filterParcelAssignmentReadings } from "./parcel-reading-history";

export type ReadingPermissions = { canReadMeters: boolean; canSubmitOwnMeterReadings: boolean; memberId: number | null };
export type ReadingCaptureContext = {
  meters: Awaited<ReturnType<typeof listReadingMeters>>;
  parcels: Awaited<ReturnType<typeof listReadingParcels>>;
  submissionsAllowed: boolean;
  photoRequired: boolean;
  isDirectCapture: boolean;
  message: string | null;
};
export type SaveReadingInput = { meterId: number; date: string; value: number; art: "normal" | "jea" };
export type SavedReading = { reading: MeterReading; isSubmission: boolean };
export type ReadingWarning = {
  code: "LOWER_THAN_PREVIOUS" | "HIGHER_THAN_NEXT";
  message: string;
  referenceReadingId: number;
  referenceDate: string;
  referenceValue: number;
};
export type ReadingBlockingError = { code: "INVALID_METER" | "INVALID_DATE" | "INVALID_VALUE" | "INVALID_ART"; message: string };
export type ReadingPlausibilityResult = { warnings: ReadingWarning[]; blockingErrors: ReadingBlockingError[] };
export type ReadingConfirmationRequired = { requiresConfirmation: true; plausibility: ReadingPlausibilityResult };
export type SaveMeterReadingResult = SavedReading | ReadingConfirmationRequired;
export type SaveMeterInstallationReadingResult = MeterReading | ReadingConfirmationRequired;
export type SaveMeterRemovalReadingResult = MeterReading | ReadingConfirmationRequired;

export async function loadParcelAssignmentReadingHistory(club: ClubContext, session: BrowserSession, assignment: ParcelOverviewAssignment) {
  if (!Number.isInteger(assignment.parzelle_id) || assignment.parzelle_id <= 0) throw new Error("Die Belegung enthält keine gültige Parzelle.");
  const meters = await listReadingMetersForParcels(club, session, [assignment.parzelle_id]);
  const readings = (await Promise.all(meters.map((meter) => listReadingsForMeter(club, session, meter.id)))).flat();
  return filterParcelAssignmentReadings(assignment, meters, readings);
}

const isActiveMeter = (meter: Awaited<ReturnType<typeof listReadingMeters>>[number]) => !meter.ausgebaut_am && (!meter.status || meter.status.toLocaleLowerCase("de") === "aktiv");
const validDate = (value: string) => {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const [year, month, day] = value.split("-").map(Number);
  const date = new Date(year, month - 1, day, 12);
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
};
const isParcelMediumEnabled = (parcel: Awaited<ReturnType<typeof listReadingParcels>>[number], medium: string) =>
  medium.trim().toLocaleLowerCase("de") === "strom" ? parcel.hat_strom : medium.trim().toLocaleLowerCase("de") === "wasser" ? parcel.hat_wasser : false;

const isApprovedReading = (reading: MeterReading) => reading.freigegeben || reading.pruefstatus.toLocaleLowerCase("de") === "freigegeben";

export function checkReadingPlausibility(readings: MeterReading[], input: { meterId: number; date: string; value: number; art: string }): ReadingPlausibilityResult {
  const blockingErrors: ReadingBlockingError[] = [];
  if (!Number.isInteger(input.meterId) || input.meterId <= 0) blockingErrors.push({ code: "INVALID_METER", message: "Bitte einen gültigen Zähler auswählen." });
  if (!validDate(input.date)) blockingErrors.push({ code: "INVALID_DATE", message: "Bitte ein gültiges Ablesedatum eingeben." });
  if (!Number.isFinite(input.value) || input.value < 0) blockingErrors.push({ code: "INVALID_VALUE", message: "Bitte einen gültigen nichtnegativen Zählerstand eingeben." });
  if (!new Set(["normal", "jea", "einbau", "ausbau"]).has(input.art)) blockingErrors.push({ code: "INVALID_ART", message: "Die Ableseart ist ungültig." });
  if (blockingErrors.length) return { warnings: [], blockingErrors };

  const approved = readings.filter((reading) => reading.zaehler_id === input.meterId && isApprovedReading(reading));
  const previous = approved.filter((reading) => reading.ablesedatum.slice(0, 10) < input.date).sort((left, right) => right.ablesedatum.localeCompare(left.ablesedatum) || right.id - left.id)[0];
  const next = approved.filter((reading) => reading.ablesedatum.slice(0, 10) > input.date).sort((left, right) => left.ablesedatum.localeCompare(right.ablesedatum) || left.id - right.id)[0];
  const warnings: ReadingWarning[] = [];
  if (previous && input.value < previous.stand) warnings.push({ code: "LOWER_THAN_PREVIOUS", referenceReadingId: previous.id, referenceDate: previous.ablesedatum.slice(0, 10), referenceValue: previous.stand, message: `Der neue Zählerstand (${input.value}) liegt unter der vorherigen freigegebenen Ablesung vom ${previous.ablesedatum.slice(0, 10)} (${previous.stand}). Bitte Zähler, Eingabe sowie einen möglichen Rückwärtslauf oder Defekt vor Ort prüfen.` });
  if (next && input.value > next.stand) warnings.push({ code: "HIGHER_THAN_NEXT", referenceReadingId: next.id, referenceDate: next.ablesedatum.slice(0, 10), referenceValue: next.stand, message: `Der neue Zählerstand (${input.value}) liegt über der nachfolgenden freigegebenen Ablesung vom ${next.ablesedatum.slice(0, 10)} (${next.stand}). Bitte Zähler und Eingabe vor Ort prüfen.` });
  return { warnings, blockingErrors };
}

async function loadScope(club: ClubContext, session: BrowserSession, permissions: ReadingPermissions, date?: string) {
  const [submissionsAllowed, photoRequired, assignments] = await Promise.all([
    getBooleanSetting(club, session, "allow_user_meter_reading_submissions", false),
    getBooleanSetting(club, session, "meter_reading_photo_required", true),
    permissions.memberId && permissions.canSubmitOwnMeterReadings ? listMemberReadingAssignments(club, session, permissions.memberId) : Promise.resolve([]),
  ]);
  const isDirectCapture = permissions.canReadMeters;
  const canSubmitOwn = !isDirectCapture && permissions.canSubmitOwnMeterReadings && permissions.memberId !== null && submissionsAllowed;
  const ownParcelIds = [...new Set(assignments.map((assignment) => assignment.parzelle_id))];
  const [meters, parcels] = isDirectCapture
    ? await Promise.all([listReadingMeters(club, session), listReadingParcels(club, session)])
    : await Promise.all([listReadingMetersForParcels(club, session, ownParcelIds), listReadingParcelsByIds(club, session, ownParcelIds)]);
  const allowedParcelIds = isDirectCapture
    ? new Set(parcels.filter((parcel) => parcel.aktiv).map((parcel) => parcel.id))
    : new Set(assignments.filter((assignment) => date ? isParcelAssignmentActiveOn(assignment, date) : isParcelAssignmentActiveOn(assignment)).map((assignment) => assignment.parzelle_id));
  return { meters, parcels, submissionsAllowed, photoRequired, assignments, isDirectCapture, canSubmitOwn, allowedParcelIds };
}

export async function loadReadingCaptureContext(club: ClubContext, session: BrowserSession, permissions: ReadingPermissions): Promise<ReadingCaptureContext> {
  const scope = await loadScope(club, session, permissions);
  if (!scope.isDirectCapture && !scope.canSubmitOwn) {
    return { meters: [], parcels: [], submissionsAllowed: scope.submissionsAllowed, photoRequired: scope.photoRequired, isDirectCapture: false, message: permissions.canSubmitOwnMeterReadings ? "Eigene Zählerablesungen sind aktuell zentral deaktiviert." : "Für Zählerablesungen besteht keine Berechtigung." };
  }
  const parcels = scope.parcels.filter((parcel) => parcel.aktiv && scope.allowedParcelIds.has(parcel.id));
  const parcelIds = new Set(parcels.map((parcel) => parcel.id));
  return { meters: scope.meters.filter((meter) => isActiveMeter(meter) && parcelIds.has(meter.parzelle_id)), parcels, submissionsAllowed: scope.submissionsAllowed, photoRequired: scope.photoRequired, isDirectCapture: scope.isDirectCapture, message: null };
}

export async function saveMeterReading(club: ClubContext, session: BrowserSession, permissions: ReadingPermissions, input: SaveReadingInput & { confirmWarnings?: boolean }): Promise<SaveMeterReadingResult> {
  if (!Number.isInteger(input.meterId) || input.meterId <= 0) throw new Error("Bitte einen aktiven Zähler auswählen.");
  if (!validDate(input.date)) throw new Error("Bitte ein gültiges Ablesedatum eingeben.");
  if (!Number.isFinite(input.value) || input.value < 0) throw new Error("Bitte einen gültigen nichtnegativen Zählerstand eingeben.");
  if (input.art !== "normal" && input.art !== "jea") throw new Error("Die Ableseart ist ungültig.");
  const scope = await loadScope(club, session, permissions, input.date);
  if (!scope.isDirectCapture && !scope.canSubmitOwn) throw new Error(permissions.canSubmitOwnMeterReadings ? "Eigene Zählerablesungen sind aktuell zentral deaktiviert." : "Für Zählerablesungen besteht keine Berechtigung.");
  const meter = scope.meters.find((item) => item.id === input.meterId);
  if (!meter || !isActiveMeter(meter)) throw new Error("Der gewählte Zähler ist nicht aktiv.");
  const parcel = scope.parcels.find((item) => item.id === meter.parzelle_id);
  if (!parcel?.aktiv) throw new Error("Die Parzelle des Zählers ist nicht aktiv.");
  if (!isParcelMediumEnabled(parcel, meter.medium)) throw new Error("Das Medium des Zählers ist für diese Parzelle nicht freigeschaltet.");
  if (!scope.isDirectCapture && !scope.assignments.some((assignment) => assignment.parzelle_id === meter.parzelle_id && isParcelAssignmentActiveOn(assignment, input.date))) throw new Error("Eigene Nutzerablesungen sind nur für die eigene, am Ablesedatum belegte Parzelle zulässig.");
  const readings = await listReadingsForMeter(club, session, meter.id);
  if (readings.some((item) => item.zaehler_id === meter.id && item.ablesedatum.slice(0, 10) === input.date && item.art.toLowerCase() === input.art && item.pruefstatus !== "abgelehnt")) throw new Error("Für diesen Zähler, dieses Datum und diese Ableseart existiert bereits eine Ablesung.");
  const plausibility = checkReadingPlausibility(readings, input);
  if (plausibility.blockingErrors.length) throw new Error(plausibility.blockingErrors[0].message);
  if (plausibility.warnings.length && !input.confirmWarnings) return { requiresConfirmation: true, plausibility };
  const isSubmission = !scope.isDirectCapture;
  const created = await createMeterReading(club, session, { zaehler_id: meter.id, stand: input.value, ablesedatum: input.date, art: input.art, freigegeben: !isSubmission, pruefstatus: isSubmission ? "eingereicht" : "freigegeben" });
  if (!created[0]) throw new Error("Die Ablesung konnte nicht gespeichert werden.");
  return { reading: created[0], isSubmission };
}

export async function getMeterInstallationPhotoRequired(club: ClubContext, session: BrowserSession) {
  return getBooleanSetting(club, session, "meter_reading_photo_required", true);
}

export async function saveMeterInstallationReading(club: ClubContext, session: BrowserSession, permissions: { canManageMeterChanges: boolean }, input: { meterId: number; date: string; value: number; confirmWarnings?: boolean }): Promise<SaveMeterInstallationReadingResult> {
  if (!permissions.canManageMeterChanges) throw new Error("Für die Einbauablesung besteht keine Berechtigung.");
  if (!Number.isInteger(input.meterId) || input.meterId <= 0) throw new Error("Der eingebaute Zähler ist ungültig.");
  if (!validDate(input.date)) throw new Error("Bitte ein gültiges Ablesedatum eingeben.");
  if (!Number.isFinite(input.value) || input.value < 0) throw new Error("Bitte einen gültigen nichtnegativen Zählerstand eingeben.");
  const meter = (await listReadingMeters(club, session)).find((item) => item.id === input.meterId);
  if (!meter || !isActiveMeter(meter)) throw new Error("Der eingebaute Zähler ist nicht aktiv.");
  const readings = await listReadingsForMeter(club, session, meter.id);
  if (readings.some((item) => item.ablesedatum.slice(0, 10) === input.date && item.art.toLowerCase() === "einbau")) throw new Error("Für diesen Zähler und dieses Einbaudatum existiert bereits eine Einbauablesung.");
  const plausibility = checkReadingPlausibility(readings, { ...input, art: "einbau" });
  if (plausibility.blockingErrors.length) throw new Error(plausibility.blockingErrors[0].message);
  if (plausibility.warnings.length && !input.confirmWarnings) return { requiresConfirmation: true, plausibility };
  const created = await createMeterReading(club, session, { zaehler_id: meter.id, stand: input.value, ablesedatum: input.date, art: "einbau", freigegeben: true, pruefstatus: "freigegeben" });
  if (!created[0]) throw new Error("Die Einbauablesung konnte nicht gespeichert werden.");
  return created[0];
}

export async function saveMeterRemovalReading(club: ClubContext, session: BrowserSession, permissions: { canManageMeterChanges: boolean }, input: { meterId: number; date: string; value: number; confirmWarnings?: boolean }): Promise<SaveMeterRemovalReadingResult> {
  if (!permissions.canManageMeterChanges) throw new Error("Für die Ausbauablesung besteht keine Berechtigung.");
  if (!Number.isInteger(input.meterId) || input.meterId <= 0 || !validDate(input.date) || !Number.isFinite(input.value) || input.value < 0) throw new Error("Ausbaudatum und Endstand müssen gültig sein.");
  const meter = (await listReadingMeters(club, session)).find((item) => item.id === input.meterId);
  if (!meter || !isActiveMeter(meter)) throw new Error("Der auszubauende Zähler ist nicht aktiv.");
  const readings = await listReadingsForMeter(club, session, meter.id);
  const existing = readings.find((item) => item.ablesedatum.slice(0, 10) === input.date && item.art.toLowerCase() === "ausbau");
  if (existing) return existing;
  const plausibility = checkReadingPlausibility(readings, { ...input, art: "ausbau" });
  if (plausibility.blockingErrors.length) throw new Error(plausibility.blockingErrors[0].message);
  if (plausibility.warnings.length && !input.confirmWarnings) return { requiresConfirmation: true, plausibility };
  const created = await createMeterReading(club, session, { zaehler_id: meter.id, stand: input.value, ablesedatum: input.date, art: "ausbau", freigegeben: true, pruefstatus: "freigegeben" });
  if (!created[0]) throw new Error("Die Ausbauablesung konnte nicht gespeichert werden.");
  return created[0];
}
