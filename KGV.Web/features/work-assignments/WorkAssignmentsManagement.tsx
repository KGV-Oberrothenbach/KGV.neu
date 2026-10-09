import { useCallback, useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { useEditLock } from "../../lib/use-edit-lock";
import { deactivateWorkAssignment, loadWorkAssignmentsManagement, normalizeAndValidateWorkAssignment, prepareNextWorkAssignment, removeWorkAssignment, saveWorkAssignment, updateWorkAssignmentDateDefaults, workAssignmentDefaults } from "../../services/work-assignments/work-assignment-service";
import { WorkAssignmentEditor } from "./WorkAssignmentEditor";
import { WorkAssignmentList } from "./WorkAssignmentList";
import { WorkAssignmentParticipants } from "./WorkAssignmentParticipants";
import { type WorkAssignment } from "../../models/work-assignments/work-assignment";

export function WorkAssignmentsManagement({ session, canEdit, onBack }: { session: BrowserSession; canEdit: boolean; onBack: () => void }) {
  const [items, setItems] = useState<WorkAssignment[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [creating, setCreating] = useState(false);
  const [draft, setDraft] = useState<Partial<WorkAssignment>>({});
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const load = useCallback(() => loadWorkAssignmentsManagement(session).then(setItems).catch((cause: Error) => {
    setMessage(cause.message);
    return [] as WorkAssignment[];
  }), [session]);

  useEffect(() => { void load(); }, [load]);
  const selected = items.find((item) => item.id === selectedId) ?? null;
  const selectedIndex = selected ? items.findIndex((item) => item.id === selected.id) : -1;
  const editLock = useEditLock(session, "arbeitseinsatz", selected?.id, Boolean(selected && canEdit && !creating));
  useEffect(() => { if (editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [editLock.message]);

  function startNew() { setCreating(true); setSelectedId(null); setDraft(workAssignmentDefaults()); setMessage(""); }
  function selectEntry(id: number) { setCreating(false); setSelectedId(id); setDraft(items.find((item) => item.id === id) ?? workAssignmentDefaults()); setMessage(""); }
  function moveSelection(offset: number) { const target = items[selectedIndex + offset]; if (target) selectEntry(target.id); }
  function set(key: keyof WorkAssignment, value: string | number | boolean | null) {
    setDraft((current) => key === "datum" && creating
      ? updateWorkAssignmentDateDefaults(current, String(value ?? ""))
      : { ...current, [key]: value });
  }

  function prepareNextShift(source: Omit<WorkAssignment, "id">) {
    setCreating(true); setSelectedId(null); setDraft(prepareNextWorkAssignment(source));
    setMessage("Arbeitseinsatz gespeichert. Die nächste Schicht ist bereits vorbefüllt.");
  }

  async function save(prepareNext = false) {
    const validation = normalizeAndValidateWorkAssignment(draft); if (!validation.payload) { setMessage(validation.error ?? "Ungültiger Arbeitseinsatz."); return; }
    if (!creating && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    setSaving(true);
    setMessage("");
    try {
      const payload = validation.payload; const rows = await saveWorkAssignment(session, creating ? null : selected?.id ?? null, payload);
      const persisted = { ...payload, id: rows[0]?.id ?? selected?.id ?? 0 } as WorkAssignment;
      await load();
      if (prepareNext) prepareNextShift(persisted);
      else { setCreating(false); setSelectedId(persisted.id || null); setMessage("Arbeitseinsatz gespeichert."); }
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Arbeitseinsatz konnte nicht gespeichert werden.");
    } finally {
      setSaving(false);
    }
  }

  async function cancelAssignment() {
    if (!selected) return;
    if (!editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    if (!window.confirm(`Den Arbeitseinsatz „${selected.titel ?? "Ohne Titel"}“ wirklich absagen?`)) return;
    setSaving(true);
    setMessage("");
    try { await deactivateWorkAssignment(session, selected.id); await load(); setMessage("Arbeitseinsatz wurde abgesagt."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Arbeitseinsatz konnte nicht abgesagt werden."); }
    finally { setSaving(false); }
  }

  async function removeAssignment() {
    if (!selected) return;
    if (!editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    if (!window.confirm(`Den Arbeitseinsatz „${selected.titel ?? "Ohne Titel"}“ einschließlich seiner Anmeldungen endgültig löschen?`)) return;
    setSaving(true);
    setMessage("");
    try { await removeWorkAssignment(session, selected.id); setSelectedId(null); setCreating(false); await load(); setMessage("Arbeitseinsatz wurde gelöscht."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Arbeitseinsatz konnte nicht gelöscht werden."); }
    finally { setSaving(false); }
  }

  return <section className="data-workspace work-assignment-management"><div className="data-toolbar"><div className="editor-actions"><button className="secondary-action" onClick={onBack}>Zur Startseite</button><button className="secondary-action" onClick={() => void load()}>Aktualisieren</button></div><span>{items.length} Arbeitseinsätze</span>{canEdit && <button onClick={startNew}>Arbeitseinsatz anlegen</button>}</div>{message && <p className="notice" role="status">{message}</p>}<div className="split-view"><WorkAssignmentList items={items} selectedId={selected?.id ?? null} creating={creating} onSelect={selectEntry} /><WorkAssignmentEditor selected={selected} selectedIndex={selectedIndex} itemCount={items.length} creating={creating} draft={draft} canEdit={canEdit} saving={saving} onChange={set} onMove={moveSelection} onSave={(prepareNext) => void save(prepareNext)} onCancel={() => void cancelAssignment()} onRemove={() => void removeAssignment()} onClose={() => { setCreating(false); setSelectedId(null); setMessage(""); }}>{selected && canEdit && <WorkAssignmentParticipants session={session} assignment={selected} />}</WorkAssignmentEditor></div></section>;
}
