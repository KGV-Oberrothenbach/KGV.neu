import { type BrowserSession } from "../../lib/supabase-auth";
import { listParcelAssignments, listParcelGardens } from "../../repositories/parcels/parcel-repository";

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
