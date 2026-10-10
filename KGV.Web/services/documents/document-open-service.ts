import type { BrowserSession } from "../../lib/supabase-auth";
import { resolveDocumentFileOpenUrl, type DocumentFileReference } from "../../repositories/documents/document-file-repository";

export function resolveDocumentOpenUrl(session: BrowserSession, document: DocumentFileReference): Promise<string> {
  return resolveDocumentFileOpenUrl(session, document);
}
