"use client";
import { useSelectedClub } from "../auth/AuthProvider";

import { useEffect, useState } from "react";
import { SignaturePad } from "../../components/signature/SignaturePad";
import type { BrowserSession } from "../../lib/supabase-auth";
import { emptyLegalRepresentativeDraft, loadLegalRepresentativeContext, validateLegalRepresentative, type LegalRepresentativeDraft, type LegalRepresentativeOption } from "../../services/contracts/legal-representative-service";
import { loadMembershipApplicationData, type MembershipApplicationData } from "../../services/contracts/membership-application-service";
import { generateContract } from "../../services/contracts/contract-generation-service";
import { ContractPreview } from "./ContractPreview";
import { LegalRepresentativeFields } from "./LegalRepresentativeFields";

export function MembershipApplicationFlow({ session, memberId, onSaved }: { session: BrowserSession; memberId: number; onSaved: () => Promise<void> }) {
  const club = useSelectedClub();
  const [data, setData] = useState<MembershipApplicationData | null>(null);
  const [startDate, setStartDate] = useState("");
  const [fee, setFee] = useState("");
  const [admission, setAdmission] = useState("");
  const [draft, setDraft] = useState<LegalRepresentativeDraft>(emptyLegalRepresentativeDraft());
  const [options, setOptions] = useState<LegalRepresentativeOption[]>([]);
  const [minor, setMinor] = useState(false);
  const [step, setStep] = useState<"edit" | "preview" | "signatures">("edit");
  const [applicationSignature, setApplicationSignature] = useState("");
  const [privacySignature, setPrivacySignature] = useState("");
  const [representativeApplicationSignature, setRepresentativeApplicationSignature] = useState("");
  const [representativePrivacySignature, setRepresentativePrivacySignature] = useState("");
  const [includeSecondaryMember, setIncludeSecondaryMember] = useState(false);
  const [secondaryApplicationSignature, setSecondaryApplicationSignature] = useState("");
  const [secondaryPrivacySignature, setSecondaryPrivacySignature] = useState("");
  const [boardSignature, setBoardSignature] = useState("");
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");

  useEffect(() => {
    let active = true;
    loadMembershipApplicationData(club, session, memberId).then((value) => {
      if (!active) return;
      setData(value); setStartDate(value.startDate); setFee(String(value.annualMemberFee)); setAdmission(String(value.admissionFee));
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

  const request = (action: "preview" | "finalize") => ({ action, type: "mitgliedsantrag" as const, member_id: memberId, start_date: startDate, member_fee: Number(fee.replace(",", ".")), admission_fee: Number(admission.replace(",", ".")), representative: minor ? { mode: draft.mode, member_id: draft.memberId ?? undefined, vorname: draft.vorname, nachname: draft.nachname, adresse_abweichend: draft.adresseAbweichend, adresse: draft.adresse, plz: draft.plz, ort: draft.ort } : undefined, include_secondary_member: !minor && includeSecondaryMember, signature_application_member: applicationSignature || undefined, signature_privacy_member: privacySignature || undefined, signature_application_representative: representativeApplicationSignature || undefined, signature_privacy_representative: representativePrivacySignature || undefined, signature_application_secondary: secondaryApplicationSignature || undefined, signature_privacy_secondary: secondaryPrivacySignature || undefined, signature_board: boardSignature || undefined });
  const validate = () => { if (minor) validateLegalRepresentative(draft); };

  const preview = async () => {
    try { validate(); setBusy(true); setMessage(""); const result = await generateContract(club, session, request("preview")); if (result.previewUrl) window.open(result.previewUrl, "_blank", "noopener,noreferrer"); setStep("preview"); }
    catch (error) { setMessage(error instanceof Error ? error.message : "Vorschau konnte nicht erstellt werden."); }
    finally { setBusy(false); }
  };

  const finalize = async () => {
    try {
      validate();
      if (!applicationSignature || !privacySignature || !boardSignature || (minor && (!representativeApplicationSignature || !representativePrivacySignature)) || (!minor && includeSecondaryMember && (!secondaryApplicationSignature || !secondaryPrivacySignature))) throw new Error("Alle erforderlichen Signaturen müssen erfasst werden.");
      setBusy(true); setMessage("");
      const result = await generateContract(club, session, request("finalize"));
      setApplicationSignature(""); setPrivacySignature(""); setRepresentativeApplicationSignature(""); setRepresentativePrivacySignature(""); setSecondaryApplicationSignature(""); setSecondaryPrivacySignature(""); setBoardSignature(""); setStep("edit");
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
  return <section className="contract-composer">{message && <p className="notice" role="status">{message}</p>}{step !== "signatures" && <fieldset disabled={busy}><label>Beginn<input type="date" value={startDate} onChange={(event) => setStartDate(event.target.value)} /></label><label>Mitgliedsbeitrag jährlich<input inputMode="decimal" value={fee} onChange={(event) => setFee(event.target.value)} /></label><label>Aufnahmegebühr<input inputMode="decimal" value={admission} onChange={(event) => setAdmission(event.target.value)} /></label><p className="detail-hint wide">{data.status === "signed" ? "Signierter Mitgliedsantrag vorhanden." : data.status === "unsigned" ? "Unsignierter Mitgliedsantrag vorhanden." : "Kein Mitgliedsantrag vorhanden."}</p>{minor && <LegalRepresentativeFields draft={draft} options={options} onChange={setDraft} />}{!minor && data.secondaryMember && <label className="wide"><input type="checkbox" checked={includeSecondaryMember} onChange={(event) => setIncludeSecondaryMember(event.target.checked)} /> {`${data.secondaryMember.vorname ?? ""} ${data.secondaryMember.name ?? ""}`.trim() || "Nebenmitglied"} als zweite private Partei mitunterzeichnen lassen</label>}</fieldset>}{step === "edit" && <div className="editor-actions"><button disabled={busy || data.status !== "none"} onClick={preview}>PDF-Vorschau</button></div>}{step === "preview" && <ContractPreview onBack={() => setStep("edit")} onContinue={() => setStep("signatures")} />}{step === "signatures" && <><div className="signature-grid"><SignaturePad label="Antragsteller/in – Mitgliedsantrag" value={applicationSignature} onChange={setApplicationSignature} />{minor && <SignaturePad label="Gesetzliche/r Vertreter/in – Mitgliedsantrag" value={representativeApplicationSignature} onChange={setRepresentativeApplicationSignature} />}{!minor && includeSecondaryMember && <SignaturePad label="Nebenmitglied – Mitgliedsantrag" value={secondaryApplicationSignature} onChange={setSecondaryApplicationSignature} />}<SignaturePad label="Für den Verein" value={boardSignature} onChange={setBoardSignature} /><SignaturePad label="Antragsteller/in – Datenschutzerklärung" value={privacySignature} onChange={setPrivacySignature} />{minor && <SignaturePad label="Gesetzliche/r Vertreter/in – Datenschutzerklärung" value={representativePrivacySignature} onChange={setRepresentativePrivacySignature} />}{!minor && includeSecondaryMember && <SignaturePad label="Nebenmitglied – Datenschutzerklärung" value={secondaryPrivacySignature} onChange={setSecondaryPrivacySignature} />}</div><div className="editor-actions"><button className="secondary-action" disabled={busy} onClick={() => setStep("edit")}>Zurück zur Bearbeitung</button><button disabled={busy} onClick={finalize}>{busy ? "Verarbeitet …" : "Signiert sicher ablegen"}</button></div></>}</section>;
}
