import { useSelectedClub } from "../auth/AuthProvider";
import { useCallback, useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { endMaintenanceAssignmentForContract, loadMemberMaintenanceContracts } from "../../services/maintenance/maintenance-assignment-service";
import { type MaintenanceAssignment } from "../../models/maintenance/maintenance-assignment";
import { maintenanceWorkHoursEffectLabel } from "../../models/maintenance/maintenance-contract";
import { AssignContractsToMember } from "./assignments/AssignContractsToMember";

const localDate = () => { const date = new Date(); return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`; };
const formatDate = (value: string) => { const [year, month, day] = value.split("-"); return year && month && day ? `${day}.${month}.${year}` : value; };
export function MemberMaintenanceContracts({ session, memberId, canManage }: { session: BrowserSession; memberId: number; canManage: boolean }) {
  const club = useSelectedClub();
  const [data, setData] = useState<Awaited<ReturnType<typeof loadMemberMaintenanceContracts>> | null>(null); const [message, setMessage] = useState(""); const [assigning, setAssigning] = useState(false); const [endDates, setEndDates] = useState<Record<number, string>>({});
  const load = useCallback(async () => { try { setData(await loadMemberMaintenanceContracts(club, session, memberId)); setMessage(""); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Wartungsverträge konnten nicht geladen werden."); } }, [session, memberId, club]);
  useEffect(() => {
    let active = true;
    loadMemberMaintenanceContracts(club, session, memberId).then((items) => {
      if (!active) return;
      setData(items); setMessage("");
    }).catch((cause) => {
      if (active) setMessage(cause instanceof Error ? cause.message : "Wartungsverträge konnten nicht geladen werden.");
    });
    return () => { active = false; };
  }, [session, memberId, club]);
  if (!data) return <section className="data-workspace"><p>{message || "Wartungsverträge werden geladen…"}</p></section>;
  const assignments = data.assignments; const contracts = new Map(data.contracts.map((contract) => [contract.id, contract]));
  const renderSection = (title: string, timeStatus: string, items: MaintenanceAssignment[]) => <section><h3>{title}</h3>{items.length ? <div className="maintenance-assignments">{items.map((assignment) => { const contract = contracts.get(assignment.maintenanceContractId); return <article key={assignment.id}><div><strong>{contract?.title ?? "Wartungsvertrag"}</strong><span>{timeStatus} · {contract?.area || "Kein Bereich"} · {contract ? maintenanceWorkHoursEffectLabel(contract) : "Keine Angabe"}</span><span>{formatDate(assignment.validFrom)} – {assignment.validUntil ? formatDate(assignment.validUntil) : "offen"}</span>{contract?.description && <small>{contract.description}</small>}</div>{canManage && timeStatus === "Aktuell" && <div><input type="date" min={assignment.validFrom} value={endDates[assignment.id] || localDate()} onChange={(event) => setEndDates({ ...endDates, [assignment.id]: event.target.value })} /><button className="secondary-action" onClick={async () => { try { await endMaintenanceAssignmentForContract(club, session, assignment, endDates[assignment.id] || localDate()); await load(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Zuordnung konnte nicht beendet werden."); } }}>Beenden</button></div>}</article>; })}</div> : <p>Keine Zuordnungen in diesem Zeitraum.</p>}</section>;
  const memberName = [data.member.vorname, data.member.name].filter(Boolean).join(" ") || `Mitglied #${memberId}`;
  const principalName = data.principalMember ? [data.principalMember.vorname, data.principalMember.name].filter(Boolean).join(" ") : `Mitglied #${data.principalMemberId}`;
  return <section className="data-workspace maintenance-workspace"><div className="data-toolbar"><div><strong>Wartungsverträge von {memberName}</strong><small>{data.member.hauptmitglied_id ? `Nebenmitglied – Wartungsverträge gelten für den gemeinsamen Hauptmitglied-Kontext ${principalName}` : "Hauptmitglied"}</small></div>{canManage && <button onClick={() => setAssigning(true)}>Wartungsverträge zuweisen</button>}</div>{message && <p className="notice">{message}</p>}{assigning ? <AssignContractsToMember session={session} memberId={memberId} onCancel={() => setAssigning(false)} onChanged={load} onSaved={async (count) => { setAssigning(false); await load(); setMessage(`${count} ${count === 1 ? "Vertrag wurde" : "Verträge wurden"} zugeordnet.`); }} /> : <>{renderSection("Aktuell", "Aktuell", assignments.active)}{renderSection("Zukünftig", "Zukünftig", assignments.future)}{renderSection("Historisch", "Historisch", assignments.history)}</>}</section>;
}
