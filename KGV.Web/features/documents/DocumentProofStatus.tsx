"use client";

import { useState } from "react";
import type { DocumentProofStatus } from "../../models/documents/document-proof-status";

type Props = {
  subject: "Mitgliedsantrag" | "Pachtvertrag" | "Wartungsvertrag";
  status: DocumentProofStatus;
  dateLabel: "Dokumentdatum" | "Vertragsdatum";
  onOpenDocument?: (documentId: number) => Promise<void>;
};

const textForStatus = {
  unterschrieben: "unterschrieben vorhanden",
  nur_unsigniert: "nur nicht unterzeichneter Nachweis vorhanden",
  kein_nachweis: "kein Nachweis vorhanden",
} as const;

export function DocumentProofStatusDisplay({ subject, status, dateLabel, onOpenDocument }: Props) {
  const [opening, setOpening] = useState(false);
  const [openError, setOpenError] = useState("");
  const label = `${subject}: ${textForStatus[status.status]}`;

  async function openDocument() {
    if (!status.dokumentId || !onOpenDocument) return;
    setOpening(true);
    setOpenError("");
    try {
      await onOpenDocument(status.dokumentId);
    } catch (cause) {
      setOpenError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht geöffnet werden.");
    } finally {
      setOpening(false);
    }
  }

  return <div className="document-proof-status"><span className={`status-badge proof-status ${status.status}`}>{label}</span>{status.relevantesDatum && <small>{dateLabel} {formatDate(status.relevantesDatum)}</small>}{status.dokumentId && onOpenDocument && <button type="button" className="table-action secondary-action" disabled={opening} onClick={() => void openDocument()}>{opening ? "Öffne …" : "Dokument öffnen"}</button>}{openError && <small role="alert">{openError}</small>}</div>;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("de-DE").format(new Date(`${value.slice(0, 10)}T12:00:00`));
}
