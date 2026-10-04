"use client";

import { useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { useEditLock } from "../../lib/use-edit-lock";
import { type Member } from "../../models/members/member";
import { createNewSecondaryMember, loadSecondaryMember, type SecondaryMemberCreateInput, type SecondaryMemberPermissions, type SecondaryMemberUpdateInput, updateExistingSecondaryMember } from "../../services/members/secondary-member-service";

type Draft = SecondaryMemberCreateInput;
const today = () => new Date().toISOString().slice(0, 10);

export function SecondaryMember({ session, mainMember, canManageSecondary }: { session: BrowserSession; mainMember: Member; canManageSecondary: boolean }) {
  const [secondary, setSecondary] = useState<Member | null>(null);
  const [mode, setMode] = useState<"view" | "create" | "edit">("view");
  const [draft, setDraft] = useState<Draft | null>(null);
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const permissions: SecondaryMemberPermissions = { canManageSecondary };
  const editLock = useEditLock(session, "mitglied", secondary?.id, Boolean(mode === "edit" && secondary));

  async function load() {
    try { setSecondary(await loadSecondaryMember(session, mainMember.id)); }
    catch { setMessage("Nebenmitglied konnte nicht geladen werden."); }
  }
  useEffect(() => {
    let active = true;
    loadSecondaryMember(session, mainMember.id)
      .then((member) => { if (active) setSecondary(member); })
      .catch(() => { if (active) setMessage("Nebenmitglied konnte nicht geladen werden."); });
    return () => { active = false; };
  }, [session, mainMember.id]);

  function beginCreate() {
    setDraft({ vorname: "", name: mainMember.name ?? "", adresseUebernehmen: true, adresse: mainMember.adresse, plz: mainMember.plz, ort: mainMember.ort, telefon: null, handy: null, email: null, geburtsdatum: null, mitglied_seit: today(), whatsapp_einwilligung: false });
    setMode("create"); setMessage("");
  }
  function beginEdit() {
    if (!secondary) return;
    setDraft({ vorname: secondary.vorname, name: secondary.name, adresseUebernehmen: false, adresse: secondary.adresse, plz: secondary.plz, ort: secondary.ort, telefon: secondary.telefon, handy: secondary.handy, email: secondary.email, geburtsdatum: secondary.geburtsdatum, mitglied_seit: secondary.mitglied_seit, whatsapp_einwilligung: secondary.whatsapp_einwilligung });
    setMode("edit"); setMessage("");
  }
  function update<K extends keyof Draft>(key: K, value: Draft[K]) { setDraft((current) => current ? { ...current, [key]: value } : current); }
  async function save() {
    if (!draft) return;
    setSaving(true); setMessage("");
    try {
      if (mode === "create") await createNewSecondaryMember(session, mainMember.id, draft, permissions);
      else if (secondary) {
        const updateInput: SecondaryMemberUpdateInput = { adresse: draft.adresse, plz: draft.plz, ort: draft.ort, telefon: draft.telefon, handy: draft.handy, email: draft.email, geburtsdatum: draft.geburtsdatum, mitglied_seit: draft.mitglied_seit, whatsapp_einwilligung: draft.whatsapp_einwilligung };
        await updateExistingSecondaryMember(session, mainMember.id, secondary.id, updateInput, permissions);
      }
      await load(); setMode("view");
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Nebenmitglied konnte nicht gespeichert werden."); }
    finally { setSaving(false); }
  }

  if (mode === "view") return <section className="secondary-member"><h3>Nebenmitglied</h3>{message && <p className="notice">{message}</p>}{secondary ? <><p><strong>{secondary.vorname} {secondary.name}</strong><br />Mitglied #{secondary.id}{secondary.email ? ` · ${secondary.email}` : ""}</p>{canManageSecondary && <div className="editor-actions"><button onClick={beginEdit}>Nebenmitglied bearbeiten</button></div>}</> : <><p>Kein Nebenmitglied vorhanden</p>{canManageSecondary && <button onClick={beginCreate}>Nebenmitglied anlegen</button>}</>}</section>;
  const isCreate = mode === "create";
  return <section className="secondary-member"><h3>{isCreate ? "Nebenmitglied anlegen" : "Nebenmitglied bearbeiten"}</h3>{editLock.message && <p className="notice">{editLock.message}</p>}{message && <p className="notice">{message}</p>}<fieldset disabled={saving || (!isCreate && !editLock.acquired)}>{isCreate ? <><label>Vorname *<input value={draft?.vorname ?? ""} onChange={(event) => update("vorname", event.target.value)} /></label><label>Nachname *<input value={draft?.name ?? ""} onChange={(event) => update("name", event.target.value)} /></label><label className="check"><input type="checkbox" checked={Boolean(draft?.adresseUebernehmen)} onChange={(event) => { update("adresseUebernehmen", event.target.checked); if (event.target.checked) { update("adresse", mainMember.adresse); update("plz", mainMember.plz); update("ort", mainMember.ort); } }} /> Adresse des Hauptmitglieds übernehmen</label></> : <p><strong>{secondary?.vorname} {secondary?.name}</strong></p>}<label>E-Mail<input type="email" value={draft?.email ?? ""} readOnly={Boolean(!isCreate && secondary?.auth_user_id)} onChange={(event) => update("email", event.target.value)} /></label>{!isCreate && secondary?.auth_user_id && <p className="detail-hint">Die Mailadresse ist hier schreibgeschützt.</p>}<label>Geburtsdatum<input type="date" value={draft?.geburtsdatum ?? ""} onChange={(event) => update("geburtsdatum", event.target.value || null)} /></label><label className="wide">Straße / Hausnummer *<input value={draft?.adresse ?? ""} onChange={(event) => update("adresse", event.target.value)} /></label><label>PLZ *<input value={draft?.plz ?? ""} onChange={(event) => update("plz", event.target.value)} /></label><label>Ort *<input value={draft?.ort ?? ""} onChange={(event) => update("ort", event.target.value)} /></label><label>Telefon<input value={draft?.telefon ?? ""} onChange={(event) => update("telefon", event.target.value)} /></label><label>Mobilnummer<input value={draft?.handy ?? ""} onChange={(event) => update("handy", event.target.value)} /></label><label>Mitglied seit *<input type="date" value={draft?.mitglied_seit ?? ""} onChange={(event) => update("mitglied_seit", event.target.value || null)} /></label><label className="check"><input type="checkbox" checked={Boolean(draft?.whatsapp_einwilligung)} onChange={(event) => update("whatsapp_einwilligung", event.target.checked)} /> WhatsApp-Einwilligung</label></fieldset><div className="editor-actions"><button disabled={saving || (!isCreate && !editLock.acquired)} onClick={save}>{saving ? "Speichert …" : isCreate ? "Nebenmitglied anlegen" : editLock.checking ? "Sperre wird geprüft …" : "Speichern"}</button><button className="secondary-action" disabled={saving} onClick={() => { setMode("view"); setDraft(null); setMessage(""); }}>Abbrechen</button></div></section>;
}
