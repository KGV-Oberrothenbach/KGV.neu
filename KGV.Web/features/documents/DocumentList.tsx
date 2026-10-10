"use client";

import { type ReactNode, useCallback, useEffect, useState } from "react";
import { archiveDocument, type BrowserSession, uploadDocument, writeSupabase } from "../../lib/supabase-auth";
import type { Document, DocumentOwner } from "../../models/documents/document";
import { resolveDocumentOpenUrl } from "../../services/documents/document-open-service";
import { loadDocumentsForOwner } from "../../services/documents/document-service";

type DocumentListProps = {
  session: BrowserSession;
  owner: DocumentOwner;
  compact?: boolean;
  canManage?: boolean;
  beforeList?: (reload: () => Promise<void>) => ReactNode;
};

export function DocumentList({ session, owner, compact = false, canManage = false, beforeList }: DocumentListProps) {
  const [items, setItems] = useState<Document[]>([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [opening, setOpening] = useState<number | null>(null);
  const [file, setFile] = useState<File | null>(null);
  const [title, setTitle] = useState("");
  const [uploading, setUploading] = useState(false);
  const [archiving, setArchiving] = useState<number | null>(null);

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
      const uploaded = await uploadDocument(session, file, {
        kind: owner.kind === "member" ? "mitglied" : "parzelle",
        id: owner.id,
        title: title.trim(),
      });
      await writeSupabase<Document>(session, "dokument", "POST", {
        mitglied_id: owner.kind === "member" ? owner.id : null,
        parzelle_id: owner.kind === "parcel" ? owner.id : null,
        bucket: "dokumente",
        storage_path: uploaded.storagePath,
        drive_file_id: uploaded.driveFileId,
        titel: title.trim(),
        dateiname: uploaded.fileName,
        mime_type: uploaded.mimeType,
        size_bytes: uploaded.sizeBytes,
      });
      setFile(null);
      setTitle("");
      await load();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht gespeichert werden.");
    } finally {
      setUploading(false);
    }
  }

  async function archive(item: Document) {
    const reason = window.prompt("Begründung für die Archivierung (mindestens 3 Zeichen):");
    if (!reason) return;
    const password = window.prompt("Archivpasswort:");
    if (!password) return;
    setArchiving(item.id);
    setError("");
    try {
      await archiveDocument(session, item.id, password, reason);
      await load();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht archiviert werden.");
    } finally {
      setArchiving(null);
    }
  }

  return <section className={compact ? "parcel-documents" : "data-workspace"} aria-label="Dokumente">
    {compact ? <h3>Parzellen-Dokumente</h3> : <p className="document-intro">Dokumente werden ausschließlich über den geschützten Vereins-Dokumentendienst geöffnet. Die Browser-App speichert keine Dokumentkopie lokal.</p>}
    {error && <p className="notice" role="alert">{error}</p>}
    {beforeList?.(load)}
    {canManage && <fieldset className="document-upload" disabled={uploading}><label>Titel<input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="z. B. Pachtvertrag 2026" /></label><label>Datei<input type="file" accept="application/pdf,image/*,.doc,.docx,.odt" onChange={(event) => setFile(event.target.files?.[0] ?? null)} /></label><button type="button" onClick={upload}>{uploading ? "Lädt hoch …" : "Dokument hochladen"}</button></fieldset>}
    {isLoading ? <p className="empty-state">Dokumente werden geladen …</p> : <div className="data-table-wrap"><table><thead><tr><th>Titel</th><th>Datei</th><th>Geändert</th><th>Größe</th><th></th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td><strong>{item.titel ?? "Ohne Titel"}</strong></td><td>{item.dateiname ?? "–"}</td><td>{formatDate(item.updated_at)}</td><td>{formatBytes(item.size_bytes)}</td><td><button className="table-action" disabled={opening === item.id} onClick={() => void openDocument(item)}>{opening === item.id ? "Öffne …" : item.mime_type === "application/pdf" ? "Vorschau" : "Öffnen"}</button>{canManage && <button className="table-action secondary-action" disabled={archiving === item.id} onClick={() => void archive(item)}>{archiving === item.id ? "Archiviert …" : "Archivieren"}</button>}</td></tr>)}</tbody></table>{items.length === 0 && <p className="empty-state">Keine aktiven Dokumente vorhanden.</p>}</div>}
    {!compact && <p className="detail-hint">Mitgliedsantrag und Pachtvertrag verwenden die offiziellen PDF-Vorlagen. Vorschau, Unterschriften und sichere Ablage erfolgen über den geschützten Server-Dienst.</p>}
  </section>;
}

function formatDate(value?: string | null) {
  return value ? new Intl.DateTimeFormat("de-DE").format(new Date(`${value.slice(0, 10)}T12:00:00`)) : "–";
}

function formatBytes(value: number | null) {
  if (!value) return "–";
  return value < 1024 * 1024 ? `${Math.round(value / 1024)} KB` : `${(value / (1024 * 1024)).toFixed(1)} MB`;
}
