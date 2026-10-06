"use client";

import { useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { useEditLock } from "../../lib/use-edit-lock";
import { type Member } from "../../models/members/member";
import { endMembership, loadSecondaryMemberForMembershipEnd } from "../../services/members/member-service";
import { type MembershipEndDecision } from "../../repositories/members/member-repository";

export function MembershipEnd({ session, mainMember, canManageMembership, onSaved }: { session: BrowserSession; mainMember: Member; canManageMembership: boolean; onSaved: (member: Member) => void }) {
  const [ending, setEnding] = useState(false);
  const [secondary, setSecondary] = useState<Member | null>(null);
  const [loadingSecondary, setLoadingSecondary] = useState(false);
  const [decision, setDecision] = useState<MembershipEndDecision | "">("");
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const editLock = useEditLock(session, "mitglied", mainMember.id, ending && canManageMembership);

  useEffect(() => {
    if (!ending) return;
    let active = true;
    loadSecondaryMemberForMembershipEnd(session, mainMember.id)
      .then((member) => { if (active) { setSecondary(member); setDecision(""); } })
      .catch((cause: Error) => { if (active) setMessage(cause.message || "Nebenmitglied konnte nicht geladen werden."); })
      .finally(() => { if (active) setLoadingSecondary(false); });
    return () => { active = false; };
  }, [ending, mainMember.id, session]);

  function cancel() {
    setEnding(false);
    setSecondary(null);
    setDecision("");
    setMessage("");
  }

  function beginMembershipEnd() {
    setLoadingSecondary(true);
    setMessage("");
    setSecondary(null);
    setDecision("");
    setEnding(true);
  }

  function confirmationText() {
    const mainName = [mainMember.vorname, mainMember.name].filter(Boolean).join(" ") || `Mitglied #${mainMember.id}`;
    const secondaryName = secondary ? ([secondary.vorname, secondary.name].filter(Boolean).join(" ") || `Mitglied #${secondary.id}`) : "";
    if (!secondary) return `Soll die Mitgliedschaft von ${mainName} zum heutigen Vereinsdatum beendet werden?`;
    if (decision === "end_secondary") return `Sollen die Mitgliedschaften von ${mainName} und ${secondaryName} zum heutigen Vereinsdatum beendet werden?`;
    return `Soll die Mitgliedschaft von ${mainName} beendet und ${secondaryName} als Hauptmitglied weitergeführt werden?`;
  }

  async function submit() {
    if (editLock.checking || !editLock.acquired) {
      setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft.");
      return;
    }
    const secondaryDecision: MembershipEndDecision | null = secondary && decision ? decision : null;
    if (loadingSecondary || (secondary && !secondaryDecision)) return;
    if (!window.confirm(confirmationText())) return;

    setSaving(true);
    setMessage("");
    try {
      const result = await endMembership(session, mainMember.id, secondaryDecision);
      onSaved(result.updated_main_member);
      setMessage(result.message);
      setEnding(false);
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Mitgliedschaft konnte nicht beendet werden.");
    } finally {
      setSaving(false);
    }
  }

  if (!ending) return <section className="secondary-member membership-end"><h3>Mitgliedschaft beenden</h3>{message && <p className="notice" role="status">{message}</p>}<div className="editor-actions"><button className="reject-action" disabled={!canManageMembership} onClick={beginMembershipEnd}>Mitgliedschaft beenden</button></div></section>;

  const lockReady = !editLock.checking && editLock.acquired;
  return <section className="secondary-member membership-end"><h3>Mitgliedschaft beenden</h3>{editLock.message && <p className="notice" role="status">{editLock.message}</p>}{message && <p className="notice" role="status">{message}</p>}{loadingSecondary ? <p>Lädt Nebenmitglied …</p> : secondary ? <><p>Nebenmitglied: <strong>{secondary.vorname} {secondary.name}</strong></p><label>Folgeentscheidung<select value={decision} disabled={saving || !lockReady} onChange={(event) => setDecision(event.target.value as MembershipEndDecision | "")}><option value="">Bitte wählen</option><option value="end_secondary">Nebenmitglied ebenfalls beenden</option><option value="promote_secondary">Nebenmitglied zum Hauptmitglied machen</option></select></label></> : <p>Kein Nebenmitglied vorhanden.</p>}<div className="editor-actions"><button className="reject-action" disabled={saving || loadingSecondary || !lockReady || Boolean(secondary && !decision)} onClick={submit}>{saving ? "Beendet …" : editLock.checking ? "Sperre wird geprüft …" : "Endgültig beenden"}</button><button className="secondary-action" disabled={saving} onClick={cancel}>Abbrechen</button></div></section>;
}
