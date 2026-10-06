import { readSupabase, type BrowserSession } from "../../lib/supabase-auth";

export type MembershipApplicationDocument = {
  id: number;
  titel: string | null;
  dateiname: string | null;
  updated_at: string;
};

export function listMembershipApplicationDocuments(session: BrowserSession, memberId: number) {
  return readSupabase<MembershipApplicationDocument>(session, "dokument", {
    select: "id,titel,dateiname,updated_at",
    mitglied_id: `eq.${memberId}`,
    archiviert_at: "is.null",
    order: "updated_at.desc",
  });
}

export type LegalRepresentativeRelation = { id: number; minderjaehriges_mitglied_id: number; vertreter_mitglied_id: number; gueltig_ab: string; gueltig_bis: string | null; bemerkung: string | null };

export function listLegalRepresentativeRelations(session: BrowserSession, memberId: number) {
  return readSupabase<LegalRepresentativeRelation>(session, "mitglied_gesetzlicher_vertreter", {
    select: "id,minderjaehriges_mitglied_id,vertreter_mitglied_id,gueltig_ab,gueltig_bis,bemerkung",
    minderjaehriges_mitglied_id: `eq.${memberId}`,
    order: "gueltig_ab.desc",
  });
}
