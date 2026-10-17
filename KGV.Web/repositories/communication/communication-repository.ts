import { readSupabaseRest, writeSupabaseRest } from "../../lib/supabase/rest-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { CommunicationItem } from "../../models/communication/communication-item";

const select = "id,vorgangstyp,dokumenttyp,status,mitglied_id,parzelle_id,revision,signatur_revision,signatur_gestartet_am,empfaenger_email,betreff,mailtext,versendet_am,fehlertext,gedruckt_am,archiviert_am,zugewiesen_an,erstellt_am,erstellt_von,bearbeitet_am,bearbeitet_von,mitglied:mitglied_id(id,vorname,name),parzelle:parzelle_id(id,garten_nr)";

export function listCommunicationItems(club: ClubContext, session: BrowserSession, query: Record<string, string>) {
  return readSupabaseRest<CommunicationItem>({ club, session }, "kommunikationsvorgang", { select, order: "bearbeitet_am.desc,erstellt_am.desc,id.desc", limit: "500", ...query });
}

export async function claimCommunicationItem(club: ClubContext, session: BrowserSession, id: number, userId: string) {
  const rows = await writeSupabaseRest<CommunicationItem>({ club, session }, "kommunikationsvorgang", "PATCH", { zugewiesen_an: userId }, { id: `eq.${id}`, zugewiesen_an: "is.null" });
  if (rows.length !== 1) throw new Error("Dieser Vorgang wurde zwischenzeitlich von einer anderen Person übernommen.");
  return rows[0];
}

export async function releaseCommunicationItem(club: ClubContext, session: BrowserSession, id: number, userId: string) {
  const rows = await writeSupabaseRest<CommunicationItem>({ club, session }, "kommunikationsvorgang", "PATCH", { zugewiesen_an: null }, { id: `eq.${id}`, zugewiesen_an: `eq.${userId}` });
  if (rows.length !== 1) throw new Error("Der Vorgang ist nicht mehr dir zugewiesen.");
  return rows[0];
}
