"use client";

import { useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { useEditLock } from "../../lib/use-edit-lock";
import { type Member } from "../../models/members/member";
import { type MemberCreatePermissions, type MemberEditPermissions, createMainMember, getMember, updateExistingMember } from "../../services/members/member-service";
import { SecondaryMember } from "./SecondaryMember";
import { MembershipEnd } from "./MembershipEnd";

export function MemberStammdaten({ session, memberId, canEdit, canCreate, canManageSecondary, editPermissions, createPermissions, onSaved, onCancel }: { session: BrowserSession; memberId: number | null; canEdit: boolean; canCreate: boolean; canManageSecondary: boolean; editPermissions: MemberEditPermissions; createPermissions: MemberCreatePermissions; onSaved: (member: Member) => void; onCancel: () => void }) {
  const [member, setMember] = useState<Member | null>(null);
  const [loading, setLoading] = useState(!canCreate);
  const [message, setMessage] = useState("");

  useEffect(() => {
    let active = true;
    if (canCreate || !memberId) {
      void Promise.resolve().then(() => { if (active) { setMember(null); setLoading(false); } });
      return () => { active = false; };
    }
    void Promise.resolve().then(() => { if (active) { setLoading(true); setMessage(""); } });
    getMember(session, memberId)
      .then((item) => { if (active) { setMember(item); if (!item) setMessage("Das ausgewählte Mitglied konnte nicht geladen werden."); } })
      .catch((cause: Error) => { if (active) setMessage(cause.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [session, memberId, canCreate]);

  if (loading) return <section className="data-workspace"><p>Lädt …</p></section>;
  if (message) return <section className="data-workspace"><p className="notice">{message}</p></section>;
  return <section className="data-workspace member-master-data"><MemberDetail session={session} member={member} canEdit={canEdit} canCreate={canCreate} canManageSecondary={canManageSecondary} editPermissions={editPermissions} createPermissions={createPermissions} onSaved={(saved) => { setMember(saved); onSaved(saved); }} onCancel={onCancel} /></section>;
}

function MemberDetail({ session, member, canEdit, canCreate, canManageSecondary, editPermissions, createPermissions, onSaved, onCancel }: { session: BrowserSession; member: Member | null; canEdit: boolean; canCreate: boolean; canManageSecondary: boolean; editPermissions: MemberEditPermissions; createPermissions: MemberCreatePermissions; onSaved: (member: Member) => void; onCancel: () => void }) {
  const [draft, setDraft] = useState<Partial<Member>>({}); const [editing, setEditing] = useState(canCreate); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const editLock = useEditLock(session, "mitglied", member?.id, Boolean(member && editing && canEdit && !canCreate));
  useEffect(() => { void Promise.resolve().then(() => { setDraft(member ?? { vorname: "", name: "", email: "", aktiv: true, whatsapp_einwilligung: false, email_rechnung_einwilligung: false, email_info_einwilligung: false, arbeitsstunden_altersregel_typ: null, mitglied_seit: new Date().toISOString().slice(0, 10), mitglied_ende: null }); setEditing(canCreate); setMessage(""); }); }, [member, canCreate]);
  const field = (key: keyof Member) => ({ value: String(draft[key] ?? ""), onChange: (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => setDraft({ ...draft, [key]: event.target.value }) });
  const emailReadOnly = !canCreate && (member?.auth_user_id != null || !editPermissions.canEditAllMembers);
  async function save() { const payload = { vorname: draft.vorname?.trim() || "", name: draft.name?.trim() || "", email: draft.email?.trim() || null, geburtsdatum: draft.geburtsdatum || null, arbeitsstunden_altersregel_typ: draft.arbeitsstunden_altersregel_typ || null, adresse: draft.adresse?.trim() || null, plz: draft.plz?.trim() || null, ort: draft.ort?.trim() || null, telefon: draft.telefon?.trim() || null, handy: draft.handy?.trim() || null, whatsapp_einwilligung: Boolean(draft.whatsapp_einwilligung), email_rechnung_einwilligung: Boolean(draft.email_rechnung_einwilligung), email_info_einwilligung: Boolean(draft.email_info_einwilligung), mitglied_seit: draft.mitglied_seit || null, bemerkung: draft.bemerkung?.trim() || null }; setSaving(true); setMessage(""); try { const saved = canCreate ? await createMainMember(session, payload, createPermissions) : await updateExistingMember(session, member!.id, payload, editPermissions); onSaved(saved); setMessage("Gespeichert."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Speichern nicht möglich."); } finally { setSaving(false); } }
  if (!member && !canCreate) return <aside className="detail-panel"><h2>Mitglied</h2><p>Wähle links ein Mitglied aus.</p></aside>;
  return <aside className="detail-panel member-editor"><div className="detail-title"><div><h2>{canCreate ? "Mitglied anlegen" : "Stammdaten"}</h2>{member && <p>Mitglied #{member.id}</p>}</div>{canEdit && !editing && <button onClick={() => setEditing(true)}>Bearbeiten</button>}</div>{editLock.message && <p className="notice" role="status">{editLock.message}</p>}{message && <p className="notice" role="status">{message}</p>}<fieldset disabled={!editing || saving || (!canCreate && !editLock.acquired)}><label>Vorname *<input {...field("vorname")} /></label><label>Nachname *<input {...field("name")} /></label><label>E-Mail<input type="email" {...field("email")} readOnly={emailReadOnly} /></label>{member?.auth_user_id && <p className="detail-hint">Die Mailadresse ist hier schreibgeschützt. Sie muss vom Nutzer selbst über den bestehenden Supabase-/OTP-Mailänderungsweg geändert werden.</p>}<label>Geburtsdatum<input type="date" {...field("geburtsdatum")} /></label>{(canCreate || member?.hauptmitglied_id === null) && <label>Arbeitsstunden-Altersregel<select value={draft.arbeitsstunden_altersregel_typ ?? ""} onChange={(event) => setDraft({ ...draft, arbeitsstunden_altersregel_typ: event.target.value })}><option value="">Bitte wählen</option><option value="mann80">mann80</option><option value="frau75">frau75</option></select></label>}<label className="wide">Straße / Hausnummer<input {...field("adresse")} /></label><label>PLZ<input {...field("plz")} /></label><label>Ort<input {...field("ort")} /></label><label>Telefon<input {...field("telefon")} /></label><label>Mobilnummer<input {...field("handy")} /></label><label>Mitglied seit<input type="date" {...field("mitglied_seit")} /></label><label className="check"><input type="checkbox" checked={Boolean(draft.whatsapp_einwilligung)} onChange={(event) => setDraft({ ...draft, whatsapp_einwilligung: event.target.checked })} /> WhatsApp-Einwilligung</label><label className="check"><input type="checkbox" checked={Boolean(draft.email_rechnung_einwilligung)} onChange={(event) => setDraft({ ...draft, email_rechnung_einwilligung: event.target.checked })} /> Rechnung per E-Mail</label><label className="check"><input type="checkbox" checked={Boolean(draft.email_info_einwilligung)} onChange={(event) => setDraft({ ...draft, email_info_einwilligung: event.target.checked })} /> Info per E-Mail</label><label className="wide">Bemerkung<textarea value={draft.bemerkung ?? ""} onChange={(event) => setDraft({ ...draft, bemerkung: event.target.value })} /></label></fieldset>{editing && <div className="editor-actions"><button onClick={save} disabled={saving || (!canCreate && !editLock.acquired)}>{saving ? "Speichert …" : editLock.checking ? "Sperre wird geprüft …" : "Speichern"}</button><button className="secondary-action" onClick={() => { if (canCreate) onCancel(); else { setDraft(member ?? {}); setEditing(false); } }} disabled={saving}>Abbrechen</button></div>}{member && !member.hauptmitglied_id && <><SecondaryMember session={session} mainMember={member} canManageSecondary={canManageSecondary} />{canManageSecondary && <MembershipEnd session={session} mainMember={member} canManageMembership={canManageSecondary} onSaved={onSaved} />}</>}<p className="detail-hint">Der Mitgliedsantrag wird nach Abschluss der Stammdaten im Dokumentbereich erzeugt und dort zur Unterschrift übergeben. Die Daten werden nur mit den Rechten des angemeldeten Vereinskontos gespeichert.</p></aside>;
}
