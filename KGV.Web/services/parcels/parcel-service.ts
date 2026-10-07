import { type BrowserSession } from "../../lib/supabase-auth";
import {
  listParcelAssignments,
  listParcelGardens,
  listParcelOverviewAssignments,
  listParcelOverviewMembers,
  listParcelOverviewParcels,
  updateParcelMasterData,
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
