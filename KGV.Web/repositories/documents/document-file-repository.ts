import { invokeEdgeFunctionRaw } from "../../lib/supabase/edge-client";
import { createSignedStorageUrl } from "../../lib/supabase/storage-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";

export type DocumentFileReference = {
  id: number;
  drive_file_id: string | null;
  bucket: string | null;
  storage_path: string | null;
};

export async function resolveDocumentFileOpenUrl(club: ClubContext, session: BrowserSession, document: DocumentFileReference): Promise<string> {
  if (document.drive_file_id?.trim()) {
    const response = await invokeEdgeFunctionRaw({ club, session }, "kgv-upload-document", {
      method: "POST",
      body: JSON.stringify({ action: "download", document_id: document.id }),
      contentType: "application/json",
    });
    if (!response.ok) {
      const detail = await response.json().catch(() => null) as { message?: string } | null;
      throw new Error(detail?.message ?? "Das Dokument kann derzeit nicht geöffnet werden.");
    }
    return URL.createObjectURL(await response.blob());
  }

  if (document.bucket && document.storage_path) {
    try {
      return await createSignedStorageUrl({ club, session }, document.bucket, document.storage_path);
    } catch {
      throw new Error("Das Dokument kann derzeit nicht geöffnet werden.");
    }
  }

  throw new Error("Für dieses Dokument fehlt eine sichere Ablage.");
}
