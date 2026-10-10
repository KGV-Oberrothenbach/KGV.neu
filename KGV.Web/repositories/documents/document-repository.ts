import { readSupabase, type BrowserSession } from "../../lib/supabase-auth";
import { assertDocumentOwner, documentSelect, memberDocumentOwner, parcelDocumentOwner, type Document, type DocumentOwner } from "../../models/documents/document";

const activeDocumentQuery = {
  select: documentSelect,
  archiviert_at: "is.null",
  order: "updated_at.desc.nullslast,id.desc",
  limit: "500",
};

export function listDocumentsForOwner(session: BrowserSession, owner: DocumentOwner) {
  assertDocumentOwner(owner);
  return readSupabase<Document>(session, "dokument", {
    ...activeDocumentQuery,
    [owner.kind === "member" ? "mitglied_id" : "parzelle_id"]: `eq.${owner.id}`,
  });
}

export function listMemberDocuments(session: BrowserSession, memberId: number) {
  return listDocumentsForOwner(session, memberDocumentOwner(memberId));
}

export function listParcelDocuments(session: BrowserSession, parcelId: number) {
  return listDocumentsForOwner(session, parcelDocumentOwner(parcelId));
}
