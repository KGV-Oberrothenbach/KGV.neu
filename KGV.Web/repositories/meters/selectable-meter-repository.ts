import { readSupabaseRest } from "../../lib/supabase/rest-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";

export type SelectableActiveMeter = {
  id: number;
  medium: string;
  zaehlernummer: string;
  parzelle_id: number | null;
  ist_hauptzaehler: boolean;
  anlagenbereich: string | null;
  eingebaut_am: string;
  display_name: string;
};

const isActiveOn = (meter: SelectableMeterRow, onDate: string) =>
  meter.status?.toLocaleLowerCase("de") === "aktiv"
  && !meter.ausgebaut_am
  && meter.eingebaut_am.slice(0, 10) <= onDate;

type SelectableMeterRow = Omit<SelectableActiveMeter, "display_name"> & { status: string | null; ausgebaut_am: string | null };

const displayName = (meter: SelectableMeterRow) => {
  const medium = meter.medium.toLocaleLowerCase("de") === "wasser" ? "Wasser" : "Strom";
  const bereich = meter.anlagenbereich?.trim();
  return meter.ist_hauptzaehler ? `${medium}${bereich ? ` ${bereich}` : ""}` : `${medium} – ${meter.zaehlernummer}`;
};

/** UI-neutraler Auswahlvertrag. Die bestehende JEA-Oberfläche bindet ihn erst in G3 an. */
export async function listSelectableActiveMeters(club: ClubContext, session: BrowserSession, input: { parcelId?: number; mainMeterMode: boolean; onDate: string }): Promise<SelectableActiveMeter[]> {
  const rows = await readSupabaseRest<SelectableMeterRow>({ club, session }, "zaehler", {
    select: "id,medium,zaehlernummer,parzelle_id,ist_hauptzaehler,anlagenbereich,eingebaut_am,ausgebaut_am,status",
    ist_hauptzaehler: input.mainMeterMode ? "eq.true" : "eq.false",
    ...(input.mainMeterMode ? { parzelle_id: "is.null" } : input.parcelId ? { parzelle_id: `eq.${input.parcelId}` } : { id: "eq.-1" }),
    limit: "100",
  });
  return rows.filter((meter) => isActiveOn(meter, input.onDate)).map((meter) => ({
    id: meter.id, medium: meter.medium, zaehlernummer: meter.zaehlernummer, parzelle_id: meter.parzelle_id,
    ist_hauptzaehler: meter.ist_hauptzaehler, anlagenbereich: meter.anlagenbereich, eingebaut_am: meter.eingebaut_am,
    display_name: displayName(meter),
  }));
}
