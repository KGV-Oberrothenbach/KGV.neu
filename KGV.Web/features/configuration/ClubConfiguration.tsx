"use client";
import { useSelectedClub } from "../auth/AuthProvider";

import { useEffect, useState } from "react";
import { type BrowserSession } from "../../lib/supabase-auth";
import { useEditLock } from "../../lib/use-edit-lock";
import { emptyClubConfiguration, type ClubConfigurationDraft } from "../../models/configuration/club-configuration";
import { loadClubConfiguration, saveClubConfiguration } from "../../services/configuration/club-configuration-service";

const fields: Array<[keyof ClubConfigurationDraft, string, string?]> = [["vereinsname", "Vereinsname"], ["kurzname", "Kurzname"], ["registerangabe", "Registerangabe"], ["strasse", "Straße"], ["plz", "PLZ"], ["ort", "Ort"], ["standard_email", "Standard-E-Mail", "email"], ["standard_telefon", "Standard-Telefon"], ["website", "Website", "url"], ["kontoinhaber", "Kontoinhaber"], ["bankname", "Bankname"], ["iban", "IBAN"], ["bic", "BIC"], ["verwendungszweck_mitgliedsantrag", "Verwendungszweck Mitgliedsantrag"], ["verwendungszweck_pachtvertrag", "Verwendungszweck Pachtvertrag"], ["dokument_ort", "Dokumentort"], ["datenschutz_version", "Datenschutz-Version"], ["datenschutz_stand", "Datenschutz-Stand", "date"]];

export function ClubConfiguration({ session, canManage }: { session: BrowserSession; canManage: boolean }) {
  const club = useSelectedClub();
  const [draft, setDraft] = useState<ClubConfigurationDraft>(emptyClubConfiguration);
  const [exists, setExists] = useState(false);
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const editLock = useEditLock(club, session, "vereinskonfiguration", draft.id ?? 0, Boolean(exists && draft.id));

  async function load() {
    try {
      const result = await loadClubConfiguration(club, session);
      setDraft(result.draft);
      setExists(result.exists);
      setMessage(result.exists ? "" : "Noch keine aktive Vereinskonfiguration vorhanden. Sie wird beim ersten Speichern angelegt.");
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Vereinskonfiguration konnte nicht geladen werden."); }
  }
  useEffect(() => { if (canManage) void load(); }, [session, canManage]);
  useEffect(() => { if (exists && editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [exists, editLock.message]);

  function update(key: keyof ClubConfigurationDraft, value: string) { setDraft((current) => ({ ...current, [key]: value })); }
  async function save() {
    if (!canManage) { setMessage("Für die Vereinskonfiguration fehlen die erforderlichen Rechte."); return; }
    if (exists && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    setSaving(true); setMessage("");
    try { const saved = await saveClubConfiguration(club, session, draft); setDraft({ ...saved }); setExists(true); setMessage("Vereinskonfiguration wurde gespeichert."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Vereinskonfiguration konnte nicht gespeichert werden."); }
    finally { setSaving(false); }
  }

  if (!canManage) return <section className="data-workspace club-configuration"><p className="notice" role="alert">Für die Vereinskonfiguration fehlen die erforderlichen Rechte.</p></section>;
  return <section className="data-workspace club-configuration"><p className="document-intro">Diese Angaben werden zentral für Verträge, Anschreiben und Dokumentmetadaten verwendet.</p>{message && <p className="notice" role="status">{message}</p>}<fieldset disabled={saving} className="admin-form-grid">{fields.map(([key, label, type]) => <label key={key}>{label}<input type={type ?? "text"} value={String(draft[key] ?? "")} onChange={(event) => update(key, event.target.value)} /></label>)}<label className="wide">Standard-Hinweistext<textarea value={draft.standard_hinweistext ?? ""} onChange={(event) => update("standard_hinweistext", event.target.value)} /></label><label className="wide">Datenschutztext<textarea value={draft.datenschutz_text ?? ""} onChange={(event) => update("datenschutz_text", event.target.value)} /></label></fieldset><div className="editor-actions"><button disabled={saving} onClick={save}>{saving ? "Speichert …" : "Vereinskonfiguration speichern"}</button></div></section>;
}
