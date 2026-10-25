"use client";

import { useCallback, useEffect, useState } from "react";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { DocumentType } from "../../models/documents/document-type";
import { documentTypeEffectLabels, documentTypeEffects, isDocumentTypeInUse, loadDocumentTypes, saveManagedDocumentType } from "../../services/documents/document-type-service";
import { useSelectedClub } from "../auth/AuthProvider";

type Draft = { id: number | null; bezeichnung: string; aktiv: boolean; sortierung: string; wirkungen: DocumentType["wirkungen"] };
const emptyDraft = (): Draft => ({ id: null, bezeichnung: "", aktiv: true, sortierung: "0", wirkungen: [] });

export function DocumentTypeManagement({ session, canManage }: { session: BrowserSession; canManage: boolean }) {
  const club = useSelectedClub();
  const [types, setTypes] = useState<DocumentType[]>([]);
  const [draft, setDraft] = useState<Draft | null>(null);
  const [inUse, setInUse] = useState(false);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setTypes(await loadDocumentTypes(club, session));
      setMessage("");
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Dokumenttypen konnten nicht geladen werden.");
    } finally {
      setLoading(false);
    }
  }, [club, session]);

  useEffect(() => {
    const timer = window.setTimeout(() => { void load(); }, 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  async function edit(type: DocumentType) {
    setDraft({ id: type.id, bezeichnung: type.bezeichnung, aktiv: type.aktiv, sortierung: String(type.sortierung), wirkungen: [...type.wirkungen] });
    setInUse(false);
    try {
      setInUse(await isDocumentTypeInUse(club, session, type.id));
    } catch {
      setMessage("Die Verwendung dieses Dokumenttyps konnte nicht geprüft werden.");
    }
  }

  function toggleEffect(effect: DocumentType["wirkungen"][number]) {
    if (!draft) return;
    setDraft({ ...draft, wirkungen: draft.wirkungen.includes(effect) ? draft.wirkungen.filter((item) => item !== effect) : [...draft.wirkungen, effect] });
  }

  async function save() {
    if (!draft) return;
    const sortierung = Number(draft.sortierung);
    if (!Number.isInteger(sortierung)) { setMessage("Die Sortierung muss eine ganze Zahl sein."); return; }
    setSaving(true);
    try {
      await saveManagedDocumentType(club, session, { ...draft, sortierung });
      setDraft(null);
      setInUse(false);
      await load();
      setMessage("Dokumenttyp gespeichert.");
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Der Dokumenttyp konnte nicht gespeichert werden.");
    } finally {
      setSaving(false);
    }
  }

  return <section className="data-workspace" aria-label="Dokumenttypen verwalten"><p className="document-intro">Dokumenttypen sind allgemeine Stammdaten. Ihre fachliche Wirkung wird ausschließlich über die zugeordneten Wirkungen gepflegt.</p>{!canManage && <p className="detail-hint">Dokumenttypen können vom Vorstand eingesehen, aber nur von Admins bearbeitet werden.</p>}{message && <p className="notice" role="status">{message}</p>}<div className="data-toolbar"><strong>{types.length} Dokumenttypen</strong><div className="editor-actions">{canManage && <button onClick={() => { setDraft(emptyDraft()); setInUse(false); }}>Neu</button>}<button className="secondary-action" disabled={loading || saving} onClick={() => void load()}>Aktualisieren</button></div></div>{loading ? <p className="empty-state">Dokumenttypen werden geladen …</p> : <div className="data-table-wrap"><table><thead><tr><th>Bezeichnung</th><th>Status</th><th>Sortierung</th><th>Wirkungen</th>{canManage && <th></th>}</tr></thead><tbody>{types.map((type) => <tr key={type.id}><td><strong>{type.bezeichnung}</strong></td><td>{type.aktiv ? "Aktiv" : "Inaktiv"}</td><td>{type.sortierung}</td><td>{type.wirkungen.length ? type.wirkungen.map((effect) => <small key={effect}>{documentTypeEffectLabels[effect]}</small>) : "Keine fachliche Wirkung"}</td>{canManage && <td><button className="secondary-action" onClick={() => void edit(type)}>Bearbeiten</button></td>}</tr>)}{types.length === 0 && <tr><td colSpan={canManage ? 5 : 4}>Keine Dokumenttypen vorhanden.</td></tr>}</tbody></table></div>}{draft && <section className="detail-panel"><h2>{draft.id === null ? "Neuer Dokumenttyp" : "Dokumenttyp bearbeiten"}</h2>{inUse && <p className="notice">Dieser Dokumenttyp wird bereits verwendet. Änderungen an den Wirkungen können bestehende Nachweisstatus verändern.</p>}<fieldset className="admin-form-grid" disabled={saving}><label>Bezeichnung<input value={draft.bezeichnung} onChange={(event) => setDraft({ ...draft, bezeichnung: event.target.value })} /></label><label>Sortierung<input type="number" step="1" value={draft.sortierung} onChange={(event) => setDraft({ ...draft, sortierung: event.target.value })} /></label><label>Aktiv<select value={String(draft.aktiv)} onChange={(event) => setDraft({ ...draft, aktiv: event.target.value === "true" })}><option value="true">Ja</option><option value="false">Nein</option></select></label><fieldset className="wide"><legend>Wirkungen</legend>{documentTypeEffects.map((effect) => <label className="check" key={effect}><input type="checkbox" checked={draft.wirkungen.includes(effect)} onChange={() => toggleEffect(effect)} /> {documentTypeEffectLabels[effect]}</label>)}</fieldset></fieldset><div className="editor-actions"><button disabled={saving || !draft.bezeichnung.trim() || !Number.isInteger(Number(draft.sortierung))} onClick={() => void save()}>{saving ? "Speichert …" : "Speichern"}</button><button className="secondary-action" disabled={saving} onClick={() => { setDraft(null); setInUse(false); }}>Abbrechen</button></div></section>}</section>;
}
