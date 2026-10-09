import { type WorkAssignment } from "./work-assignment-types";

export function WorkAssignmentList({ items, selectedId, creating, onSelect }: { items: WorkAssignment[]; selectedId: number | null; creating: boolean; onSelect: (id: number) => void }) {
  return <div className="data-table-wrap"><table><thead><tr><th>Datum</th><th>Zeit</th><th>Titel</th><th>Treffpunkt</th><th>Teilnehmer</th><th>Status</th></tr></thead><tbody>{items.map((item) => <tr key={item.id} className={selectedId === item.id && !creating ? "selected-row" : ""} onClick={() => onSelect(item.id)}><td>{formatDate(item.datum)}</td><td>{formatTimeRange(item.start_uhrzeit, item.end_uhrzeit)}</td><td><strong>{item.titel}</strong></td><td>{item.treffpunkt ?? "–"}</td><td>{item.max_teilnehmer || "unbegrenzt"}</td><td>{item.aktiv ? "aktiv" : "abgesagt"}</td></tr>)}</tbody></table>{items.length === 0 && <p className="empty-state">Keine Arbeitseinsätze vorhanden.</p>}</div>;
}

function formatDate(value: string | null | undefined) {
  if (!value) return "–";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "–" : new Intl.DateTimeFormat("de-DE").format(date);
}

function formatTimeRange(start: string | null | undefined, end: string | null | undefined) {
  const from = start?.slice(0, 5) ?? "";
  const to = end?.slice(0, 5) ?? "";
  if (from && to) return `${from}–${to} Uhr`;
  if (from) return `${from} Uhr`;
  if (to) return `bis ${to} Uhr`;
  return "–";
}
