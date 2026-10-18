"use client";

import { useState } from "react";
import type { BrowserSession } from "../../lib/supabase-auth";
import { useSelectedClub } from "../auth/AuthProvider";
import { completePreparedContractFinalReview, openPreparedContractFinalDocument, type PreparedContractResumeContext } from "../../services/communication/prepared-contract-service";

export function PreparedFinalReview({ session, context, onCompleted }: { session: BrowserSession; context: PreparedContractResumeContext; onCompleted: () => Promise<void> }) {
  const club = useSelectedClub();
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const openDocument = async () => { try { setBusy(true); setMessage(""); const url = await openPreparedContractFinalDocument(club, session, context); window.open(url, "_blank", "noopener,noreferrer"); } catch (error) { setMessage(error instanceof Error ? error.message : "Das finale Dokument konnte nicht geöffnet werden."); } finally { setBusy(false); } };
  const complete = async () => { try { setBusy(true); setMessage(""); await completePreparedContractFinalReview(club, session, context, session.user.id); await onCompleted(); setMessage("Der Kommunikationsvorgang wurde abgeschlossen."); } catch (error) { setMessage(error instanceof Error ? error.message : "Der Kommunikationsvorgang konnte nicht abgeschlossen werden."); } finally { setBusy(false); } };
  return <section className="contract-composer"><h3>Letzte Prüfung</h3><p className="detail-hint">Das finale Dokument wurde erstellt. Bitte öffnen und prüfen Sie es vor dem Abschluss.</p>{message && <p className="notice" role="status">{message}</p>}<div className="editor-actions"><button type="button" className="secondary-action" disabled={busy} onClick={() => void openDocument()}>Finales Dokument öffnen</button><button type="button" disabled={busy} onClick={() => void complete()}>{busy ? "Verarbeitet …" : "Abschließen"}</button></div></section>;
}
