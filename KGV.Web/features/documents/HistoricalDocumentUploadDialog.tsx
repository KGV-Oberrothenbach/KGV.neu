"use client";

import { useEffect, useMemo, useState } from "react";
import { useSelectedClub } from "../auth/AuthProvider";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { DocumentType, HistoricalLeaseRelationship, HistoricalMaintenanceAssignment, PendingHistoricalDocumentRegistration } from "../../models/documents/document-type";
import { loadMemberLeaseRelationships } from "../../services/contracts/lease-service";
import { loadHistoricalDocumentMaintenanceAssignments } from "../../services/maintenance/maintenance-assignment-service";
import { loadActiveDocumentTypes } from "../../services/documents/document-type-service";
import { registerHistoricalDocumentForMember, savePendingHistoricalDocumentRegistration } from "../../services/documents/historical-document-registration-service";
import { uploadDocumentForOwner } from "../../services/documents/document-upload-service";
import { memberDocumentOwner } from "../../models/documents/document";
import type { DocumentProofStatus } from "../../models/documents/document-proof-status";
import { loadLeaseDocumentProofStatuses } from "../../services/documents/document-proof-status-service";
import { resolveActiveDocumentOpenUrl } from "../../services/documents/document-open-service";
import { DocumentProofStatusDisplay } from "./DocumentProofStatus";

type Props = {
  session: BrowserSession;
  memberId: number;
  pending: PendingHistoricalDocumentRegistration | null;
  onPendingChange: (pending: PendingHistoricalDocumentRegistration | null) => void;
  onCompleted: () => Promise<void>;
  onClose: () => void;
};

const acceptedFiles = "application/pdf,image/jpeg,image/png,image/webp,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document,application/vnd.oasis.opendocument.text,.pdf,.jpg,.jpeg,.png,.webp,.doc,.docx,.odt";
const today = () => new Date().toISOString().slice(0, 10);
const formatDate = (value: string | null) => value ? new Intl.DateTimeFormat("de-DE").format(new Date(`${value.slice(0, 10)}T12:00:00`)) : "offen";

function requiresPacht(type: DocumentType | undefined) {
  return Boolean(type?.wirkungen.includes("pachtbezug_erforderlich") || type?.wirkungen.includes("pachtvertrag_nachweis"));
}
function requiresWartung(type: DocumentType | undefined) {
  return Boolean(type?.wirkungen.includes("wartungsvertragsbezug_erforderlich") || type?.wirkungen.includes("wartungsvertrag_nachweis"));
}
function leaseLabel(item: HistoricalLeaseRelationship) {
  const parcel = item.gartenNr ? `Parzelle ${item.gartenNr}` : `Parzelle nicht verfügbar`;
  return `${parcel}${item.anlage ? ` – ${item.anlage}` : ""} – ${item.rolle === "hauptpaechter" ? "Hauptpächter" : "Mitpächter"} – ${formatDate(item.gueltigAb)} bis ${formatDate(item.gueltigBis)}`;
}
function maintenanceLabel(item: HistoricalMaintenanceAssignment) {
  return `${item.bezeichnung} – ${formatDate(item.gueltigAb)} bis ${formatDate(item.gueltigBis)}`;
}

export function HistoricalDocumentUploadDialog({ session, memberId, pending, onPendingChange, onCompleted, onClose }: Props) {
  const club = useSelectedClub();
  const [types, setTypes] = useState<DocumentType[]>([]);
  const [leases, setLeases] = useState<HistoricalLeaseRelationship[]>([]);
  const [assignments, setAssignments] = useState<HistoricalMaintenanceAssignment[]>([]);
  const [leaseProofStatuses, setLeaseProofStatuses] = useState<Map<number, DocumentProofStatus>>(new Map());
  const [leaseProofStatusError, setLeaseProofStatusError] = useState("");
  const [principalMemberId, setPrincipalMemberId] = useState<number | null>(null);
  const [file, setFile] = useState<File | null>(null);
  const [typeId, setTypeId] = useState(String(pending?.dokumenttypId ?? ""));
  const [documentDate, setDocumentDate] = useState(pending?.dokumentdatum ?? today());
  const [signed, setSigned] = useState<"ja" | "nein">(pending?.unterzeichnet === false ? "nein" : "ja");
  const [leaseId, setLeaseId] = useState(pending?.pachtverhaeltnisId ? String(pending.pachtverhaeltnisId) : "");
  const [assignmentId, setAssignmentId] = useState(pending?.wartungsvertragZuordnungId ? String(pending.wartungsvertragZuordnungId) : "");
  const [loading, setLoading] = useState(true);
  const [phase, setPhase] = useState<"idle" | "upload" | "register">("idle");
  const [message, setMessage] = useState(pending ? "Die Datei ist bereits gespeichert. Schließen Sie die fachliche Zuordnung erneut ab." : "");

  useEffect(() => {
    let active = true;
    Promise.all([
      loadActiveDocumentTypes(club, session),
      loadMemberLeaseRelationships(club, session, memberId),
      loadHistoricalDocumentMaintenanceAssignments(club, session, memberId),
    ]).then(async ([nextTypes, nextLeases, maintenance]) => {
      if (!active) return;
      setTypes(nextTypes); setLeases(nextLeases); setAssignments(maintenance.assignments); setPrincipalMemberId(maintenance.principalMemberId);
      try {
        const statuses = await loadLeaseDocumentProofStatuses(club, session, [...new Set(nextLeases.map((item) => item.pachtverhaeltnisId))]);
        if (active) { setLeaseProofStatuses(statuses); setLeaseProofStatusError(""); }
      } catch {
        if (active) setLeaseProofStatusError("Der Pachtvertrag-Status konnte nicht geladen werden.");
      }
    }).catch(() => { if (active) setMessage("Die Auswahldaten für die fachliche Zuordnung konnten nicht geladen werden."); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [club, memberId, session]);

  const selectedType = useMemo(() => types.find((type) => type.id === Number(typeId)), [types, typeId]);
  const needsLease = requiresPacht(selectedType);
  const needsAssignment = requiresWartung(selectedType);
  const maintenanceAllowed = principalMemberId === null || principalMemberId === memberId;
  const matchingLeases = leases.filter((item) => item.gueltigAb <= documentDate && (!item.gueltigBis || item.gueltigBis >= documentDate));

  async function openStatusDocument(documentId: number) {
    const documentUrl = await resolveActiveDocumentOpenUrl(club, session, documentId);
    window.open(documentUrl, "_blank", "noopener,noreferrer");
  }

  async function register(documentId: number) {
    setPhase("register");
    const registration: PendingHistoricalDocumentRegistration = {
      memberId,
      documentId,
      dokumenttypId: Number(typeId),
      dokumentdatum: documentDate,
      unterzeichnet: signed === "ja",
      pachtverhaeltnisId: needsLease ? Number(leaseId) : null,
      wartungsvertragZuordnungId: needsAssignment ? Number(assignmentId) : null,
    };
    savePendingHistoricalDocumentRegistration(registration);
    onPendingChange(registration);
    try {
      await registerHistoricalDocumentForMember(club, session, memberId, registration);
      onPendingChange(null);
      await onCompleted();
      onClose();
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Die fachliche Zuordnung konnte nicht abgeschlossen werden.");
    } finally { setPhase("idle"); }
  }

  async function submit() {
    setMessage("");
    if (!selectedType || !/^\d{4}-\d{2}-\d{2}$/.test(documentDate)) { setMessage("Bitte wählen Sie Dokumenttyp und ein gültiges Dokumentdatum."); return; }
    if (needsLease && !leaseId) { setMessage("Bitte wählen Sie eine Pachtbeziehung aus."); return; }
    if (needsAssignment && (!maintenanceAllowed || !assignmentId)) { setMessage("Bitte wählen Sie eine Wartungsvertragszuordnung des Hauptmitglieds aus."); return; }
    if (pending) { await register(pending.documentId); return; }
    if (!file) { setMessage("Bitte wählen Sie eine Datei oder einen Scan aus."); return; }
    setPhase("upload");
    try {
      const title = `${selectedType.bezeichnung} – ${documentDate}`;
      const document = await uploadDocumentForOwner(club, session, memberDocumentOwner(memberId), file, title);
      await register(document.id);
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Die Datei konnte nicht hochgeladen werden.");
      setPhase("idle");
    }
  }

  return <div className="modal-backdrop" role="presentation"><section className="home-detail-modal" role="dialog" aria-modal="true" aria-labelledby="historical-document-title">
    <header className="home-detail-header"><div><span className="eyebrow">Mitgliedsdokument</span><h2 id="historical-document-title">Neues Dokument / Scan</h2></div></header>
    {message && <p className="notice" role="status">{message}</p>}
    {loading ? <p>Auswahldaten werden geladen …</p> : <fieldset disabled={phase !== "idle"}>
      {!pending && <label>Datei oder Scan<input type="file" accept={acceptedFiles} onChange={(event) => setFile(event.target.files?.[0] ?? null)} /><small>Für Fotos kann auf einem Mobilgerät die Kameraauswahl angeboten werden.</small></label>}
      {pending && <p><strong>Bereits hochgeladenes Dokument:</strong> #{pending.documentId}. Es wird keine Datei erneut übertragen.</p>}
      <label>Dokumenttyp<select value={typeId} onChange={(event) => { setTypeId(event.target.value); setLeaseId(""); setAssignmentId(""); }}><option value="">Dokumenttyp wählen</option>{types.map((type) => <option key={type.id} value={type.id}>{type.bezeichnung}</option>)}</select></label>
      <label>Dokumentdatum<input type="date" value={documentDate} onChange={(event) => setDocumentDate(event.target.value)} /></label>
      <label>Unterzeichnet<select value={signed} onChange={(event) => setSigned(event.target.value as "ja" | "nein")}><option value="ja">Ja</option><option value="nein">Nein</option></select></label>
      {needsLease && <><label>Pachtbeziehung<select value={leaseId} onChange={(event) => setLeaseId(event.target.value)}><option value="">Pachtbeziehung wählen</option>{leases.map((item) => <option key={`${item.pachtverhaeltnisId}:${item.gueltigAb}:${item.rolle}`} value={item.pachtverhaeltnisId}>{leaseLabel(item)}{matchingLeases.includes(item) ? "" : " · außerhalb des Dokumentdatums"}</option>)}</select>{leases.length === 0 && <small>Für dieses Mitglied sind keine fachlichen Pachtbeziehungen lesbar.</small>}</label>{leaseProofStatusError && <p className="notice" role="alert">{leaseProofStatusError}</p>}{leaseId && leaseProofStatuses.get(Number(leaseId)) && <DocumentProofStatusDisplay subject="Pachtvertrag" status={leaseProofStatuses.get(Number(leaseId))!} dateLabel="Vertragsdatum" onOpenDocument={openStatusDocument} />}</>}
      {needsAssignment && <label>Wartungsvertragszuordnung<select value={assignmentId} disabled={!maintenanceAllowed} onChange={(event) => setAssignmentId(event.target.value)}><option value="">Wartungsvertragszuordnung wählen</option>{assignments.map((item) => <option key={item.id} value={item.id}>{maintenanceLabel(item)}</option>)}</select>{!maintenanceAllowed && <small>Diese Zuordnung kann nur im Kontext des Hauptmitglieds registriert werden.</small>}</label>}
    </fieldset>}
    <div className="editor-actions"><button disabled={loading || phase !== "idle"} onClick={() => void submit()}>{phase === "upload" ? "Datei wird hochgeladen …" : phase === "register" ? "Dokument wird fachlich zugeordnet …" : pending ? "Zuordnung erneut versuchen" : "Hochladen und zuordnen"}</button><button className="secondary-action" disabled={phase !== "idle"} onClick={onClose}>Schließen</button></div>
  </section></div>;
}
