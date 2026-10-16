"use client";

import { useEffect, useState } from "react";
import { useSelectedClub } from "../auth/AuthProvider";
import type { BrowserSession } from "../../lib/supabase-auth";
import { loadLeaseContractDocumentState, openLeaseContractDocument, type LeaseContractDocumentState } from "../../services/contracts/lease-service";
import { LeaseContractFlow } from "./LeaseContractFlow";

export function LeaseContractAction({ session, memberId, parcelId, startDate, canCreate }: { session: BrowserSession; memberId: number; parcelId: number; startDate: string; canCreate: boolean }) {
  const club = useSelectedClub();
  const [state, setState] = useState<LeaseContractDocumentState | null>(null);
  const [creating, setCreating] = useState(false);
  const [opening, setOpening] = useState(false);
  const [message, setMessage] = useState("");
  const load = async () => { try { setState(await loadLeaseContractDocumentState(club, session, parcelId)); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Pachtvertrag konnte nicht geprüft werden."); } };
  useEffect(() => { let active = true; void loadLeaseContractDocumentState(club, session, parcelId).then((value) => { if (active) setState(value); }).catch((cause: unknown) => { if (active) setMessage(cause instanceof Error ? cause.message : "Pachtvertrag konnte nicht geprüft werden."); }); return () => { active = false; }; }, [club, session, parcelId]);
  const openExisting = async () => { if (!state?.existingDocument) return; try { setOpening(true); setMessage(""); const url = await openLeaseContractDocument(club, session, state.existingDocument); if (!url) throw new Error("Pachtvertrag konnte nicht geöffnet werden."); window.open(url, "_blank", "noopener,noreferrer"); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Pachtvertrag konnte nicht geöffnet werden."); } finally { setOpening(false); } };
  if (!state) return <section className="parcel-section"><h3>Pachtvertrag</h3><p className="detail-hint">Pachtvertrag wird geprüft …</p>{message && <p className="notice">{message}</p>}</section>;
  if (state.status !== "none") return <section className="parcel-section"><h3>Pachtvertrag</h3><p className="detail-hint">{state.status === "signed" ? "Pachtvertrag vorhanden." : "Unsignierter Pachtvertrag vorhanden."}</p>{message && <p className="notice">{message}</p>}<button className="secondary-action" disabled={opening} onClick={() => void openExisting()}>{opening ? "Öffne …" : "Pachtvertrag öffnen"}</button></section>;
  return <section className="parcel-section"><h3>Pachtvertrag</h3>{message && <p className="notice">{message}</p>}{creating ? <LeaseContractFlow session={session} memberId={memberId} initialParcelId={parcelId} initialStartDate={startDate} fixedParcel onSaved={async () => { await load(); setCreating(false); }} /> : canCreate ? <button onClick={() => setCreating(true)}>Pachtvertrag erstellen</button> : <p className="detail-hint">Mit den aktuellen Rechten kann kein Pachtvertrag erstellt werden.</p>}</section>;
}
