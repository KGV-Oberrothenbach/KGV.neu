import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { DocumentOwner } from "../../models/documents/document";
import { listDocumentsForOwner, listMemberDocuments, listParcelDocuments } from "../../repositories/documents/document-repository";

export function loadDocumentsForOwner(club: ClubContext, session: BrowserSession, owner: DocumentOwner) {
  return listDocumentsForOwner(club, session, owner);
}

export function loadMemberDocuments(club: ClubContext, session: BrowserSession, memberId: number) {
  return listMemberDocuments(club, session, memberId);
}

export function loadParcelDocuments(club: ClubContext, session: BrowserSession, parcelId: number) {
  return listParcelDocuments(club, session, parcelId);
}
