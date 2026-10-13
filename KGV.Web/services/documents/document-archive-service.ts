import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { archiveDocumentRequest } from "../../repositories/documents/document-archive-repository";

export async function archiveDocument(club: ClubContext, session: BrowserSession, documentId: number, reason: string, password: string): Promise<void> {
  const normalizedReason = reason.trim();
  if (normalizedReason.length < 3) throw new Error("Die Archivbegründung muss mindestens 3 Zeichen enthalten.");
  if (!password) throw new Error("Das Archivpasswort ist erforderlich.");
  await archiveDocumentRequest(club, session, documentId, normalizedReason, password);
}
