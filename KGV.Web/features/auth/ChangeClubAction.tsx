"use client";

import { useState } from "react";

export function ChangeClubAction({ label, className, onConfirm, pendingPhotoCount = 0 }: { label: string; className?: string; onConfirm: () => Promise<void>; pendingPhotoCount?: number }) {
  const [confirming, setConfirming] = useState(false);
  const [changing, setChanging] = useState(false);

  async function confirm() {
    setChanging(true);
    try {
      await onConfirm();
    } finally {
      setChanging(false);
    }
  }

  return <><button className={className} type="button" disabled={changing} onClick={() => setConfirming(true)}>{label}</button>{confirming && <div className="modal-backdrop" role="presentation"><section className="home-detail-modal privacy-modal" role="dialog" aria-modal="true" aria-labelledby="change-club-title"><h2 id="change-club-title">Verein wechseln</h2><p>Du wirst abgemeldet. Die Vereinszuordnung und lokalen Anmeldedaten werden in diesem Browser gelöscht. Anschließend kannst du einen anderen Verein auswählen.</p>{pendingPhotoCount > 0 && <p className="notice">Es gibt noch {pendingPhotoCount} eigene lokale Foto-Upload{pendingPhotoCount === 1 ? "" : "s"}. Sie bleiben diesem Benutzer und Verein zugeordnet, werden nach dem Wechsel aber nicht automatisch hochgeladen.</p>}<div className="editor-actions"><button type="button" disabled={changing} onClick={confirm}>{changing ? "Wechsel läuft …" : "Abmelden und wechseln"}</button><button type="button" className="secondary-action" disabled={changing} onClick={() => setConfirming(false)}>Abbrechen</button></div></section></div>}</>;
}
