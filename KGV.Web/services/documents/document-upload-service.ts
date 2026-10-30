import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { DocumentOwner } from "../../models/documents/document";
import { uploadDocumentForOwnerRequest } from "../../repositories/documents/document-upload-repository";
import { reserveHistoricalContractNumberRequest } from "../../repositories/documents/document-upload-repository";

export function uploadDocumentForOwner(club: ClubContext, session: BrowserSession, owner: DocumentOwner, file: File, title: string) {
  const normalizedTitle = title.trim();
  if (!normalizedTitle) return Promise.reject(new Error("Titel ist erforderlich."));
  return uploadDocumentForOwnerRequest(club, session, owner, file, normalizedTitle);
}

export function reserveHistoricalContractNumber(club: ClubContext, session: BrowserSession, documentId: number) {
  if (!Number.isInteger(documentId) || documentId <= 0) return Promise.reject(new Error("Das historische Dokument besitzt keine gültige Quelle."));
  return reserveHistoricalContractNumberRequest(club, session, documentId);
}

export { downloadHistoricalContractDocumentRequest as loadHistoricalContractDocument } from "../../repositories/documents/document-upload-repository";

export function uploadHistoricalContractDocument(club: ClubContext, session: BrowserSession, memberId: number, file: File, title: string, contractType: "historischer_ma_pv_erstvertrag" | "historischer_pv_folgevertrag", operationId: string) {
  if (!title.trim()) return Promise.reject(new Error("Titel ist erforderlich."));
  return uploadDocumentForOwnerRequest(club, session, { kind: "member", id: memberId }, file, title.trim(), contractType, operationId);
}
