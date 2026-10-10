import { callSupabaseFunctionRaw, type BrowserSession } from "../../lib/supabase-auth";

export async function archiveDocumentRequest(session: BrowserSession, documentId: number, reason: string, password: string): Promise<void> {
  const response = await callSupabaseFunctionRaw(session, "kgv-upload-document", {
    method: "POST",
    body: JSON.stringify({ action: "archive", document_id: documentId, archive_password: password, reason }),
    contentType: "application/json",
  });
  if (!response.ok) {
    const detail = await response.json().catch(() => null) as { message?: string } | null;
    throw new Error(detail?.message ?? "Das Dokument konnte nicht archiviert werden.");
  }
}
