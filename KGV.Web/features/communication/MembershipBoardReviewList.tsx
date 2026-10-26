"use client";

import { useCallback, useEffect, useState } from "react";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { MembershipBoardAuthorizationSource, MembershipBoardReview, MembershipBoardVote } from "../../models/communication/membership-board-review";
import { canDecideMembershipBoardReview, decideMembershipBoardReview, ensureAdmissionConfirmationProcess, listMembershipBoardReviews, listMembershipBoardVotes, voteMembershipBoardReview } from "../../repositories/communication/membership-board-review-repository";
import { resolveActiveDocumentOpenUrl } from "../../services/documents/document-open-service";
import { generateContract } from "../../services/contracts/contract-generation-service";
import { useSelectedClub } from "../auth/AuthProvider";

const sourceText = (source: MembershipBoardAuthorizationSource) => source.berechtigungsart === "vertretung"
  ? `Vertretung für ${source.vertretene_funktionsbezeichnung_snapshot ?? source.funktionsbezeichnung_snapshot}`
  : `Direkt: ${source.funktionsbezeichnung_snapshot}`;

export function MembershipBoardReviewList({ session }: { session: BrowserSession }) {
  const club = useSelectedClub();
  const [rows, setRows] = useState<MembershipBoardReview[]>([]);
  const [selected, setSelected] = useState<MembershipBoardReview | null>(null);
  const [votes, setVotes] = useState<MembershipBoardVote[]>([]);
  const [canDecide, setCanDecide] = useState(false);
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try {
      const [items, decision] = await Promise.all([
        listMembershipBoardReviews(club, session),
        canDecideMembershipBoardReview(club, session),
      ]);
      setRows(items);
      setSelected(current => items.find(item => item.bearbeitung_id === current?.bearbeitung_id) ?? items[0] ?? null);
      setCanDecide(decision === true);
    } catch {
      setCanDecide(false);
      setMessage("Vorstandsbearbeitungen konnten nicht geladen werden.");
    }
  }, [club, session]);

  useEffect(() => {
    const timer = window.setTimeout(() => { void load(); }, 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  useEffect(() => {
    if (!selected) return;
    void listMembershipBoardVotes(club, session, selected.bearbeitung_id)
      .then(setVotes)
      .catch(() => setMessage("Stimmen konnten nicht geladen werden."));
  }, [club, session, selected]);

  const openDocument = async () => {
    if (!selected?.final_dokument_id) {
      setMessage("Das finale Mitgliedsantragsdokument ist nicht verfügbar.");
      return;
    }
    try {
      const url = await resolveActiveDocumentOpenUrl(club, session, selected.final_dokument_id);
      window.open(url, "_blank", "noopener,noreferrer");
    } catch {
      setMessage("Der Mitgliedsantrag kann mit Ihren aktuellen Berechtigungen nicht geöffnet werden.");
    }
  };

  const vote = async (value: "ja" | "nein") => {
    if (!selected) return;
    const reason = value === "nein" ? window.prompt("Persönliche Begründung (optional)") : null;
    try {
      await voteMembershipBoardReview(club, session, selected.bearbeitung_id, value, reason);
      await load();
    } catch {
      setMessage("Sie sind für diese Abstimmung nicht berechtigt oder die Bearbeitung ist abgeschlossen.");
    }
  };

  const decide = async (result: "annehmen" | "ablehnen") => {
    if (!selected || !canDecide) return;
    const path = selected.einstimmig_ja ? "einstimmig" : "vorstandssitzung";
    const date = path === "einstimmig" ? null : window.prompt("Sitzungsdatum (JJJJ-MM-TT)");
    const reason = result === "ablehnen" ? window.prompt("Abschlussbegründung") : null;
    const admission = result === "annehmen"
      ? window.prompt("Aufnahmedatum (JJJJ-MM-TT)", new Date().toISOString().slice(0, 10))
      : null;
    try {
      setBusy(true);
      setMessage("");
      await decideMembershipBoardReview(club, session, selected.bearbeitung_id, result, path, date, reason, admission);
    } catch {
      setMessage("Sie sind nicht entscheidungsberechtigt oder die Abschlussangaben sind unvollständig.");
      setBusy(false);
      return;
    }

    if (result === "annehmen") await createAdmissionConfirmation(selected);
    else {
      setMessage("Der Mitgliedsantrag wurde abgelehnt.");
      setBusy(false);
      await load();
    }
  };

  const createAdmissionConfirmation = async (review: MembershipBoardReview) => {
    try {
      setBusy(true);
      const process = await ensureAdmissionConfirmationProcess(club, session, review.bearbeitung_id);
      if (process.status === "erledigt") {
        setMessage("Das Mitglied wurde aufgenommen; die Aufnahmebestätigung liegt bereits vor.");
      } else {
        await generateContract(club, session, {
          action: "finalize",
          type: "aufnahmebestaetigung",
          member_id: review.mitglied_id,
          start_date: new Date().toISOString().slice(0, 10),
          kommunikationsvorgang_id: process.id,
          source_revision: process.revision,
        });
        setMessage("Das Mitglied wurde aufgenommen; die Aufnahmebestätigung wurde erstellt.");
      }
    } catch {
      setMessage("Das Mitglied wurde aufgenommen. Die Aufnahmebestätigung steht noch aus und kann erneut erstellt werden.");
    } finally {
      setBusy(false);
      await load();
    }
  };

  return <section className="data-workspace">
    <h2>Vorstand · Mitgliedsanträge</h2>
    {message && <p className="notice">{message}</p>}
    <div className="split-view">
      <div className="data-table-wrap"><table><thead><tr><th>Antragsteller</th><th>Stimmen</th><th>Status</th></tr></thead><tbody>
        {rows.map(row => <tr key={row.bearbeitung_id} onClick={() => setSelected(row)} className={selected?.bearbeitung_id === row.bearbeitung_id ? "selected-row" : ""}>
          <td>{row.antragsteller}</td><td>{row.abgegebene_stimmen} von {row.snapshot_personen} · Ja {row.ja_stimmen} · Nein {row.nein_stimmen}</td>
          <td>{row.entscheidung === "offen" ? row.einstimmig_ja ? "Einstimmige Zustimmung" : row.konflikt ? "Mindestens eine Gegenstimme" : "Entscheidung offen" : row.entscheidung === "angenommen" ? "Aufnahme bestätigt" : "Aufnahme abgelehnt"}</td>
        </tr>)}
      </tbody></table></div>
      {selected && <aside className="detail-panel">
        <h3>{selected.antragsteller}</h3>
        <p>{selected.einstimmig_ja ? "Einstimmige Zustimmung" : selected.konflikt ? "Mindestens eine Gegenstimme" : "Entscheidung offen"}</p>
        <button className="secondary-action" onClick={() => void openDocument()} disabled={!selected.final_dokument_id}>Mitgliedsantrag öffnen</button>
        {votes.map(voteRow => <section key={voteRow.person_id}>
          <p><strong>{voteRow.name_snapshot}</strong>: {voteRow.stimme ?? "Noch keine Stimme"}{voteRow.nein_begruendung ? ` – ${voteRow.nein_begruendung}` : ""}</p>
          {voteRow.berechtigungsquellen.map((source, index) => <small key={`${source.snapshot_person_id}-${index}`} className="detail-hint">{sourceText(source)}<br /></small>)}
        </section>)}
        {selected.entscheidung === "offen" && <div className="editor-actions">
          <button onClick={() => void vote("ja")} disabled={busy}>Zustimmen</button><button className="secondary-action" onClick={() => void vote("nein")} disabled={busy}>Ablehnen</button>
          {canDecide && <><button onClick={() => void decide("annehmen")} disabled={busy}>Aufnahme bestätigen</button><button className="secondary-action" onClick={() => void decide("ablehnen")} disabled={busy}>Antrag ablehnen</button></>}
        </div>}
        {selected.entscheidung === "angenommen" && <div className="editor-actions">
          <p>{selected.aufnahmebestaetigung_status === "erledigt" ? "Aufnahmebestätigung erstellt." : "Das Mitglied wurde aufgenommen. Die Aufnahmebestätigung steht noch aus."}</p>
          {selected.aufnahmebestaetigung_status !== "erledigt" && canDecide && <button onClick={() => void createAdmissionConfirmation(selected)} disabled={busy}>Aufnahmebestätigung erneut erstellen</button>}
        </div>}
      </aside>}
    </div>
  </section>;
}
