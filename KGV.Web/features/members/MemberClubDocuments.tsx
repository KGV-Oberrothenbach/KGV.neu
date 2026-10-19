"use client";

import { useEffect, useMemo, useState } from "react";
import { useSelectedClub } from "../auth/AuthProvider";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { Member } from "../../models/members/member";
import type { MemberClubDocumentDeliveryHistoryItem, MemberClubDocumentStatus, MemberClubDocumentStatusItem } from "../../models/club-documents/member-club-document-status";
import { recordMemberClubDocumentsPaper, sendMemberClubDocumentsDigital } from "../../services/club-documents/member-club-document-delivery-service";
import { getMemberClubDocumentDeliveryHistory, getMemberClubDocumentStatus } from "../../services/club-documents/member-club-document-status-service";

const dateOnly = () => new Date().toISOString().slice(0, 10);
const localNow = () => { const now = new Date(); now.setMinutes(now.getMinutes() - now.getTimezoneOffset()); return now.toISOString().slice(0, 16); };
const formatDate = (value: string | null) => value ? new Intl.DateTimeFormat("de-DE", { dateStyle: "medium" }).format(new Date(value)) : "–";
const formatDateTime = (value: string | null) => value ? new Intl.DateTimeFormat("de-DE", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value)) : "–";
const statusText = (item: MemberClubDocumentStatusItem) => item.status === "bereitgestellt" ? "Aktuell bereitgestellt" : item.status === "fehlt" ? "Aktuelle Fassung fehlt beim Mitglied" : "Keine aktuelle Vereinsfassung hinterlegt";
function deliveryError(cause: unknown) {
  const error = cause as Error & { code?: string };
  if (error.code === "DOCUMENT_FILE_MISSING") return "Mindestens eine ausgewählte Fassung hat keine hinterlegte PDF-Datei.";
  if (error.code === "DIGITAL_SEND_FAILED") return "Der Versand wurde vom Maildienst nicht bestätigt.";
  if (error.code === "DELIVERY_RECORD_FAILED") return "Die E-Mail wurde versandt, aber der Bereitstellungsnachweis konnte nicht gespeichert werden.";
  if (/E-Mail-Adresse/i.test(error?.message ?? "")) return "Für dieses Mitglied ist keine E-Mail-Adresse hinterlegt.";
  if (/Berechtigung/i.test(error?.message ?? "")) return "Für diese Bereitstellung fehlt die Berechtigung.";
  if (/Fassung/i.test(error?.message ?? "")) return "Die ausgewählten Dokumentfassungen sind nicht gültig.";
  return error instanceof Error ? error.message : "Die Bereitstellung konnte nicht abgeschlossen werden.";
}

export function MemberClubDocuments({ session, member, onStatusUpdated }: { session: BrowserSession; member: Member; onStatusUpdated?: (status: MemberClubDocumentStatus) => void }) {
  const club = useSelectedClub();
  const [status, setStatus] = useState<MemberClubDocumentStatus | null>(null);
  const [history, setHistory] = useState<MemberClubDocumentDeliveryHistoryItem[]>([]);
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const [message, setMessage] = useState(""); const [loading, setLoading] = useState(true); const [saving, setSaving] = useState(false);
  const [dialog, setDialog] = useState<"digital" | "papier" | null>(null); const [paperAt, setPaperAt] = useState(localNow()); const [paperConfirmed, setPaperConfirmed] = useState(false);
  const load = async () => { setLoading(true); try { const [nextStatus, nextHistory] = await Promise.all([getMemberClubDocumentStatus(club, session, member.id, dateOnly()), getMemberClubDocumentDeliveryHistory(club, session, member.id)]); setStatus(nextStatus); setHistory(nextHistory); setSelectedIds(nextStatus.documents.filter((item) => item.status === "fehlt" && item.vereinsdokument_fassung_id !== null).map((item) => item.vereinsdokument_fassung_id!)); onStatusUpdated?.(nextStatus); } catch (cause) { setMessage(deliveryError(cause)); } finally { setLoading(false); } };
  useEffect(() => {
    let active = true;
    void Promise.all([getMemberClubDocumentStatus(club, session, member.id, dateOnly()), getMemberClubDocumentDeliveryHistory(club, session, member.id)])
      .then(([nextStatus, nextHistory]) => { if (active) { setStatus(nextStatus); setHistory(nextHistory); setSelectedIds(nextStatus.documents.filter((item) => item.status === "fehlt" && item.vereinsdokument_fassung_id !== null).map((item) => item.vereinsdokument_fassung_id!)); } })
      .catch((cause) => { if (active) setMessage(deliveryError(cause)); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [club, session, member.id]);
  const missing = useMemo(() => status?.documents.filter((item) => item.status === "fehlt" && item.vereinsdokument_fassung_id !== null) ?? [], [status]);
  const selected = missing.filter((item) => selectedIds.includes(item.vereinsdokument_fassung_id!));
  const toggle = (id: number) => setSelectedIds((current) => current.includes(id) ? current.filter((item) => item !== id) : [...current, id]);
  const close = () => { setDialog(null); setPaperConfirmed(false); };
  async function sendDigital() { setSaving(true); setMessage(""); try { await sendMemberClubDocumentsDigital(club, session, member.id, selected.map((item) => item.vereinsdokument_fassung_id!)); close(); setMessage("Die Vereinsunterlagen wurden digital bereitgestellt."); await load(); } catch (cause) { setMessage(deliveryError(cause)); } finally { setSaving(false); } }
  async function recordPaper() { const deliveredAt = new Date(paperAt); if (Number.isNaN(deliveredAt.valueOf()) || deliveredAt > new Date()) { setMessage("Eine Papierübergabe darf nicht in der Zukunft liegen."); return; } setSaving(true); setMessage(""); try { await recordMemberClubDocumentsPaper(club, session, member.id, selected.map((item) => item.vereinsdokument_fassung_id!), deliveredAt.toISOString()); close(); setMessage("Die Papierübergabe wurde erfasst."); await load(); } catch (cause) { setMessage(deliveryError(cause)); } finally { setSaving(false); } }
  const noSelection = selected.length === 0;
  return <section className="secondary-member member-club-documents"><h3>Vereinsunterlagen</h3>{message && <p className="notice" role="status">{message}</p>}{loading ? <p>Lädt Vereinsunterlagen …</p> : <>{status?.hasMissingClubVersion && <p className="notice">Für mindestens eine Pflichtunterlage ist keine aktuelle Fassung hinterlegt.</p>}<p><strong>{status?.complete ? "Pflichtunterlagen vollständig" : missing.length ? "Unterlagen fehlen beim Mitglied" : "Keine aktuellen Pflichtunterlagen fehlen."}</strong></p><div className="data-table-wrap"><table><thead><tr><th>Unterlage</th><th>Aktuelle Fassung</th><th>Gültigkeit</th><th>Status</th><th>Letzte Bereitstellung</th></tr></thead><tbody>{status?.documents.map((item) => <tr key={item.dokumenttyp_code}><td>{item.bezeichnung}</td><td>{item.fassung ?? "–"}</td><td>{formatDate(item.gueltig_ab)}{item.gueltig_bis ? ` bis ${formatDate(item.gueltig_bis)}` : ""}</td><td>{statusText(item)}</td><td>{item.bereitgestellt_am ? `${item.bereitstellungsart === "digital" ? "digital" : "Papier"} · ${formatDateTime(item.bereitgestellt_am)}` : "–"}</td></tr>)}</tbody></table></div>{missing.length > 0 && <fieldset disabled={saving}><legend>Fehlende aktuelle Unterlagen auswählen</legend>{missing.map((item) => <label className="check" key={item.vereinsdokument_fassung_id}><input type="checkbox" checked={selectedIds.includes(item.vereinsdokument_fassung_id!)} onChange={() => toggle(item.vereinsdokument_fassung_id!)} /> {item.bezeichnung} · Fassung {item.fassung}</label>)}</fieldset>}<div className="editor-actions"><button disabled={saving || noSelection} onClick={() => setDialog("digital")}>Fehlende Unterlagen digital senden</button><button className="secondary-action" disabled={saving || noSelection} onClick={() => { setPaperAt(localNow()); setDialog("papier"); }}>Papierübergabe erfassen</button></div>{noSelection && !status?.hasMissingClubVersion && <p>Keine aktuellen Pflichtunterlagen fehlen.</p>}{dialog === "digital" && <div className="secondary-form"><h4>Digitalen Versand bestätigen</h4><p><strong>Mitglied:</strong> {member.vorname} {member.name}<br /><strong>E-Mail:</strong> {member.email || "nicht hinterlegt"}</p><p>Zu senden: {selected.map((item) => `${item.bezeichnung} (${item.fassung})`).join(", ")}</p><div className="editor-actions"><button disabled={saving || !member.email} onClick={() => void sendDigital()}>{saving ? "Versendet …" : "Jetzt digital senden"}</button><button className="secondary-action" disabled={saving} onClick={close}>Abbrechen</button></div>{!member.email && <p className="notice">Für dieses Mitglied ist keine E-Mail-Adresse hinterlegt.</p>}</div>}{dialog === "papier" && <div className="secondary-form"><h4>Papierübergabe bestätigen</h4><p>{selected.map((item) => `${item.bezeichnung} (${item.fassung})`).join(", ")}</p><label>Tatsächlicher Übergabezeitpunkt<input type="datetime-local" max={localNow()} value={paperAt} onChange={(event) => setPaperAt(event.target.value)} /></label><label className="check"><input type="checkbox" checked={paperConfirmed} onChange={(event) => setPaperConfirmed(event.target.checked)} /> Die ausgewählten Unterlagen wurden dem Mitglied tatsächlich in Papierform übergeben.</label><div className="editor-actions"><button disabled={saving || !paperConfirmed} onClick={() => void recordPaper()}>{saving ? "Speichert …" : "Übergabe erfassen"}</button><button className="secondary-action" disabled={saving} onClick={close}>Abbrechen</button></div></div>}<h4>Bereitstellungshistorie</h4><div className="data-table-wrap"><table><thead><tr><th>Unterlage</th><th>Fassung</th><th>Art</th><th>Bereitgestellt am</th><th>Status</th></tr></thead><tbody>{history.map((item) => <tr key={item.bereitstellung_id}><td>{item.bezeichnung}</td><td>{item.fassung}</td><td>{item.bereitstellungsart === "digital" ? "Digital" : "Papier"}</td><td>{formatDateTime(item.bereitgestellt_am)}</td><td>{item.storniert_am ? `Storniert · ${formatDateTime(item.storniert_am)}${item.storno_grund ? ` · ${item.storno_grund}` : ""}` : "Aktiv"}</td></tr>)}{history.length === 0 && <tr><td colSpan={5}>Noch keine Bereitstellung erfasst.</td></tr>}</tbody></table></div></>}</section>;
}
