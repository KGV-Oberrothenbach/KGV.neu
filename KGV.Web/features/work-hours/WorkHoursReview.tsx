"use client";
import { useSelectedClub } from "../auth/AuthProvider";

import { useCallback, useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { useEditLock } from "../../lib/use-edit-lock";
import { listMemberNames } from "../../repositories/members/member-repository";
import { loadWorkHourHistory, loadWorkHoursReview, reviewOpenWorkHour } from "../../services/work-hours/work-hours-service";
import { type WorkHour, type WorkHourHistory } from "./work-hours-types";

const formatDate = (value: string) => value ? new Intl.DateTimeFormat("de-DE").format(new Date(`${value.slice(0, 10)}T00:00:00`)) : "–";
type MemberLabel = { id: number; mitgliedsnummer: number; vorname: string | null; name: string | null };

export function WorkHoursReview({ session, saisonId, canManageWorkHours }: { session: BrowserSession; saisonId: number | null; canManageWorkHours: boolean }) {
  const club = useSelectedClub();
  const [items, setItems] = useState<WorkHour[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [comment, setComment] = useState("");
  const [hours, setHours] = useState("");
  const [date, setDate] = useState("");
  const [workType, setWorkType] = useState("");
  const [history, setHistory] = useState<WorkHourHistory[]>([]);
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);
  const [membersById, setMembersById] = useState<Map<number, MemberLabel>>(new Map());

  const loadMemberLabels = useCallback((ids: number[]) => {
    const uniqueIds = [...new Set(ids)];
    if (!uniqueIds.length) return;
    void listMemberNames(club, session, uniqueIds).then((members) => {
      setMembersById((current) => new Map([...current, ...members.map((member) => [member.id, member] as const)]));
    });
  }, [club, session]);

  const memberLabel = (id: number) => {
    const member = membersById.get(id);
    if (!member) return "Mgl.-Nr. –";
    const name = [member.vorname, member.name].filter(Boolean).join(" ");
    const number = `Mgl.-Nr. ${member.mitgliedsnummer}`;
    return name ? `${name} · ${number}` : number;
  };

  useEffect(() => {
    let active = true;
    const update = (items: WorkHour[]) => {
      if (!active) return;
      setSelectedId(null);
      setHistory([]);
      setItems(items);
      loadMemberLabels(items.map((item) => item.mitglied_id));
      setError("");
    };
    if (!saisonId) {
      void Promise.resolve().then(() => update([]));
    } else {
      void loadWorkHoursReview(club, session, saisonId).then(update).catch((cause: Error) => {
        if (active) setError(cause.message);
      });
    }
    return () => { active = false; };
  }, [saisonId, session, club, loadMemberLabels]);

  const open = items.filter((item) => !item.freigegeben && item.status !== "abgelehnt");
  const selected = open.find((item) => item.id === selectedId) ?? null;
  const editLock = useEditLock(club, session, "arbeitsstunde", selected?.id, Boolean(selected));

  useEffect(() => {
    if (!selected) return;
    let active = true;
    void Promise.resolve().then(() => {
      if (!active) return;
      setHours(String(selected.stunden));
      setDate(selected.datum?.slice(0, 10) ?? "");
      setWorkType(selected.art_der_arbeit);
    });
    void loadWorkHourHistory(club, session, selected.id).then((next) => { if (active) { setHistory(next); loadMemberLabels([selected.mitglied_id, ...next.map((entry) => entry.geprueft_von)]); } }).catch(() => { if (active) setHistory([]); });
    return () => { active = false; };
  }, [selected, session, club, loadMemberLabels]);
  useEffect(() => { if (editLock.message) queueMicrotask(() => setError(editLock.message)); }, [editLock.message]);

  async function reload() {
    if (!saisonId) { setItems([]); return; }
    try {
      const next = await loadWorkHoursReview(club, session, saisonId);
      setItems(next);
      loadMemberLabels(next.map((item) => item.mitglied_id));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Arbeitsstunden konnten nicht geladen werden.");
    }
  }

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
      await reviewOpenWorkHour(club, session, canManageWorkHours, selected, { action, comment, date, hours: correctedHours, workType });
      setComment("");
      setSelectedId(null);
      await reload();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Die Entscheidung konnte nicht gespeichert werden.");
    } finally {
      setSaving(false);
    }
  }

  return <section className="data-workspace"><div className="meter-summary"><div><span>Offen</span><strong>{open.length}</strong></div><div><span>Freigegeben</span><strong>{items.filter((item) => item.freigegeben).length}</strong></div><div><span>Stunden offen</span><strong>{open.reduce((sum, item) => sum + Number(item.stunden), 0)}</strong></div></div>{error && <p className="notice" role="alert">{error}</p>}<div className="split-view review-workspace"><div className="data-table-wrap"><table><thead><tr><th>Datum</th><th>Mitglied</th><th>Arbeit</th><th>Stunden</th></tr></thead><tbody>{open.map((item) => <tr key={item.id} className={selected?.id === item.id ? "selected-row" : ""} onClick={() => setSelectedId(item.id)}><td>{formatDate(item.datum)}</td><td>{memberLabel(item.mitglied_id)}</td><td>{item.art_der_arbeit}</td><td>{item.stunden}</td></tr>)}</tbody></table>{open.length === 0 && <p className="empty-state">Keine offenen Arbeitsstunden.</p>}</div><aside className="detail-panel review-panel"><h2>Prüfung</h2>{selected ? <><p><strong>{memberLabel(selected.mitglied_id)}</strong><br />{formatDate(selected.datum)} · {selected.stunden} Stunden</p><label>Prüfkommentar *<textarea value={comment} onChange={(event) => setComment(event.target.value)} placeholder="Begründung für die Entscheidung" /></label><details><summary>Korrekturwerte</summary><label>Datum<input type="date" value={date} disabled={selected.arbeitseinsatz_anmeldung_id != null} onChange={(event) => setDate(event.target.value)} /></label><label>Stunden<input inputMode="decimal" value={hours} onChange={(event) => setHours(event.target.value)} /></label><label>Art der Arbeit<input value={workType} onChange={(event) => setWorkType(event.target.value)} /></label></details><div className="review-actions"><button disabled={saving} onClick={() => decide("freigeben")}>Freigeben</button><button disabled={saving} className="secondary-action" onClick={() => decide("korrigieren")}>Korrigieren</button><button disabled={saving} className="reject-action" onClick={() => decide("ablehnen")}>Ablehnen</button><button disabled={saving} className="reject-action" onClick={() => decide("loeschen")}>Löschen</button></div><h3>Verlauf</h3>{history.length ? <ul className="review-history">{history.map((entry) => <li key={entry.id}><strong>{entry.aktion}</strong><span>{formatDate(entry.geprueft_am)} · {memberLabel(entry.geprueft_von)}</span><p>{entry.begruendung}</p></li>)}</ul> : <p>Noch kein Verlauf vorhanden.</p>}</> : <p>Wähle links eine offene Arbeitsstunde aus.</p>}</aside></div></section>;
}
