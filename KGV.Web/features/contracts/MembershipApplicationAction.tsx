"use client";

import { useEffect, useState } from "react";
import { useSelectedClub } from "../auth/AuthProvider";
import type { BrowserSession } from "../../lib/supabase-auth";
import { loadMembershipApplicationDocumentState, openMembershipApplicationDocument, type MembershipApplicationDocumentState } from "../../services/contracts/membership-application-service";
import { MembershipApplicationFlow } from "./MembershipApplicationFlow";

export function MembershipApplicationAction({ session, memberId, canCreate, onOpenCommunication }: { session: BrowserSession; memberId: number; canCreate: boolean; onOpenCommunication?: () => void }) {
  const club = useSelectedClub();
  const [state, setState] = useState<MembershipApplicationDocumentState | null>(null);
  const [creating, setCreating] = useState(false);
  const [opening, setOpening] = useState(false);
  const [message, setMessage] = useState("");
  const load = async () => { try { setState(await loadMembershipApplicationDocumentState(club, session, memberId)); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Mitgliedsantrag konnte nicht geprüft werden."); } };
  useEffect(() => { let active = true; void loadMembershipApplicationDocumentState(club, session, memberId).then((value) => { if (active) setState(value); }).catch((cause: unknown) => { if (active) setMessage(cause instanceof Error ? cause.message : "Mitgliedsantrag konnte nicht geprüft werden."); }); return () => { active = false; }; }, [club, session, memberId]);
  const openExisting = async () => { if (!state?.existingDocument) return; try { setOpening(true); setMessage(""); const url = await openMembershipApplicationDocument(club, session, state.existingDocument); if (!url) throw new Error("Mitgliedsantrag konnte nicht geöffnet werden."); window.open(url, "_blank", "noopener,noreferrer"); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Mitgliedsantrag konnte nicht geöffnet werden."); } finally { setOpening(false); } };
  if (!state) return <section className="parcel-section"><h3>Mitgliedsantrag</h3><p className="detail-hint">Mitgliedsantrag wird geprüft …</p>{message && <p className="notice">{message}</p>}</section>;
  if (state.status !== "none") return <section className="parcel-section"><h3>Mitgliedsantrag</h3><p className="detail-hint">{state.status === "signed" ? "Mitgliedsantrag vorhanden." : "Unsignierter Mitgliedsantrag vorhanden."}</p>{message && <p className="notice">{message}</p>}<button className="secondary-action" disabled={opening} onClick={() => void openExisting()}>{opening ? "Öffne …" : "Mitgliedsantrag öffnen"}</button></section>;
  return <section className="parcel-section"><h3>Mitgliedsantrag</h3>{message && <p className="notice">{message}</p>}{creating ? <MembershipApplicationFlow session={session} memberId={memberId} onSaved={async () => { await load(); setCreating(false); }} onPrepared={async () => { await load(); setCreating(false); window.dispatchEvent(new Event("kgv:open-communication")); onOpenCommunication?.(); }} /> : canCreate ? <button onClick={() => setCreating(true)}>Mitgliedsantrag erstellen</button> : <p className="detail-hint">Mit den aktuellen Rechten kann kein Mitgliedsantrag erstellt werden.</p>}</section>;
}
