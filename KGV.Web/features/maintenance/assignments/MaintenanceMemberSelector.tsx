import { type MaintenanceMemberCandidate } from "../../../services/maintenance/maintenance-assignment-service";

type Props = {
  candidates: MaintenanceMemberCandidate[];
  selectedMemberIds: Set<number>;
  disabledReason: (candidate: MaintenanceMemberCandidate) => string | null;
  onToggle: (candidate: MaintenanceMemberCandidate) => void;
};

export function MaintenanceMemberSelector({ candidates, selectedMemberIds, disabledReason, onToggle }: Props) {
  return <div className="maintenance-assignments">{candidates.map((candidate) => {
    const disabled = disabledReason(candidate);
    const selected = selectedMemberIds.has(candidate.selectedMemberId);
    return <label key={candidate.selectedMemberId} className="maintenance-member-candidate"><input type="checkbox" checked={selected} disabled={Boolean(disabled)} onChange={() => onToggle(candidate)} /><span><strong>{candidate.displayName}</strong><small>{candidate.memberContext} · {candidate.gardenNumbers.length ? `Gärten ${candidate.gardenNumbers.join(", ")}` : "Kein aktiver Garten"}</small>{(disabled ?? candidate.statusText) && <small className="context-note">{disabled ?? candidate.statusText}</small>}</span></label>;
  })}</div>;
}
