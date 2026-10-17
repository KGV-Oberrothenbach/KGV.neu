import { type ReactNode } from "react";
import { type MaintenanceContractOverview } from "../../models/maintenance/maintenance-contract-overview";
import { type MaintenanceAssignment } from "../../models/maintenance/maintenance-assignment";
import { type ClassifiedMaintenanceAssignments } from "../../services/maintenance/maintenance-assignment-service";

type Props = {
  contract: MaintenanceContractOverview;
  assignments: ClassifiedMaintenanceAssignments;
  memberLabel: (id: number) => string;
  memberContextLabel: (id: number) => string;
  gardenLabel: (id: number) => string;
  formatDate: (value: string) => string;
  endDateForAssignment: (id: number) => string;
  onEndDateChange: (id: number, value: string) => void;
  canManage: boolean;
  onEdit: () => void;
  onEnd: (assignment: MaintenanceAssignment) => void;
  memberAssignmentPanel?: ReactNode;
};

const effect = (item: MaintenanceContractOverview) => item.workHoursEffect === "exempt" ? "Vollbefreiung" : item.workHoursEffect === "credit" ? `${item.workHoursCredit} h Gutschrift pro Saison` : "Keine Anrechnung";

const AssignmentSection = ({ title, items, memberLabel, memberContextLabel, gardenLabel, formatDate, endDateForAssignment, onEndDateChange, canManage, onEnd }: Pick<Props, "memberLabel" | "memberContextLabel" | "gardenLabel" | "formatDate" | "endDateForAssignment" | "onEndDateChange" | "canManage" | "onEnd"> & { title: string; items: MaintenanceAssignment[] }) => items.length > 0 && <section><h4>{title}</h4><div className="maintenance-assignments">{items.map((item) => <article key={item.id}><div><strong>{memberLabel(item.principalMemberId)}</strong><span>{memberContextLabel(item.principalMemberId)} · Garten {gardenLabel(item.principalMemberId)}</span><span>{formatDate(item.validFrom)} – {item.validUntil ? formatDate(item.validUntil) : "aktiv"}</span>{item.note && <small>{item.note}</small>}</div>{canManage && title === "Aktuell zugeordnet" && <div><input type="date" min={item.validFrom} value={endDateForAssignment(item.id)} onChange={(event) => onEndDateChange(item.id, event.target.value)} /><button className="secondary-action" onClick={() => onEnd(item)}>Beenden</button></div>}</article>)}</div></section>;

export function MaintenanceContractDetail(props: Props) {
  const { contract, assignments, memberLabel, memberContextLabel, gardenLabel, formatDate, endDateForAssignment, onEndDateChange, canManage, onEdit, onEnd, memberAssignmentPanel } = props;
  return <aside className="detail-panel maintenance-detail"><div className="detail-title"><div><h2>{contract.title}</h2><p><strong>Bereich</strong><br />{contract.area || "Kein Bereich"}</p></div>{canManage && <button onClick={onEdit}>Bearbeiten</button>}</div><section><h3>Beschreibung</h3><p>{contract.description || "Keine Beschreibung hinterlegt."}</p></section><div className="maintenance-summary"><div><span>Max. Belegung</span><strong>{contract.maxActiveAssignments}</strong></div><div><span>Aktuelle Belegung</span><strong>{contract.occupied}</strong></div><div><span>Frei</span><strong>{contract.available}</strong></div></div><div className="context-note"><p><strong>Arbeitsstundenwirkung</strong><br />{effect(contract)}</p><p><strong>Status</strong><br />{contract.active ? "aktiv" : "inaktiv"}</p></div>{contract.note && <section><h3>Bemerkung</h3><p>{contract.note}</p></section>}<section className="maintenance-member-assignment"><h3>Mitgliederzuordnung</h3>{assignments.active.length ? <AssignmentSection title="Aktuell zugeordnet" items={assignments.active} memberLabel={memberLabel} memberContextLabel={memberContextLabel} gardenLabel={gardenLabel} formatDate={formatDate} endDateForAssignment={endDateForAssignment} onEndDateChange={onEndDateChange} canManage={canManage} onEnd={onEnd}/> : <p>Keine Mitglieder aktuell zugeordnet.</p>}<AssignmentSection title="Zukünftige Zuordnungen" items={assignments.future} memberLabel={memberLabel} memberContextLabel={memberContextLabel} gardenLabel={gardenLabel} formatDate={formatDate} endDateForAssignment={endDateForAssignment} onEndDateChange={onEndDateChange} canManage={canManage} onEnd={onEnd}/><AssignmentSection title="Historie" items={assignments.history} memberLabel={memberLabel} memberContextLabel={memberContextLabel} gardenLabel={gardenLabel} formatDate={formatDate} endDateForAssignment={endDateForAssignment} onEndDateChange={onEndDateChange} canManage={canManage} onEnd={onEnd}/>{memberAssignmentPanel}</section></aside>;
}
