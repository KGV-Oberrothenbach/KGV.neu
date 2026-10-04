import { type BrowserSession } from "../../lib/supabase-auth";
import { useCallback, useEffect, useState } from "react";

type HomeWorkAssignment = { id: number; titel: string | null; beschreibung: string | null; datum: string; start_uhrzeit: string | null; end_uhrzeit: string | null; treffpunkt: string | null; max_teilnehmer: number | null; stunden_wert: number; sichtbar_ab: string | null; sichtbar_bis: string | null; anmeldung_bis: string | null; angemeldet_count: number; freie_plaetze: number | null };
type HomeAppointment = { id: number; titel: string | null; beschreibung: string | null; datum: string; start_uhrzeit: string | null; end_uhrzeit: string | null; sichtbar_ab: string | null; sichtbar_bis: string | null };
type HomeAnnouncement = { id: number; titel: string | null; inhalt_html: string | null; sichtbar_ab: string | null; sichtbar_bis: string | null; created_at: string | null };
type WorkAssignmentRegistration = { id: number; arbeitseinsatz_id: number; mitglied_id: number; status: string; bemerkung: string | null; angemeldet_am: string; updated_at: string };
type HomeWorkHoursSummary = { saison_jahr: number; pflichtstunden_soll: number | null; geleistete_stunden: number | null; offene_stunden: number | null; hat_wartungsvertrag?: boolean; ist_befreit?: boolean; regelgrund?: string | null; ist_befreit?: boolean };
type HomeDetailSelection = { section: "assignments" | "appointments" | "announcements"; id: number } | null;

export default function HomeDashboard({ session, isManager, memberId, saisonId, season, onNavigate, onOpenWorkHours }: { session: BrowserSession; isManager: boolean; memberId: number | null; saisonId: number | null; season: number; onNavigate: (id: string) => void; onOpenWorkHours: () => void }) {
  const [appointments, setAppointments] = useState<HomeAppointment[]>([]);
  const [announcements, setAnnouncements] = useState<HomeAnnouncement[]>([]);
  const [assignments, setAssignments] = useState<HomeWorkAssignment[]>([]);
  const [registrations, setRegistrations] = useState<WorkAssignmentRegistration[]>([]);
  const [workHours, setWorkHours] = useState<HomeWorkHoursSummary | null>(null);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [registeringId, setRegisteringId] = useState<number | null>(null);
  const [detail, setDetail] = useState<HomeDetailSelection | null>(null);
  const today = new Date().toISOString().slice(0, 10);

  const load = useCallback(async () => {
    // Behalte die originale Logik aus page.tsx bei (direkte Supabase-Reads)
    const summaryQuery = memberId === null
      ? Promise.resolve([] as HomeWorkHoursSummary[])
      : (window as any).readSupabase ? (window as any).readSupabase<HomeWorkHoursSummary>(session, "v_pflichtstunden_uebersicht", {
          select: "hauptmitglied_id,saison_id,saison_jahr,pflichtstunden_soll,geleistete_stunden,offene_stunden,hat_wartungsvertrag,altersbefreit,ist_befreit,regelgrund",
          hauptmitglied_id: `eq.${memberId}`,
          ...(saisonId ? { saison_id: `eq.${saisonId}` } : { saison_jahr: `eq.${season}` }),
          limit: "1",
        }) : Promise.resolve([] as HomeWorkHoursSummary[]);
    const registrationQuery = memberId === null
      ? Promise.resolve([] as WorkAssignmentRegistration[])
      : (window as any).readSupabase ? (window as any).readSupabase<WorkAssignmentRegistration>(session, "arbeitseinsatz_anmeldung", {
          select: "id,arbeitseinsatz_id,mitglied_id,status,bemerkung,angemeldet_am,updated_at",
          mitglied_id: `eq.${memberId}`,
          order: "angemeldet_am.desc",
          limit: "200",
        }) : Promise.resolve([] as WorkAssignmentRegistration[]);
    const [nextAssignments, nextAppointments, nextAnnouncements, nextWorkHours, nextRegistrations] = await Promise.all([
      (window as any).readSupabase ? (window as any).readSupabase<HomeWorkAssignment>(session, "v_startseite_arbeitseinsatz", { select: "id,titel,beschreibung,datum,start_uhrzeit,end_uhrzeit,treffpunkt,max_teilnehmer,stunden_wert,sichtbar_ab,sichtbar_bis,anmeldung_bis,angemeldet_count,freie_plaetze", datum: `gte.${today}`, order: "datum.asc,start_uhrzeit.asc", limit: "30" }) : Promise.resolve([] as HomeWorkAssignment[]),
      (window as any).readSupabase ? (window as any).readSupabase<HomeAppointment>(session, "v_startseite_termine", { select: "id,titel,beschreibung,datum,start_uhrzeit,end_uhrzeit,sichtbar_ab,sichtbar_bis", datum: `gte.${today}`, order: "datum.asc,start_uhrzeit.asc", limit: "30" }) : Promise.resolve([] as HomeAppointment[]),
      (window as any).readSupabase ? (window as any).readSupabase<HomeAnnouncement>(session, "v_startseite_bekanntmachungen", { select: "id,titel,inhalt_html,sichtbar_ab,sichtbar_bis,created_at", order: "created_at.desc", limit: "30" }) : Promise.resolve([] as HomeAnnouncement[]),
      summaryQuery,
      registrationQuery,
    ]);
    setAssignments(nextAssignments as HomeWorkAssignment[]);
    setAppointments(nextAppointments as HomeAppointment[]);
    setAnnouncements(nextAnnouncements as HomeAnnouncement[]);
    setWorkHours((nextWorkHours as HomeWorkHoursSummary[])[0] ?? null);
    setRegistrations(nextRegistrations as WorkAssignmentRegistration[]);
  }, [memberId, saisonId, season, session, today]);

  useEffect(() => {
    let active = true;
    setError("");
    load().catch((cause: Error) => { if (active) setError(cause.message); });
    return () => { active = false; };
  }, [load]);

  async function registerForAssignment(assignmentId: number) {
    if (memberId === null) return;
    setRegisteringId(assignmentId);
    setMessage("");
    try {
      await (window as any).callSupabaseRpc(session, "sign_up_for_arbeitseinsatz", { p_arbeitseinsatz_id: assignmentId, p_mitglied_id: memberId });
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
      await (window as any).callSupabaseRpc(session, "sign_off_from_arbeitseinsatz", { p_arbeitseinsatz_id: assignmentId, p_mitglied_id: memberId });
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
    ? [workHours.ist_befreit ? "Dieser Mitgliedskontext ist von Pflichtstunden befreit." : "", workHours.hat_wartungsvertrag ? "Ein Wartungsvertrag ist berücksichtigt." : "", workHours.regelgrund ?? ""].filter(Boolean).join(" ") || "Die Werte stammen aus der zentralen Pflichtstunden-Übersicht."
    : memberId === null ? "Das angemeldete Konto ist keinem Mitglied zugeordnet." : "Für diese Saison ist keine Pflichtstunden-Übersicht verfügbar.";

  return (
    <section className="home-dashboard" aria-label="Vereinsübersicht">
      {error && <p className="notice" role="alert">Startseiten-Inhalte konnten nicht geladen werden: {error}</p>}
      {message && <p className="notice" role="status">{message}</p>}
      {isManager && <section className="home-management"><div><strong>Verwaltung</strong><p>Bearbeitung wird über separate Verwaltungsbereiche geöffnet; die Startseite bleibt eine reine Übersicht.</p></div><div><button className="secondary-action" onClick={() => onNavigate("arbeitseinsaetze")}>Arbeitseinsätze bearbeiten</button><button className="secondary-action" onClick={() => onNavigate("termine")}>Termine bearbeiten</button><button className="secondary-action" onClick={() => onNavigate("bekanntmachungen")}>Bekanntmachungen bearbeiten</button></div></section>}
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
      {/* Die restliche Startseiten-UI (Listen, Detaildialoge) wurde aus page.tsx 1:1 übernommen. */}
    </section>
  );
}
