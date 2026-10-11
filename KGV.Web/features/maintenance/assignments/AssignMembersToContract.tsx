import { useEffect, useMemo, useState } from "react";
import { type BrowserSession } from "../../../lib/supabase-auth";
import { type MaintenanceContract } from "../../../models/maintenance/maintenance-contract";
import { assignMembersToMaintenanceContract, loadMaintenanceMemberCandidates, type MaintenanceMemberCandidate } from "../../../services/maintenance/maintenance-assignment-service";
import { MaintenanceMemberSelector } from "./MaintenanceMemberSelector";

const localDate = () => {
  const date = new Date();
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
};

export function AssignMembersToContract({ session, contract, onSaved, onCancel }: { session: BrowserSession; contract: MaintenanceContract; onSaved: (count: number) => Promise<void>; onCancel: () => void }) {
  const [validFrom, setValidFrom] = useState(localDate); const [note, setNote] = useState(""); const [query, setQuery] = useState(""); const [sortBy, setSortBy] = useState<"name" | "garden">("name"); const [candidates, setCandidates] = useState<MaintenanceMemberCandidate[]>([]); const [availableCapacity, setAvailableCapacity] = useState(0); const [selectedMemberIds, setSelectedMemberIds] = useState<Set<number>>(new Set()); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false); const [loading, setLoading] = useState(true);
  async function load() { if (!validFrom) return; setLoading(true); setMessage(""); try { const result = await loadMaintenanceMemberCandidates(session, contract.id, validFrom); setCandidates(result.candidates); setAvailableCapacity(result.availableCapacity); setSelectedMemberIds(new Set()); } catch (cause) { setCandidates([]); setMessage(cause instanceof Error ? cause.message : "Mitglieder konnten nicht geladen werden."); } finally { setLoading(false); } }
  useEffect(() => { void load(); }, [validFrom]);
  const selectedPrincipalIds = useMemo(() => new Set(candidates.filter((candidate) => selectedMemberIds.has(candidate.selectedMemberId)).map((candidate) => candidate.principalMemberId)), [candidates, selectedMemberIds]);
  const visibleCandidates = useMemo(() => {
    const search = query.trim().toLocaleLowerCase("de");
    return candidates.filter((candidate) => !search || `${candidate.displayName} ${candidate.gardenNumbers.join(" ")}`.toLocaleLowerCase("de").includes(search)).sort((left, right) => sortBy === "garden"
      ? Number(!left.gardenNumbers.length) - Number(!right.gardenNumbers.length) || (left.gardenNumbers[0] ?? "").localeCompare(right.gardenNumbers[0] ?? "", "de", { numeric: true, sensitivity: "base" }) || left.displayName.localeCompare(right.displayName, "de")
      : left.displayName.localeCompare(right.displayName, "de") || (left.gardenNumbers[0] ?? "").localeCompare(right.gardenNumbers[0] ?? "", "de", { numeric: true, sensitivity: "base" }));
  }, [candidates, query, sortBy]);
  const disabledReason = (candidate: MaintenanceMemberCandidate) => {
    if (selectedMemberIds.has(candidate.selectedMemberId)) return null;
    if (!candidate.selectable) return candidate.statusText ?? "Nicht auswählbar";
    if (selectedPrincipalIds.has(candidate.principalMemberId)) return "Hauptmitglied-Kontext bereits ausgewählt";
    if (selectedPrincipalIds.size >= availableCapacity) return "Für den gewählten Zeitraum ist das Kontingent erreicht";
    return null;
  };
  const toggle = (candidate: MaintenanceMemberCandidate) => { if (disabledReason(candidate)) return; setSelectedMemberIds((current) => { const next = new Set(current); if (next.has(candidate.selectedMemberId)) next.delete(candidate.selectedMemberId); else next.add(candidate.selectedMemberId); return next; }); };
  async function save() { if (!selectedMemberIds.size || saving) return; setSaving(true); setMessage(""); try { const created = await assignMembersToMaintenanceContract(session, { maintenanceContractId: contract.id, selectedMemberIds: [...selectedMemberIds], validFrom, validUntil: null, note }); await onSaved(created.length); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Zuordnungen konnten nicht gespeichert werden."); await load(); } finally { setSaving(false); } }
  const remaining = Math.max(0, availableCapacity - selectedPrincipalIds.size);
  return <section className="detail-panel maintenance-assignment-form"><div className="detail-title"><div><h2>Mitglieder zuweisen</h2><p>{contract.title}</p></div><button className="secondary-action" disabled={saving} onClick={onCancel}>Abbrechen</button></div>{message && <p className="notice" role="status">{message}</p>}<label>Gültig ab<input type="date" value={validFrom} disabled={saving} onChange={(event) => setValidFrom(event.target.value)} /></label><label>Bemerkung<input value={note} disabled={saving} onChange={(event) => setNote(event.target.value)} /></label><label>Mitglied suchen<input placeholder="Mitglied suchen…" value={query} disabled={saving} onChange={(event) => setQuery(event.target.value)} /></label><label>Sortierung<select value={sortBy} disabled={saving} onChange={(event) => setSortBy(event.target.value as "name" | "garden")}><option value="name">Name</option><option value="garden">Gartennummer</option></select></label><p className="context-note">Für den gewählten Zeitraum noch {remaining} {remaining === 1 ? "Platz" : "Plätze"} verfügbar.</p>{loading ? <p>Mitglieder werden geladen…</p> : <MaintenanceMemberSelector candidates={visibleCandidates} selectedMemberIds={selectedMemberIds} disabledReason={disabledReason} onToggle={toggle} />}<button disabled={saving || loading || !validFrom || selectedMemberIds.size === 0} onClick={() => void save()}>{saving ? "Zuordnungen werden gespeichert…" : `${selectedMemberIds.size} ${selectedMemberIds.size === 1 ? "Mitglied" : "Mitglieder"} zuordnen`}</button></section>;
}
