import { type BrowserSession } from "../../lib/supabase-auth";
import { loadMeterReferenceData } from "../meters/meter-service";
import { formatParcelTenantName, isParcelAssignmentActiveOn } from "../parcels/parcel-service";
import {
  conditionalUpdateReviewReading,
  getReviewReading,
  listReviewAssignments,
  listReviewMembers,
  listReviewReadings,
  type ReviewMutation,
  type ReviewReadingRecord,
} from "../../repositories/readings/reading-review-repository";

export type ReadingReviewStatus = "eingereicht" | "freigegeben" | "abgelehnt";
export type ReadingReviewPermissions = { canApproveMeterReadings: boolean; reviewerMemberId: number | null };
export type ReadingReviewAction = "freigeben" | "ablehnen" | "korrigieren" | "entfernen";

export type ReadingReviewItem = {
  readingId: number;
  meterId: number;
  parcelId: number | null;
  gartenNr: string;
  anlage: string;
  medium: string;
  zaehlernummer: string;
  ablesedatum: string;
  stand: number;
  art: string;
  freigegeben: boolean;
  pruefstatus: ReadingReviewStatus;
  pruefkommentar: string | null;
  geprueftVon: number | null;
  geprueftAm: string | null;
  geprueftVonName: string;
  mitgliedId: number | null;
  mitgliedName: string;
  fotoPfad: string | null;
  fotoDateiname: string | null;
  fotoDriveFileId: string | null;
  decisionLabel: string;
};

export type ReadingReviewData = { open: ReadingReviewItem[]; reviewed: ReadingReviewItem[]; historyByReadingId: Map<number, ReadingReviewItem[]> };

const unavailable = "Quelle im Modell nicht verfügbar";
const correctionPrefix = "Korrigiert im Prüfprozess: ";
const removalPrefix = "Im Prüfprozess entfernt: ";

export function normalizeReadingReviewStatus(pruefstatus: string | null | undefined, freigegeben = false): ReadingReviewStatus {
  if (freigegeben) return "freigegeben";
  switch (pruefstatus?.trim().toLocaleLowerCase("de")) {
    case "freigegeben": case "genehmigt": case "approved": return "freigegeben";
    case "abgelehnt": case "rejected": return "abgelehnt";
    case "eingereicht": case "offen": case "pending": default: return "eingereicht";
  }
}

const isOpen = (reading: Pick<ReviewReadingRecord, "freigegeben" | "pruefstatus">) => !reading.freigegeben && normalizeReadingReviewStatus(reading.pruefstatus, reading.freigegeben) === "eingereicht";
const isValidDate = (value: string) => {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const [year, month, day] = value.split("-").map(Number);
  const date = new Date(year, month - 1, day, 12);
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
};

function decisionLabel(status: ReadingReviewStatus, comment: string | null) {
  if (status === "abgelehnt" && comment?.startsWith(removalPrefix)) return "Entfernt";
  if (status === "freigegeben" && comment?.startsWith(correctionPrefix)) return "Korrigiert / freigegeben";
  return status;
}

export async function loadReadingReviewData(session: BrowserSession, permissions: ReadingReviewPermissions): Promise<ReadingReviewData> {
  if (!permissions.canApproveMeterReadings) return { open: [], reviewed: [], historyByReadingId: new Map() };
  const [readings, references, assignments, members] = await Promise.all([
    listReviewReadings(session),
    loadMeterReferenceData(session),
    listReviewAssignments(session),
    listReviewMembers(session),
  ]);
  const meters = new Map(references.meters.map((meter) => [meter.id, meter]));
  const parcels = new Map(references.parcels.map((parcel) => [parcel.id, parcel]));
  const membersById = new Map(members.map((member) => [member.id, member]));
  const toItem = (reading: ReviewReadingRecord): ReadingReviewItem => {
    const meter = meters.get(reading.zaehler_id);
    const parcel = meter ? parcels.get(meter.parzelle_id) : undefined;
    const assignment = meter
      ? assignments.filter((item) => item.parzelle_id === meter.parzelle_id && isParcelAssignmentActiveOn(item, reading.ablesedatum.slice(0, 10))).sort((left, right) => (right.von_datum ?? "").localeCompare(left.von_datum ?? ""))[0]
      : undefined;
    const member = assignment ? membersById.get(assignment.mitglied_id) : undefined;
    const reviewer = reading.geprueft_von ? membersById.get(reading.geprueft_von) : undefined;
    const status = normalizeReadingReviewStatus(reading.pruefstatus, reading.freigegeben);
    return {
      readingId: reading.id, meterId: reading.zaehler_id, parcelId: meter?.parzelle_id ?? null,
      gartenNr: parcel?.garten_nr ?? unavailable, anlage: parcel?.Anlage ?? "", medium: meter?.medium ?? unavailable, zaehlernummer: meter?.zaehlernummer ?? unavailable,
      ablesedatum: reading.ablesedatum, stand: reading.stand, art: reading.art, freigegeben: reading.freigegeben, pruefstatus: status,
      pruefkommentar: reading.pruefkommentar, geprueftVon: reading.geprueft_von, geprueftAm: reading.geprueft_am,
      geprueftVonName: reading.geprueft_von ? formatParcelTenantName(reviewer) : unavailable,
      mitgliedId: assignment?.mitglied_id ?? null, mitgliedName: assignment && member ? formatParcelTenantName(member) : unavailable,
      fotoPfad: reading.foto_pfad, fotoDateiname: reading.foto_dateiname, fotoDriveFileId: reading.foto_drive_file_id,
      decisionLabel: decisionLabel(status, reading.pruefkommentar),
    };
  };
  const items = readings.map(toItem);
  const open = items.filter((item) => !item.freigegeben && item.pruefstatus === "eingereicht");
  const reviewed = items.filter((item) => !( !item.freigegeben && item.pruefstatus === "eingereicht"));
  const historyByReadingId = new Map(open.map((item) => [item.readingId, items.filter((candidate) => candidate.meterId === item.meterId && candidate.readingId !== item.readingId).sort((left, right) => right.ablesedatum.localeCompare(left.ablesedatum)).slice(0, 5)]));
  return { open, reviewed, historyByReadingId };
}

export async function reviewMeterReading(session: BrowserSession, permissions: ReadingReviewPermissions, input: { readingId: number; action: ReadingReviewAction; comment: string; date?: string; value?: number }) {
  if (!permissions.canApproveMeterReadings) throw new Error("Für die Prüfung besteht keine Berechtigung.");
  if (!Number.isInteger(permissions.reviewerMemberId) || (permissions.reviewerMemberId ?? 0) <= 0) throw new Error("Für die Prüfung ist ein verknüpftes Mitgliedskonto erforderlich.");
  if (!Number.isInteger(input.readingId) || input.readingId <= 0) throw new Error("Die Ablesung ist ungültig.");
  const comment = input.comment.trim();
  if (!comment) throw new Error("Ein Prüfkommentar ist erforderlich.");
  const existing = await getReviewReading(session, input.readingId);
  if (!existing || !isOpen(existing)) throw new Error("Ablesung wurde inzwischen bereits bearbeitet.");
  if (input.action === "korrigieren" && (!input.date || !isValidDate(input.date) || !Number.isFinite(input.value) || (input.value ?? -1) < 0)) throw new Error("Für die Korrektur müssen Datum und Zählerstand gültig sein.");
  const rejected = input.action === "ablehnen" || input.action === "entfernen";
  const mutation: ReviewMutation = {
    ablesedatum: input.action === "korrigieren" ? input.date! : existing.ablesedatum,
    stand: input.action === "korrigieren" ? input.value! : existing.stand,
    pruefstatus: rejected ? "abgelehnt" : "freigegeben",
    pruefkommentar: `${input.action === "korrigieren" ? correctionPrefix : input.action === "entfernen" ? removalPrefix : ""}${comment}`,
    geprueft_von: permissions.reviewerMemberId,
    geprueft_am: new Date().toISOString(),
    freigegeben: !rejected,
  };
  const updated = await conditionalUpdateReviewReading(session, input.readingId, mutation);
  if (!updated[0]) throw new Error("Ablesung wurde inzwischen bereits bearbeitet.");
  return updated[0];
}
