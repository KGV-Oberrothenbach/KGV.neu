"use client";

import { useCallback, useEffect, useState } from "react";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { DocumentType } from "../../models/documents/document-type";
import { documentTypeEffectLabels, documentTypeEffects, isDocumentTypeInUse, loadDocumentTypes, saveManagedDocumentType, saveManagedDocumentEffects } from "../../services/documents/document-type-service";
import { useSelectedClub } from "../auth/AuthProvider";

type Effects = DocumentType["wirkungen"];
type Draft = { id: number | null; bezeichnung: string; aktiv: boolean; sortierung: string; wirkungen: Effects };
const emptyDraft = (): Draft => ({ id: null, bezeichnung: "", aktiv: true, sortierung: "0", wirkungen: [] });
const sameEffects = (a: Effects, b: Effects) =>
  a.length === b.length && [...a].sort().every((effect, index) => effect === [...b].sort()[index]);

export function DocumentTypeManagement({ session, canManage, canManageEffects }: { session: BrowserSession; canManage: boolean; canManageEffects: boolean }) {
  const club = useSelectedClub();
  const [types, setTypes] = useState<DocumentType[]>([]);
  const [draft, setDraft] = useState<Draft | null>(null);
  const [originalEffects, setOriginalEffects] = useState<Effects>([]);
  // null means the authoritative usage check is not yet available: fail closed.
  const [inUse, setInUse] = useState<boolean | null>(null);
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
    if (!canManage && !canManageEffects) return;
    setDraft({ id: type.id, bezeichnung: type.bezeichnung, aktiv: type.aktiv, sortierung: String(type.sortierung), wirkungen: [...type.wirkungen] });
    setOriginalEffects([...type.wirkungen]);
    setInUse(null);
    try {
      const used = await isDocumentTypeInUse(club, session, type.id);
      if (typeof used !== "boolean") throw new Error("Ungültige Verwendungsauskunft");
      setInUse(used);
    } catch {
      setMessage("Die Verwendung dieses Dokumenttyps konnte nicht geprüft werden. Wirkungsänderungen sind gesperrt.");
    }
  }

  function toggleEffect(effect: Effects[number]) {
    if (!draft || !canManageEffects || inUse !== false) return;
    setDraft({ ...draft, wirkungen: draft.wirkungen.includes(effect) ? draft.wirkungen.filter((item) => item !== effect) : [...draft.wirkungen, effect] });
  }

  async function save() {
    if (!draft || (!canManage && !canManageEffects)) return;
    const sortierung = Number(draft.sortierung);
    if (canManage && !Number.isInteger(sortierung)) { setMessage("Die Sortierung muss eine ganze Zahl sein."); return; }
    const effectsChanged = !sameEffects(draft.wirkungen, originalEffects);
    if (effectsChanged && (!canManageEffects || inUse !== false)) {
      setMessage("Die Wirkungen können nicht bearbeitet werden; Verwendung prüfen oder Daten aktualisieren.");
      return;
    }
    setSaving(true);
    try {
      let typeId = draft.id;
      // No effect data is sent into the metadata RPC.
      if (canManage) typeId = await saveManagedDocumentType(club, session, { id: draft.id, bezeichnung: draft.bezeichnung, aktiv: draft.aktiv, sortierung });
      if (effectsChanged) {
        if (typeId === null) throw new Error("Dokumenttyp muss zunächst angelegt werden.");
        await saveManagedDocumentEffects(club, session, typeId, draft.wirkungen, originalEffects);
      }
      setDraft(null);
      setInUse(null);
      await load();
      setMessage("Dokumenttyp gespeichert.");
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Der Dokumenttyp konnte nicht gespeichert werden.");
    } finally {
      setSaving(false);
    }
  }

  const effectsLocked = !canManageEffects || inUse !== false;
  return <section className="data-workspace" aria-label="Dokumenttypen verwalten">
    <p className="document-intro">Dokumenttypen sind allgemeine Stammdaten. Ihre fachliche Wirkung wird ausschließlich über die zugeordneten Wirkungen gepflegt.</p>
    {!canManage && !canManageEffects && <p className="detail-hint">Typdaten und fachliche Wirkungen werden ausschließlich mit ihren jeweiligen Fachrechten bearbeitet.</p>}
    {message && <p className="notice" role="status">{message}</p>}
    <div className="data-toolbar"><strong>{types.length} Dokumenttypen</strong><div className="editor-actions">
      {canManage && <button onClick={() => { setDraft(emptyDraft()); setOriginalEffects([]); setInUse(false); }}>Neu</button>}
      <button className="secondary-action" disabled={loading || saving} onClick={() => void load()}>Aktualisieren</button>
    </div></div>
    {loading ? <p className="empty-state">Dokumenttypen werden geladen …</p> : <div className="data-table-wrap"><table><thead><tr><th>Bezeichnung</th><th>Status</th><th>Sortierung</th><th>Wirkungen</th>{(canManage || canManageEffects) && <th></th>}</tr></thead><tbody>
      {types.map((type) => <tr key={type.id}><td><strong>{type.bezeichnung}</strong></td><td>{type.aktiv ? "Aktiv" : "Inaktiv"}</td><td>{type.sortierung}</td><td>{type.wirkungen.length ? type.wirkungen.map((effect) => <small key={effect}>{documentTypeEffectLabels[effect]}</small>) : "Keine fachliche Wirkung"}</td>{(canManage || canManageEffects) && <td><button className="secondary-action" onClick={() => void edit(type)}>Bearbeiten</button></td>}</tr>)}
      {types.length === 0 && <tr><td colSpan={canManage || canManageEffects ? 5 : 4}>Keine Dokumenttypen vorhanden.</td></tr>}
    </tbody></table></div>}
    {draft && <section className="detail-panel"><h2>{draft.id === null ? "Neuer Dokumenttyp" : "Dokumenttyp bearbeiten"}</h2>
      {inUse === true && <p className="notice">Dieser Dokumenttyp wird bereits verwendet. Die Wirkungen sind historisch gesperrt; Typ-Metadaten dürfen separat geändert werden.</p>}
      {inUse === null && <p className="notice">Die serverseitige Verwendungsprüfung steht noch aus. Wirkungsänderungen sind gesperrt.</p>}
      <fieldset className="admin-form-grid" disabled={saving}>
        <label>Bezeichnung<input disabled={!canManage} value={draft.bezeichnung} onChange={(event) => setDraft({ ...draft, bezeichnung: event.target.value })} /></label>
        <label>Sortierung<input disabled={!canManage} type="number" step="1" value={draft.sortierung} onChange={(event) => setDraft({ ...draft, sortierung: event.target.value })} /></label>
        <label>Aktiv<select disabled={!canManage} value={String(draft.aktiv)} onChange={(event) => setDraft({ ...draft, aktiv: event.target.value === "true" })}><option value="true">Ja</option><option value="false">Nein</option></select></label>
        <fieldset className="wide" disabled={effectsLocked}><legend>Wirkungen</legend>
          {documentTypeEffects.map((effect) => <label className="check" key={effect}><input type="checkbox" checked={draft.wirkungen.includes(effect)} onChange={() => toggleEffect(effect)} /> {documentTypeEffectLabels[effect]}</label>)}
        </fieldset>
      </fieldset>
      <div className="editor-actions">
        <button disabled={saving || (!canManage && !canManageEffects) || (canManage && (!draft.bezeichnung.trim() || !Number.isInteger(Number(draft.sortierung)))) || (!canManage && sameEffects(draft.wirkungen, originalEffects))} onClick={() => void save()}>{saving ? "Speichert …" : "Speichern"}</button>
        <button className="secondary-action" disabled={saving} onClick={() => { setDraft(null); setInUse(null); }}>Abbrechen</button>
      </div>
    </section>}
  </section>;
}
