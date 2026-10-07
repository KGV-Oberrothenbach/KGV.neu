import { type BrowserSession } from "../../lib/supabase-auth";
import {
  listMeterDueStatuses,
  listMeterOverviewParcels,
  listMeters,
  type Meter,
  type MeterDueStatus,
  type MeterOverviewParcel,
} from "../../repositories/meters/meter-repository";

export type MeterOverviewData = {
  meters: Meter[];
  parcels: MeterOverviewParcel[];
  dueStatuses: MeterDueStatus[];
};

const gardenNumberSortKey = (gardenNumber: string | null) => {
  const text = gardenNumber?.trim() ?? "";
  const number = text.match(/^\d+/)?.[0];
  return `${number ? number.padStart(12, "0") : "999999999999"}|${text}`;
};

const compareByGardenNumber = (left: MeterDueStatus, right: MeterDueStatus) =>
  gardenNumberSortKey(left.garten_nr).localeCompare(gardenNumberSortKey(right.garten_nr), "de", { sensitivity: "base" })
  || (left.anlage ?? "").localeCompare(right.anlage ?? "", "de", { sensitivity: "base" })
  || (left.medium ?? "").localeCompare(right.medium ?? "", "de", { sensitivity: "base" })
  || (left.zaehlernummer ?? "").localeCompare(right.zaehlernummer ?? "", "de", { sensitivity: "base" });

export const meterDueStatusLabel = (status: string | null) => {
  switch (status?.trim().toLowerCase()) {
    case "ueberfaellig": return "Überfällig";
    case "bald_faellig": return "Bald fällig";
    default: return "OK";
  }
};

export async function loadMeterOverview(session: BrowserSession): Promise<MeterOverviewData> {
  const [meters, parcels, dueStatuses] = await Promise.all([
    listMeters(session),
    listMeterOverviewParcels(session),
    listMeterDueStatuses(session),
  ]);

  return { meters, parcels, dueStatuses: [...dueStatuses].sort(compareByGardenNumber) };
}
