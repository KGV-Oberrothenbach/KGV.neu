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

const today = () => new Date().toISOString().slice(0, 10);
const isActiveMeter = (meter: Awaited<ReturnType<typeof listReadingMeters>>[number]) => !meter.ausgebaut_am && (!meter.status || meter.status.toLocaleLowerCase("de") === "aktiv");
const validDate = (value: string) => /^\d{4}-\d{2}-\d{2}$/.test(value) && !Number.isNaN(new Date(`${value}T12:00:00`).valueOf());

async function loadScope(session: BrowserSession, permissions: ReadingPermissions, date: string) {
  const [submissionsAllowed, photoRequired, assignments] = await Promise.all([
    getBooleanSetting(session, "allow_user_meter_reading_submissions", false),
    getBooleanSetting(session, "meter_reading_photo_required", true),
    permissions.memberId && permissions.canSubmitOwnMeterReadings ? listMemberReadingAssignments(session, permissions.memberId) : Promise.resolve([]),
  ]);
  const isDirectCapture = permissions.canReadMeters;
  const canSubmitOwn = !isDirectCapture && permissions.canSubmitOwnMeterReadings && permissions.memberId !== null && submissionsAllowed;
  const ownParcelIds = [...new Set(assignments.map((assignment) => assignment.parzelle_id))];
  const [meters, parcels] = isDirectCapture
    ? await Promise.all([listReadingMeters(session), listReadingParcels(session)])
    : await Promise.all([listReadingMetersForParcels(session, ownParcelIds), listReadingParcelsByIds(session, ownParcelIds)]);
  const allowedParcelIds = isDirectCapture
    ? new Set(parcels.filter((parcel) => parcel.aktiv).map((parcel) => parcel.id))
    : new Set(assignments.filter((assignment) => isParcelAssignmentActiveOn(assignment, date)).map((assignment) => assignment.parzelle_id));
  return { meters, parcels, submissionsAllowed, photoRequired, assignments, isDirectCapture, canSubmitOwn, allowedParcelIds };
}

export async function loadReadingCaptureContext(session: BrowserSession, permissions: ReadingPermissions): Promise<ReadingCaptureContext> {
  const scope = await loadScope(session, permissions, today());
  if (!scope.isDirectCapture && !scope.canSubmitOwn) {
    return { meters: [], parcels: [], submissionsAllowed: scope.submissionsAllowed, photoRequired: scope.photoRequired, isDirectCapture: false, message: permissions.canSubmitOwnMeterReadings ? "Eigene Zählerablesungen sind aktuell zentral deaktiviert." : "Für Zählerablesungen besteht keine Berechtigung." };
  }
  const parcels = scope.parcels.filter((parcel) => parcel.aktiv && scope.allowedParcelIds.has(parcel.id));
  const parcelIds = new Set(parcels.map((parcel) => parcel.id));
  return { meters: scope.meters.filter((meter) => isActiveMeter(meter) && parcelIds.has(meter.parzelle_id)), parcels, submissionsAllowed: scope.submissionsAllowed, photoRequired: scope.photoRequired, isDirectCapture: scope.isDirectCapture, message: null };
}

export async function saveMeterReading(session: BrowserSession, permissions: ReadingPermissions, input: SaveReadingInput): Promise<SavedReading> {
  if (!Number.isInteger(input.meterId) || input.meterId <= 0) throw new Error("Bitte einen aktiven Zähler auswählen.");
  if (!validDate(input.date)) throw new Error("Bitte ein gültiges Ablesedatum eingeben.");
  if (!Number.isFinite(input.value) || input.value < 0) throw new Error("Bitte einen gültigen nichtnegativen Zählerstand eingeben.");
  if (input.art !== "normal" && input.art !== "jea") throw new Error("Die Ableseart ist ungültig.");
  const scope = await loadScope(session, permissions, input.date);
  if (!scope.isDirectCapture && !scope.canSubmitOwn) throw new Error(permissions.canSubmitOwnMeterReadings ? "Eigene Zählerablesungen sind aktuell zentral deaktiviert." : "Für Zählerablesungen besteht keine Berechtigung.");
  const meter = scope.meters.find((item) => item.id === input.meterId);
  if (!meter || !isActiveMeter(meter)) throw new Error("Der gewählte Zähler ist nicht aktiv.");
  const parcel = scope.parcels.find((item) => item.id === meter.parzelle_id);
  if (!parcel?.aktiv) throw new Error("Die Parzelle des Zählers ist nicht aktiv.");
  if (!scope.isDirectCapture && !scope.assignments.some((assignment) => assignment.parzelle_id === meter.parzelle_id && isParcelAssignmentActiveOn(assignment, input.date))) throw new Error("Eigene Nutzerablesungen sind nur für die eigene, am Ablesedatum belegte Parzelle zulässig.");
  const readings = await listReadingsForMeter(session, meter.id);
  const latest = readings.filter((item) => item.zaehler_id === meter.id && item.freigegeben).sort((left, right) => right.ablesedatum.localeCompare(left.ablesedatum))[0];
  if (latest && latest.ablesedatum.slice(0, 10) <= input.date && input.value < latest.stand) throw new Error(`Der Stand darf nicht unter der letzten freigegebenen Ablesung (${latest.stand}) liegen.`);
  if (readings.some((item) => item.zaehler_id === meter.id && item.ablesedatum.slice(0, 10) === input.date && item.art.toLowerCase() === input.art && item.pruefstatus !== "abgelehnt")) throw new Error("Für diesen Zähler, dieses Datum und diese Ableseart existiert bereits eine Ablesung.");
  const isSubmission = !scope.isDirectCapture;
  const created = await createMeterReading(session, { zaehler_id: meter.id, stand: input.value, ablesedatum: input.date, art: input.art, freigegeben: !isSubmission, pruefstatus: isSubmission ? "eingereicht" : "freigegeben" });
  if (!created[0]) throw new Error("Die Ablesung konnte nicht gespeichert werden.");
  return { reading: created[0], isSubmission };
}
