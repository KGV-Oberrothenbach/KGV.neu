import { useEffect, useState } from "react";
import { writeSupabase, type BrowserSession } from "../../lib/supabase-auth";
import { type WorkHour } from "../work-hours/work-hours-types";
import { loadWorkAssignmentParticipants } from "../../services/work-assignments/work-assignment-service";
import { type WorkAssignment, type WorkAssignmentMember, type WorkAssignmentRegistration } from "./work-assignment-types";

export function WorkAssignmentParticipants({ session, assignment, saisonId }: { session: BrowserSession; assignment: WorkAssignment; saisonId: number | null }) {
  const [registrations, setRegistrations] = useState<WorkAssignmentRegistration[]>([]);
  const [members, setMembers] = useState<WorkAssignmentMember[]>([]);
  const [memberId, setMemberId] = useState("");
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const load = () => loadWorkAssignmentParticipants(session, assignment.id).then(({ registrations: nextRegistrations, members: nextMembers }) => {
    setRegistrations(nextRegistrations);
    setMembers(nextMembers);
  }).catch((cause: Error) => setMessage(cause.message));

  useEffect(() => { void load(); }, [session, assignment.id]);

  const activeRegistrations = registrations.filter((item) => item.status === "angemeldet" || item.status === "teilgenommen");
  const capacity = assignment.max_teilnehmer ? Number(assignment.max_teilnehmer) : null;
  const deadlinePassed = Boolean(assignment.anmeldung_bis && assignment.anmeldung_bis.slice(0, 16) < currentLocalDateTime());
  const registrationBlocked = !assignment.aktiv || deadlinePassed || (capacity !== null && activeRegistrations.length >= capacity);

  async function register() {
    const id = Number(memberId);
    if (!id) return;
    if (registrationBlocked) {
      setMessage(!assignment.aktiv ? "Der Arbeitseinsatz ist abgesagt." : deadlinePassed ? "Der Anmeldeschluss ist abgelaufen." : "Die Teilnehmerbegrenzung ist erreicht.");
      return;
    }
    setSaving(true);
    setMessage("");
    try {
      const current = registrations.find((item) => item.mitglied_id === id);
      if (current) await writeSupabase<WorkAssignmentRegistration>(session, "arbeitseinsatz_anmeldung", "PATCH", { status: "angemeldet" }, { id: `eq.${current.id}` });
      else await writeSupabase<WorkAssignmentRegistration>(session, "arbeitseinsatz_anmeldung", "POST", { arbeitseinsatz_id: assignment.id, mitglied_id: id, status: "angemeldet" });
      setMemberId("");
      await load();
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Anmeldung konnte nicht gespeichert werden.");
    } finally {
      setSaving(false);
    }
  }

  async function setStatus(item: WorkAssignmentRegistration, status: WorkAssignmentRegistration["status"]) {
    setSaving(true);
    setMessage("");
    try {
      await writeSupabase<WorkAssignmentRegistration>(session, "arbeitseinsatz_anmeldung", "PATCH", { status }, { id: `eq.${item.id}` });
      if (status === "teilgenommen") {
        if (!saisonId) throw new Error("Für die Übernahme fehlt die aktive Saison.");
        await writeSupabase<WorkHour>(session, "arbeitsstunde", "POST", { mitglied_id: item.mitglied_id, saison_id: saisonId, datum: assignment.datum, stunden: assignment.stunden_wert, art_der_arbeit: assignment.titel ?? "Arbeitseinsatz", status: "offen", freigegeben: false });
      }
      await load();
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Teilnahme konnte nicht gespeichert werden.");
    } finally {
      setSaving(false);
    }
  }

  const name = (id: number) => {
    const member = members.find((candidate) => candidate.id === id);
    return member ? `${member.name ?? ""}, ${member.vorname ?? ""}` : `Mitglied #${id}`;
  };

  return <section className="assignment-participants"><div className="detail-title"><div><h3>Teilnehmer</h3><p>{activeRegistrations.length}{capacity ? ` von ${capacity}` : " · unbegrenzt"}</p></div></div>{message && <p className="notice">{message}</p>}{registrationBlocked && <p className="context-note">{!assignment.aktiv ? "Der Einsatz ist abgesagt; neue Anmeldungen sind gesperrt." : deadlinePassed ? "Der Anmeldeschluss ist abgelaufen." : "Die maximale Teilnehmerzahl ist erreicht."}</p>}<div className="participant-add"><select value={memberId} disabled={registrationBlocked} onChange={(event) => setMemberId(event.target.value)}><option value="">Mitglied auswählen</option>{members.filter((member) => !registrations.some((item) => item.mitglied_id === member.id && (item.status === "angemeldet" || item.status === "teilgenommen"))).map((member) => <option key={member.id} value={member.id}>{member.name}, {member.vorname}</option>)}</select><button disabled={saving || !memberId || registrationBlocked} onClick={() => void register()}>Anmelden</button></div><ul>{registrations.map((item) => <li key={item.id}><span><strong>{name(item.mitglied_id)}</strong><small>{item.status}</small></span><div>{item.status !== "abgesagt" && <button className="secondary-action" disabled={saving} onClick={() => void setStatus(item, "abgesagt")}>Abmelden</button>}<button disabled={saving || item.status === "teilgenommen" || item.status === "abgesagt"} onClick={() => void setStatus(item, "teilgenommen")}>Teilnahme übernehmen</button></div></li>)}</ul>{registrations.length === 0 && <p>Noch keine Teilnehmer.</p>}</section>;
}

function currentLocalDateTime() {
  const parts = new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Berlin", year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", hourCycle: "h23" }).formatToParts(new Date()).reduce<Record<string, string>>((result, part) => {
    result[part.type] = part.value;
    return result;
  }, {});
  return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}:${parts.minute}`;
}
