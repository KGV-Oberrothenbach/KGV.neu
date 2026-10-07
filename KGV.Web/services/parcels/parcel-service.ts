import { type BrowserSession } from "../../lib/supabase-auth";
import {
  listParcelAssignments,
  listParcelGardens,
  listParcelOverviewAssignments,
  listParcelOverviewMembers,
  listParcelOverviewParcels,
  listParcelOverviewParcelsByIds,
  listMemberParcelAssignments,
  listParcelAssignmentsForParcel,
  updateParcelMasterData,
  createParcelAssignment,
  getParcelAssignment,
  updateParcelAssignmentEnd,
  type ParcelOverviewAssignment,
  type ParcelOverviewMember,
  type ParcelOverviewParcel,
} from "../../repositories/parcels/parcel-repository";

export type ParcelMasterDataSaveInput = {
  parcelId: number;
  flaeche_qm: number | null;
  hat_strom: boolean;
  hat_wasser: boolean;
};

export const saveParcelMasterData = (session: BrowserSession, { parcelId, flaeche_qm, hat_strom, hat_wasser }: ParcelMasterDataSaveInput) =>
  updateParcelMasterData(session, parcelId, { flaeche_qm, hat_strom, hat_wasser });

export type ParcelAssignmentInput = { memberId: number; parcelId: number; startDate: string };

export async function listAssignableParcels(session: BrowserSession) {
  const [parcels, assignments] = await Promise.all([listParcelOverviewParcels(session), listParcelOverviewAssignments(session)]);
  const occupiedParcelIds = new Set(assignments.filter((assignment) => isParcelAssignmentActiveOn(assignment)).map((assignment) => assignment.parzelle_id));
  return parcels
    .filter((parcel) => parcel.aktiv && !occupiedParcelIds.has(parcel.id))
    .sort((left, right) => gardenNumberSortKey(left.garten_nr).localeCompare(gardenNumberSortKey(right.garten_nr), "de", { sensitivity: "base" }));
}

export async function assignParcelToMember(session: BrowserSession, { memberId, parcelId, startDate }: ParcelAssignmentInput) {
  if (!Number.isInteger(memberId) || memberId <= 0 || !Number.isInteger(parcelId) || parcelId <= 0) throw new Error("Mitglied und Parzelle müssen gültig sein.");
  if (!/^\d{4}-\d{2}-\d{2}$/.test(startDate)) throw new Error("Das Zuweisungsdatum ist ungültig.");
  const date = new Date(`${startDate}T12:00:00`);
  if (Number.isNaN(date.valueOf()) || `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}` !== startDate) throw new Error("Das Zuweisungsdatum ist ungültig.");
  const assignments = await listParcelAssignmentsForParcel(session, parcelId);
  if (assignments.some((assignment) => isParcelAssignmentActiveOn(assignment, startDate))) throw new Error("Die Parzelle ist zum gewählten Datum bereits belegt.");
  return createParcelAssignment(session, { parzelle_id: parcelId, mitglied_id: memberId, von_datum: startDate, bis_datum: null });
}

export async function endParcelAssignment(session: BrowserSession, { assignmentId, endDate, reason }: { assignmentId: number; endDate: string; reason?: string }) {
  if (!Number.isInteger(assignmentId) || assignmentId <= 0) throw new Error("Die Belegung ist ungültig.");
  if (!/^\d{4}-\d{2}-\d{2}$/.test(endDate)) throw new Error("Das Enddatum ist ungültig.");
  const date = new Date(`${endDate}T12:00:00`);
  if (Number.isNaN(date.valueOf()) || `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}` !== endDate) throw new Error("Das Enddatum ist ungültig.");
  const assignment = await getParcelAssignment(session, assignmentId);
  if (!assignment) throw new Error("Die Belegung wurde nicht gefunden.");
  if (assignment.von_datum && endDate < assignment.von_datum) throw new Error("Das Enddatum darf nicht vor dem Beginn liegen.");
  const normalizedReason = reason?.trim() || null;
  if (normalizedReason && !["kuendigung", "tod", "wechsel", "sonstiges"].includes(normalizedReason)) throw new Error("Der Beendigungsgrund ist ungültig.");
  return updateParcelAssignmentEnd(session, assignmentId, { bis_datum: endDate, beendigungsgrund: normalizedReason });
}

export type MemberParcelItem = {
  assignment: ParcelOverviewAssignment;
  parcel: ParcelOverviewParcel | null;
  isCurrent: boolean;
};

export async function listMemberParcels(session: BrowserSession, memberId: number): Promise<MemberParcelItem[]> {
  const assignments = await listMemberParcelAssignments(session, memberId);
  const parcels = await listParcelOverviewParcelsByIds(session, [...new Set(assignments.map((assignment) => assignment.parzelle_id))]);
  const parcelsById = new Map(parcels.map((parcel) => [parcel.id, parcel]));
  return assignments
    .map((assignment) => ({ assignment, parcel: parcelsById.get(assignment.parzelle_id) ?? null, isCurrent: isParcelAssignmentActiveOn(assignment) }))
    .sort((left, right) => (right.assignment.von_datum ?? "").localeCompare(left.assignment.von_datum ?? ""));
}

export type ParcelOverviewItem = {
  parcel: ParcelOverviewParcel;
  currentAssignment: ParcelOverviewAssignment | null;
  currentTenant: ParcelOverviewMember | null;
  tenantDisplayName: string;
  isOccupied: boolean;
  statusText: "vergeben" | "frei";
  gardenNumberSortKey: string;
  searchText: string;
};

export type ParcelOverview = {
  parcels: ParcelOverviewParcel[];
  assignments: ParcelOverviewAssignment[];
  members: ParcelOverviewMember[];
  items: ParcelOverviewItem[];
};

const localDate = () => {
  const date = new Date();
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
};

export const isParcelAssignmentActiveOn = (assignment: Pick<ParcelOverviewAssignment, "von_datum" | "bis_datum">, date = localDate()) =>
  (!assignment.von_datum || assignment.von_datum <= date) && (!assignment.bis_datum || assignment.bis_datum >= date);

export function findCurrentParcelAssignment(assignments: ParcelOverviewAssignment[], date = localDate()) {
  return assignments
    .filter((assignment) => isParcelAssignmentActiveOn(assignment, date))
    .sort((left, right) => (right.von_datum ?? "").localeCompare(left.von_datum ?? ""))[0] ?? null;
}

export const gardenNumberSortKey = (gardenNumber: string) =>
  `${(gardenNumber.match(/\d/g)?.join("") ?? "").padStart(8, "0")}|${gardenNumber}`;

export function formatParcelTenantName(member?: ParcelOverviewMember | null) {
  if (!member) return "Nicht verpachtet";
  const lastName = member.name?.trim() ?? "";
  const firstName = member.vorname?.trim() ?? "";
  const fullName = [lastName, firstName].filter(Boolean).join(", ");
  return fullName || member.email?.trim() || `Mitglied #${member.id}`;
}

export async function listParcelOverview(session: BrowserSession): Promise<ParcelOverview> {
  const [parcels, assignments, members] = await Promise.all([
    listParcelOverviewParcels(session),
    listParcelOverviewAssignments(session),
    listParcelOverviewMembers(session),
  ]);
  const membersById = new Map(members.map((member) => [member.id, member]));
  const assignmentsByParcelId = new Map<number, ParcelOverviewAssignment[]>();
  for (const assignment of assignments) {
    const parcelAssignments = assignmentsByParcelId.get(assignment.parzelle_id) ?? [];
    parcelAssignments.push(assignment);
    assignmentsByParcelId.set(assignment.parzelle_id, parcelAssignments);
  }

  const items = parcels.map((parcel) => {
    const currentAssignment = findCurrentParcelAssignment(assignmentsByParcelId.get(parcel.id) ?? []);
    const currentTenant = currentAssignment ? membersById.get(currentAssignment.mitglied_id) ?? null : null;
    const isOccupied = currentAssignment !== null;
    const tenantDisplayName = formatParcelTenantName(currentTenant);
    return {
      parcel,
      currentAssignment,
      currentTenant,
      tenantDisplayName,
      isOccupied,
      statusText: isOccupied ? "vergeben" as const : "frei" as const,
      gardenNumberSortKey: gardenNumberSortKey(parcel.garten_nr),
      searchText: `${parcel.garten_nr} ${parcel.Anlage} ${tenantDisplayName}`.trim(),
    };
  }).sort((left, right) => left.gardenNumberSortKey.localeCompare(right.gardenNumberSortKey, "de", { sensitivity: "base" }));

  return { parcels, assignments, members, items };
}

export async function listCurrentGardenNumbersByMemberId(session: BrowserSession, memberIds: number[]) {
  const [assignments, parcels] = await Promise.all([
    listParcelAssignments(session, memberIds),
    listParcelGardens(session),
  ]);
  const today = new Date().toISOString().slice(0, 10);
  const gardenNumberByParcelId = new Map(parcels.map((parcel) => [parcel.id, parcel.garten_nr]));
  const result = new Map<number, string[]>();

  for (const assignment of assignments) {
    if ((assignment.von_datum && assignment.von_datum > today) || (assignment.bis_datum && assignment.bis_datum < today)) continue;
    const gardenNumber = gardenNumberByParcelId.get(assignment.parzelle_id);
    if (!gardenNumber) continue;
    const numbers = result.get(assignment.mitglied_id) ?? [];
    if (!numbers.includes(gardenNumber)) numbers.push(gardenNumber);
    result.set(assignment.mitglied_id, numbers);
  }

  for (const numbers of result.values()) numbers.sort((left, right) => left.localeCompare(right, "de", { numeric: true }));
  return result;
}
