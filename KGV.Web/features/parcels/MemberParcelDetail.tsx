"use client";

import type { MemberParcelItem } from "../../services/parcels/parcel-service";

const formatDate = (value?: string | null) => value ? new Intl.DateTimeFormat("de-DE").format(new Date(`${value.slice(0, 10)}T12:00:00`)) : "–";

export function MemberParcelDetail({ items, selectedParcelId, onSelect, onOpenMeters }: { items: MemberParcelItem[]; selectedParcelId: number | null; onSelect: (parcelId: number) => void; onOpenMeters: () => void }) {
  const selectedIndex = Math.max(0, items.findIndex((item) => item.assignment.parzelle_id === selectedParcelId));
  const selected = items[selectedIndex] ?? null;
  if (!selected) return <aside className="detail-panel"><h2>Gärten des Mitglieds</h2><p>Wähle links eine Parzelle aus.</p></aside>;
  const { assignment, parcel } = selected;
  const hasPrevious = selectedIndex > 0;
  const hasNext = selectedIndex < items.length - 1;
  return <aside className="detail-panel parcel-detail"><div className="detail-title"><div><h2>Garten {parcel?.garten_nr ?? `#${assignment.parzelle_id}`}</h2><p>{parcel?.Anlage ?? "Anlage nicht hinterlegt"} · {selected.isCurrent ? "aktuelle Zuordnung" : "historische Zuordnung"}</p></div><span>{selectedIndex + 1} / {items.length}</span></div><div className="parcel-facts"><div><span>Fläche</span><strong>{parcel?.flaeche_qm ?? "–"} m²</strong></div><div><span>Strom</span><strong>{parcel?.hat_strom ? "Ja" : "Nein"}</strong></div><div><span>Wasser</span><strong>{parcel?.hat_wasser ? "Ja" : "Nein"}</strong></div><div><span>Beginn der Zuordnung</span><strong>{formatDate(assignment.von_datum)}</strong></div><div><span>Ende der Zuordnung</span><strong>{assignment.bis_datum ? formatDate(assignment.bis_datum) : "–"}</strong></div>{parcel?.rfid_strom && <div><span>RFID Strom</span><strong>{parcel.rfid_strom}</strong></div>}{parcel?.rfid_wasser && <div><span>RFID Wasser</span><strong>{parcel.rfid_wasser}</strong></div>}</div><div className="editor-actions"><button className="secondary-action" onClick={onOpenMeters}>Zähler / Ablesungen</button><button className="secondary-action" disabled={!hasPrevious} onClick={() => onSelect(items[selectedIndex - 1].assignment.parzelle_id)}>Vorherige Parzelle</button><button className="secondary-action" disabled={!hasNext} onClick={() => onSelect(items[selectedIndex + 1].assignment.parzelle_id)}>Nächste Parzelle</button></div></aside>;
}
