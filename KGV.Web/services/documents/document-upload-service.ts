import type { BrowserSession } from "../../lib/supabase-auth";
import type { DocumentOwner } from "../../models/documents/document";
import { uploadDocumentForOwnerRequest } from "../../repositories/documents/document-upload-repository";

export function uploadDocumentForOwner(session: BrowserSession, owner: DocumentOwner, file: File, title: string) {
  const normalizedTitle = title.trim();
  if (!normalizedTitle) return Promise.reject(new Error("Titel ist erforderlich."));
  return uploadDocumentForOwnerRequest(session, owner, file, normalizedTitle);
}
