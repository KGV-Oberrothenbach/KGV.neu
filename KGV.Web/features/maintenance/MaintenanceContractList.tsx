import { type MaintenanceContractOverview } from "../../models/maintenance/maintenance-contract-overview";
import { maintenanceWorkHoursEffectLabel } from "../../models/maintenance/maintenance-contract";

export function MaintenanceContractList({ items, selectedId, onSelect }: { items: MaintenanceContractOverview[]; selectedId: number | null; onSelect: (id: number) => void }) {
  return <div className="data-table-wrap"><table><thead><tr><th>Titel</th><th>Status</th><th>Belegung heute</th><th>Arbeitsstundenwirkung</th></tr></thead><tbody>{items.map((item) => <tr key={item.id} className={selectedId === item.id ? "selected-row" : ""} onClick={() => onSelect(item.id)}><td><strong>{item.title}</strong>{item.contractNumber && <small> · Vertrags-Nr. {item.contractNumber}</small>}</td><td>{item.active ? "Aktiv" : "Inaktiv"}</td><td>{item.occupied} von {item.maxActiveAssignments}</td><td>{maintenanceWorkHoursEffectLabel(item)}</td></tr>)}</tbody></table>{items.length === 0 && <p className="empty-state">Keine Wartungsverträge vorhanden.</p>}</div>;
}
