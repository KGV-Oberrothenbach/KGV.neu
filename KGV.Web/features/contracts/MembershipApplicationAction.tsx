"use client";

import { useEffect, useState } from "react";
import { useSelectedClub } from "../auth/AuthProvider";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { CommunicationItem } from "../../models/communication/communication-item";
import { findActivePreparedContract } from "../../repositories/communication/communication-repository";
import { membershipApplicationCommunicationLabel } from "../../services/contracts/membership-application-communication-status";
import { loadMembershipApplicationDocumentState, openMembershipApplicationDocument, type MembershipApplicationDocumentState } from "../../services/contracts/membership-application-service";
import { MembershipApplicationFlow } from "./MembershipApplicationFlow";
import { canStartMembershipApplication, type MembershipAdmissionStatus } from "../../services/contracts/membership-application-lifecycle";

export function MembershipApplicationAction({ session, memberId, admissionStatus, canCreate }: { session: BrowserSession; memberId: number; admissionStatus: MembershipAdmissionStatus; canCreate: boolean }) {
  const club = useSelectedClub();
  const [state, setState] = useState<MembershipApplicationDocumentState | null>(null);
  const [activeContract, setActiveContract] = useState<CommunicationItem | null>(null);
  const [creating, setCreating] = useState(false);
  const [opening, setOpening] = useState(false);
  const [message, setMessage] = useState("");
  const load = async () => { try { const [documentState, contract] = await Promise.all([loadMembershipApplicationDocumentState(club, session, memberId), findActivePreparedContract(club, session, "mitgliedsantrag", memberId, null)]); setState(documentState); setActiveContract(contract); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Mitgliedsantrag konnte nicht geprüft werden."); } };
  useEffect(() => { let active = true; void Promise.all([loadMembershipApplicationDocumentState(club, session, memberId), findActivePreparedContract(club, session, "mitgliedsantrag", memberId, null)]).then(([documentState, contract]) => { if (active) { setState(documentState); setActiveContract(contract); } }).catch((cause: unknown) => { if (active) setMessage(cause instanceof Error ? cause.message : "Mitgliedsantrag konnte nicht geprüft werden."); }); return () => { active = false; }; }, [club, session, memberId]);
  const openExisting = async () => { if (!state?.existingDocument) return; try { setOpening(true); setMessage(""); const url = await openMembershipApplicationDocument(club, session, state.existingDocument); if (!url) throw new Error("Mitgliedsantrag konnte nicht geöffnet werden."); window.open(url, "_blank", "noopener,noreferrer"); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Mitgliedsantrag konnte nicht geöffnet werden."); } finally { setOpening(false); } };
  const openCommunication = () => window.dispatchEvent(new CustomEvent("kgv:open-communication", { detail: { contractId: activeContract?.id } }));
  if (!state) return <section className="parcel-section"><h3>Mitgliedsantrag</h3><p className="detail-hint">Mitgliedsantrag wird geprüft …</p>{message && <p className="notice">{message}</p>}</section>;
  const activeLabel = membershipApplicationCommunicationLabel(activeContract?.status);
  if (activeLabel) return <section className="parcel-section"><h3>Mitgliedsantrag</h3><p className="detail-hint">{activeLabel}</p>{message && <p className="notice">{message}</p>}<button className="secondary-action" onClick={openCommunication}>In Vorgängen öffnen</button></section>;
  if (state.status !== "none" && !canStartMembershipApplication(admissionStatus, state.status)) return <section className="parcel-section"><h3>Mitgliedsantrag</h3><p className="detail-hint">{state.status === "signed" ? "Mitgliedsantrag vorhanden." : "Unsignierter Mitgliedsantrag vorhanden."}</p>{message && <p className="notice">{message}</p>}<button className="secondary-action" disabled={opening} onClick={() => void openExisting()}>{opening ? "Öffne …" : "Mitgliedsantrag öffnen"}</button></section>;
  return <section className="parcel-section"><h3>Mitgliedsantrag</h3>{message && <p className="notice">{message}</p>}{state.existingDocument && <><p className="detail-hint">Der frühere Mitgliedsantrag bleibt historisch erhalten.</p><button className="secondary-action" disabled={opening} onClick={() => void openExisting()}>{opening ? "Öffne …" : "Früheren Mitgliedsantrag öffnen"}</button></>}{creating ? <MembershipApplicationFlow session={session} memberId={memberId} onSaved={async () => { await load(); setCreating(false); }} onCancel={() => setCreating(false)} onPrepared={async () => { await load(); setCreating(false); window.dispatchEvent(new Event("kgv:open-communication")); }} /> : canCreate ? <button onClick={() => setCreating(true)}>{admissionStatus === "abgelehnt" ? "Neuen Mitgliedsantrag erstellen" : "Mitgliedsantrag erstellen"}</button> : <p className="detail-hint">Mit den aktuellen Rechten kann kein Mitgliedsantrag erstellt werden.</p>}</section>;
}
