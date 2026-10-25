import { readSupabaseRest } from "../../lib/supabase/rest-client";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { DocumentProofStatusCode } from "../../models/documents/document-proof-status";

export type MemberDocumentProofStatusRow = {
  mitglied_id: number;
  nachweisstatus: DocumentProofStatusCode;
  dokumentdatum: string | null;
  dokument_id: number | null;
};

export type LeaseDocumentProofStatusRow = {
  pachtverhaeltnis_id: number;
  nachweisstatus: DocumentProofStatusCode;
  vertragsdatum: string | null;
  dokument_id: number | null;
};

export type MaintenanceDocumentProofStatusRow = {
  wartungsvertrag_zuordnung_id: number;
  nachweisstatus: DocumentProofStatusCode;
  dokumentdatum: string | null;
  dokument_id: number | null;
};

export function loadMemberDocumentProofStatusRow(club: ClubContext, session: BrowserSession, memberId: number) {
  return readSupabaseRest<MemberDocumentProofStatusRow>({ club, session }, "v_mitglied_mitgliedsantrag_nachweisstatus", {
    select: "mitglied_id,nachweisstatus,dokumentdatum,dokument_id",
    mitglied_id: `eq.${memberId}`,
    limit: "1",
  });
}

export function loadLeaseDocumentProofStatusRows(club: ClubContext, session: BrowserSession, leaseIds: number[]) {
  return readSupabaseRest<LeaseDocumentProofStatusRow>({ club, session }, "v_pachtverhaeltnis_pachtvertrag_nachweisstatus", {
    select: "pachtverhaeltnis_id,nachweisstatus,vertragsdatum,dokument_id",
    pachtverhaeltnis_id: `in.(${leaseIds.join(",")})`,
  });
}

export function loadMaintenanceDocumentProofStatusRows(club: ClubContext, session: BrowserSession, assignmentIds: number[]) {
  return readSupabaseRest<MaintenanceDocumentProofStatusRow>({ club, session }, "v_wartungsvertrag_zuordnung_nachweisstatus", {
    select: "wartungsvertrag_zuordnung_id,nachweisstatus,dokumentdatum,dokument_id",
    wartungsvertrag_zuordnung_id: `in.(${assignmentIds.join(",")})`,
  });
}
