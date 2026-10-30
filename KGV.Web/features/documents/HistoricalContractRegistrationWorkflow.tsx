"use client";

import { useEffect, useMemo, useState } from "react";
import { useSelectedClub } from "../auth/AuthProvider";
import type { BrowserSession } from "../../lib/supabase-auth";
import type { Member, MemberSearchResult } from "../../models/members/member";
import type { HistoricalLeaseRelationship } from "../../models/documents/document-type";
import { loadMemberSearchResults, getMember } from "../../services/members/member-service";
import { loadMemberLeaseRelationships } from "../../services/contracts/lease-service";
import { uploadDocumentForOwner, reserveHistoricalContractNumber } from "../../services/documents/document-upload-service";
import { memberDocumentOwner } from "../../models/documents/document";
import { registerHistoricalContractForWeb } from "../../services/documents/historical-contract-registration-service";

type ContractKind = "historischer_ma_pv_erstvertrag" | "historischer_pv_folgevertrag";
const steps = ["Vertragsart", "Parteien / Parzellen", "Dokument", "Vertragsdaten / Historie", "Änderungen und Hinweise", "Zusammenfassung", "Übernehmen"];
const today = () => new Date().toISOString().slice(0, 10);
const label = (member: Pick<MemberSearchResult, "mitgliedsnummer" | "vorname" | "name">) => `Mgl.-Nr. ${member.mitgliedsnummer ?? "–"} – ${[member.vorname, member.name].filter(Boolean).join(" ")}`;
const leaseLabel = (lease: HistoricalLeaseRelationship) => `Garten ${lease.gartenNr ?? "–"}${lease.anlage ? ` – ${lease.anlage}` : ""}`;

export function HistoricalContractRegistrationWorkflow({ session }: { session: BrowserSession }) {
  const club = useSelectedClub();
  const [step, setStep] = useState(0);
  const [kind, setKind] = useState<ContractKind>("historischer_ma_pv_erstvertrag");
  const [members, setMembers] = useState<MemberSearchResult[]>([]);
  const [query, setQuery] = useState("");
  const [primaryId, setPrimaryId] = useState<number | null>(null);
  const [secondaryId, setSecondaryId] = useState<number | null>(null);
  const [primary, setPrimary] = useState<Member | null>(null);
  const [leases, setLeases] = useState<HistoricalLeaseRelationship[]>([]);
  const [selectedLeaseIds, setSelectedLeaseIds] = useState<number[]>([]);
  const [file, setFile] = useState<File | null>(null);
  const [contractDate, setContractDate] = useState(today());
  const [signed, setSigned] = useState(true);
  const [previousEnds, setPreviousEnds] = useState<Record<string, string>>({});
  const [correctMembershipSince, setCorrectMembershipSince] = useState(false);
  const [membershipSince, setMembershipSince] = useState("");
  const [remarks, setRemarks] = useState("");
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const [result, setResult] = useState<{ number: number; stamped: boolean } | null>(null);

  useEffect(() => { let active = true; loadMemberSearchResults(club, session).then((items) => { if (active) setMembers(items); }).catch(() => { if (active) setMessage("Mitglieder konnten nicht geladen werden."); }); return () => { active = false; }; }, [club, session]);
  useEffect(() => { if (!primaryId) return; let active = true; Promise.all([getMember(club, session, primaryId), loadMemberLeaseRelationships(club, session, primaryId)]).then(([member, relationships]) => { if (!active) return; setPrimary(member); setMembershipSince(member?.mitglied_seit ?? ""); setLeases(relationships); }).catch(() => { if (active) setMessage("Mitglied oder Pachtbeziehungen konnten nicht geladen werden."); }); return () => { active = false; }; }, [club, session, primaryId]);

  const visibleMembers = useMemo(() => members.filter((member) => !query.trim() || label(member).toLocaleLowerCase("de").includes(query.trim().toLocaleLowerCase("de"))), [members, query]);
  const selectedLeases = leases.filter((lease) => selectedLeaseIds.includes(lease.pachtverhaeltnisId));
  const secondary = members.find((member) => member.id === secondaryId) ?? null;
  const primarySearch = members.find((member) => member.id === primaryId) ?? null;
  const defaultPreviousEnd = contractDate ? new Date(`${contractDate}T12:00:00`) : null;
  if (defaultPreviousEnd) defaultPreviousEnd.setDate(defaultPreviousEnd.getDate() - 1);
  const suggestedEnd = defaultPreviousEnd ? defaultPreviousEnd.toISOString().slice(0, 10) : "";

  function toggleLease(id: number) { setSelectedLeaseIds((current) => current.includes(id) ? current.filter((value) => value !== id) : [...current, id]); }
  function next() {
    setMessage("");
    if (step === 1 && (!primaryId || selectedLeaseIds.length === 0)) { setMessage("Bitte Hauptmitglied und mindestens eine Pachtbeziehung wählen."); return; }
    if (step === 2 && !file) { setMessage("Bitte ein PDF oder einen vorhandenen Scan auswählen."); return; }
    if (step === 3 && !/^\d{4}-\d{2}-\d{2}$/.test(contractDate)) { setMessage("Bitte ein gültiges Vertragsdatum eingeben."); return; }
    setStep((current) => Math.min(current + 1, steps.length - 1));
  }
  async function submit() {
    if (!primaryId || !file || selectedLeaseIds.length === 0) return;
    setSaving(true); setMessage("");
    try {
      const title = `${kind === "historischer_ma_pv_erstvertrag" ? "MA + PV – Erstvertrag" : "PV – Folgevertrag"} – ${contractDate}`;
      const document = await uploadDocumentForOwner(club, session, memberDocumentOwner(primaryId), file, title);
      const rows = await registerHistoricalContractForWeb(club, session, { documentId: document.id, documentTypeCode: kind, contractDate, signed, leaseRelationshipIds: selectedLeaseIds, previousEndDates: previousEnds, membershipSinceCorrection: correctMembershipSince && membershipSince ? { memberId: primaryId, date: membershipSince } : null });
      if (!rows.length) throw new Error("Der Server hat keine Vertragsfassung bestätigt.");
      const number = await reserveHistoricalContractNumber(club, session, document.id);
      setResult({ number: number.nummer, stamped: number.pdfGestempelt });
      setStep(steps.length - 1);
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Der Altvertrag konnte nicht übernommen werden."); }
    finally { setSaving(false); }
  }

  return <section className="data-workspace" aria-label="Altvertrag nacherfassen">
    <div className="data-toolbar"><strong>Schritt {step + 1} von {steps.length}: {steps[step]}</strong><span>{kind === "historischer_ma_pv_erstvertrag" ? "MA + PV – Erstvertrag" : "PV – Folgevertrag"}</span></div>
    {message && <p className="notice" role="alert">{message}</p>}
    {step === 0 && <fieldset><legend>Welche historische Vertragsart liegt vor?</legend><label><input type="radio" checked={kind === "historischer_ma_pv_erstvertrag"} onChange={() => setKind("historischer_ma_pv_erstvertrag")} /> <strong>MA + PV – Erstvertrag</strong><small>Ein gemeinsames ursprüngliches Dokument für Mitgliedsaufnahme und ersten Pachtvertrag.</small></label><label><input type="radio" checked={kind === "historischer_pv_folgevertrag"} onChange={() => setKind("historischer_pv_folgevertrag")} /> <strong>PV – Folgevertrag</strong><small>Ein späterer Pachtvertragsstand eines vorhandenen Mitglieds.</small></label></fieldset>}
    {step === 1 && <fieldset><legend>Vertragsparteien und betroffene Parzellen</legend><label>Mitglied suchen<input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Mitgliedsnummer oder Name" /></label><div className="data-table-wrap"><table><thead><tr><th>Mitglied</th><th>Auswahl</th></tr></thead><tbody>{visibleMembers.slice(0, 20).map((member) => <tr key={member.id}><td>{label(member)}</td><td><button type="button" className={primaryId === member.id ? "secondary-action" : ""} onClick={() => { setPrimaryId(member.id); setPrimary(null); setLeases([]); setSelectedLeaseIds([]); setSecondaryId(null); }}>Hauptmitglied wählen</button></td></tr>)}</tbody></table></div>{primarySearch && <><p><strong>Hauptmitglied:</strong> {label(primarySearch)} · Mitglied seit: {primary?.mitglied_seit ?? "–"}</p><label>Zweiter vorhandener Pächter<select value={secondaryId ?? ""} onChange={(event) => setSecondaryId(event.target.value ? Number(event.target.value) : null)}><option value="">Kein zweiter Pächter</option>{members.filter((member) => member.id !== primaryId).map((member) => <option key={member.id} value={member.id}>{label(member)}</option>)}</select></label>{secondary && <p className="context-note">{label(secondary)} wird für die Vertragszusammenfassung und den PDF-Stempel berücksichtigt. Es wird keine Mitgliedschaft erzeugt.</p>}<fieldset><legend>Pachtbeziehungen / Parzellen</legend>{leases.map((lease) => <label key={lease.pachtverhaeltnisId}><input type="checkbox" checked={selectedLeaseIds.includes(lease.pachtverhaeltnisId)} onChange={() => toggleLease(lease.pachtverhaeltnisId)} /> {leaseLabel(lease)} <small>({lease.rolle}, seit {lease.gueltigAb})</small></label>)}{leases.length === 0 && <p>Keine bestehenden Pachtbeziehungen für dieses Mitglied gefunden.</p>}</fieldset></>}</fieldset>}
    {step === 2 && <fieldset><legend>Dokument</legend><label>PDF oder Scan<input type="file" accept="application/pdf,image/*,.pdf,.jpg,.jpeg,.png,.webp" onChange={(event) => setFile(event.target.files?.[0] ?? null)} /></label>{file && <p><strong>{file.name}</strong> · {(file.size / 1024).toFixed(0)} KB</p>}<p className="context-note">Das Dokument wird erst beim Übernehmen fachlich als Vertrag registriert und danach nummeriert sowie – bei PDF – gestempelt.</p></fieldset>}
    {step === 3 && <fieldset><legend>Vertragsdaten und Historie</legend><label>Vertragsdatum<input type="date" value={contractDate} onChange={(event) => setContractDate(event.target.value)} /></label><label>Unterzeichnet<input type="checkbox" checked={signed} onChange={(event) => setSigned(event.target.checked)} /></label>{selectedLeases.map((lease) => <label key={lease.pachtverhaeltnisId}>Vorgängerende für {leaseLabel(lease)}<input type="date" value={previousEnds[String(lease.pachtverhaeltnisId)] ?? suggestedEnd} onChange={(event) => setPreviousEnds((current) => ({ ...current, [lease.pachtverhaeltnisId]: event.target.value }))} /><small>Vorschlag: Tag vor Vertragsbeginn; bei einem nachträglich älteren Vertrag ordnet G6a ihn zeitlich vor dem jüngeren Stand ein.</small></label>)}<label>Bemerkung (optional)<textarea value={remarks} onChange={(event) => setRemarks(event.target.value)} /></label></fieldset>}
    {step === 4 && <fieldset><legend>Änderungen und Hinweise</legend>{kind === "historischer_ma_pv_erstvertrag" && primary?.mitglied_seit && contractDate < primary.mitglied_seit && <><label><input type="checkbox" checked={correctMembershipSince} onChange={(event) => setCorrectMembershipSince(event.target.checked)} /> Mitglied seit bewusst anhand dieses Erstvertrags korrigieren</label>{correctMembershipSince && <label>Neues Mitglied-seit-Datum<input type="date" value={membershipSince} onChange={(event) => setMembershipSince(event.target.value)} /></label>}</>}<p>Ein Wegfall einer Vertragspartei beendet keine Mitgliedschaft automatisch. Ein ggf. Mitgliedschaftsende und Aktiv/Passiv-Wechsel bleiben bewusste Folgeentscheidungen.</p>{secondary && <p>Vertragsparteien: {label(primarySearch!)} und {label(secondary)}.</p>}</fieldset>}
    {step === 5 && <section><h2>Zusammenfassung</h2><dl><dt>Vertragsart</dt><dd>{kind === "historischer_ma_pv_erstvertrag" ? "MA + PV – Erstvertrag" : "PV – Folgevertrag"}</dd><dt>Vertragsdatum</dt><dd>{contractDate}</dd><dt>Pächter</dt><dd>{primarySearch ? label(primarySearch) : "–"}{secondary ? ` / ${label(secondary)}` : ""}</dd><dt>Parzellen</dt><dd>{selectedLeases.map(leaseLabel).join(", ") || "–"}</dd><dt>Dokument</dt><dd>{file?.name ?? "–"}</dd><dt>Historische Vertragsnummer</dt><dd>wird beim Übernehmen automatisch vergeben</dd><dt>PDF-Stempel</dt><dd>Mgl.-Nr. {[primarySearch?.mitgliedsnummer, secondary?.mitgliedsnummer].filter(Boolean).join(" / ")}<br />Vertrags-Nr. wird automatisch vergeben</dd></dl></section>}
    {result && <p className="notice" role="status">Altvertrag übernommen. Vertrags-Nr. {result.number}{result.stamped ? " wurde im PDF gestempelt." : " wurde vergeben; der Scan ist kein PDF."}</p>}
    <div className="editor-actions">{step > 0 && !result && <button className="secondary-action" disabled={saving} onClick={() => setStep((current) => current - 1)}>Zurück</button>}{step < 5 && <button disabled={saving} onClick={next}>Weiter</button>}{step === 5 && <button disabled={saving} onClick={() => void submit()}>{saving ? "Altvertrag wird übernommen …" : "Altvertrag übernehmen"}</button>}</div>
  </section>;
}
