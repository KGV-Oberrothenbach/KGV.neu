"use client";

import { type ReactNode, useCallback, useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { DocumentArchiveDialog } from "./DocumentArchiveDialog";
import { ContractFollowUpActions } from "./contracts/ContractFollowUpActions";
import type { Document, DocumentOwner } from "../../models/documents/document";
import { documentCapabilitiesForOwner, type DocumentAccessContext } from "../../services/documents/document-access-service";
import { archiveDocument } from "../../services/documents/document-archive-service";
import { canFollowUpLeaseContract, hasSignedLeaseContract } from "../../services/documents/contract-document-service";
import { formDocumentStatusLabel, formDocumentTypeLabel, resolveFormDocumentMetadata } from "../../services/documents/document-metadata-service";
import { resolveDocumentOpenUrl } from "../../services/documents/document-open-service";
import { loadDocumentsForOwner } from "../../services/documents/document-service";
import { uploadDocumentForOwner } from "../../services/documents/document-upload-service";

type DocumentListProps = {
  session: BrowserSession;
  owner: DocumentOwner;
  compact?: boolean;
  accessContext: DocumentAccessContext;
  beforeList?: (reload: () => Promise<void>) => ReactNode;
};

export function DocumentList({ session, owner, compact = false, accessContext, beforeList }: DocumentListProps) {
  const [items, setItems] = useState<Document[]>([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [opening, setOpening] = useState<number | null>(null);
  const [file, setFile] = useState<File | null>(null);
  const [title, setTitle] = useState("");
  const [uploading, setUploading] = useState(false);
  const [archiving, setArchiving] = useState(false);
  const [documentToArchive, setDocumentToArchive] = useState<Document | null>(null);
  const capabilities = documentCapabilitiesForOwner(accessContext, owner);
  const hasSignedContract = hasSignedLeaseContract(items);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError("");
    try {
      const currentOwner: DocumentOwner = owner.kind === "member"
        ? { kind: "member", id: owner.id }
        : { kind: "parcel", id: owner.id };
      setItems(await loadDocumentsForOwner(session, currentOwner));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Dokumente konnten nicht geladen werden.");
      setItems([]);
    } finally {
      setIsLoading(false);
    }
  }, [session, owner.kind, owner.id]);

  useEffect(() => {
    let cancelled = false;
    queueMicrotask(() => {
      if (!cancelled) {
        setItems([]);
        setError("");
        setIsLoading(true);
      }
    });
    const currentOwner: DocumentOwner = owner.kind === "member"
      ? { kind: "member", id: owner.id }
      : { kind: "parcel", id: owner.id };
    void loadDocumentsForOwner(session, currentOwner)
      .then((documents) => { if (!cancelled) setItems(documents); })
      .catch((cause: unknown) => {
        if (!cancelled) {
          setError(cause instanceof Error ? cause.message : "Dokumente konnten nicht geladen werden.");
          setItems([]);
        }
      })
      .finally(() => { if (!cancelled) setIsLoading(false); });
    return () => { cancelled = true; };
  }, [session, owner.kind, owner.id]);

  async function openDocument(item: Document) {
    setOpening(item.id);
    setError("");
    try {
      const documentUrl = await resolveDocumentOpenUrl(session, item);
      window.open(documentUrl, "_blank", "noopener,noreferrer");
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht geöffnet werden.");
    } finally {
      setOpening(null);
    }
  }

  async function upload() {
    if (!file || !title.trim()) {
      setError("Titel und Datei sind erforderlich.");
      return;
    }
    setUploading(true);
    setError("");
    try {
      await uploadDocumentForOwner(session, owner, file, title);
      setFile(null);
      setTitle("");
      await load();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht gespeichert werden.");
    } finally {
      setUploading(false);
    }
  }

  async function archive(reason: string, password: string) {
    if (!documentToArchive) return;
    setArchiving(true);
    setError("");
    try {
      await archiveDocument(session, documentToArchive.id, reason, password);
      await load();
      setDocumentToArchive(null);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht archiviert werden.");
      throw cause;
    } finally {
      setArchiving(false);
    }
  }

  return <section className={compact ? "parcel-documents" : "data-workspace"} aria-label="Dokumente">
    {compact ? <h3>Parzellen-Dokumente</h3> : <p className="document-intro">Dokumente werden ausschließlich über den geschützten Vereins-Dokumentendienst geöffnet. Die Browser-App speichert keine Dokumentkopie lokal.</p>}
    {error && <p className="notice" role="alert">{error}</p>}
    {beforeList?.(load)}
    {capabilities.canUpload && <fieldset className="document-upload" disabled={uploading}><label>Titel<input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="z. B. Pachtvertrag 2026" /></label><label>Datei<input type="file" accept="application/pdf,image/jpeg,image/png,image/webp,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document,application/vnd.oasis.opendocument.text,.pdf,.jpg,.jpeg,.png,.webp,.doc,.docx,.odt" onChange={(event) => setFile(event.target.files?.[0] ?? null)} /></label><button type="button" onClick={upload}>{uploading ? "Lädt hoch …" : "Dokument hochladen"}</button></fieldset>}
    {isLoading ? <p className="empty-state">Dokumente werden geladen …</p> : <div className="data-table-wrap"><table><thead><tr><th>Titel</th><th>Datei</th><th>Geändert</th><th>Größe</th><th></th></tr></thead><tbody>{items.map((item) => { const metadata = resolveFormDocumentMetadata(item); const canFollowUp = canFollowUpLeaseContract(item, items, capabilities.canUpload); return <tr key={item.id}><td><strong>{item.titel ?? "Ohne Titel"}</strong>{metadata && <small>{formDocumentTypeLabel(metadata.type)} · {formDocumentStatusLabel(metadata.status)}</small>}{metadata?.type === "pachtvertrag" && metadata.status === "unsigniert" && hasSignedContract && <small>Signierte Endfassung vorhanden.</small>}</td><td>{item.dateiname ?? "–"}</td><td>{formatDate(item.updated_at)}</td><td>{formatBytes(item.size_bytes)}</td><td><button className="table-action" disabled={opening === item.id} onClick={() => void openDocument(item)}>{opening === item.id ? "Öffne …" : item.mime_type === "application/pdf" ? "Vorschau" : "Öffnen"}</button>{canFollowUp && <ContractFollowUpActions session={session} document={item} onSaved={load} />}{capabilities.canArchive && <button className="table-action secondary-action" disabled={archiving} onClick={() => setDocumentToArchive(item)}>{archiving ? "Archiviert …" : "Archivieren"}</button>}</td></tr>; })}</tbody></table>{items.length === 0 && <p className="empty-state">Keine aktiven Dokumente vorhanden.</p>}</div>}
    {!compact && <p className="detail-hint">Mitgliedsantrag und Pachtvertrag verwenden die offiziellen PDF-Vorlagen. Vorschau, Unterschriften und sichere Ablage erfolgen über den geschützten Server-Dienst.</p>}
    {documentToArchive && <DocumentArchiveDialog document={documentToArchive} busy={archiving} onCancel={() => setDocumentToArchive(null)} onArchive={archive} />}
  </section>;
}

function formatDate(value?: string | null) {
  return value ? new Intl.DateTimeFormat("de-DE").format(new Date(`${value.slice(0, 10)}T12:00:00`)) : "–";
}

function formatBytes(value: number | null) {
  if (!value) return "–";
  return value < 1024 * 1024 ? `${Math.round(value / 1024)} KB` : `${(value / (1024 * 1024)).toFixed(1)} MB`;
}
