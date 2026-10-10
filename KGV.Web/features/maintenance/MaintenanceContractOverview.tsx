import { type ReactNode } from "react";
import { type MaintenanceContractOverview as ContractOverview } from "../../models/maintenance/maintenance-contract-overview";
import { type MaintenanceAssignment } from "../../models/maintenance/maintenance-assignment";
import { type ClassifiedMaintenanceAssignments } from "../../services/maintenance/maintenance-assignment-service";
import { MaintenanceContractList } from "./MaintenanceContractList";
import { MaintenanceContractDetail } from "./MaintenanceContractDetail";

export function MaintenanceContractOverview({ items, selectedId, onSelect, assignments, memberLabel, gardenLabel, canManage, onEdit, onEnd, onCreate, assignmentPanel }: { items: ContractOverview[]; selectedId: number | null; onSelect: (id: number) => void; assignments: ClassifiedMaintenanceAssignments; memberLabel: (id: number) => string; gardenLabel: (id: number) => string; canManage: boolean; onEdit: () => void; onEnd: (assignment: MaintenanceAssignment) => void; onCreate: () => void; assignmentPanel?: ReactNode }) {
  const selected = items.find((item) => item.id === selectedId);
  return <section className="data-workspace maintenance-workspace"><div className="data-toolbar"><span>{items.length} Wartungsverträge</span>{canManage && <button onClick={onCreate}>Wartungsvertrag anlegen</button>}</div><div className="split-view"><MaintenanceContractList items={items} selectedId={selectedId} onSelect={onSelect}/>{selected ? <aside><MaintenanceContractDetail contract={selected} assignments={assignments} memberLabel={memberLabel} gardenLabel={gardenLabel} canManage={canManage} onEdit={onEdit} onEnd={onEnd}/>{assignmentPanel}</aside> : <aside className="detail-panel"><h2>Wartungsvertrag</h2><p>Wähle links einen Vertrag aus.</p></aside>}</div></section>;
}
