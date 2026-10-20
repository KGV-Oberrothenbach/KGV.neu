import { readSupabaseRest } from "../../lib/supabase/rest-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";

type LeaseRelationshipRow = { id: number };
type LeaseContractVersionRow = { id: number };
export type ActiveLeaseRelationshipStatus = { relationshipId: number | null; contractVersionCount: number };

export async function getActiveLeaseRelationshipStatus(club: ClubContext, session: BrowserSession, parcelId: number): Promise<ActiveLeaseRelationshipStatus> {
  const active = await readSupabaseRest<LeaseRelationshipRow>({ club, session }, "pachtverhaeltnis", { select: "id", parzelle_id: `eq.${parcelId}`, beendet_am: "is.null", limit: "2" });
  if (active.length > 1) throw new Error("Für die Parzelle bestehen mehrere aktive Pachtverhältnisse.");
  if (active.length === 0) return { relationshipId: null, contractVersionCount: 0 };
  const versions = await readSupabaseRest<LeaseContractVersionRow>({ club, session }, "pachtvertragsfassung", { select: "id", pachtverhaeltnis_id: `eq.${active[0].id}`, limit: "2" });
  return { relationshipId: active[0].id, contractVersionCount: versions.length };
}
