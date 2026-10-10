"use client";

import { type FormEvent, useState } from "react";
import type { Document } from "../../models/documents/document";

type DocumentArchiveDialogProps = {
  document: Document;
  busy: boolean;
  onCancel: () => void;
  onArchive: (reason: string, password: string) => Promise<void>;
};

export function DocumentArchiveDialog({ document, busy, onCancel, onArchive }: DocumentArchiveDialogProps) {
  const [reason, setReason] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalizedReason = reason.trim();
    if (normalizedReason.length < 3) {
      setError("Die Archivbegründung muss mindestens 3 Zeichen enthalten.");
      return;
    }
    setError("");
    try {
      await onArchive(normalizedReason, password);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht archiviert werden.");
    }
  }

  return <div className="modal-backdrop" role="presentation"><section className="home-detail-modal" role="dialog" aria-modal="true" aria-labelledby="document-archive-title">
    <header className="home-detail-header"><div><span className="eyebrow">Dokumentarchivierung</span><h2 id="document-archive-title">Dokument archivieren</h2></div></header>
    <p><strong>{document.titel ?? "Ohne Titel"}</strong><br />{document.dateiname ?? "Ohne Dateiname"}</p>
    {error && <p className="notice" role="alert">{error}</p>}
    <form onSubmit={(event) => void submit(event)}>
      <label>Begründung<textarea value={reason} minLength={3} required disabled={busy} onChange={(event) => setReason(event.target.value)} /></label>
      <label>Archivpasswort<input type="password" value={password} required disabled={busy} autoComplete="current-password" onChange={(event) => setPassword(event.target.value)} /></label>
      <div className="editor-actions"><button type="submit" disabled={busy || reason.trim().length < 3}>{busy ? "Archivieren …" : "Archivieren"}</button><button type="button" className="secondary-action" disabled={busy} onClick={onCancel}>Abbrechen</button></div>
    </form>
  </section></div>;
}
