"use client";
import { useSelectedClub } from "../auth/AuthProvider";

import { useEffect, useState } from "react";
import { SignaturePad } from "../../components/signature/SignaturePad";
import type { BrowserSession } from "../../lib/supabase-auth";
import { emptyLegalRepresentativeDraft, loadLegalRepresentativeContext, validateLegalRepresentative, type LegalRepresentativeDraft, type LegalRepresentativeOption } from "../../services/contracts/legal-representative-service";
import { calculateMembershipApplicationContribution, loadMembershipApplicationData, type MembershipApplicationData } from "../../services/contracts/membership-application-service";
import { buildMembershipApplicationSignatureSteps, canFinalizeMembershipApplication, canStartMembershipApplicationSignatures, hasCompletedMembershipApplicationSignatures, type MembershipApplicationSignatureKey } from "../../services/contracts/membership-application-flow-service";
import { generateContract } from "../../services/contracts/contract-generation-service";
import { ContractPreview } from "./ContractPreview";
import { LegalRepresentativeFields } from "./LegalRepresentativeFields";
import { completePreparedContractSigning, loadPreparedContractForEditing, loadPreparedContractForSignature, parkPreparedContract, savePreparedContractIfChanged, startPreparedContractSigning, type PreparedContractResumeContext } from "../../services/communication/prepared-contract-service";
import { preparedContractSchemaVersion, type PreparedMembershipApplicationDraft } from "../../models/communication/prepared-contract-draft";

export function MembershipApplicationFlow({ session, memberId, onSaved, preparedContractId, resumeMode = "edit" }: { session: BrowserSession; memberId: number; onSaved: () => Promise<void>; preparedContractId?: number; resumeMode?: "edit" | "signatures" }) {
  const club = useSelectedClub();
  const [data, setData] = useState<MembershipApplicationData | null>(null);
  const [startDate, setStartDate] = useState("");
  const [fee, setFee] = useState("");
  const [draft, setDraft] = useState<LegalRepresentativeDraft>(emptyLegalRepresentativeDraft());
  const [options, setOptions] = useState<LegalRepresentativeOption[]>([]);
  const [minor, setMinor] = useState(false);
  const [step, setStep] = useState<"edit" | "preview" | "signatures">("edit");
  const [includeSecondaryMember, setIncludeSecondaryMember] = useState(false);
  const [signatures, setSignatures] = useState<Partial<Record<MembershipApplicationSignatureKey, string>>>({});
  const [signatureIndex, setSignatureIndex] = useState(0);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [resumeContext, setResumeContext] = useState<PreparedContractResumeContext | null>(null);
  const [finalDocumentCreated, setFinalDocumentCreated] = useState(false);

  useEffect(() => {
    let active = true;
    loadMembershipApplicationData(club, session, memberId).then((value) => {
      if (!active) return;
      setData(value); setStartDate(value.startDate); setFee(String(value.annualMemberFee));
    }).catch((error) => active && setMessage(error instanceof Error ? error.message : "Mitgliedsantrag konnte nicht vorbereitet werden."));
    return () => { active = false; };
  }, [session, memberId, club]);

  useEffect(() => {
    if (!data || !startDate) return;
    let active = true;
    loadLegalRepresentativeContext(club, session, data.member, startDate).then((value) => {
      if (!active) return;
      setMinor(value.isMinor); setOptions(value.options); if (!preparedContractId) setDraft(value.draft);
    }).catch((error) => active && setMessage(error instanceof Error ? error.message : "Vertreter konnte nicht vorbereitet werden."));
    return () => { active = false; };
  }, [session, data, startDate, club]);

  useEffect(() => {
    if (!preparedContractId || !data) return;
    let active = true;
    const loadPrepared = resumeMode === "signatures" ? loadPreparedContractForSignature : loadPreparedContractForEditing;
    loadPrepared(club, session, preparedContractId, session.user.id, "mitgliedsantrag").then((context) => {
      if (!active) return;
      const saved = context.entwurf_daten as PreparedMembershipApplicationDraft;
      if (saved.memberId !== memberId) throw new Error("Der gespeicherte Mitgliedsantrag passt nicht zum Mitglied.");
      setResumeContext(context); setStartDate(saved.startDate); setFee(String(saved.memberFee)); setDraft(saved.representative ?? emptyLegalRepresentativeDraft()); setIncludeSecondaryMember(saved.includeSecondaryMember); setSignatures({}); setSignatureIndex(0); setStep(resumeMode === "signatures" ? "signatures" : "edit");
    }).catch((error) => active && setMessage(error instanceof Error ? error.message : "Der vorbereitete Mitgliedsantrag konnte nicht geöffnet werden."));
    return () => { active = false; };
  }, [club, session, preparedContractId, memberId, data, resumeMode]);

  const request = (action: "preview" | "finalize") => ({ action, type: "mitgliedsantrag" as const, member_id: memberId, start_date: startDate, member_fee: Number(fee.replace(",", ".")), admission_fee: data?.admissionFee, representative: minor ? { mode: draft.mode, member_id: draft.memberId ?? undefined, vorname: draft.vorname, nachname: draft.nachname, adresse_abweichend: draft.adresseAbweichend, adresse: draft.adresse, plz: draft.plz, ort: draft.ort } : undefined, include_secondary_member: !minor && includeSecondaryMember, signature_application_member: signatures.applicationMember || undefined, signature_privacy_member: signatures.privacyMember || undefined, signature_application_representative: minor ? signatures.applicationSecondParty || undefined : undefined, signature_privacy_representative: minor ? signatures.privacySecondParty || undefined : undefined, signature_application_secondary: !minor ? signatures.applicationSecondParty || undefined : undefined, signature_privacy_secondary: !minor ? signatures.privacySecondParty || undefined : undefined, signature_board: signatures.board || undefined });
  const validate = () => { if (minor) validateLegalRepresentative(draft); };
  const preparedDraft = (): PreparedMembershipApplicationDraft => ({ schema_version: preparedContractSchemaVersion, document_type: "mitgliedsantrag", memberId, startDate, memberFee: Number(fee.replace(",", ".")), representative: minor ? draft : null, includeSecondaryMember: !minor && includeSecondaryMember });
  const savePrepared = async () => {
    if (!resumeContext) return null;
    const updated = await savePreparedContractIfChanged(club, session, session.user.id, resumeContext, preparedDraft());
    setResumeContext(updated);
    if (updated !== resumeContext) setMessage("Die Änderungen am vorbereiteten Vertrag wurden gespeichert.");
    return updated;
  };
  const beginSignatures = async () => {
    try {
      validate(); setBusy(true); setMessage("");
      let context = resumeContext;
      if (context?.status === "vorbereitet") context = await savePrepared() ?? context;
      if (!context) { const parked = await parkPreparedContract(club, session, session.user.id, preparedDraft()); context = await loadPreparedContractForEditing(club, session, parked.id, session.user.id, "mitgliedsantrag"); }
      const started = context.status === "zur_signatur" ? context : await startPreparedContractSigning(club, session, context, session.user.id);
      setResumeContext(started); setSignatures({}); setSignatureIndex(0); setStep("signatures");
    } catch (error) { setMessage(error instanceof Error ? error.message : "Die Signatur konnte nicht gestartet werden."); }
    finally { setBusy(false); }
  };
  const completeWorkflow = async () => {
    if (!resumeContext || resumeContext.status !== "zur_signatur") return true;
    try { await completePreparedContractSigning(club, session, resumeContext.id, resumeContext.revision, session.user.id); return true; }
    catch { setFinalDocumentCreated(true); setMessage("Das Vertragsdokument wurde erfolgreich erstellt, der Kommunikationsvorgang konnte jedoch noch nicht abgeschlossen werden. Das Dokument darf nicht erneut erzeugt werden."); return false; }
  };
  const retryWorkflowCompletion = async () => { try { setBusy(true); if (await completeWorkflow()) { setFinalDocumentCreated(false); await onSaved(); setMessage("Der Kommunikationsvorgang wurde abgeschlossen."); } } finally { setBusy(false); } };

  const preview = async () => {
    try { validate(); setBusy(true); setMessage(""); await savePrepared(); const result = await generateContract(club, session, request("preview")); if (result.previewUrl) window.open(result.previewUrl, "_blank", "noopener,noreferrer"); setStep("preview"); }
    catch (error) { setMessage(error instanceof Error ? error.message : "Vorschau konnte nicht erstellt werden."); }
    finally { setBusy(false); }
  };
  const park = async () => { try { validate(); setBusy(true); if (resumeContext) { await savePrepared(); } else { await parkPreparedContract(club, session, session.user.id, preparedDraft()); setMessage("Der Mitgliedsantrag wurde zur Signatur vorbereitet und unter Kommunikation > Zur Signatur > Meine gespeichert."); } } catch (error) { setMessage(error instanceof Error ? error.message : "Der Vorgang konnte nicht vorbereitet werden."); } finally { setBusy(false); } };

  const finalize = async () => {
    try {
      validate();
      const signatureSteps = buildMembershipApplicationSignatureSteps({ isMinor: minor, includeSecondaryMember });
      if (!canFinalizeMembershipApplication(step, signatureSteps, signatures)) throw new Error("Alle erforderlichen Signaturen müssen nach der Vorschau einzeln erfasst werden.");
      setBusy(true); setMessage("");
      const result = await generateContract(club, session, request("finalize"));
      if (!await completeWorkflow()) return;
      setSignatures({}); setSignatureIndex(0); setStep("edit");
      try {
        const refreshed = await loadMembershipApplicationData(club, session, memberId);
        setData(refreshed);
        await onSaved();
        setMessage(result.message ?? "Der signierte Mitgliedsantrag wurde gespeichert.");
      } catch {
        setData((current) => current ? { ...current, status: "signed" } : current);
        setMessage("Der Mitgliedsantrag wurde gespeichert, die Ansicht konnte aber nicht vollständig aktualisiert werden.");
      }
    } catch (error) { setMessage(error instanceof Error ? error.message : "Mitgliedsantrag konnte nicht gespeichert werden."); }
    finally { setBusy(false); }
  };

  if (!data) return <p className="detail-hint">Mitgliedsantrag wird geladen …</p>;
  if (data.member.hauptmitglied_id !== null) return <section className="contract-composer"><p className="detail-hint">Für Nebenmitglieder kann kein eigener Mitgliedsantrag erstellt werden. Die Mitunterzeichnung beim Mitgliedsantrag des Hauptmitglieds bleibt möglich.</p></section>;
  const annualMemberFee = Number(fee.replace(",", "."));
  const contribution = Number.isFinite(annualMemberFee) ? calculateMembershipApplicationContribution(annualMemberFee, startDate, data.season.jahr) : null;
  const signatureSteps = buildMembershipApplicationSignatureSteps({ isMinor: minor, includeSecondaryMember });
  const currentSignatureStep = signatureSteps[signatureIndex] ?? null;
  const signaturesComplete = hasCompletedMembershipApplicationSignatures(signatureSteps, signatures);
  const advanceSignature = () => { if (!currentSignatureStep || !signatures[currentSignatureStep.key]) return; setSignatureIndex((current) => Math.min(current + 1, signatureSteps.length)); };

  return <section className="contract-composer">
    {message && <p className="notice" role="status">{message}</p>}
    {finalDocumentCreated && <div className="editor-actions"><button disabled={busy} onClick={() => void retryWorkflowCompletion()}>Vorgang erneut abschließen</button></div>}
    {step === "edit" && <>
      <fieldset disabled={busy}>
        <label>Beginn<span className="detail-hint">{new Intl.DateTimeFormat("de-DE").format(new Date(`${startDate}T12:00:00`))}</span></label>
        <label>Jahresbeitrag laut Saison<span className="detail-hint">{data.annualMemberFee.toFixed(2)} €</span></label>
        <label>Mitgliedsbeitrag jährlich<input inputMode="decimal" value={fee} onChange={(event) => setFee(event.target.value)} /></label>
        <p className="detail-hint wide">{contribution ? `Aufnahmejahr: ${contribution.proratedMemberFee.toFixed(2)} € anteiliger Beitrag für ${contribution.contributionMonths} Monate.` : "Bitte einen gültigen Jahresbeitrag eingeben."}</p>
        {minor && <LegalRepresentativeFields draft={draft} options={options} onChange={setDraft} />}
        {!minor && data.secondaryMember && <label className="wide"><input type="checkbox" checked={includeSecondaryMember} onChange={(event) => setIncludeSecondaryMember(event.target.checked)} /> {`${data.secondaryMember.vorname ?? ""} ${data.secondaryMember.name ?? ""}`.trim() || "Nebenmitglied"} als zweite private Partei mitunterzeichnen lassen</label>}
      </fieldset>
      <div className="editor-actions"><button disabled={busy || data.status !== "none" || !contribution} onClick={preview}>Vorschau</button></div>
    </>}
    {step === "preview" && <ContractPreview onBack={() => setStep("edit")} onPark={() => void park()} parkLabel={resumeContext ? "Änderungen speichern" : undefined} onContinue={() => { if (!canStartMembershipApplicationSignatures(step)) return; void beginSignatures(); }} />}
    {step === "signatures" && <section className="signature-grid">
      {currentSignatureStep ? <>
        <p className="detail-hint wide">Signatur {signatureIndex + 1} von {signatureSteps.length}: {currentSignatureStep.label}</p>
        <SignaturePad key={currentSignatureStep.key} label={currentSignatureStep.label} value={signatures[currentSignatureStep.key] ?? ""} onChange={(value) => setSignatures((current) => ({ ...current, [currentSignatureStep.key]: value }))} />
        <div className="editor-actions"><button disabled={busy || !signatures[currentSignatureStep.key]} onClick={advanceSignature}>{signatureIndex + 1 === signatureSteps.length ? "Signatur übernehmen" : "Speichern und Weiter"}</button></div>
      </> : <>
        <p className="detail-hint wide">Alle {signatureSteps.length} erforderlichen Signaturen wurden erfasst.</p>
        <div className="editor-actions"><button disabled={busy || !signaturesComplete} onClick={finalize}>{busy ? "Verarbeitet …" : "Signiertes Dokument speichern"}</button></div>
      </>}
    </section>}
  </section>;
}
