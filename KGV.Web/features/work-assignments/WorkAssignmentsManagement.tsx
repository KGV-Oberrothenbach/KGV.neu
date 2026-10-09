import { useEffect, useState } from "react";
import { deleteSupabase, writeSupabase, type BrowserSession } from "../../lib/supabase-auth";
import { useEditLock } from "../../lib/use-edit-lock";
import { loadWorkAssignmentsManagement } from "../../services/work-assignments/work-assignment-service";
import { WorkAssignmentEditor } from "./WorkAssignmentEditor";
import { WorkAssignmentList } from "./WorkAssignmentList";
import { WorkAssignmentParticipants } from "./WorkAssignmentParticipants";
import { type WorkAssignment } from "./work-assignment-types";

export function WorkAssignmentsManagement({ session, canEdit, saisonId, onBack }: { session: BrowserSession; canEdit: boolean; saisonId: number | null; onBack: () => void }) {
  const [items, setItems] = useState<WorkAssignment[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [creating, setCreating] = useState(false);
  const [draft, setDraft] = useState<Partial<WorkAssignment>>({});
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const load = () => loadWorkAssignmentsManagement(session).then(setItems).catch((cause: Error) => {
    setMessage(cause.message);
    return [] as WorkAssignment[];
  });

  useEffect(() => { void load(); }, [session]);
  const selected = items.find((item) => item.id === selectedId) ?? null;
  const selectedIndex = selected ? items.findIndex((item) => item.id === selected.id) : -1;
  const editLock = useEditLock(session, "arbeitseinsatz", selected?.id, Boolean(selected && canEdit && !creating));
  useEffect(() => { if (editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [editLock.message]);

  function startNew() { setCreating(true); setSelectedId(null); setDraft(emptyDraft()); setMessage(""); }
  function selectEntry(id: number) { setCreating(false); setSelectedId(id); setDraft(items.find((item) => item.id === id) ?? emptyDraft()); setMessage(""); }
  function moveSelection(offset: number) { const target = items[selectedIndex + offset]; if (target) selectEntry(target.id); }
  function set(key: keyof WorkAssignment, value: string | number | boolean | null) { setDraft({ ...draft, [key]: value }); }

  function prepareNextShift(source: Partial<WorkAssignment>) {
    const toMinutes = (value: string | null | undefined, fallback: number) => { if (!value) return fallback; const [hours, minutes] = value.split(":").map(Number); return Number.isFinite(hours + minutes) ? hours * 60 + minutes : fallback; };
    const toTime = (value: number) => `${String(Math.floor(value / 60)).padStart(2, "0")}:${String(value % 60).padStart(2, "0")}`;
    const start = toMinutes(source.start_uhrzeit, 600);
    const end = toMinutes(source.end_uhrzeit, 780);
    const duration = end > start ? end - start : 180;
    const nextStart = Math.min(end, 1439);
    const nextEnd = Math.min(nextStart + duration, 1439);
    setCreating(true);
    setSelectedId(null);
    setDraft({ ...source, id: undefined, start_uhrzeit: toTime(nextStart), end_uhrzeit: toTime(nextEnd) });
    setMessage("Arbeitseinsatz gespeichert. Die nächste Schicht ist bereits vorbefüllt.");
  }

  async function save(prepareNext = false) {
    const hours = Number(draft.stunden_wert ?? 0);
    const capacity = draft.max_teilnehmer === null || draft.max_teilnehmer === undefined || draft.max_teilnehmer === "" ? null : Number(draft.max_teilnehmer);
    if (!draft.titel?.trim() || !draft.datum) { setMessage("Titel und Datum sind erforderlich."); return; }
    if (draft.start_uhrzeit && draft.end_uhrzeit && draft.end_uhrzeit < draft.start_uhrzeit) { setMessage("Das Ende darf nicht vor dem Beginn liegen."); return; }
    if (draft.sichtbar_ab && draft.sichtbar_bis && draft.sichtbar_bis < draft.sichtbar_ab) { setMessage("Das Sichtbarkeitsende darf nicht vor dem Beginn liegen."); return; }
    if (draft.anmeldung_bis && draft.anmeldung_bis.slice(0, 10) > draft.datum) { setMessage("Der Anmeldeschluss darf nicht nach dem Einsatztag liegen."); return; }
    if (!Number.isFinite(hours) || hours < 0 || (capacity !== null && (!Number.isInteger(capacity) || capacity < 1))) { setMessage("Stundenwert und Teilnehmerbegrenzung sind ungültig."); return; }
    if (!creating && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    setSaving(true);
    setMessage("");
    try {
      const payload: Omit<WorkAssignment, "id"> = { titel: draft.titel.trim(), beschreibung: draft.beschreibung?.trim() || null, datum: draft.datum, start_uhrzeit: draft.start_uhrzeit || null, end_uhrzeit: draft.end_uhrzeit || null, treffpunkt: draft.treffpunkt?.trim() || null, max_teilnehmer: capacity, stunden_wert: hours, sichtbar_ab: draft.sichtbar_ab || null, sichtbar_bis: draft.sichtbar_bis || null, anmeldung_bis: draft.anmeldung_bis || null, aktiv: draft.aktiv !== false };
      const rows = creating ? await writeSupabase<WorkAssignment>(session, "arbeitseinsatz", "POST", payload) : await writeSupabase<WorkAssignment>(session, "arbeitseinsatz", "PATCH", payload, { id: `eq.${selected?.id}` });
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
    try { await writeSupabase<WorkAssignment>(session, "arbeitseinsatz", "PATCH", { aktiv: false }, { id: `eq.${selected.id}` }); await load(); setMessage("Arbeitseinsatz wurde abgesagt."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Arbeitseinsatz konnte nicht abgesagt werden."); }
    finally { setSaving(false); }
  }

  async function removeAssignment() {
    if (!selected) return;
    if (!editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    if (!window.confirm(`Den Arbeitseinsatz „${selected.titel ?? "Ohne Titel"}“ einschließlich seiner Anmeldungen endgültig löschen?`)) return;
    setSaving(true);
    setMessage("");
    try { await deleteSupabase(session, "arbeitseinsatz", { id: `eq.${selected.id}` }); setSelectedId(null); setCreating(false); await load(); setMessage("Arbeitseinsatz wurde gelöscht."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Arbeitseinsatz konnte nicht gelöscht werden."); }
    finally { setSaving(false); }
  }

  return <section className="data-workspace work-assignment-management"><div className="data-toolbar"><div className="editor-actions"><button className="secondary-action" onClick={onBack}>Zur Startseite</button><button className="secondary-action" onClick={() => void load()}>Aktualisieren</button></div><span>{items.length} Arbeitseinsätze</span>{canEdit && <button onClick={startNew}>Arbeitseinsatz anlegen</button>}</div>{message && <p className="notice" role="status">{message}</p>}<div className="split-view"><WorkAssignmentList items={items} selectedId={selected?.id ?? null} creating={creating} onSelect={selectEntry} /><WorkAssignmentEditor selected={selected} selectedIndex={selectedIndex} itemCount={items.length} creating={creating} draft={draft} canEdit={canEdit} saving={saving} onChange={set} onMove={moveSelection} onSave={(prepareNext) => void save(prepareNext)} onCancel={() => void cancelAssignment()} onRemove={() => void removeAssignment()} onClose={() => { setCreating(false); setSelectedId(null); setMessage(""); }}>{selected && canEdit && <WorkAssignmentParticipants session={session} assignment={selected} saisonId={saisonId} />}</WorkAssignmentEditor></div></section>;
}

function emptyDraft(): Partial<WorkAssignment> {
  const now = currentLocalDateTime();
  const today = now.slice(0, 10);
  return { titel: "", beschreibung: "", datum: today, start_uhrzeit: "10:00", end_uhrzeit: "13:00", treffpunkt: "", max_teilnehmer: null, stunden_wert: 0, sichtbar_ab: now, sichtbar_bis: `${today}T23:59`, anmeldung_bis: "", aktiv: true };
}

function currentLocalDateTime() {
  const parts = new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Berlin", year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", hourCycle: "h23" }).formatToParts(new Date()).reduce<Record<string, string>>((result, part) => {
    result[part.type] = part.value;
    return result;
  }, {});
  return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}:${parts.minute}`;
}
