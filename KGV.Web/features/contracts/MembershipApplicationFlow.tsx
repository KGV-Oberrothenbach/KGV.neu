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

export function MembershipApplicationFlow({ session, memberId, onSaved }: { session: BrowserSession; memberId: number; onSaved: () => Promise<void> }) {
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
      setMinor(value.isMinor); setOptions(value.options); setDraft(value.draft);
    }).catch((error) => active && setMessage(error instanceof Error ? error.message : "Vertreter konnte nicht vorbereitet werden."));
    return () => { active = false; };
  }, [session, data, startDate, club]);

  const request = (action: "preview" | "finalize") => ({ action, type: "mitgliedsantrag" as const, member_id: memberId, start_date: startDate, member_fee: Number(fee.replace(",", ".")), admission_fee: data?.admissionFee, representative: minor ? { mode: draft.mode, member_id: draft.memberId ?? undefined, vorname: draft.vorname, nachname: draft.nachname, adresse_abweichend: draft.adresseAbweichend, adresse: draft.adresse, plz: draft.plz, ort: draft.ort } : undefined, include_secondary_member: !minor && includeSecondaryMember, signature_application_member: signatures.applicationMember || undefined, signature_privacy_member: signatures.privacyMember || undefined, signature_application_representative: minor ? signatures.applicationSecondParty || undefined : undefined, signature_privacy_representative: minor ? signatures.privacySecondParty || undefined : undefined, signature_application_secondary: !minor ? signatures.applicationSecondParty || undefined : undefined, signature_privacy_secondary: !minor ? signatures.privacySecondParty || undefined : undefined, signature_board: signatures.board || undefined });
  const validate = () => { if (minor) validateLegalRepresentative(draft); };

  const preview = async () => {
    try { validate(); setBusy(true); setMessage(""); const result = await generateContract(club, session, request("preview")); if (result.previewUrl) window.open(result.previewUrl, "_blank", "noopener,noreferrer"); setStep("preview"); }
    catch (error) { setMessage(error instanceof Error ? error.message : "Vorschau konnte nicht erstellt werden."); }
    finally { setBusy(false); }
  };

  const finalize = async () => {
    try {
      validate();
      const signatureSteps = buildMembershipApplicationSignatureSteps({ isMinor: minor, includeSecondaryMember });
      if (!canFinalizeMembershipApplication(step, signatureSteps, signatures)) throw new Error("Alle erforderlichen Signaturen müssen nach der Vorschau einzeln erfasst werden.");
      setBusy(true); setMessage("");
      const result = await generateContract(club, session, request("finalize"));
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
    {step === "preview" && <ContractPreview onBack={() => setStep("edit")} onContinue={() => { if (!canStartMembershipApplicationSignatures(step)) return; setSignatures({}); setSignatureIndex(0); setStep("signatures"); }} />}
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
