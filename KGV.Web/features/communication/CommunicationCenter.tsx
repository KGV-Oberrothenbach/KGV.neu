"use client";

import { useCallback, useEffect, useState } from "react";
import { useSelectedClub } from "../auth/AuthProvider";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { AssignmentFilter, CommunicationArea, CommunicationItem } from "../../models/communication/communication-item";
import { claimCommunicationItem, completePrintCommunicationItem, listCommunicationItems, releaseCommunicationItem } from "../../repositories/communication/communication-repository";
import { communicationActorLabel, communicationDocumentLabel, communicationMemberLabel, communicationParcelLabel, communicationQuery, communicationStatusLabel, communicationTypeLabel, isHistoryCommunicationItem } from "../../services/communication/communication-service";
import { loadPreparedContractForEditing, loadPreparedContractForFinalReview, loadPreparedContractForSignature } from "../../services/communication/prepared-contract-service";
import type { PreparedLeaseContractDraft, PreparedMembershipApplicationDraft } from "../../models/communication/prepared-contract-draft";
import { MembershipApplicationFlow } from "../contracts/MembershipApplicationFlow";
import { LeaseContractFlow } from "../contracts/LeaseContractFlow";

const tabs: Array<[CommunicationArea, string]> = [["signature", "Signaturen"], ["mail", "Mailausgang"], ["sent", "Versendet"], ["print", "Druckausgabe"], ["history", "Verlauf"]];
const assignments: Array<[AssignmentFilter, string]> = [["unassigned", "Unzugeordnet"], ["mine", "Meine"], ["all", "Alle"]];
type ResumeMode = "edit" | "signatures" | "review";
type Opened = { id: number; documentType: "mitgliedsantrag" | "pachtvertrag"; memberId: number; parcelId: number | null; startDate: string; resumeMode: ResumeMode };

export function CommunicationCenter({ session, userId, openCounts, onChanged }: { session: BrowserSession; userId: string; openCounts?: { signature: number; mail: number; print: number }; onChanged?: () => void }) {
  const club = useSelectedClub();
  const [area, setArea] = useState<CommunicationArea>("signature");
  const [assignment, setAssignment] = useState<AssignmentFilter>("mine");
  const [items, setItems] = useState<CommunicationItem[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<number | null>(null);
  const [message, setMessage] = useState("");
  const [opened, setOpened] = useState<Opened | null>(null);
  const [showAllHistory, setShowAllHistory] = useState(false);
  const load = useCallback(async () => {
    setLoading(true); setMessage("");
    try {
      const historyPeriod: Record<string, string> = area === "history" && !showAllHistory ? { bearbeitet_am: `gte.${new Date(Date.now() - 90 * 24 * 60 * 60 * 1000).toISOString()}` } : {};
      const rows = await listCommunicationItems(club, session, { ...communicationQuery(area, assignment, userId), ...historyPeriod });
      const visibleRows = area === "history" ? rows.filter(isHistoryCommunicationItem) : rows;
      setItems(visibleRows);
      setSelectedId(current => visibleRows.some(row => row.id === current) ? current : visibleRows[0]?.id ?? null);
    } catch (cause) {
      setItems([]); setSelectedId(null); setMessage(cause instanceof Error ? cause.message : "Die Vorgänge konnten nicht geladen werden.");
    } finally { setLoading(false); }
  }, [area, assignment, club, session, showAllHistory, userId]);
  useEffect(() => { void load(); }, [load]);
  const selected = items.find(item => item.id === selectedId) ?? null;
  async function claim(item: CommunicationItem) { setBusy(item.id); setMessage(""); try { await claimCommunicationItem(club, session, item.id, userId); await load(); onChanged?.(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Übernehmen fehlgeschlagen."); } finally { setBusy(null); } }
  async function release(item: CommunicationItem) { setBusy(item.id); setMessage(""); try { await releaseCommunicationItem(club, session, item.id, userId); await load(); onChanged?.(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Freigeben fehlgeschlagen."); } finally { setBusy(null); } }
  async function completePrint(item: CommunicationItem) { setBusy(item.id); setMessage(""); try { await completePrintCommunicationItem(club, session, item.id, userId); await load(); onChanged?.(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Druckabschluss fehlgeschlagen."); } finally { setBusy(null); } }
  async function open(item: CommunicationItem, resumeMode: ResumeMode = "edit") {
    if ((!isPreparedContract(item) && !isStartedContract(item) && !isFinalReviewContract(item)) || item.zugewiesen_an && item.zugewiesen_an !== userId) return;
    setBusy(item.id); setMessage("");
    try {
      if (resumeMode === "edit" && item.zugewiesen_an === null) await claimCommunicationItem(club, session, item.id, userId);
      const context = await (resumeMode === "edit" ? loadPreparedContractForEditing : resumeMode === "signatures" ? loadPreparedContractForSignature : loadPreparedContractForFinalReview)(club, session, item.id, userId, item.dokumenttyp as "mitgliedsantrag" | "pachtvertrag");
      const startDate = context.dokumenttyp === "mitgliedsantrag" ? (context.entwurf_daten as PreparedMembershipApplicationDraft).startDate : (context.entwurf_daten as PreparedLeaseContractDraft).draft.startDate;
      setOpened({ id: context.id, documentType: context.dokumenttyp, memberId: context.mitglied_id, parcelId: context.parzelle_id, startDate, resumeMode });
      await load(); onChanged?.();
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Der Vertragsvorgang konnte nicht geöffnet werden."); } finally { setBusy(null); }
  }
  const workflowChanged = async () => { await load(); onChanged?.(); };
  if (opened) return <section className="data-workspace communication-center" aria-label="Vorgänge"><div className="editor-actions"><button type="button" className="secondary-action" onClick={() => { setOpened(null); void workflowChanged(); }}>Zurück zu Vorgängen</button></div>{opened.documentType === "mitgliedsantrag" ? <MembershipApplicationFlow session={session} memberId={opened.memberId} preparedContractId={opened.id} resumeMode={opened.resumeMode} onSaved={workflowChanged} onCancel={() => { setOpened(null); void workflowChanged(); }} /> : opened.parcelId !== null ? <LeaseContractFlow session={session} memberId={opened.memberId} parcelId={opened.parcelId} startDate={opened.startDate} preparedContractId={opened.id} resumeMode={opened.resumeMode} onSaved={workflowChanged} onCancel={() => { setOpened(null); void workflowChanged(); }} /> : <p className="notice" role="alert">Dem vorbereiteten Pachtvertrag fehlt eine Parzelle.</p>}</section>;
  return <section className="data-workspace communication-center" aria-label="Vorgänge">
    <div className="tabs" role="tablist">{tabs.map(([id, label]) => <button key={id} className={area === id ? "tab active" : "tab"} onClick={() => setArea(id)}>{label}{isOpenArea(id) && (openCounts?.[id] ?? 0) > 0 && <span className="nav-badge">{openCounts![id]}</span>}</button>)}</div>
    <div className="tabs communication-filters" role="tablist">{assignments.map(([id, label]) => <button key={id} className={assignment === id ? "tab active" : "tab"} onClick={() => setAssignment(id)}>{label}</button>)}{area === "history" && <button className="secondary-action" onClick={() => setShowAllHistory(value => !value)}>{showAllHistory ? "Letzte 90 Tage" : "Alle anzeigen"}</button>}<button className="secondary-action" onClick={() => void load()} disabled={loading}>Aktualisieren</button></div>
    {area === "history" && !showAllHistory && <p className="detail-hint">Es werden die letzten 90 Tage angezeigt.</p>}{message && <p className="notice" role="alert">{message}</p>}
    {loading ? <p>Lädt …</p> : <div className="split-view communication-layout"><div className="data-table-wrap"><table><thead><tr><th>Dokument</th><th>Status</th><th>Mitglied</th><th>Parzelle</th><th>{area === "sent" ? "Versendet" : "Erstellt"}</th><th>Zuweisung</th><th>Aktion</th></tr></thead><tbody>{items.map(item => <tr key={item.id} className={selected?.id === item.id ? "selected-row" : ""} onClick={() => setSelectedId(item.id)}><td><strong>{communicationDocumentLabel(item.dokumenttyp)}</strong><small>{communicationTypeLabel(item.vorgangstyp)}{item.empfaenger_email ? ` · ${item.empfaenger_email}` : ""}{item.betreff ? ` · ${item.betreff}` : ""}</small></td><td>{communicationStatusLabel(item.status)}{item.status === "fehler" && item.fehlertext && <small className="error-text">{item.fehlertext}</small>}</td><td>{communicationMemberLabel(item)}</td><td>{communicationParcelLabel(item)}</td><td>{formatDate(area === "sent" ? item.versendet_am : item.erstellt_am)}</td><td>{communicationActorLabel(item.zugewiesen_an)}</td><td onClick={event => event.stopPropagation()}>{area === "print" && item.status === "druck_bereit" && item.zugewiesen_an === userId ? <button className="secondary-action" disabled={busy === item.id} onClick={() => void completePrint(item)}>Druck erledigt</button> : isPreparedContract(item) && (item.zugewiesen_an === null || item.zugewiesen_an === userId) ? <button className="secondary-action" disabled={busy === item.id} onClick={() => void open(item)}>Öffnen</button> : isStartedContract(item) && item.zugewiesen_an === userId ? <button className="secondary-action" disabled={busy === item.id} onClick={() => void open(item, "signatures")}>Signatur fortsetzen</button> : isFinalReviewContract(item) && item.zugewiesen_an === userId ? <button className="secondary-action" disabled={busy === item.id} onClick={() => void open(item, "review")}>Letzte Prüfung</button> : item.zugewiesen_an === null && area !== "sent" && area !== "history" ? <button className="secondary-action" disabled={busy === item.id} onClick={() => void claim(item)}>Übernehmen</button> : item.zugewiesen_an === userId && area !== "sent" && area !== "history" ? <button className="secondary-action" disabled={busy === item.id} onClick={() => void release(item)}>Freigeben</button> : "–"}</td></tr>)}</tbody></table>{items.length === 0 && <p className="empty-state">Keine Vorgänge vorhanden.</p>}</div><CommunicationDetails item={selected} /></div>}
  </section>;
}
function isPreparedContract(item: CommunicationItem): item is CommunicationItem & { dokumenttyp: "mitgliedsantrag" | "pachtvertrag" } { return item.vorgangstyp === "vertrag_signatur" && item.status === "vorbereitet" && (item.dokumenttyp === "mitgliedsantrag" || item.dokumenttyp === "pachtvertrag"); }
function isOpenArea(area: CommunicationArea): area is "signature" | "mail" | "print" { return area === "signature" || area === "mail" || area === "print"; }
function isStartedContract(item: CommunicationItem): item is CommunicationItem & { dokumenttyp: "mitgliedsantrag" | "pachtvertrag" } { return item.vorgangstyp === "vertrag_signatur" && item.status === "zur_signatur" && (item.dokumenttyp === "mitgliedsantrag" || item.dokumenttyp === "pachtvertrag"); }
function isFinalReviewContract(item: CommunicationItem): item is CommunicationItem & { dokumenttyp: "mitgliedsantrag" | "pachtvertrag" } { return item.vorgangstyp === "vertrag_signatur" && item.status === "letzte_pruefung" && item.final_dokument_id !== null && (item.dokumenttyp === "mitgliedsantrag" || item.dokumenttyp === "pachtvertrag"); }
function CommunicationDetails({ item }: { item: CommunicationItem | null }) { if (!item) return <aside className="detail-panel"><h2>Vorgang auswählen</h2><p>Wähle einen Vorgang aus.</p></aside>; return <aside className="detail-panel communication-details"><h2>{communicationDocumentLabel(item.dokumenttyp)}</h2><p>{communicationStatusLabel(item.status)} · {communicationTypeLabel(item.vorgangstyp)}</p><dl><Detail label="Vorgangs-ID" value={String(item.id)} /><Detail label="Mitglied" value={communicationMemberLabel(item)} /><Detail label="Parzelle" value={communicationParcelLabel(item)} /><Detail label="Revision" value={String(item.revision)} /><Detail label="Signatur-Revision" value={item.signatur_revision?.toString() ?? "–"} /><Detail label="Signatur gestartet" value={formatDate(item.signatur_gestartet_am)} /><Detail label="Finales Dokument" value={item.final_dokument_id?.toString() ?? "–"} /><Detail label="Erstellt" value={`${formatDate(item.erstellt_am)} · ${communicationActorLabel(item.erstellt_von)}`} /><Detail label="Bearbeitet" value={`${formatDate(item.bearbeitet_am)} · ${communicationActorLabel(item.bearbeitet_von)}`} /><Detail label="Zugewiesen an" value={communicationActorLabel(item.zugewiesen_an)} />{item.vorgangstyp === "mail" && <><Detail label="Empfänger" value={item.empfaenger_email ?? "–"} /><Detail label="Betreff" value={item.betreff ?? "–"} /><Detail label="Mailtext" value={item.mailtext ?? "–"} /><Detail label="Versendet" value={formatDate(item.versendet_am)} /></>}{item.vorgangstyp === "druck" && <Detail label="Gedruckt" value={formatDate(item.gedruckt_am)} />}{item.archiviert_am && <Detail label="Archiviert" value={formatDate(item.archiviert_am)} />}{item.fehlertext && <Detail label="Fehler" value={item.fehlertext} />}</dl></aside>; }
function Detail({ label, value }: { label: string; value: string }) { return <><dt>{label}</dt><dd>{value}</dd></>; }
function formatDate(value: string | null) { return value ? new Intl.DateTimeFormat("de-DE", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value)) : "–"; }
