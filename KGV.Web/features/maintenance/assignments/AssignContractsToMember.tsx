import { useSelectedClub } from "../../auth/AuthProvider";
import { useCallback, useEffect, useState } from "react";
import { type BrowserSession } from "../../../lib/supabase-auth";
import { assignMaintenanceContractsToMember, loadAssignableMaintenanceContractsForMember, type MaintenanceContractCandidate } from "../../../services/maintenance/maintenance-assignment-service";

const localDate = () => { const date = new Date(); return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`; };
const effect = (candidate: MaintenanceContractCandidate) => candidate.contract.exemptsFromDutyHours ? "Vollständige Befreiung" : candidate.contract.workHoursCredit > 0 ? `${candidate.contract.workHoursCredit} h Gutschrift pro Saison` : "Keine Anrechnung";

export function AssignContractsToMember({ session, memberId, onSaved, onChanged, onCancel }: { session: BrowserSession; memberId: number; onSaved: (count: number) => Promise<void>; onChanged: () => Promise<void>; onCancel: () => void }) {
  const club = useSelectedClub();
  const [validFrom, setValidFrom] = useState(localDate); const [note, setNote] = useState(""); const [candidates, setCandidates] = useState<MaintenanceContractCandidate[]>([]); const [selected, setSelected] = useState<Set<number>>(new Set()); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const load = useCallback(async () => { try { const items = await loadAssignableMaintenanceContractsForMember(club, session, memberId, validFrom); setCandidates(items); setSelected(new Set()); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Vertragskandidaten konnten nicht geladen werden."); } }, [session, memberId, validFrom, club]);
  useEffect(() => {
    let active = true;
    loadAssignableMaintenanceContractsForMember(club, session, memberId, validFrom).then((items) => {
      if (!active) return;
      setCandidates(items); setSelected(new Set()); setMessage("");
    }).catch((cause) => {
      if (active) setMessage(cause instanceof Error ? cause.message : "Vertragskandidaten konnten nicht geladen werden.");
    });
    return () => { active = false; };
  }, [session, memberId, validFrom, club]);
  async function save() { setSaving(true); setMessage(""); try { const result = await assignMaintenanceContractsToMember(club, session, { selectedMemberId: memberId, maintenanceContractIds: [...selected], validFrom, validUntil: null, note }); if (result.failures.length) { if (result.created.length) await onChanged(); await load(); const details = result.failures.map((failure) => `${candidates.find((candidate) => candidate.contract.id === failure.targetId)?.contract.title ?? `Wartungsvertrag #${failure.targetId}`}: ${failure.message}`).join(" "); setMessage(`${result.created.length} von ${selected.size} Zuordnungen wurden gespeichert. ${details}`); } else await onSaved(result.created.length); } catch (cause) { const failure = cause instanceof Error ? cause.message : "Zuordnungen konnten nicht gespeichert werden."; await load(); setMessage(failure); } finally { setSaving(false); } }
  return <section className="detail-panel maintenance-assignment-form"><div className="detail-title"><h2>Wartungsverträge zuweisen</h2><button className="secondary-action" onClick={onCancel}>Abbrechen</button></div>{message && <p className="notice">{message}</p>}<label>Gültig ab<input type="date" value={validFrom} onChange={(event) => setValidFrom(event.target.value)} /></label><label>Bemerkung<input value={note} onChange={(event) => setNote(event.target.value)} /></label><div className="maintenance-assignments">{candidates.map((candidate) => <label key={candidate.contract.id} className="maintenance-member-candidate"><input type="checkbox" checked={selected.has(candidate.contract.id)} disabled={!candidate.selectable && !selected.has(candidate.contract.id)} onChange={() => setSelected((current) => { const next = new Set(current); if (next.has(candidate.contract.id)) next.delete(candidate.contract.id); else next.add(candidate.contract.id); return next; })} /><span><strong>{candidate.contract.title}</strong><small>{candidate.contract.area || "Kein Bereich"} · {effect(candidate)}</small><small>{candidate.statusText ?? candidate.contract.description ?? "Auswählbar"}</small></span></label>)}</div><button disabled={saving || !selected.size} onClick={() => void save()}>{selected.size} {selected.size === 1 ? "Vertrag" : "Verträge"} zuordnen</button></section>;
}
