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
import { discardPreparedContractWorkflow, loadPreparedContractForEditing, loadPreparedContractForFinalReview, loadPreparedContractForSignature, markPreparedContractReadyForReview, parkPreparedContract, recoverPreparedContractFinalDocument, savePreparedContractIfChanged, startPreparedContractSigning, type PreparedContractResumeContext } from "../../services/communication/prepared-contract-service";
import { preparedContractSchemaVersion, type PreparedMembershipApplicationDraft } from "../../models/communication/prepared-contract-draft";
import { PreparedFinalReview } from "./PreparedFinalReview";
import { MemberClubDocuments } from "../members/MemberClubDocuments";
import { getMemberClubDocumentStatus } from "../../services/club-documents/member-club-document-status-service";
import { createMemberClubDocumentSnapshot, formatMemberClubDocumentSnapshotForPdf, isCompleteMemberClubDocumentSnapshot, memberClubDocumentSnapshotsEqual, type MemberClubDocumentSnapshotItem } from "../../services/contracts/member-club-document-snapshot-service";

export function MembershipApplicationFlow({ session, memberId, onSaved, onPrepared, onCancel, preparedContractId, resumeMode = "edit" }: { session: BrowserSession; memberId: number; onSaved: () => Promise<void>; onPrepared?: () => Promise<void> | void; onCancel?: () => void; preparedContractId?: number; resumeMode?: "edit" | "signatures" | "review" }) {
  const club = useSelectedClub();
  const [data, setData] = useState<MembershipApplicationData | null>(null);
  const [startDate, setStartDate] = useState("");
  const [fee, setFee] = useState("");
  const [draft, setDraft] = useState<LegalRepresentativeDraft>(emptyLegalRepresentativeDraft());
  const [options, setOptions] = useState<LegalRepresentativeOption[]>([]);
  const [minor, setMinor] = useState(false);
  const [step, setStep] = useState<"edit" | "documents" | "preview" | "signatures" | "review">("edit");
  const [includeSecondaryMember, setIncludeSecondaryMember] = useState(false);
  const [signatures, setSignatures] = useState<Partial<Record<MembershipApplicationSignatureKey, string>>>({});
  const [signatureIndex, setSignatureIndex] = useState(0);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [resumeContext, setResumeContext] = useState<PreparedContractResumeContext | null>(null);
  const [finalDocumentId, setFinalDocumentId] = useState<number | null>(null);
  const [clubDocumentSnapshot, setClubDocumentSnapshot] = useState<MemberClubDocumentSnapshotItem[] | undefined>();

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
    const loadPrepared = resumeMode === "signatures" ? loadPreparedContractForSignature : resumeMode === "review" ? loadPreparedContractForFinalReview : loadPreparedContractForEditing;
    loadPrepared(club, session, preparedContractId, session.user.id, "mitgliedsantrag").then(async (context) => {
      if (!active) return;
      const saved = context.entwurf_daten as PreparedMembershipApplicationDraft;
      if (saved.memberId !== memberId) throw new Error("Der gespeicherte Mitgliedsantrag passt nicht zum Mitglied.");
      if (resumeMode === "signatures" && context.status === "zur_signatur") {
        const review = await recoverPreparedContractFinalDocument(club, session, context, session.user.id);
        if (review) {
          if (!active) return;
          setResumeContext(review); setStartDate(saved.startDate); setFee(String(saved.memberFee)); setDraft(saved.representative ?? emptyLegalRepresentativeDraft()); setIncludeSecondaryMember(saved.includeSecondaryMember); setSignatures({}); setSignatureIndex(0); setStep("review"); setMessage("Das bereits erstellte finale Dokument wurde zur letzten Prüfung übernommen."); await onSaved(); return;
        }
      }
      setResumeContext(context); setClubDocumentSnapshot(saved.vereinsunterlagen_nachweis); setStartDate(saved.startDate); setFee(String(saved.memberFee)); setDraft(saved.representative ?? emptyLegalRepresentativeDraft()); setIncludeSecondaryMember(saved.includeSecondaryMember); setSignatures({}); setSignatureIndex(0); setStep(resumeMode === "signatures" ? "signatures" : resumeMode === "review" ? "review" : "edit");
    }).catch((error) => active && setMessage(error instanceof Error ? error.message : "Der vorbereitete Mitgliedsantrag konnte nicht geöffnet werden."));
    return () => { active = false; };
  }, [club, session, preparedContractId, memberId, data, resumeMode]);

  const request = (action: "preview" | "finalize", snapshot = clubDocumentSnapshot) => ({ action, type: "mitgliedsantrag" as const, member_id: memberId, start_date: startDate, member_fee: Number(fee.replace(",", ".")), admission_fee: data?.admissionFee, club_documents_snapshot: snapshot ? formatMemberClubDocumentSnapshotForPdf(snapshot) : undefined, ...(action === "finalize" && resumeContext ? { source_kommunikationsvorgang_id: resumeContext.id, source_revision: resumeContext.revision } : {}), representative: minor ? { mode: draft.mode, member_id: draft.memberId ?? undefined, vorname: draft.vorname, nachname: draft.nachname, adresse_abweichend: draft.adresseAbweichend, adresse: draft.adresse, plz: draft.plz, ort: draft.ort } : undefined, include_secondary_member: !minor && includeSecondaryMember, signature_application_member: signatures.applicationMember || undefined, signature_privacy_member: signatures.privacyMember || undefined, signature_application_representative: minor ? signatures.applicationSecondParty || undefined : undefined, signature_privacy_representative: minor ? signatures.privacySecondParty || undefined : undefined, signature_application_secondary: !minor ? signatures.applicationSecondParty || undefined : undefined, signature_privacy_secondary: !minor ? signatures.privacySecondParty || undefined : undefined, signature_board: signatures.board || undefined });
  const validate = () => { if (minor) validateLegalRepresentative(draft); };
  const preparedDraft = (snapshot = clubDocumentSnapshot): PreparedMembershipApplicationDraft => ({ schema_version: preparedContractSchemaVersion, document_type: "mitgliedsantrag", memberId, startDate, memberFee: Number(fee.replace(",", ".")), representative: minor ? draft : null, includeSecondaryMember: !minor && includeSecondaryMember, vereinsunterlagen_nachweis: snapshot });
  const loadCurrentClubDocumentStatus = async () => { const now = new Date(); const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`; return getMemberClubDocumentStatus(club, session, memberId, today); };
  const refreshClubDocumentSnapshot = async () => { const snapshot = createMemberClubDocumentSnapshot(await loadCurrentClubDocumentStatus()); setClubDocumentSnapshot(snapshot); return snapshot; };
  const savePrepared = async (snapshot = clubDocumentSnapshot) => {
    if (!resumeContext) return null;
    const updated = await savePreparedContractIfChanged(club, session, session.user.id, resumeContext, preparedDraft(snapshot));
    setResumeContext(updated);
    if (updated !== resumeContext) setMessage("Die Änderungen am vorbereiteten Vertrag wurden gespeichert.");
    return updated;
  };
  const beginSignatures = async () => {
    try {
      validate(); setBusy(true); setMessage("");
      let context = resumeContext;
      if (!context || !isCompleteMemberClubDocumentSnapshot((context.entwurf_daten as PreparedMembershipApplicationDraft).vereinsunterlagen_nachweis)) throw new Error("Der vorbereitete Mitgliedsantrag enthält noch keinen Vereinsunterlagen-Snapshot. Bitte erstellen Sie zuerst eine neue Vorschau.");
      const currentSnapshot = await refreshClubDocumentSnapshot();
      if (!memberClubDocumentSnapshotsEqual((context.entwurf_daten as PreparedMembershipApplicationDraft).vereinsunterlagen_nachweis, currentSnapshot)) { await savePrepared(currentSnapshot); throw new Error("Die Pflichtunterlagen haben sich seit der Vorschau geändert. Der aktualisierte Entwurf wurde gespeichert; bitte erstellen Sie eine neue Vorschau."); }
      if (context?.status === "vorbereitet") context = await savePrepared() ?? context;
      if (!context) throw new Error("Die Signatur kann nur aus der Kommunikationszentrale gestartet werden.");
      const started = context.status === "zur_signatur" ? context : await startPreparedContractSigning(club, session, context, session.user.id);
      setResumeContext(started); setSignatures({}); setSignatureIndex(0); setStep("signatures");
    } catch (error) { setMessage(error instanceof Error ? error.message : "Die Signatur konnte nicht gestartet werden."); }
    finally { setBusy(false); }
  };
  const markFinalDocumentForReview = async (documentId: number) => {
    if (!resumeContext) throw new Error("Der Kommunikationsvorgang ist nicht geladen.");
    try {
      const review = await markPreparedContractReadyForReview(club, session, resumeContext, session.user.id, documentId);
      setResumeContext(review); setFinalDocumentId(null); setSignatures({}); setSignatureIndex(0); setStep("review"); await onSaved();
    } catch (error) {
      setFinalDocumentId(documentId);
      setMessage("Das Vertragsdokument wurde erfolgreich erstellt, konnte aber noch nicht zur letzten Prüfung vorgemerkt werden. Das Dokument wird nicht erneut erzeugt.");
      throw error;
    }
  };
  const retryFinalReviewPersistence = async () => { if (finalDocumentId === null) return; try { setBusy(true); setMessage(""); await markFinalDocumentForReview(finalDocumentId); } catch { /* Die gespeicherte Dokument-ID bleibt für einen sicheren Wiederholungsversuch erhalten. */ } finally { setBusy(false); } };
  const discard = async () => { if (!resumeContext || !window.confirm("Vorgang wirklich verwerfen? Alle bisherigen Eingaben und Unterschriften dieses Vorgangs gehen verloren. Bereits versendete Vereinsunterlagen bleiben erhalten.")) return; try { setBusy(true); await discardPreparedContractWorkflow(club, session, resumeContext); await onSaved(); onCancel?.(); } catch (error) { setMessage(error instanceof Error ? error.message : "Der Vorgang konnte nicht verworfen werden."); } finally { setBusy(false); } };

  const preview = async () => {
    try { validate(); setBusy(true); setMessage(""); const status = await loadCurrentClubDocumentStatus(); let snapshot: MemberClubDocumentSnapshotItem[]; try { snapshot = createMemberClubDocumentSnapshot(status); } catch (error) { setStep("documents"); setMessage(error instanceof Error ? error.message : "Die Pflichtunterlagen sind unvollständig."); return; } setClubDocumentSnapshot(snapshot); if (resumeContext) await savePrepared(snapshot); else await parkPreparedContract(club, session, session.user.id, preparedDraft(snapshot)); const result = await generateContract(club, session, request("preview", snapshot)); if (result.previewUrl) window.open(result.previewUrl, "_blank", "noopener,noreferrer"); setStep("preview"); }
    catch (error) { setMessage(error instanceof Error ? error.message : "Vorschau konnte nicht erstellt werden."); }
    finally { setBusy(false); }
  };
  const park = async () => { try { validate(); setBusy(true); if (resumeContext) { await savePrepared(); setMessage("Die Änderungen am vorbereiteten Mitgliedsantrag wurden gespeichert."); } else { await parkPreparedContract(club, session, session.user.id, preparedDraft()); setMessage("Der Mitgliedsantrag wurde vorbereitet und unter Kommunikation > Zur Signatur > Meine gespeichert."); await onPrepared?.(); } } catch (error) { setMessage(error instanceof Error ? error.message : "Der Vorgang konnte nicht vorbereitet werden."); } finally { setBusy(false); } };

  const finalize = async () => {
    try {
      validate();
      const signatureSteps = buildMembershipApplicationSignatureSteps({ isMinor: minor, includeSecondaryMember, memberId, representativeMemberId: draft.mode === "existing" ? draft.memberId ?? undefined : undefined });
      if (!canFinalizeMembershipApplication(step, signatureSteps, signatures)) throw new Error("Alle erforderlichen Signaturen müssen nach der Vorschau einzeln erfasst werden.");
      setBusy(true); setMessage("");
      const result = await generateContract(club, session, request("finalize"));
      if (!result.documentId || result.documentId <= 0) throw new Error("Der Finalize-Vorgang hat keine gültige Dokument-ID zurückgegeben.");
      await markFinalDocumentForReview(result.documentId);
      setMessage(result.message ?? "Der signierte Mitgliedsantrag ist zur letzten Prüfung bereit.");
    } catch (error) { setMessage(error instanceof Error ? error.message : "Mitgliedsantrag konnte nicht gespeichert werden."); }
    finally { setBusy(false); }
  };

  if (!data) return <p className="detail-hint">Mitgliedsantrag wird geladen …</p>;
  if (data.member.hauptmitglied_id !== null) return <section className="contract-composer"><p className="detail-hint">Für Nebenmitglieder kann kein eigener Mitgliedsantrag erstellt werden. Die Mitunterzeichnung beim Mitgliedsantrag des Hauptmitglieds bleibt möglich.</p></section>;
  const annualMemberFee = Number(fee.replace(",", "."));
  const contribution = Number.isFinite(annualMemberFee) ? calculateMembershipApplicationContribution(annualMemberFee, startDate, data.season.jahr) : null;
  const signatureSteps = buildMembershipApplicationSignatureSteps({ isMinor: minor, includeSecondaryMember, memberId, representativeMemberId: draft.mode === "existing" ? draft.memberId ?? undefined : undefined });
  const currentSignatureStep = signatureSteps[signatureIndex] ?? null;
  const signaturesComplete = hasCompletedMembershipApplicationSignatures(signatureSteps, signatures);
  const advanceSignature = () => { if (!currentSignatureStep || !signatures[currentSignatureStep.key]) return; setSignatureIndex((current) => Math.min(current + 1, signatureSteps.length)); };

  return <section className="contract-composer">
    {message && <p className="notice" role="status">{message}</p>}
    {finalDocumentId !== null && <div className="editor-actions"><button disabled={busy} onClick={() => void retryFinalReviewPersistence()}>Finales Dokument zur Prüfung vormerken</button></div>}
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
    {step === "documents" && <><p className="notice">Die Vorschau bleibt gesperrt, bis alle aktuellen Pflichtunterlagen bereitgestellt sind.</p><MemberClubDocuments session={session} member={data.member} onStatusUpdated={(status) => { if (status.complete) { setMessage("Pflichtunterlagen sind vollständig. Bitte erstellen Sie jetzt die Vorschau."); setStep("edit"); } }} /><div className="editor-actions"><button className="secondary-action" onClick={() => setStep("edit")}>Zurück zum Mitgliedsantrag</button></div></>}
    {step === "preview" && <><ContractPreview onBack={() => setStep("edit")} onPark={() => void park()} parkLabel={resumeContext ? "Änderungen speichern" : "Zur Signatur vorbereiten"} {...(preparedContractId ? { onContinue: () => { if (!canStartMembershipApplicationSignatures(step)) return; void beginSignatures(); } } : {})} />{resumeContext && <div className="editor-actions"><button className="secondary-action" disabled={busy} onClick={() => void discard()}>Verwerfen</button></div>}</>}
    {step === "signatures" && <section className="signature-grid">
      {currentSignatureStep ? <>
        <p className="detail-hint wide">Signatur {signatureIndex + 1} von {signatureSteps.length}: {currentSignatureStep.label}</p>
        <SignaturePad key={currentSignatureStep.key} label={currentSignatureStep.label} value={signatures[currentSignatureStep.key] ?? ""} onChange={(value) => setSignatures((current) => ({ ...current, [currentSignatureStep.key]: value }))} />
        <div className="editor-actions"><button disabled={busy || !signatures[currentSignatureStep.key]} onClick={advanceSignature}>{signatureIndex + 1 === signatureSteps.length ? "Signatur übernehmen" : "Speichern und Weiter"}</button></div>
      </> : <>
        <p className="detail-hint wide">Alle {signatureSteps.length} erforderlichen Signaturen wurden erfasst.</p>
        <div className="editor-actions"><button disabled={busy || !signaturesComplete} onClick={finalize}>{busy ? "Verarbeitet …" : "Signiertes Dokument speichern"}</button></div>
      </>}
    {resumeContext && <div className="editor-actions"><button className="secondary-action" disabled={busy} onClick={() => void discard()}>Verwerfen</button></div>}</section>}
    {step === "review" && resumeContext && <PreparedFinalReview session={session} context={resumeContext} onCompleted={async () => { await onSaved(); onCancel?.(); }} />}
  </section>;
}
