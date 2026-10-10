import type { BrowserSession } from "../../lib/supabase-auth";
import type { DocumentOwner } from "../../models/documents/document";
import { listDocumentsForOwner, listMemberDocuments, listParcelDocuments } from "../../repositories/documents/document-repository";

export function loadDocumentsForOwner(session: BrowserSession, owner: DocumentOwner) {
  return listDocumentsForOwner(session, owner);
}

export function loadMemberDocuments(session: BrowserSession, memberId: number) {
  return listMemberDocuments(session, memberId);
}

export function loadParcelDocuments(session: BrowserSession, parcelId: number) {
  return listParcelDocuments(session, parcelId);
}
