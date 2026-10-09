"use client";

import { useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { useEditLock } from "../../lib/use-edit-lock";
import { createAdministrativeWorkHourEntry, createOwnWorkHour, isOwnOpenWorkHourEditable, loadMemberWorkHoursWorkspace, updateOwnWorkHour } from "../../services/work-hours/work-hours-service";
import { RequiredHoursSummary } from "./RequiredHoursSummary";
import { type WorkHour, type WorkHourHistory, type WorkHoursSummary } from "./work-hours-types";

const formatDate = (value: string) => value ? new Intl.DateTimeFormat("de-DE").format(new Date(`${value.slice(0, 10)}T00:00:00`)) : "–";

export function MemberWorkHours({ session, memberId, saisonId, canEditOwn, canManageWorkHours }: { session: BrowserSession; memberId: number; saisonId: number | null; canEditOwn: boolean; canManageWorkHours: boolean }) {
  const [items, setItems] = useState<WorkHour[]>([]);
  const [history, setHistory] = useState<WorkHourHistory[]>([]);
  const [summary, setSummary] = useState<WorkHoursSummary | null>(null);
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [hours, setHours] = useState("");
  const [workType, setWorkType] = useState("");
  const [editingId, setEditingId] = useState<number | null>(null);
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);

  const load = async () => {
    setMessage("");
    if (!saisonId) { setItems([]); setHistory([]); setSummary(null); return; }
    try {
      const workspace = await loadMemberWorkHoursWorkspace(session, memberId, saisonId);
      setItems(workspace.workHours);
      setHistory(workspace.history);
      setSummary(workspace.summary);
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Arbeitsstunden konnten nicht geladen werden.");
    }
  };

  useEffect(() => { void load(); }, [session, memberId, saisonId]);
  const editLock = useEditLock(session, "arbeitsstunde", editingId, editingId !== null);
  useEffect(() => { if (editingId && editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [editingId, editLock.message]);

  function resetForm() { setEditingId(null); setDate(new Date().toISOString().slice(0, 10)); setHours(""); setWorkType(""); }
  async function save() {
    const value = Number(hours.replace(",", "."));
    if (editingId && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    setSaving(true);
    setMessage("");
    try {
      const input = { date, hours: value, workType };
      if (editingId) await updateOwnWorkHour(session, items.find((item) => item.id === editingId), memberId, input);
      else if (canManageWorkHours) await createAdministrativeWorkHourEntry(session, canManageWorkHours, memberId, saisonId, input);
      else await createOwnWorkHour(session, memberId, saisonId, input);
      resetForm();
      await load();
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Arbeitsstunde konnte nicht gespeichert werden.");
    } finally {
      setSaving(false);
    }
  }
  function edit(item: WorkHour) { if (!isOwnOpenWorkHourEditable(item, memberId)) return; setEditingId(item.id); setDate(item.datum.slice(0, 10)); setHours(String(item.stunden)); setWorkType(item.art_der_arbeit); }

  const canCreate = canEditOwn || canManageWorkHours;
  return <section className="data-workspace"><RequiredHoursSummary summary={summary} />{message && <p className="notice">{message}</p>}{canCreate && <section className="detail-panel member-editor"><h2>{editingId ? "Arbeitsstunde bearbeiten" : canManageWorkHours ? "Arbeitsstunde administrativ erfassen" : "Arbeitsstunde erfassen"}</h2>{canManageWorkHours && !editingId && <p>Der Eintrag wird sofort genehmigt gespeichert.</p>}<fieldset><label>Datum<input type="date" value={date} onChange={(event) => setDate(event.target.value)} /></label><label>Stunden<input inputMode="decimal" value={hours} onChange={(event) => setHours(event.target.value)} /></label><label className="wide">Art der Arbeit<input value={workType} onChange={(event) => setWorkType(event.target.value)} /></label></fieldset><div className="editor-actions"><button disabled={saving || !saisonId} onClick={save}>{editingId ? "Änderung speichern" : canManageWorkHours ? "Sofort genehmigt speichern" : "Arbeitsstunde speichern"}</button>{editingId && <button className="secondary-action" onClick={resetForm}>Abbrechen</button>}</div></section>}<div className="split-view"><div className="data-table-wrap"><table><thead><tr><th>Datum</th><th>Arbeit</th><th>Stunden</th><th>Status</th><th></th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td>{formatDate(item.datum)}</td><td>{item.art_der_arbeit}</td><td>{item.stunden}</td><td>{item.freigegeben ? "Freigegeben" : item.status === "abgelehnt" ? "Abgelehnt" : "Offen"}</td><td>{canEditOwn && isOwnOpenWorkHourEditable(item, memberId) && <button className="table-action" onClick={() => edit(item)}>Bearbeiten</button>}</td></tr>)}</tbody></table>{items.length === 0 && <p className="empty-state">Noch keine Arbeitsstunden vorhanden.</p>}</div><aside className="detail-panel"><h2>Prüfverlauf</h2>{history.length ? <ul className="review-history">{history.map((entry) => <li key={entry.id}><strong>{entry.aktion}</strong><span>{formatDate(entry.geprueft_am)} · Mitglied #{entry.geprueft_von}</span><p>{entry.begruendung}</p></li>)}</ul> : <p>Noch kein Prüfverlauf für die gewählte Saison vorhanden.</p>}</aside></div></section>;
}
