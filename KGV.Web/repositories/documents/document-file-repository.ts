import { invokeEdgeFunctionRaw } from "../../lib/supabase/edge-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { DocumentUploadError } from "./document-upload-repository";

export type DocumentFileReference = {
  id: number;
  drive_file_id: string | null;
  bucket: string | null;
  storage_path: string | null;
};

export async function resolveDocumentFileOpenUrl(club: ClubContext, session: BrowserSession, document: DocumentFileReference): Promise<string> {
  if (document.drive_file_id?.trim() || (document.bucket && document.storage_path)) {
    // Always obtain bytes through the caller-bound document-ID authorization path.
    // Do not mint a revocation-resistant signed URL for a private document.
    const response = await invokeEdgeFunctionRaw({ club, session }, "kgv-upload-document", {
      method: "POST",
      body: JSON.stringify({ action: "download", document_id: document.id }),
      contentType: "application/json",
    });
    if (!response.ok) {
      const detail = await response.json().catch(() => null) as { message?: string; error_code?: string; request_id?: string } | null;
      throw new DocumentUploadError(response.status, detail ?? { message: "Das Dokument kann derzeit nicht geöffnet werden.", error_code: "DOWNLOAD_HTTP_ERROR" });
    }
    return URL.createObjectURL(await response.blob());
  }

  throw new Error("Für dieses Dokument fehlt eine sichere Ablage.");
}
