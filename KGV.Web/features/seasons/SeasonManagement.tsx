"use client";
import { useSelectedClub } from "../auth/AuthProvider";

import { useEffect, useState } from "react";
import { useEditLock } from "../../lib/use-edit-lock";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { Season } from "../../models/seasons/season";
import { canEditSeason, createNextSeasonProposal, getSeasons, saveSeason } from "../../services/seasons/season-service";

export default function SeasonManagement({ session, onSeasonSaved }: { session: BrowserSession; onSeasonSaved: (season: Season) => void }) {
  const club = useSelectedClub();
  const empty = createNextSeasonProposal([]);
  const [items, setItems] = useState<Season[]>([]);
  const [draft, setDraft] = useState<Season>(empty);
  const [isNew, setIsNew] = useState(false);
  const [editingSeasonId, setEditingSeasonId] = useState<number | null>(null);
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);

  async function load(preferredYear?: number) {
    try {
      const loaded = await getSeasons(club, session);
      setItems(loaded);
      const selected = loaded.find((item) => item.jahr === preferredYear) ?? loaded[0];
      if (selected) {
        setDraft(selected);
        setIsNew(false);
        setEditingSeasonId(selected.id);
      } else {
        setDraft(createNextSeasonProposal(loaded));
        setIsNew(true);
        setEditingSeasonId(null);
        setMessage("Neue Saison wurde auf Basis des Vorjahres vorgeschlagen.");
      }
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Saisons konnten nicht geladen werden.");
    }
  }

  useEffect(() => {
    let active = true;
    getSeasons(club, session).then((loaded) => {
      if (!active) return;
      setItems(loaded);
      const selected = loaded[0];
      if (selected) {
        setDraft(selected);
        setIsNew(false);
        setEditingSeasonId(selected.id);
      } else {
        setDraft(createNextSeasonProposal(loaded));
        setIsNew(true);
        setEditingSeasonId(null);
        setMessage("Neue Saison wurde auf Basis des Vorjahres vorgeschlagen.");
      }
    }).catch((cause: unknown) => {
      if (active) setMessage(cause instanceof Error ? cause.message : "Saisons konnten nicht geladen werden.");
    });
    return () => { active = false; };
  }, [session, club]);

  function suggest() {
    setDraft(createNextSeasonProposal(items));
    setIsNew(true);
    setEditingSeasonId(null);
    setMessage("Neue Saison wurde auf Basis des Vorjahres vorgeschlagen.");
  }

  function numberField(key: keyof Season, value: string, nullable = false) {
    setDraft({ ...draft, [key]: value === "" && nullable ? null : Number(value.replace(",", ".")) });
  }

  const editable = canEditSeason(draft);
  const editLock = useEditLock(club, session, "saison", draft.id, Boolean(!isNew && editable));

  useEffect(() => {
    if (!isNew && editLock.message) queueMicrotask(() => setMessage(editLock.message));
  }, [isNew, editLock.message]);

  async function save() {
    if (!editable) {
      setMessage("Vergangene Jahre können nicht bearbeitet werden.");
      return;
    }
    if (!isNew && !editLock.acquired) {
      setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft.");
      return;
    }
    setSaving(true);
    setMessage("");
    try {
      const result = await saveSeason(club, session, draft, isNew ? {} : { existingSeasonId: editingSeasonId ?? draft.id });
      await load(result.jahr);
      onSeasonSaved(result);
      setMessage(`Saison ${result.jahr} wurde gespeichert.`);
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Saison konnte nicht gespeichert werden.");
    } finally {
      setSaving(false);
    }
  }

  return <section className="data-workspace admin-workspace">{message && <p className="notice" role="status">{message}</p>}<div className="data-toolbar"><span>{items.length} Saisons</span><button onClick={suggest}>Neue Saison vorschlagen</button></div><div className="split-view"><div className="data-table-wrap"><table><thead><tr><th>Jahr</th><th>Pflichtstunden</th><th>Fehlstunde</th><th>Status</th></tr></thead><tbody>{items.map((item) => <tr key={item.id} className={!isNew && draft.id === item.id ? "selected-row" : ""} onClick={() => { setDraft(item); setIsNew(false); setEditingSeasonId(item.id); setMessage(""); }}><td><strong>{item.jahr}</strong></td><td>{item.pflichtstunden_soll}</td><td>{formatEuro(item.euro_pro_fehlstunde)}</td><td>{canEditSeason(item) ? "bearbeitbar" : "schreibgeschützt"}</td></tr>)}</tbody></table></div><aside className="detail-panel admin-form"><div className="detail-title"><div><h2>{isNew ? "Neue Saison" : `Saison ${draft.jahr}`}</h2><p>{editable ? "Preise und Pflichtstunden pflegen" : "Vergangene Saisons bleiben unverändert"}</p></div></div><fieldset disabled={!editable || saving}><label>Kalenderjahr<input type="number" min="1900" max="3000" value={draft.jahr} readOnly={!isNew} onChange={(event) => { const year = Number(event.target.value); setDraft({ ...draft, id: year, jahr: year }); }} /></label><label>Pacht je m² (€)<input inputMode="decimal" value={draft.pacht_pro_qm ?? ""} onChange={(event) => numberField("pacht_pro_qm", event.target.value, true)} /></label><label>Mitgliedsbeitrag (€)<input inputMode="decimal" value={draft.mitgliedsbeitrag ?? ""} onChange={(event) => numberField("mitgliedsbeitrag", event.target.value, true)} /></label><label>Beitrag Nebenmitglied (€)<input inputMode="decimal" value={draft.mitgliedsbeitrag_nebenmitglied ?? ""} onChange={(event) => numberField("mitgliedsbeitrag_nebenmitglied", event.target.value, true)} /></label><label>Aufnahmegebühr (€)<input inputMode="decimal" value={draft.aufnahmegebuehr ?? ""} onChange={(event) => numberField("aufnahmegebuehr", event.target.value, true)} /></label><label>Gebühr Bauantrag (€)<input inputMode="decimal" value={draft.gebuehr_bauantrag ?? ""} onChange={(event) => numberField("gebuehr_bauantrag", event.target.value, true)} /></label><label>Pflichtstunden Soll<input inputMode="decimal" value={draft.pflichtstunden_soll} onChange={(event) => numberField("pflichtstunden_soll", event.target.value)} /></label><label>Euro je Fehlstunde<input inputMode="decimal" value={draft.euro_pro_fehlstunde} onChange={(event) => numberField("euro_pro_fehlstunde", event.target.value)} /></label><label className="wide">Bemerkung<textarea value={draft.bemerkung ?? ""} onChange={(event) => setDraft({ ...draft, bemerkung: event.target.value })} /></label></fieldset><div className="editor-actions"><button disabled={!editable || saving} onClick={save}>{saving ? "Speichert …" : "Saison speichern"}</button></div></aside></div></section>;
}

function formatEuro(value: number | null | undefined) {
  return value == null ? "–" : new Intl.NumberFormat("de-DE", { style: "currency", currency: "EUR" }).format(value);
}
