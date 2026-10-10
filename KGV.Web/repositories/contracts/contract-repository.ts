import { readSupabase, type BrowserSession } from "../../lib/supabase-auth";
import type { Document } from "../../models/documents/document";
import { loadMemberDocuments, loadParcelDocuments } from "../../services/documents/document-service";

export type MembershipApplicationDocument = Document;

export function listMembershipApplicationDocuments(session: BrowserSession, memberId: number) {
  return loadMemberDocuments(session, memberId);
}

export type LeaseContractDocument = Document;
export function listLeaseContractDocuments(session: BrowserSession, parcelId: number) {
  return loadParcelDocuments(session, parcelId);
}

export type LegalRepresentativeRelation = { id: number; minderjaehriges_mitglied_id: number; vertreter_mitglied_id: number; gueltig_ab: string; gueltig_bis: string | null; bemerkung: string | null };

export function listLegalRepresentativeRelations(session: BrowserSession, memberId: number) {
  return readSupabase<LegalRepresentativeRelation>(session, "mitglied_gesetzlicher_vertreter", {
    select: "id,minderjaehriges_mitglied_id,vertreter_mitglied_id,gueltig_ab,gueltig_bis,bemerkung",
    minderjaehriges_mitglied_id: `eq.${memberId}`,
    order: "gueltig_ab.desc",
  });
}
