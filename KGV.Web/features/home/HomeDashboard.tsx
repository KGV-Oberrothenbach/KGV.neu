"use client";

import { type ReactNode, useCallback, useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import HomeDetail from "./HomeDetail";
import { workAssignmentHasStarted } from "../../services/work-assignments/work-assignment-service";
import {
  loadHomeDashboard,
  registerForHomeWorkAssignment,
  signOffFromHomeAssignment,
  type HomeAnnouncement,
  type HomeAppointment,
  type HomeDetailSelection,
  type HomeWorkAssignment,
  type HomeWorkHoursSummary,
  type WorkAssignmentRegistration,
} from "../../services/home/home-service";

type HomeDashboardProps = {
  session: BrowserSession;
  isManager: boolean;
  canManageWorkAssignments: boolean;
  canManageAppointments: boolean;
  canManageAnnouncements: boolean;
  memberId: number | null;
  saisonId: number | null;
  season: number;
  onNavigate: (id: string) => void;
  onOpenWorkHours: () => void;
};

export default function HomeDashboard({ session, isManager, canManageWorkAssignments, canManageAppointments, canManageAnnouncements, memberId, saisonId, season, onNavigate, onOpenWorkHours }: HomeDashboardProps) {
  const [appointments, setAppointments] = useState<HomeAppointment[]>([]);
  const [announcements, setAnnouncements] = useState<HomeAnnouncement[]>([]);
  const [assignments, setAssignments] = useState<HomeWorkAssignment[]>([]);
  const [registrations, setRegistrations] = useState<WorkAssignmentRegistration[]>([]);
  const [workHours, setWorkHours] = useState<HomeWorkHoursSummary | null>(null);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [registeringId, setRegisteringId] = useState<number | null>(null);
  const [detail, setDetail] = useState<HomeDetailSelection | null>(null);

  const load = useCallback(async () => {
    const next = await loadHomeDashboard(session, memberId, saisonId, season);
    setAssignments(next.assignments);
    setAppointments(next.appointments);
    setAnnouncements(next.announcements);
    setWorkHours(next.workHours);
    setRegistrations(next.registrations);
  }, [memberId, saisonId, season, session]);

  useEffect(() => {
    let active = true;
    const refresh = async () => {
      try {
        await load();
        if (active) setError("");
      } catch (cause) {
        if (active) setError(cause instanceof Error ? cause.message : "Startseiten-Inhalte konnten nicht geladen werden.");
      }
    };
    void refresh();
    return () => { active = false; };
  }, [load]);

  async function registerForAssignment(assignmentId: number) {
    if (memberId === null) return;
    setRegisteringId(assignmentId);
    setMessage("");
    try {
      await registerForHomeWorkAssignment(session, assignmentId, memberId);
      await load();
      setMessage("Du bist für den Arbeitseinsatz angemeldet.");
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Die Anmeldung konnte nicht gespeichert werden.");
    } finally {
      setRegisteringId(null);
    }
  }

  async function signOffFromAssignment(assignmentId: number) {
    if (memberId === null) return;
    setRegisteringId(assignmentId);
    setMessage("");
    try {
      await signOffFromHomeAssignment(session, assignmentId, memberId);
      await load();
      setMessage("Du wurdest vom Arbeitseinsatz abgemeldet.");
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Die Abmeldung konnte nicht gespeichert werden.");
    } finally {
      setRegisteringId(null);
    }
  }

  const formatHours = (value: number | null | undefined) => value === null || value === undefined ? "–" : `${Number(value).toLocaleString("de-DE", { maximumFractionDigits: 2 })} h`;
  const workHoursInfo = workHours
    ? [workHours.ist_befreit ? "Dieser Mitgliedskontext ist von Pflichtstunden befreit." : "", workHours.hat_wartungsvertrag ? "Ein Wartungsvertrag ist berücksichtigt." : "", !workHours.ist_befreit && workHours.wartungsvertrag_gutschrift_stunden > 0 ? `WV-Gutschrift: ${formatHours(workHours.wartungsvertrag_gutschrift_stunden)}.` : "", workHours.regelgrund ?? ""].filter(Boolean).join(" ") || "Die Werte stammen aus der zentralen Pflichtstunden-Übersicht."
    : memberId === null ? "Das angemeldete Konto ist keinem Mitglied zugeordnet." : "Für diese Saison ist keine Pflichtstunden-Übersicht verfügbar.";

  return <section className="home-dashboard" aria-label="Vereinsübersicht">
    {error && <p className="notice" role="alert">Startseiten-Inhalte konnten nicht geladen werden: {error}</p>}
    {message && <p className="notice" role="status">{message}</p>}
    {(isManager || canManageWorkAssignments || canManageAppointments || canManageAnnouncements) && <section className="home-management"><div><strong>Verwaltung</strong><p>Bearbeitung wird über separate Verwaltungsbereiche geöffnet; die Startseite bleibt eine reine Übersicht.</p></div><div>{canManageWorkAssignments && <button className="secondary-action" onClick={() => onNavigate("arbeitseinsaetze")}>Arbeitseinsätze bearbeiten</button>}{canManageAppointments && <button className="secondary-action" onClick={() => onNavigate("termine")}>Termine bearbeiten</button>}{canManageAnnouncements && <button className="secondary-action" onClick={() => onNavigate("bekanntmachungen")}>Bekanntmachungen bearbeiten</button>}</div></section>}
    <section className="home-work-hours" aria-labelledby="home-work-hours-title">
      <h2 id="home-work-hours-title">Meine Arbeitsstunden {workHours?.saison_jahr ?? season}</h2>
      <div className="home-work-hours-grid">
        <div><span>Sollstunden</span><strong>{formatHours(workHours?.pflichtstunden_soll)}</strong><small>{workHours?.regelgrund || "Sollstunden laut zentraler Pflichtstunden-Übersicht."}</small></div>
        <div><span>Geleistete Stunden</span><strong>{formatHours(workHours?.geleistete_stunden)}</strong><small>Bereits freigegebene Arbeitsstunden.</small></div>
        <div><span>Offene Stunden</span><strong>{formatHours(workHours?.offene_stunden)}</strong><small>Noch zu leistende Pflichtstunden.</small></div>
      </div>
      <p>{workHoursInfo}</p>
      {memberId !== null && <button onClick={onOpenWorkHours}>Arbeitsstunden erfassen</button>}
    </section>
    {detail && <HomeDetail session={session} selection={detail} assignments={assignments} appointments={appointments} announcements={announcements} registrations={registrations} memberId={memberId} canManageWorkAssignments={canManageWorkAssignments} canManageAppointments={canManageAppointments} canManageAnnouncements={canManageAnnouncements} busy={registeringId !== null} onClose={() => setDetail(null)} onSelect={setDetail} onRegister={registerForAssignment} onSignOff={signOffFromAssignment} onNavigate={(target) => { setDetail(null); onNavigate(target); }} />}
    <div className="home-content-grid">
      <HomeContentSection title="Arbeitseinsätze" empty="Aktuell liegen keine veröffentlichten Arbeitseinsätze vor.">{assignments.map((item) => {
        const registered = registrations.some((entry) => entry.arbeitseinsatz_id === item.id && entry.status === "angemeldet");
        const registrationOpen = memberId !== null && !registered && item.freie_plaetze !== 0 && (!item.anmeldung_bis || item.anmeldung_bis >= workAssignmentBerlinNow()) && !workAssignmentHasStarted(item);
        return <article key={item.id} className="home-item"><h2>{item.titel ?? "Arbeitseinsatz"}</h2><p>{item.beschreibung || "Keine Beschreibung hinterlegt."}</p><span>Einsatzdatum: {formatDate(item.datum)}</span>{(item.start_uhrzeit || item.end_uhrzeit) && <span>Uhrzeit: {formatTimeRange(item.start_uhrzeit, item.end_uhrzeit)}</span>}{item.treffpunkt && <span>Treffpunkt: {item.treffpunkt}</span>}<strong className="home-registration-state">{registered ? "Du bist angemeldet" : item.freie_plaetze === null ? "Anmeldung möglich" : `${item.freie_plaetze} freie Plätze`}</strong><div className="home-item-actions"><button className="secondary-action" onClick={() => setDetail({ kind: "assignment", id: item.id })}>Details</button>{registrationOpen && <button disabled={registeringId === item.id} onClick={() => registerForAssignment(item.id)}>{registeringId === item.id ? "Wird angemeldet …" : "Anmelden"}</button>}</div></article>;
      })}</HomeContentSection>
      <HomeContentSection title="Termine" empty="Aktuell liegen keine veröffentlichten Termine vor.">{appointments.map((item) => <article key={item.id} className="home-item"><h2>{item.titel ?? "Termin"}</h2><p>{item.beschreibung || "Keine Beschreibung hinterlegt."}</p><span>{formatDate(item.datum)}</span>{(item.start_uhrzeit || item.end_uhrzeit) && <span>Uhrzeit: {formatTimeRange(item.start_uhrzeit, item.end_uhrzeit)}</span>}<button className="secondary-action" onClick={() => setDetail({ kind: "appointment", id: item.id })}>Details</button></article>)}</HomeContentSection>
      <HomeContentSection title="Bekanntmachungen" empty="Aktuell liegen keine veröffentlichten Bekanntmachungen vor.">{announcements.map((item) => <article key={item.id} className="home-item"><h2>{item.titel ?? "Bekanntmachung"}</h2><p>{plainText(item.inhalt_html) || "Kein Inhalt hinterlegt."}</p><span>{item.sichtbar_ab || item.created_at ? formatDate(item.sichtbar_ab ?? item.created_at ?? "") : ""}</span><button className="secondary-action" onClick={() => setDetail({ kind: "announcement", id: item.id })}>Details</button></article>)}</HomeContentSection>
    </div>
  </section>;
}

function workAssignmentBerlinNow() { const parts = new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Berlin", year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", hourCycle: "h23" }).formatToParts(new Date()).reduce<Record<string, string>>((value, part) => { value[part.type] = part.value; return value; }, {}); return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}:${parts.minute}`; }

function HomeContentSection({ title, empty, children }: { title: string; empty: string; children: ReactNode }) {
  const entries = Array.isArray(children) ? children : [children];
  return <section className="home-content-section"><h2>{title}</h2><div className="home-item-list">{entries.length > 0 ? entries : <p className="empty-state">{empty}</p>}</div></section>;
}

function formatDate(value: string | null | undefined) {
  if (!value) return "–";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString("de-DE");
}

function formatTimeRange(start: string | null | undefined, end: string | null | undefined) {
  return [start, end].filter(Boolean).map((value) => String(value).slice(0, 5)).join(" – ");
}

function plainText(value: string | null) {
  return (value ?? "").replace(/<[^>]+>/g, " ").replace(/\s+/g, " ").trim();
}
