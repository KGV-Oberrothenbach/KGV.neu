"use client";

import { useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { useEditLock } from "../../lib/use-edit-lock";
import { loadWorkHourHistory, loadWorkHoursReview, reviewOpenWorkHour } from "../../services/work-hours/work-hours-service";
import { type WorkHour, type WorkHourHistory } from "./work-hours-types";

const formatDate = (value: string) => value ? new Intl.DateTimeFormat("de-DE").format(new Date(`${value.slice(0, 10)}T00:00:00`)) : "–";

export function WorkHoursReview({ session, canManageWorkHours }: { session: BrowserSession; canManageWorkHours: boolean }) {
  const [items, setItems] = useState<WorkHour[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [comment, setComment] = useState("");
  const [hours, setHours] = useState("");
  const [date, setDate] = useState("");
  const [workType, setWorkType] = useState("");
  const [history, setHistory] = useState<WorkHourHistory[]>([]);
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  const load = () => loadWorkHoursReview(session).then(setItems).catch((cause: Error) => setError(cause.message));
  useEffect(() => { void load(); }, [session]);

  const open = items.filter((item) => !item.freigegeben && item.status !== "abgelehnt");
  const selected = open.find((item) => item.id === selectedId) ?? null;
  const editLock = useEditLock(session, "arbeitsstunde", selected?.id, Boolean(selected));

  useEffect(() => {
    if (!selected) { setHistory([]); return; }
    setHours(String(selected.stunden));
    setDate(selected.datum?.slice(0, 10) ?? "");
    setWorkType(selected.art_der_arbeit);
    loadWorkHourHistory(session, selected.id).then(setHistory).catch(() => setHistory([]));
  }, [selectedId]);
  useEffect(() => { if (editLock.message) queueMicrotask(() => setError(editLock.message)); }, [editLock.message]);

  async function decide(action: "freigeben" | "ablehnen" | "korrigieren" | "loeschen") {
    if (!selected || !canManageWorkHours) { setError("Für die Prüfung fehlt ManageWorkHours."); return; }
    if (!editLock.acquired) { setError(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    if (!comment.trim()) { setError("Ein Prüfkommentar ist erforderlich."); return; }
    if (action === "loeschen" && !window.confirm("Diese offene Arbeitsstunde wirklich löschen? Der Prüfverlauf bleibt erhalten.")) return;
    const correctedHours = Number(hours.replace(",", "."));
    if (action === "korrigieren" && (!Number.isFinite(correctedHours) || correctedHours <= 0 || !workType.trim() || !date)) { setError("Für die Korrektur müssen Datum, Stunden und Art der Arbeit gültig sein."); return; }

    setSaving(true);
    setError("");
    try {
      await reviewOpenWorkHour(session, canManageWorkHours, selected, { action, comment, date, hours: correctedHours, workType });
      setComment("");
      setSelectedId(null);
      await load();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Die Entscheidung konnte nicht gespeichert werden.");
    } finally {
      setSaving(false);
    }
  }

  return <section className="data-workspace"><div className="meter-summary"><div><span>Offen</span><strong>{open.length}</strong></div><div><span>Freigegeben</span><strong>{items.filter((item) => item.freigegeben).length}</strong></div><div><span>Stunden offen</span><strong>{open.reduce((sum, item) => sum + Number(item.stunden), 0)}</strong></div></div>{error && <p className="notice" role="alert">{error}</p>}<div className="split-view review-workspace"><div className="data-table-wrap"><table><thead><tr><th>Datum</th><th>Mitglied</th><th>Arbeit</th><th>Stunden</th></tr></thead><tbody>{open.map((item) => <tr key={item.id} className={selected?.id === item.id ? "selected-row" : ""} onClick={() => setSelectedId(item.id)}><td>{formatDate(item.datum)}</td><td>#{item.mitglied_id}</td><td>{item.art_der_arbeit}</td><td>{item.stunden}</td></tr>)}</tbody></table>{open.length === 0 && <p className="empty-state">Keine offenen Arbeitsstunden.</p>}</div><aside className="detail-panel review-panel"><h2>Prüfung</h2>{selected ? <><p><strong>Mitglied #{selected.mitglied_id}</strong><br />{formatDate(selected.datum)} · {selected.stunden} Stunden</p><label>Prüfkommentar *<textarea value={comment} onChange={(event) => setComment(event.target.value)} placeholder="Begründung für die Entscheidung" /></label><details><summary>Korrekturwerte</summary><label>Datum<input type="date" value={date} onChange={(event) => setDate(event.target.value)} /></label><label>Stunden<input inputMode="decimal" value={hours} onChange={(event) => setHours(event.target.value)} /></label><label>Art der Arbeit<input value={workType} onChange={(event) => setWorkType(event.target.value)} /></label></details><div className="review-actions"><button disabled={saving} onClick={() => decide("freigeben")}>Freigeben</button><button disabled={saving} className="secondary-action" onClick={() => decide("korrigieren")}>Korrigieren</button><button disabled={saving} className="reject-action" onClick={() => decide("ablehnen")}>Ablehnen</button><button disabled={saving} className="reject-action" onClick={() => decide("loeschen")}>Löschen</button></div><h3>Verlauf</h3>{history.length ? <ul className="review-history">{history.map((entry) => <li key={entry.id}><strong>{entry.aktion}</strong><span>{formatDate(entry.geprueft_am)} · Mitglied #{entry.geprueft_von}</span><p>{entry.begruendung}</p></li>)}</ul> : <p>Noch kein Verlauf vorhanden.</p>}</> : <p>Wähle links eine offene Arbeitsstunde aus.</p>}</aside></div></section>;
}
