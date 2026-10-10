import { callSupabaseFunctionRaw, createStorageSignedUrl, type BrowserSession } from "../../lib/supabase-auth";

export type DocumentFileReference = {
  id: number;
  drive_file_id: string | null;
  bucket: string | null;
  storage_path: string | null;
};

export async function resolveDocumentFileOpenUrl(session: BrowserSession, document: DocumentFileReference): Promise<string> {
  if (document.drive_file_id?.trim()) {
    const response = await callSupabaseFunctionRaw(session, "kgv-upload-document", {
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
    return createStorageSignedUrl(session, document.bucket, document.storage_path);
  }

  throw new Error("Für dieses Dokument fehlt eine sichere Ablage.");
}
