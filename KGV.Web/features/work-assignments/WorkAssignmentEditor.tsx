import { type WorkAssignment } from "./work-assignment-types";

type Props = {
  selected: WorkAssignment | null;
  selectedIndex: number;
  itemCount: number;
  creating: boolean;
  draft: Partial<WorkAssignment>;
  canEdit: boolean;
  saving: boolean;
  onChange: (key: keyof WorkAssignment, value: string | number | boolean | null) => void;
  onMove: (offset: number) => void;
  onSave: (prepareNext: boolean) => void;
  onCancel: () => void;
  onRemove: () => void;
  onClose: () => void;
  children?: React.ReactNode;
};

export function WorkAssignmentEditor({ selected, selectedIndex, itemCount, creating, draft, canEdit, saving, onChange, onMove, onSave, onCancel, onRemove, onClose, children }: Props) {
  return <aside className="detail-panel member-editor"><div className="detail-title"><div><h2>{creating ? "Neuer Arbeitseinsatz" : selected ? "Arbeitseinsatz bearbeiten" : "Auswahl"}</h2>{selected && <p>{selectedIndex + 1} von {itemCount}</p>}</div>{selected && <div className="record-navigation"><button className="secondary-action" disabled={selectedIndex <= 0} onClick={() => onMove(-1)} aria-label="Vorheriger Arbeitseinsatz">←</button><button className="secondary-action" disabled={selectedIndex < 0 || selectedIndex >= itemCount - 1} onClick={() => onMove(1)} aria-label="Nächster Arbeitseinsatz">→</button></div>}</div>
    {(selected || creating) ? <><fieldset disabled={!canEdit || saving}><label className="wide">Titel *<input value={draft.titel ?? ""} onChange={(event) => onChange("titel", event.target.value)} /></label><label>Datum *<input type="date" value={draft.datum ?? ""} onChange={(event) => onChange("datum", event.target.value)} /></label><label>Beginn<input type="time" value={draft.start_uhrzeit ?? ""} onChange={(event) => onChange("start_uhrzeit", event.target.value)} /></label><label>Ende<input type="time" value={draft.end_uhrzeit ?? ""} onChange={(event) => onChange("end_uhrzeit", event.target.value)} /></label><label>Treffpunkt<input value={draft.treffpunkt ?? ""} onChange={(event) => onChange("treffpunkt", event.target.value)} /></label><label>Max. Teilnehmer<input type="number" min="1" value={draft.max_teilnehmer ?? ""} onChange={(event) => onChange("max_teilnehmer", event.target.value ? Number(event.target.value) : null)} placeholder="unbegrenzt" /></label><label>Stundenwert<input type="number" min="0" step="0.25" value={draft.stunden_wert ?? 0} onChange={(event) => onChange("stunden_wert", Number(event.target.value))} /></label><label>Sichtbar ab<input type="datetime-local" value={(draft.sichtbar_ab ?? "").slice(0, 16)} onChange={(event) => onChange("sichtbar_ab", event.target.value)} /></label><label>Sichtbar bis<input type="datetime-local" value={(draft.sichtbar_bis ?? "").slice(0, 16)} onChange={(event) => onChange("sichtbar_bis", event.target.value)} /></label><label>Anmeldeschluss<input type="datetime-local" value={(draft.anmeldung_bis ?? "").slice(0, 16)} onChange={(event) => onChange("anmeldung_bis", event.target.value)} /></label><label className="check"><input type="checkbox" checked={draft.aktiv !== false} onChange={(event) => onChange("aktiv", event.target.checked)} /> Einsatz aktiv</label><label className="wide">Beschreibung<textarea value={draft.beschreibung ?? ""} onChange={(event) => onChange("beschreibung", event.target.value)} /></label></fieldset>
      {canEdit && <div className="editor-actions assignment-editor-actions"><button disabled={saving} onClick={() => onSave(false)}>Speichern</button><button className="secondary-action" disabled={saving} onClick={() => onSave(true)}>Speichern + nächste Schicht</button>{selected?.aktiv && <button className="reject-action" disabled={saving} onClick={onCancel}>Absagen</button>}{selected && <button className="reject-action" disabled={saving} onClick={onRemove}>Löschen</button>}<button className="secondary-action" onClick={onClose}>Schließen</button></div>}
      {children}</> : <p>Wähle links einen Arbeitseinsatz aus oder lege einen neuen an.</p>}
  </aside>;
}
