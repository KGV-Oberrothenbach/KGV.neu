"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useSelectedClub } from "../auth/AuthProvider";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { DocumentAccessContext } from "../../services/documents/document-access-service";
import { memberDocumentOwner } from "../../models/documents/document";
import type { PendingHistoricalDocumentRegistration } from "../../models/documents/document-type";
import { loadPendingHistoricalDocumentRegistration } from "../../services/documents/historical-document-registration-service";
import { loadMembershipApplicationProofStatus } from "../../services/documents/document-proof-status-service";
import { resolveActiveDocumentOpenUrl } from "../../services/documents/document-open-service";
import { DocumentList } from "./DocumentList";
import { DocumentProofStatusDisplay } from "./DocumentProofStatus";
import { HistoricalDocumentUploadDialog } from "./HistoricalDocumentUploadDialog";

export function MemberDocumentWorkspace({ session, memberId, accessContext }: { session: BrowserSession; memberId: number; accessContext: DocumentAccessContext }) {
  const club = useSelectedClub();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [pending, setPending] = useState<PendingHistoricalDocumentRegistration | null>(null);
  const [proofStatus, setProofStatus] = useState<Awaited<ReturnType<typeof loadMembershipApplicationProofStatus>> | null>(null);
  const [proofStatusError, setProofStatusError] = useState("");
  const reloadRef = useRef<(() => Promise<void>) | null>(null);
  const canUpload = accessContext.canManageDocuments;

  const loadProofStatus = useCallback(async () => {
    try {
      setProofStatus(await loadMembershipApplicationProofStatus(club, session, memberId));
      setProofStatusError("");
    } catch (cause) {
      setProofStatus(null);
      setProofStatusError(cause instanceof Error ? cause.message : "Der Mitgliedsantrag-Status konnte nicht geladen werden.");
    }
  }, [club, memberId, session]);

  async function openStatusDocument(documentId: number) {
    const documentUrl = await resolveActiveDocumentOpenUrl(club, session, documentId);
    window.open(documentUrl, "_blank", "noopener,noreferrer");
  }

  useEffect(() => {
    const timer = window.setTimeout(() => setPending(loadPendingHistoricalDocumentRegistration(memberId)), 0);
    return () => window.clearTimeout(timer);
  }, [memberId]);

  useEffect(() => {
    const timer = window.setTimeout(() => { void loadProofStatus(); }, 0);
    return () => window.clearTimeout(timer);
  }, [loadProofStatus]);

  return <section className="data-workspace"><div className="data-toolbar"><div><strong>Dokumente</strong>{pending && <small>Eine hochgeladene Datei wartet noch auf ihre fachliche Zuordnung.</small>}</div>{canUpload && <div className="editor-actions"><button onClick={() => setDialogOpen(true)}>{pending ? "Zuordnung fortsetzen" : "Neues Dokument / Scan"}</button></div>}</div>
    {proofStatusError && <p className="notice" role="alert">Mitgliedsantrag-Status konnte nicht geladen werden.</p>}
    {proofStatus && <DocumentProofStatusDisplay subject="Mitgliedsantrag" status={proofStatus} dateLabel="Dokumentdatum" onOpenDocument={openStatusDocument} />}
    <p className="detail-hint">Der Nachweisstatus berücksichtigt derzeit nur relational klassifizierte Dokumente.</p>
    <DocumentList session={session} owner={memberDocumentOwner(memberId)} accessContext={accessContext} beforeList={(reload) => { reloadRef.current = reload; return null; }} onDocumentsChanged={loadProofStatus} />
    {dialogOpen && <HistoricalDocumentUploadDialog session={session} memberId={memberId} pending={pending} onPendingChange={setPending} onCompleted={async () => { await reloadRef.current?.(); await loadProofStatus(); }} onClose={() => setDialogOpen(false)} />}
  </section>;
}
