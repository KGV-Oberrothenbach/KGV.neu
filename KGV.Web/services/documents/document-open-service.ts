import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { resolveDocumentFileOpenUrl, type DocumentFileReference } from "../../repositories/documents/document-file-repository";
import { loadActiveDocument } from "./document-service";

export function resolveDocumentOpenUrl(club: ClubContext, session: BrowserSession, document: DocumentFileReference): Promise<string> {
  return resolveDocumentFileOpenUrl(club, session, document);
}

export async function resolveActiveDocumentOpenUrl(club: ClubContext, session: BrowserSession, documentId: number) {
  const document = await loadActiveDocument(club, session, documentId);
  if (!document) throw new Error("Das zugehörige Dokument ist nicht mehr aktiv oder nicht zugreifbar.");
  return resolveDocumentOpenUrl(club, session, document);
}
