"use client";

import { useEffect, useState } from "react";
import { type BrowserSession, readSupabase } from "../../lib/supabase-auth";
import { type HomeAnnouncement, type HomeAppointment, type HomeDetailSelection, type HomeWorkAssignment, type WorkAssignmentRegistration } from "../../services/home/home-service";

type HomeDetailProps = {
  session: BrowserSession;
  selection: HomeDetailSelection;
  assignments: HomeWorkAssignment[];
  appointments: HomeAppointment[];
  announcements: HomeAnnouncement[];
  registrations: WorkAssignmentRegistration[];
  memberId: number | null;
  isManager: boolean;
  busy: boolean;
  onClose: () => void;
  onSelect: (selection: HomeDetailSelection) => void;
  onRegister: (assignmentId: number) => Promise<void>;
  onSignOff: (assignmentId: number) => Promise<void>;
  onNavigate: (target: string) => void;
};

type ParticipantMember = Pick<Member, "id" | "vorname" | "name">;
type Member = { id: number; vorname: string | null; name: string | null };

function formatDate(value: string | null | undefined) {
  if (!value) return "–";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "–" : new Intl.DateTimeFormat("de-DE").format(date);
}

function formatTimeRange(start: string | null | undefined, end: string | null | undefined) {
  const short = (value: string | null | undefined) => value ? value.slice(0, 5) : "";
  const from = short(start);
  const to = short(end);
  if (from && to) return `${from}–${to} Uhr`;
  if (from) return `${from} Uhr`;
  if (to) return `bis ${to} Uhr`;
  return "–";
}

function plainText(value: string | null) {
  return (value ?? "").replace(/<[^>]*>/g, " ").replace(/\s+/g, " ").trim();
}

export default function HomeDetail({ session, selection, assignments, appointments, announcements, registrations, memberId, isManager, busy, onClose, onSelect, onRegister, onSignOff, onNavigate }: HomeDetailProps) {
  const [participants, setParticipants] = useState<Array<WorkAssignmentRegistration & { displayName: string }>>([]);
  const [participantError, setParticipantError] = useState("");
  const assignment = selection.kind === "assignment" ? assignments.find((item) => item.id === selection.id) ?? null : null;
  const appointment = selection.kind === "appointment" ? appointments.find((item) => item.id === selection.id) ?? null : null;
  const announcement = selection.kind === "announcement" ? announcements.find((item) => item.id === selection.id) ?? null : null;
  const items = selection.kind === "assignment" ? assignments : selection.kind === "appointment" ? appointments : announcements;
  const index = items.findIndex((item) => item.id === selection.id);
  const registered = assignment ? registrations.some((entry) => entry.arbeitseinsatz_id === assignment.id && entry.status === "angemeldet") : false;
  const registrationOpen = assignment && memberId !== null && !registered && assignment.freie_plaetze !== 0 && (!assignment.anmeldung_bis || assignment.anmeldung_bis >= new Date().toISOString());

  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => { if (event.key === "Escape") onClose(); };
    window.addEventListener("keydown", closeOnEscape);
    return () => window.removeEventListener("keydown", closeOnEscape);
  }, [onClose]);

  useEffect(() => {
    let active = true;
    setParticipants([]);
    setParticipantError("");
    if (!assignment || !isManager) return () => { active = false; };
    readSupabase<WorkAssignmentRegistration>(session, "arbeitseinsatz_anmeldung", {
      select: "id,arbeitseinsatz_id,mitglied_id,status,bemerkung,angemeldet_am,updated_at",
      arbeitseinsatz_id: `eq.${assignment.id}`,
      status: "eq.angemeldet",
      order: "angemeldet_am.asc",
      limit: "500",
    }).then(async (rows) => {
      const ids = [...new Set(rows.map((row) => row.mitglied_id))];
      const members = ids.length ? await readSupabase<ParticipantMember>(session, "mitglied", { select: "id,vorname,name", id: `in.(${ids.join(",")})`, limit: "500" }) : [];
      const names = new Map(members.map((item) => [item.id, [item.vorname, item.name].filter(Boolean).join(" ") || `Mitglied #${item.id}`]));
      if (active) setParticipants(rows.map((row) => ({ ...row, displayName: names.get(row.mitglied_id) ?? `Mitglied #${row.mitglied_id}` })));
    }).catch((cause: Error) => { if (active) setParticipantError(cause.message); });
    return () => { active = false; };
  }, [assignment?.id, isManager, registrations, session]);

  function move(offset: number) {
    const next = items[index + offset];
    if (next) onSelect({ kind: selection.kind, id: next.id });
  }

  const sectionTitle = assignment ? "Arbeitseinsatz" : appointment ? "Termin" : "Bekanntmachung";
  const title = assignment?.titel ?? appointment?.titel ?? announcement?.titel ?? sectionTitle;
  const managementTarget = assignment ? "arbeitseinsaetze" : appointment ? "termine" : "bekanntmachungen";

  return <div className="home-detail-overlay" role="presentation" onMouseDown={(event) => { if (event.currentTarget === event.target) onClose(); }}>
    <section className="home-detail-dialog" role="dialog" aria-modal="true" aria-labelledby="home-detail-title">
      <header><div><span>{sectionTitle}</span><h2 id="home-detail-title">{title}</h2></div><button className="secondary-action" onClick={onClose} aria-label="Detail schließen">Schließen</button></header>
      <div className="home-detail-body">
        {assignment && <>
          <div className="home-detail-facts"><div><span>Datum</span><strong>{formatDate(assignment.datum)}</strong></div><div><span>Zeit</span><strong>{formatTimeRange(assignment.start_uhrzeit, assignment.end_uhrzeit)}</strong></div><div><span>Treffpunkt</span><strong>{assignment.treffpunkt || "–"}</strong></div><div><span>Stundenwert</span><strong>{Number(assignment.stunden_wert || 0).toLocaleString("de-DE", { maximumFractionDigits: 2 })} h</strong></div><div><span>Teilnehmer</span><strong>{assignment.angemeldet_count}{assignment.max_teilnehmer ? ` von ${assignment.max_teilnehmer}` : ""}</strong></div><div><span>Anmeldung bis</span><strong>{formatDate(assignment.anmeldung_bis)}</strong></div></div>
          <div className="home-detail-content">{assignment.beschreibung || "Keine Beschreibung hinterlegt."}</div>
          <div className="home-detail-registration"><strong>{registered ? "Du bist angemeldet." : assignment.freie_plaetze === null ? "Anmeldung möglich." : `${assignment.freie_plaetze} freie Plätze.`}</strong>{registrationOpen && <button disabled={busy} onClick={() => onRegister(assignment.id)}>Anmelden</button>}{registered && <button className="secondary-action" disabled={busy} onClick={() => onSignOff(assignment.id)}>Abmelden</button>}</div>
          {isManager && <section className="home-participants"><div><h3>Angemeldete Teilnehmer</h3><button className="secondary-action" onClick={() => onNavigate("arbeitseinsaetze")}>Teilnehmer verwalten</button></div>{participantError && <p className="notice">{participantError}</p>}{participants.length ? <ul>{participants.map((participant) => <li key={participant.id}><strong>{participant.displayName}</strong><span>{participant.status}</span></li>)}</ul> : !participantError && <p>Aktuell keine angemeldeten Teilnehmer.</p>}</section>}
        </>}
        {appointment && <><div className="home-detail-facts"><div><span>Datum</span><strong>{formatDate(appointment.datum)}</strong></div><div><span>Beginn und Ende</span><strong>{formatTimeRange(appointment.start_uhrzeit, appointment.end_uhrzeit)}</strong></div><div><span>Sichtbar ab</span><strong>{formatDate(appointment.sichtbar_ab)}</strong></div><div><span>Sichtbar bis</span><strong>{formatDate(appointment.sichtbar_bis)}</strong></div></div><div className="home-detail-content">{appointment.beschreibung || "Keine Beschreibung hinterlegt."}</div></>}
        {announcement && <><div className="home-detail-facts"><div><span>Veröffentlicht</span><strong>{formatDate(announcement.sichtbar_ab ?? announcement.created_at)}</strong></div><div><span>Sichtbar bis</span><strong>{formatDate(announcement.sichtbar_bis)}</strong></div></div><div className="home-detail-content">{plainText(announcement.inhalt_html) || "Kein Inhalt hinterlegt."}</div></>}
      </div>
      <footer><button className="secondary-action" disabled={index <= 0} onClick={() => move(-1)}>← Vorheriger Eintrag</button><span>{index >= 0 ? `${index + 1} von ${items.length}` : ""}</span><button className="secondary-action" disabled={index < 0 || index >= items.length - 1} onClick={() => move(1)}>Nächster Eintrag →</button>{isManager && <button onClick={() => onNavigate(managementTarget)}>In Verwaltung öffnen</button>}</footer>
    </section>
  </div>;
}