"use client";

import { useEffect, useState } from "react";
import { useSelectedClub } from "../auth/AuthProvider";
import type { BrowserSession } from "../../lib/supabase-auth";
import { loadLeaseContractDocumentState, openLeaseContractDocument, type LeaseContractDocumentState } from "../../services/contracts/lease-service";
import { LeaseContractFlow } from "./LeaseContractFlow";
import { listLeaseContractVersions, prepareLeaseContractReplacement, type LeaseContractVersionSummary } from "../../repositories/contracts/lease-relationship-repository";

export function LeaseContractAction({ session, memberId, parcelId, startDate, canCreate }: { session: BrowserSession; memberId: number; parcelId: number; startDate: string; canCreate: boolean }) {
  const club = useSelectedClub();
  const [state, setState] = useState<LeaseContractDocumentState | null>(null);
  const [creating, setCreating] = useState(false);
  const [opening, setOpening] = useState(false);
  const [message, setMessage] = useState("");
  const [versions, setVersions] = useState<LeaseContractVersionSummary[]>([]);
  const [preparedContractId, setPreparedContractId] = useState<number>();
  const load = async () => { try { const [documentState, contractVersions] = await Promise.all([loadLeaseContractDocumentState(club, session, parcelId), listLeaseContractVersions(club, session, parcelId)]); setState(documentState); setVersions(contractVersions); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Pachtvertrag konnte nicht geprüft werden."); } };
  useEffect(() => { let active = true; void Promise.all([loadLeaseContractDocumentState(club, session, parcelId), listLeaseContractVersions(club, session, parcelId)]).then(([value, contractVersions]) => { if (active) { setState(value); setVersions(contractVersions); } }).catch((cause: unknown) => { if (active) setMessage(cause instanceof Error ? cause.message : "Pachtvertrag konnte nicht geprüft werden."); }); return () => { active = false; }; }, [club, session, parcelId]);
  const openExisting = async () => { if (!state?.existingDocument) return; try { setOpening(true); setMessage(""); const url = await openLeaseContractDocument(club, session, state.existingDocument); if (!url) throw new Error("Pachtvertrag konnte nicht geöffnet werden."); window.open(url, "_blank", "noopener,noreferrer"); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Pachtvertrag konnte nicht geöffnet werden."); } finally { setOpening(false); } };
  if (!state) return <section className="parcel-section"><h3>Pachtvertrag</h3><p className="detail-hint">Pachtvertrag wird geprüft …</p>{message && <p className="notice">{message}</p>}</section>;
  const current = versions.find(version => version.massgeblichkeit === "massgeblich");
  const startReplacement = async () => { if (!current) return; try { setMessage(""); const id = await prepareLeaseContractReplacement(club, session, current.id); setPreparedContractId(id); setCreating(true); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Korrektur-/Ersatzvertrag konnte nicht gestartet werden."); } };
  return <section className="parcel-section"><h3>Pachtvertrag</h3>{state.status !== "none" && <><p className="detail-hint">Vorhandenes Pachtvertragsdokument.</p><button className="secondary-action" disabled={opening} onClick={() => void openExisting()}>{opening ? "Öffne …" : "Pachtvertrag öffnen"}</button></>}{versions.map(version => <p className="detail-hint" key={version.id}>{version.vertragsnummer ? `Vertrags-Nr. ${version.vertragsnummer}` : "Vertragsnummer noch nicht verfügbar"}{version.massgeblichkeit === "nicht_mehr_massgeblich" ? " · Nicht mehr maßgeblich" : ""}{version.vorgaenger_vertragsnummer ? ` · Ersetzt Vertrags-Nr. ${version.vorgaenger_vertragsnummer}` : ""}{version.nachfolger_vertragsnummer ? ` · Ersetzt durch Vertrags-Nr. ${version.nachfolger_vertragsnummer}` : ""}</p>)}{message && <p className="notice">{message}</p>}{creating ? <LeaseContractFlow session={session} memberId={memberId} parcelId={parcelId} startDate={startDate} preparedContractId={preparedContractId} onCancel={() => { setCreating(false); setPreparedContractId(undefined); }} onSaved={async () => { await load(); setCreating(false); setPreparedContractId(undefined); }} /> : canCreate ? current && state.status === "signed" ? <button onClick={() => void startReplacement()}>Korrektur-/Ersatzvertrag erstellen</button> : <button onClick={() => { setPreparedContractId(undefined); setCreating(true); }}>Pachtvertrag erstellen</button> : <p className="detail-hint">Mit den aktuellen Rechten kann kein Pachtvertrag erstellt werden.</p>}</section>;
}
