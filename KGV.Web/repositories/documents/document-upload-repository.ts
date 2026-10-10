import { callSupabaseFunctionRaw, type BrowserSession } from "../../lib/supabase-auth";
import type { Document, DocumentOwner } from "../../models/documents/document";

export async function uploadDocumentForOwnerRequest(session: BrowserSession, owner: DocumentOwner, file: File, title: string): Promise<Document> {
  const form = new FormData();
  form.set("file", file);
  form.set("owner_kind", owner.kind === "member" ? "mitglied" : "parzelle");
  form.set("owner_id", String(owner.id));
  form.set("titel", title);

  const response = await callSupabaseFunctionRaw(session, "kgv-upload-document", { method: "POST", body: form });
  const payload = await response.json().catch(() => null) as { document?: Document; message?: string } | null;
  if (!response.ok || !payload?.document) throw new Error(payload?.message ?? "Das Dokument konnte nicht hochgeladen werden.");
  return payload.document;
}
