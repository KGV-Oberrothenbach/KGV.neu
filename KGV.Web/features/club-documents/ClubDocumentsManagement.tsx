"use client";

import { useCallback, useEffect, useState } from "react";
import { useSelectedClub } from "../auth/AuthProvider";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { ClubDocumentType, ClubDocumentVersion } from "../../models/club-documents/club-document";
import { getDocumentTypes, getVersions, uploadAndCreateVersion } from "../../repositories/club-documents/club-document-repository";
import { createClubDocumentStoragePath, getValidClubDocumentVersion } from "../../services/club-documents/club-document-version-service";

type Draft = { fassung: string; gueltigAb: string; file: File | null };
const emptyDraft = (): Draft => ({ fassung: "", gueltigAb: "", file: null });
const today = () => new Date().toISOString().slice(0, 10);

function formatDate(value: string | null) {
  if (!value) return "–";
  const date = new Date(`${value.slice(0, 10)}T00:00:00`);
  return Number.isNaN(date.getTime()) ? "–" : new Intl.DateTimeFormat("de-DE").format(date);
}

function errorMessage(error: unknown) {
  const text = error instanceof Error ? error.message : "";
  if (/pdf/i.test(text)) return "Bitte wähle eine PDF-Datei aus.";
  if (/gültig|fassung|chronolog|atomar/i.test(text)) return "Die Fassung kann wegen einer zeitlich ungültigen Nachfolgefassung nicht angelegt werden.";
  if (/network|fetch|netzwerk/i.test(text)) return "Netzwerkfehler: Bitte prüfe die Verbindung und versuche es erneut.";
  if (/berechtig|permission|401|403/i.test(text)) return "Für diese Aktion fehlt die erforderliche Berechtigung.";
  if (/speicher|upload|storage/i.test(text)) return "Die PDF konnte nicht gespeichert werden. Bitte versuche es erneut.";
  return "Die Fassung konnte nicht angelegt werden. Bitte versuche es erneut.";
}

export function ClubDocumentsManagement({ session }: { session: BrowserSession }) {
  const club = useSelectedClub();
  const [types, setTypes] = useState<ClubDocumentType[]>([]);
  const [versions, setVersions] = useState<Record<string, ClubDocumentVersion[]>>({});
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState("");
  const [editingType, setEditingType] = useState<ClubDocumentType | null>(null);
  const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [confirming, setConfirming] = useState(false);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const documentTypes = await getDocumentTypes(club, session);
      const documentVersions = await Promise.all(documentTypes.map(async (type) => [type.code, await getVersions(club, session, type.code)] as const));
      setTypes(documentTypes);
      setVersions(Object.fromEntries(documentVersions));
      setMessage("");
    } catch {
      setMessage("Vereinsunterlagen konnten nicht geladen werden. Bitte prüfe die Netzwerkverbindung und deine Berechtigung.");
    } finally { setLoading(false); }
  }, [club, session]);

  useEffect(() => { void load(); }, [load]);

  function openForm(type: ClubDocumentType) {
    setEditingType(type); setDraft(emptyDraft()); setConfirming(false); setMessage("");
  }
  function closeForm() {
    setEditingType(null); setDraft(emptyDraft()); setConfirming(false);
  }
  function validate() {
    if (!draft.fassung.trim()) return "Bitte gib eine Fassung oder Bezeichnung ein.";
    if (!/^\d{4}-\d{2}-\d{2}$/.test(draft.gueltigAb) || Number.isNaN(new Date(`${draft.gueltigAb}T00:00:00`).getTime())) return "Bitte gib ein gültiges Datum ein.";
    if (!draft.file) return "Bitte wähle eine Datei aus.";
    if (draft.file.type !== "application/pdf" && !/\.pdf$/i.test(draft.file.name)) return "Es sind ausschließlich PDF-Dateien zulässig.";
    return "";
  }
  function review() {
    const error = validate();
    if (error) { setMessage(error); return; }
    setMessage(""); setConfirming(true);
  }
  async function save() {
    if (!editingType || !draft.file) return;
    const error = validate();
    if (error) { setMessage(error); setConfirming(false); return; }
    setSaving(true); setMessage("");
    try {
      const fassung = draft.fassung.trim();
      await uploadAndCreateVersion(club, session, draft.file, {
        dokumenttyp_code: editingType.code,
        fassung,
        gueltig_ab: draft.gueltigAb,
        dateiname: draft.file.name,
        storage_path: createClubDocumentStoragePath(editingType.code, fassung, draft.file.name),
      });
      const updated = await getVersions(club, session, editingType.code);
      setVersions((current) => ({ ...current, [editingType.code]: updated }));
      closeForm();
      setMessage(`Die neue Fassung für „${editingType.bezeichnung}“ wurde erfolgreich angelegt.`);
    } catch (cause) { setMessage(errorMessage(cause)); }
    finally { setSaving(false); }
  }

  return <section className="data-workspace club-documents-workspace">
    <p className="document-intro">Hier verwaltest du die gültigen und historischen Fassungen der Vereinsunterlagen. Inhaltliche Änderungen werden immer als neue Fassung angelegt.</p>
    {message && <p className="notice" role="status">{message}</p>}
    {loading ? <p className="empty-state">Vereinsunterlagen werden geladen …</p> : <div className="club-document-list">
      {types.map((type) => {
        const items = versions[type.code] ?? [];
        let valid: ClubDocumentVersion | null = null;
        try { valid = getValidClubDocumentVersion(items, type.code, today()); } catch { /* Der Service schützt die Statusanzeige vor widersprüchlichen Daten. */ }
        return <section className="club-document-card" key={type.code}>
          <div className="data-toolbar"><div><h2>{type.bezeichnung}</h2><p>{type.pflicht ? "Pflichtunterlage" : "Vereinsunterlage"}</p></div><button onClick={() => openForm(type)}>Neue Fassung</button></div>
          {items.length === 0 ? <p className="empty-state">Keine Fassung vorhanden</p> : <div className="data-table-wrap"><table><thead><tr><th>Status</th><th>Fassung</th><th>Gültig ab</th><th>Gültig bis</th><th>Dateiname</th><th>Erstellt am</th></tr></thead><tbody>{items.map((item) => {
            const status = valid?.id === item.id ? "Aktuell" : item.gueltig_ab > today() ? "Zukünftig" : "Abgelaufen";
            return <tr key={item.id}><td><span className={`status-badge document-status ${status.toLowerCase()}`}>{status}</span></td><td><strong>{item.fassung}</strong></td><td>{formatDate(item.gueltig_ab)}</td><td>{formatDate(item.gueltig_bis)}</td><td>{item.dateiname ?? "–"}</td><td>{formatDate(item.erstellt_am)}</td></tr>;
          })}</tbody></table></div>}
        </section>;
      })}
    </div>}
    {editingType && <section className="detail-panel club-document-form" aria-label={`Neue Fassung ${editingType.bezeichnung}`}>
      <div className="detail-title"><div><h2>Neue Fassung: {editingType.bezeichnung}</h2><p>Der Dokumenttyp ist fest vorausgewählt.</p></div></div>
      {!confirming ? <><fieldset disabled={saving} className="admin-form-grid"><label>Dokumenttyp<input value={editingType.bezeichnung} readOnly /></label><label>Fassung / Bezeichnung<input value={draft.fassung} onChange={(event) => setDraft({ ...draft, fassung: event.target.value })} /></label><label>Gültig ab<input type="date" value={draft.gueltigAb} onChange={(event) => setDraft({ ...draft, gueltigAb: event.target.value })} /></label><label>PDF-Datei<input type="file" accept="application/pdf,.pdf" onChange={(event) => setDraft({ ...draft, file: event.target.files?.[0] ?? null })} /></label></fieldset><div className="editor-actions"><button disabled={saving} onClick={review}>Angaben prüfen</button><button className="secondary-action" disabled={saving} onClick={closeForm}>Abbrechen</button></div></> : <><h3>Bitte vor dem Speichern prüfen</h3><dl><div><dt>Dokumenttyp</dt><dd>{editingType.bezeichnung}</dd></div><div><dt>Fassung</dt><dd>{draft.fassung}</dd></div><div><dt>Gültig ab</dt><dd>{formatDate(draft.gueltigAb)}</dd></div><div><dt>Dateiname</dt><dd>{draft.file?.name}</dd></div></dl><div className="editor-actions"><button disabled={saving} onClick={save}>{saving ? "Speichert …" : "Endgültig speichern"}</button><button className="secondary-action" disabled={saving} onClick={() => setConfirming(false)}>Zurück</button></div></>}
    </section>}
  </section>;
}
