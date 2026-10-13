import { readSupabaseRest } from "../../lib/supabase/rest-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { assertDocumentOwner, documentSelect, memberDocumentOwner, parcelDocumentOwner, type Document, type DocumentOwner } from "../../models/documents/document";

const activeDocumentQuery = {
  select: documentSelect,
  archiviert_at: "is.null",
  order: "updated_at.desc.nullslast,id.desc",
  limit: "500",
};

export function listDocumentsForOwner(club: ClubContext, session: BrowserSession, owner: DocumentOwner) {
  assertDocumentOwner(owner);
  return readSupabaseRest<Document>({ club, session }, "dokument", {
    ...activeDocumentQuery,
    [owner.kind === "member" ? "mitglied_id" : "parzelle_id"]: `eq.${owner.id}`,
  });
}

export function listMemberDocuments(club: ClubContext, session: BrowserSession, memberId: number) {
  return listDocumentsForOwner(club, session, memberDocumentOwner(memberId));
}

export function listParcelDocuments(club: ClubContext, session: BrowserSession, parcelId: number) {
  return listDocumentsForOwner(club, session, parcelDocumentOwner(parcelId));
}
