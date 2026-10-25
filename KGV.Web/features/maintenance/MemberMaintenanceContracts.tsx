import { useSelectedClub } from "../auth/AuthProvider";
import { useCallback, useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { endMaintenanceAssignmentForContract, loadMemberMaintenanceContracts } from "../../services/maintenance/maintenance-assignment-service";
import { prepareMaintenanceDocumentWorkflow } from "../../services/maintenance/maintenance-document-workflow-service";
import { loadMaintenanceDocumentProofStatuses } from "../../services/documents/document-proof-status-service";
import { resolveActiveDocumentOpenUrl } from "../../services/documents/document-open-service";
import type { DocumentProofStatus } from "../../models/documents/document-proof-status";
import { type MaintenanceAssignment } from "../../models/maintenance/maintenance-assignment";
import { maintenanceWorkHoursEffectLabel } from "../../models/maintenance/maintenance-contract";
import { AssignContractsToMember } from "./assignments/AssignContractsToMember";
import { MaintenanceContractSignatureFlow } from "./MaintenanceContractSignatureFlow";
import { MaintenanceContinuationPanel } from "./MaintenanceContinuationPanel";
import { DocumentProofStatusDisplay } from "../documents/DocumentProofStatus";

const localDate = () => { const date = new Date(); return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`; };
const formatDate = (value: string) => { const [year, month, day] = value.split("-"); return year && month && day ? `${day}.${month}.${year}` : value; };

export function MemberMaintenanceContracts({ session, memberId, canManage }: { session: BrowserSession; memberId: number; canManage: boolean }) {
  const club = useSelectedClub();
  const [data, setData] = useState<Awaited<ReturnType<typeof loadMemberMaintenanceContracts>> | null>(null);
  const [message, setMessage] = useState("");
  const [proofStatuses, setProofStatuses] = useState<Map<number, DocumentProofStatus>>(new Map());
  const [proofStatusError, setProofStatusError] = useState("");
  const [assigning, setAssigning] = useState(false);
  const [signingAssignmentId, setSigningAssignmentId] = useState<number | null>(null);
  const [endDates, setEndDates] = useState<Record<number, string>>({});

  const load = useCallback(async () => {
    try {
      const next = await loadMemberMaintenanceContracts(club, session, memberId);
      setData(next);
      setMessage("");
      try {
        const assignmentIds = [...next.assignments.active, ...next.assignments.future, ...next.assignments.history].map((assignment) => assignment.id);
        setProofStatuses(await loadMaintenanceDocumentProofStatuses(club, session, assignmentIds));
        setProofStatusError("");
      } catch {
        setProofStatuses(new Map());
        setProofStatusError("Der Wartungsvertrag-Status konnte nicht geladen werden.");
      }
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Wartungsverträge konnten nicht geladen werden.");
    }
  }, [club, memberId, session]);

  useEffect(() => {
    const timer = window.setTimeout(() => { void load(); }, 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  if (!data) return <section className="data-workspace"><p>{message || "Wartungsverträge werden geladen…"}</p></section>;
  const assignments = data.assignments;
  const contracts = new Map(data.contracts.map((contract) => [contract.id, contract]));
  const memberName = [data.member.vorname, data.member.name].filter(Boolean).join(" ") || `Mitglied #${memberId}`;
  const principalName = data.principalMember ? [data.principalMember.vorname, data.principalMember.name].filter(Boolean).join(" ") : `Mitglied #${data.principalMemberId}`;
  const workflowByAssignment = new Map(data.documentWorkflows.map((workflow) => [workflow.wartungsvertrag_zuordnung_id, workflow]));

  const prepareDocumentWorkflow = async (assignmentId: number) => {
    try {
      const workflow = await prepareMaintenanceDocumentWorkflow(club, session, assignmentId);
      setData((current) => current ? { ...current, documentWorkflows: [...current.documentWorkflows.filter((item) => item.wartungsvertrag_zuordnung_id !== assignmentId), workflow] } : current);
      setMessage(workflow.fehlermeldung ?? `Dokumentworkflow: ${workflow.status}. Eine freigegebene Vorlage und Signaturfreigabe stehen noch aus.`);
    } catch (cause) {
      console.error("Wartungsvertrags-Dokumentworkflow konnte nicht nachgeholt werden.", { assignmentId, cause });
      setMessage(cause instanceof Error ? cause.message : "Dokumentworkflow konnte nicht vorbereitet werden.");
    }
  };

  async function openStatusDocument(documentId: number) {
    const documentUrl = await resolveActiveDocumentOpenUrl(club, session, documentId);
    window.open(documentUrl, "_blank", "noopener,noreferrer");
  }

  const renderSection = (title: string, timeStatus: string, items: MaintenanceAssignment[]) => <section><h3>{title}</h3>{items.length ? <div className="maintenance-assignments">{items.map((assignment) => {
    const contract = contracts.get(assignment.maintenanceContractId);
    const workflow = workflowByAssignment.get(assignment.id);
    const proofStatus = proofStatuses.get(assignment.id);
    const isContinuation = assignment.predecessorAssignmentId !== null && assignment.predecessorAssignmentId !== undefined;
    return <article key={assignment.id}><div><strong>{contract?.title ?? "Wartungsvertrag"}</strong><span>{timeStatus} · {contract?.area || "Kein Bereich"} · {contract ? maintenanceWorkHoursEffectLabel(contract) : "Keine Angabe"}</span><span>{formatDate(assignment.validFrom)} – {assignment.validUntil ? formatDate(assignment.validUntil) : "offen"}</span>{proofStatus && <DocumentProofStatusDisplay subject="Wartungsvertrag" status={proofStatus} dateLabel="Dokumentdatum" onOpenDocument={openStatusDocument} />}{contract?.description && <small>{contract.description}</small>}{workflow && <small>Dokumentworkflow: {workflow.status}{workflow.fehlermeldung ? ` · ${workflow.fehlermeldung}` : ""}</small>}{assignment.validUntil && <MaintenanceContinuationPanel club={club} session={session} assignment={assignment} onChanged={load} />}</div>{canManage && <div>{!isContinuation && <><button className="secondary-action" onClick={() => void prepareDocumentWorkflow(assignment.id)}>Dokumentworkflow prüfen / vorbereiten</button><button className="secondary-action" disabled={workflow?.status === "abgeschlossen"} onClick={() => setSigningAssignmentId(assignment.id)}>{workflow?.status === "abgeschlossen" ? "Abgeschlossen" : "Unterschreiben"}</button></>}{timeStatus === "Aktuell" && <><input type="date" min={assignment.validFrom} value={endDates[assignment.id] || localDate()} onChange={(event) => setEndDates({ ...endDates, [assignment.id]: event.target.value })} /><button className="secondary-action" onClick={async () => { try { await endMaintenanceAssignmentForContract(club, session, assignment, endDates[assignment.id] || localDate()); await load(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Zuordnung konnte nicht beendet werden."); } }}>Beenden</button></>}</div>}</article>;
  })}</div> : <p>Keine Zuordnungen in diesem Zeitraum.</p>}</section>;

  if (signingAssignmentId) return <MaintenanceContractSignatureFlow club={club} session={session} assignmentId={signingAssignmentId} principalMemberId={data.principalMemberId} principalName={principalName} onClose={() => { setSigningAssignmentId(null); void load(); }} />;
  return <section className="data-workspace maintenance-workspace"><div className="data-toolbar"><div><strong>Wartungsverträge von {memberName}</strong><small>{data.member.hauptmitglied_id ? `Nebenmitglied – Wartungsverträge gelten für den gemeinsamen Hauptmitglied-Kontext ${principalName}` : "Hauptmitglied"}</small></div>{canManage && <button onClick={() => setAssigning(true)}>Wartungsverträge zuweisen</button>}</div>{message && <p className="notice">{message}</p>}{proofStatusError && <p className="notice" role="alert">{proofStatusError}</p>}{assigning ? <AssignContractsToMember session={session} memberId={memberId} onCancel={() => setAssigning(false)} onChanged={load} onSaved={async () => { setAssigning(false); await load(); }} /> : <>{renderSection("Aktuell", "Aktuell", assignments.active)}{renderSection("Zukünftig", "Zukünftig", assignments.future)}{renderSection("Historisch", "Historisch", assignments.history)}</>}</section>;
}
