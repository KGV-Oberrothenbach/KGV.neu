"use client";

import { useState } from "react";
import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { MaintenanceAssignment } from "../../models/maintenance/maintenance-assignment";
import type { MaintenanceContract } from "../../models/maintenance/maintenance-contract";
import { startMaintenanceMaterialChange } from "../../services/maintenance/maintenance-continuation-service";

const nextDate = () => {
  const date = new Date();
  date.setDate(date.getDate() + 1);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
};

export function MaintenanceMaterialChangePanel({ club, session, assignment, contract, onPrepared }: { club: ClubContext; session: BrowserSession; assignment: MaintenanceAssignment; contract: MaintenanceContract; onPrepared: (communicationId: number) => void }) {
  const [editing, setEditing] = useState(false);
  const [title, setTitle] = useState(contract.title);
  const [description, setDescription] = useState(contract.description ?? "");
  const [area, setArea] = useState(contract.area ?? "");
  const [maxAssignments, setMaxAssignments] = useState(String(contract.maxActiveAssignments));
  const [exempt, setExempt] = useState(contract.exemptsFromDutyHours);
  const [workHoursCredit, setWorkHoursCredit] = useState(String(contract.workHoursCredit));
  const [validFrom, setValidFrom] = useState(nextDate());
  const [note, setNote] = useState(contract.note ?? "");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);

  const submit = async () => {
    const max = Number(maxAssignments);
    const credit = Number(workHoursCredit);
    if (!title.trim() || !Number.isInteger(max) || max < 1 || !Number.isFinite(credit) || credit < 0 || !validFrom) {
      setMessage("Bitte Titel, gültiges Kontingent, Arbeitsstundenwirkung und Vertragsbeginn prüfen.");
      return;
    }
    setBusy(true);
    setMessage("");
    try {
      const rows = await startMaintenanceMaterialChange(club, session, {
        assignmentId: assignment.id,
        title: title.trim(),
        description: description.trim() || null,
        area: area.trim() || null,
        maxActiveAssignments: max,
        exemptsFromDutyHours: exempt,
        workHoursCredit: exempt ? 0 : credit,
        validFrom,
        note: note.trim() || null,
      });
      if (!rows[0]?.kommunikationsvorgang_id) throw new Error("Der neue Wartungsvertragsvorgang wurde nicht bestätigt.");
      onPrepared(rows[0].kommunikationsvorgang_id);
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : "Die grundlegende Änderung konnte nicht vorbereitet werden.");
    } finally {
      setBusy(false);
    }
  };

  return <section className="maintenance-assignment-form">
    {!editing ? <button className="secondary-action" onClick={() => setEditing(true)}>Neuen Wartungsvertrag wegen grundlegender Änderung erstellen</button> : <>
      <h4>Neuer Wartungsvertrag – grundlegende Änderung</h4>
      <p>Der bisherige Vertrag bleibt bis zum erfolgreichen Abschluss des neuen Prepared-Signaturvorgangs unverändert. Die neue 90xxx wird erst beim Signaturstart reserviert.</p>
      <label>Titel<input value={title} onChange={(event) => setTitle(event.target.value)} /></label>
      <label>Beschreibung<textarea value={description} onChange={(event) => setDescription(event.target.value)} /></label>
      <label>Bereich<input value={area} onChange={(event) => setArea(event.target.value)} /></label>
      <label>Maximale Zuordnungen<input type="number" min="1" step="1" value={maxAssignments} onChange={(event) => setMaxAssignments(event.target.value)} /></label>
      <label><input type="checkbox" checked={exempt} onChange={(event) => setExempt(event.target.checked)} /> Von Pflichtstunden befreit</label>
      {!exempt && <label>Arbeitsstunden-Gutschrift<input type="number" min="0" step="0.5" value={workHoursCredit} onChange={(event) => setWorkHoursCredit(event.target.value)} /></label>}
      <label>Neuer Vertragsbeginn<input type="date" min={nextDate()} value={validFrom} onChange={(event) => setValidFrom(event.target.value)} /></label>
      <label>Bemerkung (optional)<input value={note} onChange={(event) => setNote(event.target.value)} /></label>
      {message && <p className="notice" role="alert">{message}</p>}
      <button disabled={busy} onClick={() => void submit()}>Neuen Prepared-Vertrag vorbereiten</button>
      <button className="secondary-action" disabled={busy} onClick={() => setEditing(false)}>Abbrechen</button>
    </>}
  </section>;
}
