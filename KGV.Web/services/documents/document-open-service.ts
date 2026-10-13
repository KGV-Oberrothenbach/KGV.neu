import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { resolveDocumentFileOpenUrl, type DocumentFileReference } from "../../repositories/documents/document-file-repository";

export function resolveDocumentOpenUrl(club: ClubContext, session: BrowserSession, document: DocumentFileReference): Promise<string> {
  return resolveDocumentFileOpenUrl(club, session, document);
}
