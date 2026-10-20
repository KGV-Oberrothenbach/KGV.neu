"use client";
import { useEffect, useState } from "react";
import { useSelectedClub } from "../auth/AuthProvider";
import { SignaturePad } from "../../components/signature/SignaturePad";
import type { BrowserSession } from "../../lib/supabase-auth";
import { emptyLeaseContractDraft, loadLeaseContractData, loadLeaseContractDocumentState, openLeaseContractDocument, prepareLeaseContractRequest, type LeaseContractData, type LeaseContractDocumentState, type LeaseContractDraft } from "../../services/contracts/lease-service";
import { generateContract } from "../../services/contracts/contract-generation-service";
import { LeaseContractPreview } from "./LeaseContractPreview";
import { buildLeaseContractSignatureSteps, hasCompletedLeaseContractSignatures, type LeaseContractSignatureKey } from "../../services/contracts/lease-contract-flow-service";
import { discardPreparedContractWorkflow, loadPreparedContractForEditing, loadPreparedContractForFinalReview, loadPreparedContractForSignature, markPreparedContractReadyForReview, parkPreparedContract, recoverPreparedContractFinalDocument, savePreparedContractIfChanged, startPreparedContractSigning, type PreparedContractResumeContext } from "../../services/communication/prepared-contract-service";
import { preparedContractSchemaVersion, type PreparedLeaseContractDraft } from "../../models/communication/prepared-contract-draft";
import { PreparedFinalReview } from "./PreparedFinalReview";
import { getMemberClubDocumentStatus } from "../../services/club-documents/member-club-document-status-service";
import { createMemberClubDocumentSnapshot, formatMemberClubDocumentSnapshotForPdf, isCompleteMemberClubDocumentSnapshot, memberClubDocumentSnapshotsEqual, type MemberClubDocumentSnapshotItem } from "../../services/contracts/member-club-document-snapshot-service";
import { getActiveLeaseRelationshipStatus } from "../../repositories/contracts/lease-relationship-repository";
const name = (m: {
    vorname: string | null;
    name: string | null;
    id: number;
}) => [m.vorname, m.name].filter(Boolean).join(" ") || `Mitglied #${m.id}`;
export function LeaseContractFlow({ session, memberId, parcelId, startDate, onSaved, onCancel, preparedContractId, resumeMode = "edit" }: {
    session: BrowserSession;
    memberId: number;
    parcelId: number;
    startDate: string;
    onSaved: () => Promise<void>;
    onCancel: () => void;
    preparedContractId?: number;
    resumeMode?: "edit" | "signatures" | "review";
}) {
    const club = useSelectedClub();
    const [draft, setDraft] = useState<LeaseContractDraft>(() => ({ ...emptyLeaseContractDraft(startDate), parcelId }));
    const [data, setData] = useState<LeaseContractData | null>(null);
    const [documentState, setDocumentState] = useState<LeaseContractDocumentState | null>(null);
    const [message, setMessage] = useState("");
    const [step, setStep] = useState<"edit" | "preview" | "signatures" | "review">("edit");
    const [previewUrl, setPreviewUrl] = useState<string | null>(null);
    const [signatures, setSignatures] = useState<Partial<Record<LeaseContractSignatureKey, string>>>({});
    const [signatureIndex, setSignatureIndex] = useState(0);
    const [busy, setBusy] = useState(false);
    const [opening, setOpening] = useState(false);
    const [resumeContext, setResumeContext] = useState<PreparedContractResumeContext | null>(null);
    const [finalDocumentId, setFinalDocumentId] = useState<number | null>(null);
    const [clubDocumentSnapshot, setClubDocumentSnapshot] = useState<MemberClubDocumentSnapshotItem[] | undefined>();
    const clearSignatures = () => { setSignatures({}); setSignatureIndex(0); };
    useEffect(() => { let active = true; loadLeaseContractDocumentState(club, session, parcelId).then(async (state) => { if (!active)
        return; setDocumentState(state); const value = await loadLeaseContractData(club, session, memberId, parcelId, startDate); if (!active)
        return; setData(value); if (!preparedContractId) setDraft(current => ({ ...current, includeSecondaryMember: value.isMinor ? false : Boolean(value.secondaryMember), secondaryMemberId: value.isMinor ? null : value.secondaryMember?.id ?? null })); }).catch(e => active && setMessage(e instanceof Error ? e.message : "Pachtvertragsbasis konnte nicht geladen werden.")); return () => { active = false; }; }, [session, memberId, parcelId, startDate, club, preparedContractId]);
    useEffect(() => { if (!preparedContractId || !data)
        return; let active = true; const loadPrepared = resumeMode === "signatures" ? loadPreparedContractForSignature : resumeMode === "review" ? loadPreparedContractForFinalReview : loadPreparedContractForEditing; loadPrepared(club, session, preparedContractId, session.user.id, "pachtvertrag").then(async context => { if (!active)
        return; if (resumeMode === "signatures" && context.status === "zur_signatur") { const review = await recoverPreparedContractFinalDocument(club, session, context, session.user.id); if (review) { if (!active) return; setResumeContext(review); setStep("review"); setMessage("Das bereits erstellte finale Dokument wurde zur letzten Prüfung übernommen."); await onSaved(); return; } } const saved = context.entwurf_daten as PreparedLeaseContractDraft; if (saved.memberId !== memberId || saved.draft.parcelId !== parcelId)
        throw new Error("Der gespeicherte Pachtvertrag passt nicht zum Mitglied oder zur Parzelle."); const repairedDraft: LeaseContractDraft = { ...saved.draft, secondaryMemberId: saved.draft.includeSecondaryMember && !data.isMinor ? saved.draft.secondaryMemberId ?? data.secondaryMember?.id ?? null : null, effectiveDate: saved.draft.effectiveDate ?? null }; if (context.status === "vorbereitet" && (repairedDraft.secondaryMemberId !== saved.draft.secondaryMemberId || repairedDraft.effectiveDate !== saved.draft.effectiveDate)) context = await savePreparedContractIfChanged(club, session, session.user.id, context, { ...saved, draft: repairedDraft }); if (!active) return; setResumeContext(context); setDraft(repairedDraft); setClubDocumentSnapshot(saved.vereinsunterlagen_nachweis); clearSignatures(); setStep(resumeMode === "signatures" ? "signatures" : resumeMode === "review" ? "review" : "edit"); }).catch(e => active && setMessage(e instanceof Error ? e.message : "Der vorbereitete Pachtvertrag konnte nicht geöffnet werden.")); return () => { active = false; }; }, [club, session, preparedContractId, memberId, parcelId, data, resumeMode]);
    useEffect(() => () => { if (previewUrl)
        URL.revokeObjectURL(previewUrl); }, [previewUrl]);
    const statusMessage = documentState?.status === "signed" ? "Ein signierter Pachtvertrag ist bereits vorhanden." : documentState?.status === "unsigned" ? "Ein unsignierter Pachtvertrag ist bereits vorhanden." : "";
    const changeLeaseDraft = (next: Partial<LeaseContractDraft>) => { clearSignatures(); if (previewUrl)
        URL.revokeObjectURL(previewUrl); setPreviewUrl(null); setStep("edit"); setDraft(current => ({ ...current, ...next })); };
    const request = (action: "preview" | "finalize", snapshot: MemberClubDocumentSnapshotItem[] | undefined = resumeContext ? (resumeContext.entwurf_daten as PreparedLeaseContractDraft).vereinsunterlagen_nachweis : clubDocumentSnapshot) => { if (!data)
        throw new Error("Pachtvertragsbasis fehlt."); if (!isCompleteMemberClubDocumentSnapshot(snapshot))
        throw new Error("Der Vereinsunterlagen-Nachweis fehlt. Bitte erstellen Sie zuerst eine neue Vorschau."); return { action, type: "pachtvertrag" as const, ...prepareLeaseContractRequest(data, draft), club_documents_snapshot: formatMemberClubDocumentSnapshotForPdf(snapshot), signature_member: signatures.member || undefined, signature_secondary: signatures.secondary || undefined, signature_board: signatures.board || undefined }; };
    const preparedDraft = (snapshot: MemberClubDocumentSnapshotItem[] | undefined = clubDocumentSnapshot): PreparedLeaseContractDraft => ({ schema_version: preparedContractSchemaVersion, document_type: "pachtvertrag", memberId, draft, vereinsunterlagen_nachweis: snapshot });
    const createCurrentClubDocumentSnapshot = async () => {
        const today = new Date();
        const date = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;
        const snapshot = createMemberClubDocumentSnapshot(await getMemberClubDocumentStatus(club, session, memberId, date));
        setClubDocumentSnapshot(snapshot);
        return snapshot;
    };
    const savePrepared = async (snapshot: MemberClubDocumentSnapshotItem[] | undefined = clubDocumentSnapshot) => { if (!resumeContext)
        return null; const updated = await savePreparedContractIfChanged(club, session, session.user.id, resumeContext, preparedDraft(snapshot)); setResumeContext(updated); if (updated !== resumeContext)
        setMessage("Die Änderungen am vorbereiteten Vertrag wurden gespeichert."); return updated; };
    const beginSignatures = async () => { try {
        if (!data)
            return; setBusy(true); setMessage(""); let context = resumeContext; if (!context)
            throw new Error("Für die Signatur ist zuerst eine neue Vorschau erforderlich."); const relationship = await getActiveLeaseRelationshipStatus(club, session, parcelId); validateLeaseContractDraft({ ...data, leaseRelationship: relationship }, draft); const savedDraft = (context.entwurf_daten as PreparedLeaseContractDraft).draft; if (savedDraft.includeSecondaryMember && (!data.secondaryMember || savedDraft.secondaryMemberId !== data.secondaryMember.id)) throw new Error("Der vorbereitete Pachtvertrag enthält kein gültiges Nebenmitglied für Pächter 2. Bitte eine neue Vorschau erstellen."); const saved = (context.entwurf_daten as PreparedLeaseContractDraft).vereinsunterlagen_nachweis; if (!isCompleteMemberClubDocumentSnapshot(saved))
            throw new Error("Der gespeicherte Vereinsunterlagen-Nachweis fehlt. Bitte erstellen Sie eine neue Vorschau."); const current = await createCurrentClubDocumentSnapshot(); if (!memberClubDocumentSnapshotsEqual(saved, current)) { if (context.status === "vorbereitet")
                await savePrepared(current); throw new Error("Die Vereinsunterlagen haben sich seit der Vorschau geändert. Der Entwurf wurde aktualisiert; bitte erstellen Sie eine neue Vorschau."); }
        if (context.status === "vorbereitet") context = await savePrepared(saved) ?? context;
        const started = context.status === "zur_signatur" ? context : await startPreparedContractSigning(club, session, context, session.user.id); setResumeContext(started); clearSignatures(); setStep("signatures"); }
    catch (e) { setMessage(e instanceof Error ? e.message : "Die Signatur konnte nicht gestartet werden."); }
    finally { setBusy(false); } };
    const markFinalDocumentForReview = async (documentId: number) => { if (!resumeContext)
        throw new Error("Der Kommunikationsvorgang ist nicht geladen."); try { const review = await markPreparedContractReadyForReview(club, session, resumeContext, session.user.id, documentId); setResumeContext(review); setFinalDocumentId(null); clearSignatures(); setStep("review"); await onSaved(); }
    catch (error) { setFinalDocumentId(documentId); setMessage("Das Vertragsdokument wurde erfolgreich erstellt, konnte aber noch nicht zur letzten Prüfung vorgemerkt werden. Das Dokument wird nicht erneut erzeugt."); throw error; } };
    const retryFinalReviewPersistence = async () => { if (finalDocumentId === null)
        return; try { setBusy(true); setMessage(""); await markFinalDocumentForReview(finalDocumentId); }
    catch { /* Die gespeicherte Dokument-ID bleibt für einen sicheren Wiederholungsversuch erhalten. */ }
    finally { setBusy(false); } };
    const preview = async () => { try {
        if (!data)
            throw new Error("Pachtvertragsbasis fehlt.");
        validateLeaseContractDraft(data, draft);
        setBusy(true);
        setMessage("");
        const snapshot = await createCurrentClubDocumentSnapshot();
        let context = resumeContext;
        if (context) context = await savePrepared(snapshot) ?? context;
        else { const parked = await parkPreparedContract(club, session, session.user.id, preparedDraft(snapshot)); context = await loadPreparedContractForEditing(club, session, parked.id, session.user.id, "pachtvertrag"); setResumeContext(context); }
        const result = await generateContract(club, session, request("preview", snapshot));
        if (!result.previewUrl)
            throw new Error("Die PDF-Vorschau fehlt.");
        setPreviewUrl(result.previewUrl);
        setStep("preview");
    }
    catch (e) {
        setMessage(e instanceof Error ? e.message : "Vorschau konnte nicht erstellt werden.");
    }
    finally {
        setBusy(false);
    } };
    const discard = async () => { if (!resumeContext || !window.confirm("Vorgang wirklich verwerfen? Alle bisherigen Eingaben und Unterschriften dieses Vorgangs gehen verloren. Bereits versendete Vereinsunterlagen bleiben erhalten.")) return; try { setBusy(true); await discardPreparedContractWorkflow(club, session, resumeContext); await onSaved(); onCancel(); } catch (e) { setMessage(e instanceof Error ? e.message : "Der Vorgang konnte nicht verworfen werden."); } finally { setBusy(false); } };
    const park = async () => { try {
        if (!data)
            return;
        setBusy(true);
        if (resumeContext) {
            await savePrepared();
        }
        else {
            await parkPreparedContract(club, session, session.user.id, preparedDraft());
            setMessage("Der Pachtvertrag wurde zur Signatur vorbereitet und unter Kommunikation > Zur Signatur > Meine gespeichert.");
        }
    }
    catch (e) {
        setMessage(e instanceof Error ? e.message : "Der Vorgang konnte nicht vorbereitet werden.");
    }
    finally {
        setBusy(false);
    } };
    const finalize = async () => { try {
        const steps = buildLeaseContractSignatureSteps({ isMinor: Boolean(data?.isMinor), includeSecondaryMember: draft.includeSecondaryMember });
        if (step !== "signatures" || !hasCompletedLeaseContractSignatures(steps, signatures))
            throw new Error("Alle erforderlichen Signaturen müssen nach der Vorschau einzeln erfasst werden.");
        setBusy(true);
        setMessage("");
        const result = await generateContract(club, session, request("finalize"));
        if (!result.documentId || result.documentId <= 0)
            throw new Error("Der Finalize-Vorgang hat keine gültige Dokument-ID zurückgegeben.");
        await markFinalDocumentForReview(result.documentId);
        setMessage(result.message ?? "Der signierte Pachtvertrag ist zur letzten Prüfung bereit.");
    }
    catch (e) {
        setMessage(e instanceof Error ? e.message : "Pachtvertrag konnte nicht gespeichert werden.");
    }
    finally {
        setBusy(false);
    } };
    const openExisting = async () => { if (!documentState?.existingDocument) {
        setMessage("Für den Pachtvertrag fehlt eine sichere Ablage.");
        return;
    } try {
        setOpening(true);
        setMessage("");
        const url = await openLeaseContractDocument(club, session, documentState.existingDocument);
        if (!url)
            throw new Error("Pachtvertrag konnte nicht geöffnet werden.");
        window.open(url, "_blank", "noopener,noreferrer");
    }
    catch (e) {
        setMessage(e instanceof Error ? e.message : "Pachtvertrag konnte nicht geöffnet werden.");
    }
    finally {
        setOpening(false);
    } };
    if (finalDocumentId !== null)
        return <section className="contract-composer"><h3>Pachtvertrag</h3><p className="notice" role="status">{message}</p><button disabled={busy} onClick={() => void retryFinalReviewPersistence()}>Finales Dokument zur Prüfung vormerken</button></section>;
    if (step === "review" && resumeContext)
        return <PreparedFinalReview session={session} context={resumeContext} onCompleted={async () => { await onSaved(); onCancel(); }} />;
    const signatureSteps = buildLeaseContractSignatureSteps({ isMinor: Boolean(data?.isMinor), includeSecondaryMember: draft.includeSecondaryMember });
    const currentSignatureStep = signatureSteps[signatureIndex] ?? null;
    const advanceSignature = () => { if (!currentSignatureStep || !signatures[currentSignatureStep.key])
        return; setSignatureIndex(c => Math.min(c + 1, signatureSteps.length)); };
    return <section className="contract-composer"><h3>Pachtvertrag</h3>{(message || statusMessage) && <p className="notice" role="status">{message || statusMessage}</p>}<p className="detail-hint">Vertragsbeginn: {draft.startDate}</p>{step === "edit" && <>{data && <fieldset><legend>Altvertrag und Pächter</legend><label><input type="radio" checked={draft.hasPreviousContract === true} onChange={() => changeLeaseDraft({ hasPreviousContract: true, previousContractDate: draft.previousContractDate ?? draft.startDate })} /> Altvertrag vorhanden</label><label><input type="radio" checked={draft.hasPreviousContract === false} onChange={() => changeLeaseDraft({ hasPreviousContract: false, previousContractDate: null })} /> Kein Altvertrag</label>{draft.hasPreviousContract && <label>Datum des Altvertrags<input type="date" value={draft.previousContractDate ?? ""} onChange={event => changeLeaseDraft({ previousContractDate: event.target.value || null })} /></label>}{!data.isMinor && data.secondaryMember && <label className="wide"><input type="checkbox" checked={draft.includeSecondaryMember} onChange={event => changeLeaseDraft({ includeSecondaryMember: event.target.checked, secondaryMemberId: event.target.checked ? data.secondaryMember!.id : null })} /> {name(data.secondaryMember)} als Pächter/in 2 aufnehmen und unterschreiben lassen.</label>}</fieldset>}<fieldset><legend>Vertragsangaben</legend>{data?.leaseRelationship.relationshipId !== null && <label>Wirksam ab (bei Neufassung)<input type="date" value={draft.effectiveDate ?? ""} onChange={event => changeLeaseDraft({ effectiveDate: event.target.value || null })} /></label>}<label>Übergabedatum<input type="date" value={draft.handoverDate} onChange={event => changeLeaseDraft({ handoverDate: event.target.value })} /></label><label>Befristung bis<input type="date" value={draft.fixedUntil} onChange={event => changeLeaseDraft({ fixedUntil: event.target.value })} /></label><label>Gartenordnung / Fassung<input value={draft.gardenRulesVersion} onChange={event => changeLeaseDraft({ gardenRulesVersion: event.target.value })} /></label><label>Gartenordnung übergeben am<input type="date" value={draft.gardenRulesHandedOverAt} onChange={event => changeLeaseDraft({ gardenRulesHandedOverAt: event.target.value })} /></label><label className="wide">Zusätzliche Vereinbarungen<textarea value={draft.additionalAgreements} onChange={event => changeLeaseDraft({ additionalAgreements: event.target.value })} /></label><label><input type="checkbox" checked={draft.attachment1} onChange={event => changeLeaseDraft({ attachment1: event.target.checked })} /> Anlage 1</label><label><input type="checkbox" checked={draft.attachment2} onChange={event => changeLeaseDraft({ attachment2: event.target.checked })} /> Anlage 2</label><label><input type="checkbox" checked={draft.attachment3} onChange={event => changeLeaseDraft({ attachment3: event.target.checked })} /> Anlage 3</label><label><input type="checkbox" checked={draft.attachment4} onChange={event => changeLeaseDraft({ attachment4: event.target.checked })} /> Anlage 4</label><label>Vereinsunterzeichner<input value={draft.clubSignatory} onChange={event => changeLeaseDraft({ clubSignatory: event.target.value })} /></label></fieldset></>}<div className="editor-actions"><button type="button" className="secondary-action" disabled={busy} onClick={onCancel}>Abbrechen</button><button disabled={busy || blocked} onClick={preview}>PDF-Vorschau</button></div>{step === "preview" && previewUrl && <><LeaseContractPreview previewUrl={previewUrl} onBack={() => setStep("edit")} onCancel={onCancel} onPark={() => void park()} parkLabel={resumeContext ? "Änderungen speichern" : undefined} onContinue={() => { void beginSignatures(); }}/>{resumeContext && <button className="secondary-action" disabled={busy} onClick={() => void discard()}>Verwerfen</button>}</>}{step === "signatures" && <section className="signature-grid">{currentSignatureStep ? <><SignaturePad key={currentSignatureStep.key} label={currentSignatureStep.label} value={signatures[currentSignatureStep.key] ?? ""} onChange={value => setSignatures(c => ({ ...c, [currentSignatureStep.key]: value }))}/><button disabled={busy || !signatures[currentSignatureStep.key]} onClick={advanceSignature}>Speichern und Weiter</button></> : <button disabled={busy || !hasCompletedLeaseContractSignatures(signatureSteps, signatures)} onClick={finalize}>Signiert sicher ablegen</button>}{resumeContext && <button className="secondary-action" disabled={busy} onClick={() => void discard()}>Verwerfen</button>}</section>}</section>;
}
