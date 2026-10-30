"use client";

import { useEffect, useMemo, useState } from "react";
import { useSelectedClub } from "../auth/AuthProvider";
import type { BrowserSession } from "../../lib/supabase-auth";

import type { HistoricalLeaseRelationship } from "../../models/documents/document-type";

import { uploadHistoricalContractDocument, loadHistoricalContractDocument, reserveHistoricalContractNumber } from "../../services/documents/document-upload-service";

import { registerHistoricalContractForWeb } from "../../services/documents/historical-contract-registration-service";
import { loadHistoricalContractDocuments, type HistoricalContractDocumentLink, loadHistoricalContractMembers, loadHistoricalContractLeaseRelationships, type HistoricalContractLookupMember, loadHistoricalContractPreview, type HistoricalContractHistoryEntry } from "../../repositories/documents/historical-contract-registration-repository";

import { buildHistoricalMembershipEnds } from "../../services/documents/historical-contract-membership-commands";

type ContractKind = "historischer_ma_pv_erstvertrag" | "historischer_pv_folgevertrag";
const steps = ["Vertragsart", "Parteien / Parzellen", "Dokument", "Vertragsdaten / Historie", "Änderungen und Hinweise", "Zusammenfassung", "Übernehmen"];
const today = () => new Date().toISOString().slice(0, 10);
const label = (member: Pick<HistoricalContractLookupMember, "mitgliedsnummer" | "vorname" | "name">) => `Mgl.-Nr. ${member.mitgliedsnummer ?? "–"} – ${[member.vorname, member.name].filter(Boolean).join(" ")}`;
const leaseLabel = (lease: HistoricalLeaseRelationship) => `Garten ${lease.gartenNr ?? "–"}${lease.anlage ? ` – ${lease.anlage}` : ""}`;

export function HistoricalContractRegistrationWorkflow({ session }: { session: BrowserSession }) {
  const club = useSelectedClub();
  const [step, setStep] = useState(0);
  const [kind, setKind] = useState<ContractKind>("historischer_ma_pv_erstvertrag");
  const [members, setMembers] = useState<HistoricalContractLookupMember[]>([]);
  const [query, setQuery] = useState("");
  const [primaryId, setPrimaryId] = useState<number | null>(null);
  const [secondaryId, setSecondaryId] = useState<number | null>(null);
  const [primary, setPrimary] = useState<HistoricalContractLookupMember | null>(null);
  const [existingDocuments, setExistingDocuments] = useState<HistoricalContractDocumentLink[]>([]);
  const [leases, setLeases] = useState<HistoricalLeaseRelationship[]>([]);
  const [selectedLeaseIds, setSelectedLeaseIds] = useState<number[]>([]);
  const [file, setFile] = useState<File | null>(null);
  const [contractDate, setContractDate] = useState(today());
  const [signed, setSigned] = useState(true);
  const [previousEnds, setPreviousEnds] = useState<Record<string, string>>({});
  const [history, setHistory] = useState<HistoricalContractHistoryEntry[]>([]);
  const [correctMembershipSince, setCorrectMembershipSince] = useState(false);
  const [membershipSince, setMembershipSince] = useState("");
  const [confirmedMembershipEnds, setConfirmedMembershipEnds] = useState<Record<number, string>>({});
  const [activityChange, setActivityChange] = useState<"" | "active" | "passive">("");
  const [remarks, setRemarks] = useState("");
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const [result, setResult] = useState<{ number: number; stamped: boolean; documentId: number } | null>(null);
  const selectedLeaseKey = selectedLeaseIds.join(",");

  useEffect(() => { let active = true; loadHistoricalContractMembers(club, session).then((items) => { if (active) setMembers(items); }).catch(() => { if (active) setMessage("Mitglieder konnten nicht geladen werden."); }); return () => { active = false; }; }, [club, session]);
  useEffect(() => { if (!primaryId) return; let active = true; Promise.all([loadHistoricalContractLeaseRelationships(club, session, primaryId), loadHistoricalContractDocuments(club, session, primaryId)]).then(([relationships, documents]) => { if (!active) return; const member = members.find(item => item.id === primaryId) ?? null; setPrimary(member); setMembershipSince(member?.mitglied_seit ?? ""); setLeases(relationships); setExistingDocuments(documents); }).catch(() => { if (active) setMessage("Mitglied oder Pachtbeziehungen konnten nicht geladen werden."); }); return () => { active = false; }; }, [club, session, primaryId, members]);
  useEffect(() => { if (!selectedLeaseKey || !/^\d{4}-\d{2}-\d{2}$/.test(contractDate)) return; let active = true; const ids = selectedLeaseKey.split(",").map(Number); loadHistoricalContractPreview(club, session, ids, contractDate).then((items) => { if (!active) return; setHistory(items); setPreviousEnds((current) => ({ ...Object.fromEntries(items.filter(x => x.ist_vorgaenger && x.vorgeschlagenes_vorgaengerende).map(x => [String(x.pachtverhaeltnis_id), current[String(x.pachtverhaeltnis_id)] ?? x.vorgeschlagenes_vorgaengerende!])), ...current })); }).catch((cause) => { if (active) setMessage(cause instanceof Error ? cause.message : "Vertragshistorie konnte nicht geladen werden."); }); return () => { active = false; }; }, [club, session, selectedLeaseKey, contractDate]);

  const visibleMembers = useMemo(() => members.filter((member) => !query.trim() || label(member).toLocaleLowerCase("de").includes(query.trim().toLocaleLowerCase("de"))), [members, query]);
  const selectedLeases = leases.filter((lease) => selectedLeaseIds.includes(lease.pachtverhaeltnisId));
  const secondary = members.find((member) => member.id === secondaryId) ?? null;
  const primarySearch = members.find((member) => member.id === primaryId) ?? null;
  const defaultPreviousEnd = contractDate ? new Date(`${contractDate}T12:00:00`) : null;
  if (defaultPreviousEnd) defaultPreviousEnd.setDate(defaultPreviousEnd.getDate() - 1);
  const suggestedEnd = defaultPreviousEnd ? defaultPreviousEnd.toISOString().slice(0, 10) : "";
  const removedPartyIds = [...new Set(history.filter(x => x.ist_vorgaenger).flatMap(x => x.vertragspartei_mitglied_ids).filter(id => id !== primaryId && id !== secondaryId))];

  function toggleLease(id: number) { setHistory([]); setSelectedLeaseIds((current) => current.includes(id) ? current.filter((value) => value !== id) : [...current, id]); }
  function next() {
    setMessage("");
    if (step === 1 && (!primaryId || selectedLeaseIds.length === 0)) { setMessage("Bitte Hauptmitglied und mindestens eine Pachtbeziehung wählen."); return; }
    if (step === 2 && !file) { setMessage("Bitte ein PDF oder einen vorhandenen Scan auswählen."); return; }
    if (step === 3 && !/^\d{4}-\d{2}-\d{2}$/.test(contractDate)) { setMessage("Bitte ein gültiges Vertragsdatum eingeben."); return; }
    if (step === 4 && buildHistoricalMembershipEnds(kind, removedPartyIds, confirmedMembershipEnds).some(end => !/^\d{4}-\d{2}-\d{2}$/.test(end.mitglied_ende))) { setMessage("Bitte das bestätigte Mitgliedschafts-Enddatum angeben."); return; }
    setStep((current) => Math.min(current + 1, steps.length - 1));
  }
  async function submit() {
    if (!primaryId || !file || selectedLeaseIds.length === 0) return;
    setSaving(true); setMessage("");
    try {
      const title = `${kind === "historischer_ma_pv_erstvertrag" ? "MA + PV – Erstvertrag" : "PV – Folgevertrag"} – ${contractDate}`;
      const document = await uploadHistoricalContractDocument(club, session, primaryId, file, title, kind);
      const rows = await registerHistoricalContractForWeb(club, session, { documentId: document.id, documentTypeCode: kind, contractDate, signed, leaseRelationshipIds: selectedLeaseIds, previousEndDates: previousEnds, membershipSinceCorrection: kind === "historischer_ma_pv_erstvertrag" && correctMembershipSince && membershipSince ? { memberId: primaryId, date: membershipSince } : null, partyMemberIds: [primaryId, secondaryId].filter((id): id is number => id !== null), membershipEnds: buildHistoricalMembershipEnds(kind, removedPartyIds, confirmedMembershipEnds), activityStatusChanges: activityChange ? [{ mitglied_id: primaryId, aktiv: activityChange === "active" }] : [] });
      if (!rows.length) throw new Error("Der Server hat keine Vertragsfassung bestätigt.");
      const number = await reserveHistoricalContractNumber(club, session, document.id);
      setResult({ number: number.nummer, stamped: number.pdfGestempelt, documentId: document.id });
      setStep(steps.length - 1);
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Der Altvertrag konnte nicht übernommen werden."); }
    finally { setSaving(false); }
  }

  async function openResult(documentId = result?.documentId) {
    if (!documentId) return;
    const target = window.open("about:blank", "_blank");
    if (target) target.opener = null;
    try {
      const blob = await loadHistoricalContractDocument(club, session, documentId);
      const url = URL.createObjectURL(blob);
      if (target) target.location.href = url;
      else { const link = document.createElement("a"); link.href = url; link.download = "historischer-vertrag"; link.click(); }
      window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (cause) { target?.close(); setMessage(cause instanceof Error ? cause.message : "Vertragsdokument konnte nicht geöffnet werden."); }
  }

  return <section className="data-workspace" aria-label="Altvertrag nacherfassen">
    <div className="data-toolbar"><strong>Schritt {step + 1} von {steps.length}: {steps[step]}</strong><span>{kind === "historischer_ma_pv_erstvertrag" ? "MA + PV – Erstvertrag" : "PV – Folgevertrag"}</span></div>
    {message && <p className="notice" role="alert">{message}</p>}
    {primaryId && existingDocuments.length > 0 && <details><summary>Vorhandene historische Vertragsdokumente</summary>{existingDocuments.map(item => <p key={item.documentId}><button onClick={() => void openResult(item.documentId)}>{item.title || "Historischer Vertrag"} · {item.date || "–"} · Vertrags-Nr. {item.number ?? "–"}</button></p>)}</details>}
    {step === 0 && <fieldset><legend>Welche historische Vertragsart liegt vor?</legend><label><input type="radio" checked={kind === "historischer_ma_pv_erstvertrag"} onChange={() => setKind("historischer_ma_pv_erstvertrag")} /> <strong>MA + PV – Erstvertrag</strong><small>Ein gemeinsames ursprüngliches Dokument für Mitgliedsaufnahme und ersten Pachtvertrag.</small></label><label><input type="radio" checked={kind === "historischer_pv_folgevertrag"} onChange={() => setKind("historischer_pv_folgevertrag")} /> <strong>PV – Folgevertrag</strong><small>Ein späterer Pachtvertragsstand eines vorhandenen Mitglieds.</small></label></fieldset>}
    {step === 1 && <fieldset><legend>Vertragsparteien und betroffene Parzellen</legend><label>Mitglied suchen<input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Mitgliedsnummer oder Name" /></label><div className="data-table-wrap"><table><thead><tr><th>Mitglied</th><th>Auswahl</th></tr></thead><tbody>{visibleMembers.slice(0, 20).map((member) => <tr key={member.id}><td>{label(member)}</td><td><button type="button" className={primaryId === member.id ? "secondary-action" : ""} onClick={() => { setPrimaryId(member.id); setPrimary(null); setLeases([]); setExistingDocuments([]); setSelectedLeaseIds([]); setSecondaryId(null); setHistory([]); setConfirmedMembershipEnds({}); }}>Hauptmitglied wählen</button></td></tr>)}</tbody></table></div>{primarySearch && <><p><strong>Hauptmitglied:</strong> {label(primarySearch)} · Mitglied seit: {primary?.mitglied_seit ?? "–"}</p><label>Zweiter vorhandener Pächter<select value={secondaryId ?? ""} onChange={(event) => setSecondaryId(event.target.value ? Number(event.target.value) : null)}><option value="">Kein zweiter Pächter</option>{members.filter((member) => member.id !== primaryId).map((member) => <option key={member.id} value={member.id}>{label(member)}</option>)}</select></label>{secondary && <p className="context-note">{label(secondary)} wird für die Vertragszusammenfassung und den PDF-Stempel berücksichtigt. Es wird keine Mitgliedschaft erzeugt.</p>}<fieldset><legend>Pachtbeziehungen / Parzellen</legend>{leases.map((lease) => <label key={lease.pachtverhaeltnisId}><input type="checkbox" checked={selectedLeaseIds.includes(lease.pachtverhaeltnisId)} onChange={() => toggleLease(lease.pachtverhaeltnisId)} /> {leaseLabel(lease)} <small>({lease.rolle}, seit {lease.gueltigAb})</small></label>)}{leases.length === 0 && <p>Keine bestehenden Pachtbeziehungen für dieses Mitglied gefunden.</p>}</fieldset></>}</fieldset>}
    {step === 2 && <fieldset><legend>Dokument</legend><label>PDF oder Scan<input type="file" accept="application/pdf,image/*,.pdf,.jpg,.jpeg,.png,.webp" onChange={(event) => setFile(event.target.files?.[0] ?? null)} /></label>{file && <p><strong>{file.name}</strong> · {(file.size / 1024).toFixed(0)} KB</p>}<p className="context-note">Das Dokument wird erst beim Übernehmen fachlich als Vertrag registriert und danach nummeriert sowie – bei PDF – gestempelt.</p></fieldset>}
    {step === 3 && <fieldset><legend>Vertragsdaten und Historie</legend><label>Vertragsdatum<input type="date" value={contractDate} onChange={(event) => setContractDate(event.target.value)} /></label><label>Unterzeichnet<input type="checkbox" checked={signed} onChange={(event) => setSigned(event.target.checked)} /></label>{selectedLeases.map((lease) => <div key={lease.pachtverhaeltnisId}><label>Vorgängerende für {leaseLabel(lease)}<input type="date" value={previousEnds[String(lease.pachtverhaeltnisId)] ?? suggestedEnd} onChange={(event) => setPreviousEnds((current) => ({ ...current, [lease.pachtverhaeltnisId]: event.target.value }))} /></label><small>{history.some(x => x.pachtverhaeltnis_id === lease.pachtverhaeltnisId && x.ist_nachfolger) ? "Wird zeitlich vor dem nachfolgenden Vertragsstand einsortiert." : "Vorschlag: Tag vor Vertragsbeginn."}</small><ul>{history.filter(x => x.pachtverhaeltnis_id === lease.pachtverhaeltnisId).map(x => <li key={x.pachtvertragsfassung_id}>{x.wirksam_ab}–{x.gueltig_bis ?? "offen"} · Vertrags-Nr. {x.historische_vertragsnummer ?? "–"} · Parteien {x.vertragspartei_mitglied_ids.join(" / ") || "–"}</li>)}</ul></div>)}<label>Bemerkung (optional)<textarea value={remarks} onChange={(event) => setRemarks(event.target.value)} /></label></fieldset>}
    {step === 4 && <fieldset><legend>Änderungen und Hinweise</legend>{kind === "historischer_ma_pv_erstvertrag" && primary?.mitglied_seit && contractDate < primary.mitglied_seit && <><label><input type="checkbox" checked={correctMembershipSince} onChange={(event) => setCorrectMembershipSince(event.target.checked)} /> Mitglied seit bewusst anhand dieses Erstvertrags korrigieren</label>{correctMembershipSince && <label>Neues Mitglied-seit-Datum<input type="date" value={membershipSince} onChange={(event) => setMembershipSince(event.target.value)} /></label>}</>}<p>Ein Wegfall einer Vertragspartei beendet keine Mitgliedschaft automatisch.</p>{removedPartyIds.length > 0 && <p className="context-note">Automatisch erkannt: {removedPartyIds.map(id => members.find(member => member.id === id)).filter((member): member is HistoricalContractLookupMember => !!member).map(label).join(", ")} fällt bei den ausgewählten Parzellen heraus.</p>}{kind === "historischer_pv_folgevertrag" && removedPartyIds.map(id => { const member = members.find(item => item.id === id); return <div key={id}><label><input type="checkbox" checked={confirmedMembershipEnds[id] !== undefined} onChange={(event) => { const checked = event.target.checked; setConfirmedMembershipEnds(current => { const next = { ...current }; if (checked) next[id] = contractDate; else delete next[id]; return next; }); }} /> Mitgliedschaft von {member ? label(member) : "herausfallender Vertragspartei"} ebenfalls beenden</label>{confirmedMembershipEnds[id] !== undefined && <label>Mitgliedschaft endet am<input type="date" value={confirmedMembershipEnds[id]} onChange={(event) => { const value = event.target.value; setConfirmedMembershipEnds(current => ({ ...current, [id]: value })); }} /></label>}</div>; })}<label>Bestätigte Statusänderung des Hauptmitglieds<select value={activityChange} onChange={(event) => setActivityChange(event.target.value as "" | "active" | "passive")}><option value="">Keine Statusänderung übernehmen</option><option value="active">aktiv setzen</option><option value="passive">passiv setzen</option></select></label></fieldset>}
    {step === 5 && <section><h2>Zusammenfassung</h2><dl><dt>Vertragsart</dt><dd>{kind === "historischer_ma_pv_erstvertrag" ? "MA + PV – Erstvertrag" : "PV – Folgevertrag"}</dd><dt>Vertragsdatum</dt><dd>{contractDate}</dd><dt>Pächter</dt><dd>{primarySearch ? label(primarySearch) : "–"}{secondary ? ` / ${label(secondary)}` : ""}</dd><dt>Parzellen</dt><dd>{selectedLeases.map(leaseLabel).join(", ") || "–"}</dd><dt>Dokument</dt><dd>{file?.name ?? "–"}</dd><dt>Historische Vertragsnummer</dt><dd>wird beim Übernehmen automatisch vergeben</dd><dt>PDF-Stempel</dt><dd>Mgl.-Nr. {[primarySearch?.mitgliedsnummer, secondary?.mitgliedsnummer].filter(Boolean).join(" / ")}<br />Vertrags-Nr. wird automatisch vergeben</dd></dl></section>}
    {result && <p className="notice" role="status">Altvertrag übernommen. Vertrags-Nr. {result.number}{result.stamped ? " wurde im PDF gestempelt." : " wurde vergeben; der Scan ist kein PDF."}</p>}
    {result && <button onClick={() => void openResult()}>Vertragsdokument öffnen</button>}
    <div className="editor-actions">{step > 0 && !result && <button className="secondary-action" disabled={saving} onClick={() => setStep((current) => current - 1)}>Zurück</button>}{step < 5 && <button disabled={saving} onClick={next}>Weiter</button>}{step === 5 && <button disabled={saving} onClick={() => void submit()}>{saving ? "Altvertrag wird übernommen …" : "Altvertrag übernehmen"}</button>}</div>
  </section>;
}
