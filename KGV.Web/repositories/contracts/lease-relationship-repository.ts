import { readSupabaseRest } from "../../lib/supabase/rest-client";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";

type LeaseRelationshipRow = { id: number };
type LeaseContractVersionRow = { id: number };
export type MemberLeaseParticipationRow = { pachtverhaeltnis_id: number; rolle: "hauptpaechter" | "mitpaechter"; gueltig_ab: string; gueltig_bis: string | null };
export type MemberLeaseRelationshipRow = { id: number; parzelle_id: number; urspruenglicher_pachtbeginn: string | null; beendet_am: string | null };
export type LeaseRelationshipParcelRow = { id: number; garten_nr: string | null; Anlage: string | null };
export type ActiveLeaseRelationshipStatus = { relationshipId: number | null; contractVersionCount: number };
export type LeaseContractVersionSummary = { id: number; vertragsnummer: number | null; vorgaenger_vertragsnummer: number | null; nachfolger_vertragsnummer: number | null; massgeblichkeit: "massgeblich" | "nicht_mehr_massgeblich"; nicht_mehr_massgeblich_am: string | null };

export async function listLeaseContractVersions(club: ClubContext, session: BrowserSession, parcelId: number) {
  return readSupabaseRest<LeaseContractVersionSummary>({ club, session }, "v_pachtvertragsfassung_uebersicht", {
    select: "id,vertragsnummer,vorgaenger_vertragsnummer,nachfolger_vertragsnummer,massgeblichkeit,nicht_mehr_massgeblich_am",
    parzelle_id: `eq.${parcelId}`,
    order: "id.desc",
    limit: "100",
  });
}

export async function prepareLeaseContractReplacement(club: ClubContext, session: BrowserSession, predecessorVersionId: number) {
  const rows = await callSupabaseRpc<Array<{ kommunikationsvorgang_id: number }>>({ club, session }, "prepare_pachtvertrag_ersatz", {
    p_vorgaenger_fassung_id: predecessorVersionId,
    p_assigned_to: session.user.id,
  });
  if (rows.length !== 1) throw new Error("Der Korrektur-/Ersatzvertrag konnte nicht vorbereitet werden.");
  return rows[0].kommunikationsvorgang_id;
}

export async function getActiveLeaseRelationshipStatus(club: ClubContext, session: BrowserSession, parcelId: number): Promise<ActiveLeaseRelationshipStatus> {
  const active = await readSupabaseRest<LeaseRelationshipRow>({ club, session }, "pachtverhaeltnis", { select: "id", parzelle_id: `eq.${parcelId}`, beendet_am: "is.null", limit: "2" });
  if (active.length > 1) throw new Error("Für die Parzelle bestehen mehrere aktive Pachtverhältnisse.");
  if (active.length === 0) return { relationshipId: null, contractVersionCount: 0 };
  const versions = await readSupabaseRest<LeaseContractVersionRow>({ club, session }, "pachtvertragsfassung", { select: "id", pachtverhaeltnis_id: `eq.${active[0].id}`, limit: "2" });
  return { relationshipId: active[0].id, contractVersionCount: versions.length };
}

export async function listMemberLeaseRelationships(club: ClubContext, session: BrowserSession, memberId: number) {
  const participations = await readSupabaseRest<MemberLeaseParticipationRow>({ club, session }, "pachtverhaeltnis_beteiligung", {
    select: "pachtverhaeltnis_id,rolle,gueltig_ab,gueltig_bis",
    mitglied_id: `eq.${memberId}`,
    order: "gueltig_ab.desc",
    limit: "1000",
  });
  const relationshipIds = [...new Set(participations.map((item) => item.pachtverhaeltnis_id))];
  if (relationshipIds.length === 0) return { participations, relationships: [] as MemberLeaseRelationshipRow[], parcels: [] as LeaseRelationshipParcelRow[] };
  const relationships = await readSupabaseRest<MemberLeaseRelationshipRow>({ club, session }, "pachtverhaeltnis", {
    select: "id,parzelle_id,urspruenglicher_pachtbeginn,beendet_am",
    id: `in.(${relationshipIds.join(",")})`,
    limit: "1000",
  });
  const parcelIds = [...new Set(relationships.map((item) => item.parzelle_id))];
  const parcels = parcelIds.length === 0 ? [] : await readSupabaseRest<LeaseRelationshipParcelRow>({ club, session }, "parzelle", {
    select: "id,garten_nr,Anlage",
    id: `in.(${parcelIds.join(",")})`,
    limit: "1000",
  });
  return { participations, relationships, parcels };
}
