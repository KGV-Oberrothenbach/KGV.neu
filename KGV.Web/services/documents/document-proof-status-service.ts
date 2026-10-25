import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { DocumentProofStatus } from "../../models/documents/document-proof-status";
import {
  loadLeaseDocumentProofStatusRows,
  loadMaintenanceDocumentProofStatusRows,
  loadMemberDocumentProofStatusRow,
} from "../../repositories/documents/document-proof-status-repository";

export async function loadMembershipApplicationProofStatus(club: ClubContext, session: BrowserSession, memberId: number): Promise<DocumentProofStatus> {
  const row = (await loadMemberDocumentProofStatusRow(club, session, memberId))[0];
  // The membership view starts with the complete visible member set. A missing row is therefore equivalent to no evidence.
  return row ? { status: row.nachweisstatus, relevantesDatum: row.dokumentdatum, dokumentId: row.dokument_id } : { status: "kein_nachweis", relevantesDatum: null, dokumentId: null };
}

export async function loadLeaseDocumentProofStatuses(club: ClubContext, session: BrowserSession, leaseIds: number[]) {
  if (leaseIds.length === 0) return new Map<number, DocumentProofStatus>();
  const rows = await loadLeaseDocumentProofStatusRows(club, session, leaseIds);
  return new Map(rows.map((row) => [row.pachtverhaeltnis_id, { status: row.nachweisstatus, relevantesDatum: row.vertragsdatum, dokumentId: row.dokument_id }]));
}

export async function loadMaintenanceDocumentProofStatuses(club: ClubContext, session: BrowserSession, assignmentIds: number[]) {
  if (assignmentIds.length === 0) return new Map<number, DocumentProofStatus>();
  const rows = await loadMaintenanceDocumentProofStatusRows(club, session, assignmentIds);
  return new Map(rows.map((row) => [row.wartungsvertrag_zuordnung_id, { status: row.nachweisstatus, relevantesDatum: row.dokumentdatum, dokumentId: row.dokument_id }]));
}
