"use client";

import { useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { loadHomeAssignmentParticipants, type HomeAnnouncement, type HomeAppointment, type HomeDetailSelection, type HomeWorkAssignment, type WorkAssignmentRegistration } from "../../services/home/home-service";
import { berlinNow, workAssignmentHasStarted } from "../../services/work-assignments/work-assignment-service";
import { workAssignmentHasEnded } from "../../services/work-assignments/work-assignment-service";
import { loadLinkedWorkAssignmentWorkHour, submitLinkedWorkAssignmentWorkHour } from "../../services/work-hours/work-hours-service";
import { type WorkHour } from "../work-hours/work-hours-types";
import { SafeAnnouncementHtml } from "../announcements/SafeAnnouncementHtml";
import { downloadIcsCalendar } from "../../services/calendar/ics-calendar-service";

type HomeDetailProps = {
  session: BrowserSession;
  selection: HomeDetailSelection;
  assignments: HomeWorkAssignment[];
  appointments: HomeAppointment[];
  announcements: HomeAnnouncement[];
  registrations: WorkAssignmentRegistration[];
  memberId: number | null;
  canManageWorkAssignments: boolean;
  canManageAppointments: boolean;
  canManageAnnouncements: boolean;
  busy: boolean;
  onClose: () => void;
  onSelect: (selection: HomeDetailSelection) => void;
  onRegister: (assignmentId: number) => Promise<void>;
  onSignOff: (assignmentId: number) => Promise<void>;
  onNavigate: (target: string) => void;
};

function formatDate(value: string | null | undefined) {
  if (!value) return "–";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString("de-DE");
}

function formatTimeRange(start: string | null | undefined, end: string | null | undefined) {
  if (!start && !end) return "Ganztägig";
  if (!start) return `bis ${String(end).slice(0, 5)}`;
  if (!end) return `ab ${String(start).slice(0, 5)}`;
  return `${String(start).slice(0, 5)} – ${String(end).slice(0, 5)}`;
}


export default function HomeDetail({ session, selection, assignments, appointments, announcements, registrations, memberId, canManageWorkAssignments, canManageAppointments, canManageAnnouncements, busy, onClose, onSelect, onRegister, onSignOff, onNavigate }: HomeDetailProps) {
  const [participants, setParticipants] = useState<Array<WorkAssignmentRegistration & { displayName: string }>>([]);
  const [participantError, setParticipantError] = useState("");
  const [linkedWorkHour, setLinkedWorkHour] = useState<WorkHour | null>(null);
  const assignment = selection.kind === "assignment" ? assignments.find((item) => item.id === selection.id) ?? null : null;
  const appointment = selection.kind === "appointment" ? appointments.find((item) => item.id === selection.id) ?? null : null;
  const announcement = selection.kind === "announcement" ? announcements.find((item) => item.id === selection.id) ?? null : null;
  const items = selection.kind === "assignment" ? assignments : selection.kind === "appointment" ? appointments : announcements;
  const index = items.findIndex((item) => item.id === selection.id);
  const registered = assignment ? registrations.some((entry) => entry.arbeitseinsatz_id === assignment.id && entry.status === "angemeldet") : false;
  const registrationOpen = assignment && memberId !== null && !registered && assignment.freie_plaetze !== 0 && (!assignment.anmeldung_bis || assignment.anmeldung_bis >= berlinNow()) && !workAssignmentHasStarted(assignment);
  const canSignOff = assignment && registered && !workAssignmentHasStarted(assignment);
  const ownRegistration = assignment && memberId !== null ? registrations.find((entry) => entry.arbeitseinsatz_id === assignment.id && entry.mitglied_id === memberId) : undefined;
  const canSubmitWorkHour = Boolean(assignment?.aktiv && ownRegistration?.status === "angemeldet" && workAssignmentHasEnded(assignment) && !linkedWorkHour);

  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => { if (event.key === "Escape") onClose(); };
    window.addEventListener("keydown", closeOnEscape);
    return () => window.removeEventListener("keydown", closeOnEscape);
  }, [onClose]);

  useEffect(() => {
    let active = true;
    queueMicrotask(() => { setParticipants([]); setParticipantError(""); });
    if (!assignment || !canManageWorkAssignments) return () => { active = false; };
    loadHomeAssignmentParticipants(session, assignment.id).then((rows) => { if (active) setParticipants(rows); }).catch((cause: Error) => { if (active) setParticipantError(cause.message); });
    return () => { active = false; };
  // assignment id and session identify the remote participant source.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [assignment?.id, canManageWorkAssignments, registrations, session]);

  useEffect(() => {
    if (!ownRegistration) { queueMicrotask(() => setLinkedWorkHour(null)); return; }
    loadLinkedWorkAssignmentWorkHour(session, ownRegistration.id).then(setLinkedWorkHour).catch(() => setLinkedWorkHour(null));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ownRegistration?.id, session]);

  async function submitWorkHours() {
    if (!assignment || !ownRegistration) return;
    const hours = Number(window.prompt("Stunden", String(assignment.stunden_wert ?? 0)));
    if (!Number.isFinite(hours) || hours <= 0) return;
    const workType = window.prompt("Art der Arbeit", assignment.titel ?? "") ?? "";
    try { const row = await submitLinkedWorkAssignmentWorkHour(session, ownRegistration.id, hours, workType); setLinkedWorkHour(row); }
    catch (cause) { setParticipantError(cause instanceof Error ? cause.message : "Arbeitsstunden konnten nicht eingereicht werden."); }
  }

  function move(offset: number) {
    const next = items[index + offset];
    if (next) onSelect({ kind: selection.kind, id: next.id });
  }

  const sectionTitle = assignment ? "Arbeitseinsatz" : appointment ? "Termin" : "Bekanntmachung";
  const title = assignment?.titel ?? appointment?.titel ?? announcement?.titel ?? sectionTitle;
  const managementTarget = assignment ? "arbeitseinsaetze" : appointment ? "termine" : "bekanntmachungen";
  const canManageSelection = assignment ? canManageWorkAssignments : appointment ? canManageAppointments : canManageAnnouncements;

  return <div className="home-detail-overlay" role="presentation" onMouseDown={(event) => { if (event.currentTarget === event.target) onClose(); }}>
    <section className="home-detail-dialog" role="dialog" aria-modal="true" aria-labelledby="home-detail-title">
      <header><div><span>{sectionTitle}</span><h2 id="home-detail-title">{title}</h2></div><button className="secondary-action" onClick={onClose} aria-label="Detail schließen">Schließen</button></header>
      <div className="home-detail-body">
        {assignment && <>
          <div className="home-detail-facts"><div><span>Datum</span><strong>{formatDate(assignment.datum)}</strong></div><div><span>Zeit</span><strong>{formatTimeRange(assignment.start_uhrzeit, assignment.end_uhrzeit)}</strong></div><div><span>Treffpunkt</span><strong>{assignment.treffpunkt || "–"}</strong></div><div><span>Stundenwert</span><strong>{Number(assignment.stunden_wert || 0).toLocaleString("de-DE", { maximumFractionDigits: 2 })} h</strong></div><div><span>Teilnehmer</span><strong>{assignment.angemeldet_count}{assignment.max_teilnehmer ? ` von ${assignment.max_teilnehmer}` : ""}</strong></div><div><span>Anmeldung bis</span><strong>{formatDate(assignment.anmeldung_bis)}</strong></div></div>
          <div className="home-detail-content">{assignment.beschreibung || "Keine Beschreibung hinterlegt."}</div>
          <div className="home-detail-registration"><strong>{registered ? "Du bist angemeldet." : assignment.freie_plaetze === null ? "Anmeldung möglich." : `${assignment.freie_plaetze} freie Plätze.`}</strong>{registrationOpen && <button disabled={busy} onClick={() => onRegister(assignment.id)}>Anmelden</button>}{canSignOff && <button className="secondary-action" disabled={busy} onClick={() => onSignOff(assignment.id)}>Abmelden</button>}{canSubmitWorkHour && <button disabled={busy} onClick={() => void submitWorkHours()}>Arbeitsstunden erfassen</button>}{linkedWorkHour && <small>{linkedWorkHour.status === "offen" ? "Arbeitsstunden eingereicht – in Prüfung" : linkedWorkHour.status === "abgelehnt" ? "Arbeitsstunden abgelehnt" : "Arbeitsstunden bestätigt"}</small>}</div>
          {canManageWorkAssignments && <section className="home-participants"><div><h3>Angemeldete Teilnehmer</h3><button className="secondary-action" onClick={() => onNavigate("arbeitseinsaetze")}>Teilnehmer verwalten</button></div>{participantError && <p className="notice">{participantError}</p>}{participants.length ? <ul>{participants.map((participant) => <li key={participant.id}><strong>{participant.displayName}</strong><span>{participant.status}</span></li>)}</ul> : !participantError && <p>Aktuell keine angemeldeten Teilnehmer.</p>}</section>}
        </>}
        {appointment && <><div className="home-detail-facts"><div><span>Datum</span><strong>{formatDate(appointment.datum)}</strong></div><div><span>Beginn und Ende</span><strong>{formatTimeRange(appointment.start_uhrzeit, appointment.end_uhrzeit)}</strong></div><div><span>Sichtbar ab</span><strong>{formatDate(appointment.sichtbar_ab)}</strong></div><div><span>Sichtbar bis</span><strong>{formatDate(appointment.sichtbar_bis)}</strong></div></div><div className="home-detail-content">{appointment.beschreibung || "Keine Beschreibung hinterlegt."}</div><button onClick={() => downloadIcsCalendar({ uid: `termin-${appointment.id}@kgv-oberrothenbach`, title: appointment.titel ?? "Termin", description: appointment.beschreibung, date: appointment.datum, startTime: appointment.start_uhrzeit, endTime: appointment.end_uhrzeit })}>In Kalender übernehmen</button></>}
        {announcement && <><div className="home-detail-facts"><div><span>Sichtbar ab</span><strong>{formatDate(announcement.sichtbar_ab)}</strong></div><div><span>Sichtbar bis</span><strong>{formatDate(announcement.sichtbar_bis)}</strong></div>{announcement.created_at && <div><span>Veröffentlicht</span><strong>{formatDate(announcement.created_at)}</strong></div>}</div><div className="home-detail-content"><SafeAnnouncementHtml html={announcement.inhalt_html} /></div></>}
      </div>
      <footer><button className="secondary-action" disabled={index <= 0} onClick={() => move(-1)}>← Vorheriger Eintrag</button><span>{index >= 0 ? `${index + 1} von ${items.length}` : ""}</span><button className="secondary-action" disabled={index < 0 || index >= items.length - 1} onClick={() => move(1)}>Nächster Eintrag →</button>{canManageSelection && <button onClick={() => onNavigate(managementTarget)}>In Verwaltung öffnen</button>}</footer>
    </section>
  </div>;
}
