import { useCallback, useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { loadWorkAssignmentParticipants, manageParticipant, workAssignmentHasStarted } from "../../services/work-assignments/work-assignment-service";
import { type WorkAssignment, type WorkAssignmentMember, type WorkAssignmentRegistration } from "../../models/work-assignments/work-assignment";

export function WorkAssignmentParticipants({ session, assignment }: { session: BrowserSession; assignment: WorkAssignment }) {
  const [registrations, setRegistrations] = useState<WorkAssignmentRegistration[]>([]);
  const [members, setMembers] = useState<WorkAssignmentMember[]>([]);
  const [memberId, setMemberId] = useState("");
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const load = useCallback(() => loadWorkAssignmentParticipants(session, assignment.id).then(({ registrations: nextRegistrations, members: nextMembers }) => {
    setRegistrations(nextRegistrations);
    setMembers(nextMembers);
  }).catch((cause: Error) => setMessage(cause.message)), [session, assignment.id]);

  useEffect(() => { void load(); }, [load]);

  const activeRegistrations = registrations.filter((item) => item.status === "angemeldet");
  const capacity = assignment.max_teilnehmer ? Number(assignment.max_teilnehmer) : null;
  const started = workAssignmentHasStarted(assignment);
  const registrationBlocked = !assignment.aktiv || started || (capacity !== null && activeRegistrations.length >= capacity);

  async function registerMember(id: number, clearSelection = false) {
    if (!id) return;
    if (registrationBlocked) {
      setMessage(!assignment.aktiv ? "Der Arbeitseinsatz ist abgesagt." : started ? "Der Arbeitseinsatz hat bereits begonnen." : "Die Teilnehmerbegrenzung ist erreicht.");
      return;
    }
    setSaving(true);
    setMessage("");
    try {
      await manageParticipant(session, assignment.id, id, "anmelden");
      if (clearSelection) setMemberId("");
      await load();
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Anmeldung konnte nicht gespeichert werden.");
    } finally {
      setSaving(false);
    }
  }

  async function register() {
    const id = Number(memberId);
    if (!id) return;
    await registerMember(id, true);
  }

  async function setStatus(item: WorkAssignmentRegistration, action: "absagen" | "nicht_erschienen") {
    setSaving(true);
    setMessage("");
    try {
      await manageParticipant(session, assignment.id, item.mitglied_id, action);
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

  const labels: Record<WorkAssignmentRegistration["status"], string> = { angemeldet: "Angemeldet", abgesagt: "Abgesagt", nicht_erschienen: "Nicht erschienen", teilgenommen: "Teilgenommen" };
  const rank: Record<WorkAssignmentRegistration["status"], number> = { angemeldet: 0, teilgenommen: 1, nicht_erschienen: 2, abgesagt: 3 };
  return <section className="assignment-participants"><div className="detail-title"><div><h3>Teilnehmer</h3><p>{activeRegistrations.length}{capacity ? ` von ${capacity}` : " · unbegrenzt"}</p></div></div>{message && <p className="notice">{message}</p>}<div className="participant-add"><select value={memberId} disabled={registrationBlocked} onChange={(event) => setMemberId(event.target.value)}><option value="">Mitglied auswählen</option>{members.filter((member) => { const registration = registrations.find((item) => item.mitglied_id === member.id); return !registration || registration.status === "abgesagt"; }).map((member) => <option key={member.id} value={member.id}>{member.name}, {member.vorname}</option>)}</select><button disabled={saving || !memberId || registrationBlocked} onClick={() => void register()}>Anmelden</button></div><ul>{[...registrations].sort((a, b) => rank[a.status] - rank[b.status] || name(a.mitglied_id).localeCompare(name(b.mitglied_id), "de")).map((item) => <li key={item.id}><span><strong>{name(item.mitglied_id)}</strong><small>{labels[item.status]}</small></span><div>{item.status === "angemeldet" && !started && <button className="secondary-action" disabled={saving} onClick={() => void setStatus(item, "absagen")}>Abmelden</button>}{item.status === "angemeldet" && started && <button className="secondary-action" disabled={saving} onClick={() => void setStatus(item, "nicht_erschienen")}>Nicht erschienen</button>}{item.status === "abgesagt" && !registrationBlocked && <button className="secondary-action" disabled={saving} onClick={() => void registerMember(item.mitglied_id)}>Wieder anmelden</button>}{item.status === "nicht_erschienen" && <button className="secondary-action" disabled={saving} onClick={() => void setStatus(item, "absagen")}>Als abgesagt kennzeichnen</button>}</div></li>)}</ul>{registrations.length === 0 && <p>Noch keine Teilnehmer.</p>}</section>;
}
