import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { parsePreparedContractDraft, type PreparedContractDraft } from "../../models/communication/prepared-contract-draft";
import type { CommunicationItem } from "../../models/communication/communication-item";
import { completePreparedContract, createPreparedContract, findActivePreparedContract, findFinalDocumentByProvenance, getCommunicationContractDraft, markPreparedContractFinalDocument, startPreparedContractSignature, updatePreparedContractDraft } from "../../repositories/communication/communication-repository";
import { getActiveDocumentById } from "../../repositories/documents/document-repository";
import { resolveDocumentOpenUrl } from "../documents/document-open-service";
import { completePreparedLeaseContract } from "../../repositories/contracts/lease-contract-completion-repository";

export type PreparedContractResumeContext = {
  id: number;
  dokumenttyp: PreparedContractDraft["document_type"];
  revision: number;
  mitglied_id: number;
  parzelle_id: number | null;
  entwurf_daten: PreparedContractDraft;
  signatur_gestartet_am: string | null;
  signatur_revision: number | null;
  zugewiesen_an: string | null;
  final_dokument_id: number | null;
  status: "vorbereitet" | "zur_signatur" | "letzte_pruefung";
};

const stableValue = (value: unknown): unknown => {
  if (Array.isArray(value)) return value.map(stableValue);
  if (value && typeof value === "object") return Object.fromEntries(Object.entries(value as Record<string, unknown>).sort(([left], [right]) => left.localeCompare(right)).map(([key, item]) => [key, stableValue(item)]));
  return value;
};

export const preparedContractDraftsEqual = (left: PreparedContractDraft, right: PreparedContractDraft) => JSON.stringify(stableValue(left)) === JSON.stringify(stableValue(right));

function toResumeContext(item: CommunicationItem & { entwurf_daten: unknown }, documentType: PreparedContractDraft["document_type"], userId: string, expectedStatus: PreparedContractResumeContext["status"]): PreparedContractResumeContext {
  if (item.vorgangstyp !== "vertrag_signatur" || item.status !== expectedStatus) throw new Error("Der Vorgang ist nicht im erwarteten Vertragsstatus.");
  if (item.dokumenttyp !== documentType) throw new Error("Der Dokumenttyp des Vorgangs passt nicht zum Vertragseditor.");
  if (item.zugewiesen_an !== userId) throw new Error("Der Vorgang ist nicht dir zugewiesen.");
  if (expectedStatus === "vorbereitet" && (item.signatur_gestartet_am !== null || item.signatur_revision !== null)) throw new Error("Der Vertragsentwurf ist nicht mehr zur Bearbeitung freigegeben.");
  if (expectedStatus === "zur_signatur" && (item.signatur_gestartet_am === null || item.signatur_revision !== item.revision)) throw new Error("Die eingefrorene Signaturrevision ist ungültig.");
  if (expectedStatus === "letzte_pruefung" && (item.signatur_gestartet_am === null || item.signatur_revision !== item.revision || item.final_dokument_id === null)) throw new Error("Der Vorgang enthält kein gültiges Finaldokument zur letzten Prüfung.");
  const draft = parsePreparedContractDraft(item.entwurf_daten);
  if (draft.document_type !== documentType || draft.memberId !== item.mitglied_id) throw new Error("Der gespeicherte Vertragsentwurf passt nicht zum Vorgang.");
  if (documentType === "pachtvertrag" && (item.parzelle_id === null || (draft as Extract<PreparedContractDraft, { document_type: "pachtvertrag" }>).draft.parcelId !== item.parzelle_id)) throw new Error("Die gespeicherte Parzelle passt nicht zum Vorgang.");
  return { id: item.id, dokumenttyp: documentType, revision: item.revision, mitglied_id: item.mitglied_id, parzelle_id: item.parzelle_id, entwurf_daten: draft, signatur_gestartet_am: item.signatur_gestartet_am, signatur_revision: item.signatur_revision, final_dokument_id: item.final_dokument_id, zugewiesen_an: item.zugewiesen_an, status: expectedStatus };
}

export async function loadPreparedContractForEditing(club: ClubContext, session: BrowserSession, id: number, userId: string, documentType: PreparedContractDraft["document_type"]) {
  const item = await getCommunicationContractDraft(club, session, id);
  if (!item) throw new Error("Der vorbereitete Vertragsvorgang wurde nicht gefunden.");
  return toResumeContext(item, documentType, userId, "vorbereitet");
}

export async function loadPreparedContractForSignature(club: ClubContext, session: BrowserSession, id: number, userId: string, documentType: PreparedContractDraft["document_type"]) {
  const item = await getCommunicationContractDraft(club, session, id);
  if (!item) throw new Error("Der gestartete Vertragsvorgang wurde nicht gefunden.");
  return toResumeContext(item, documentType, userId, "zur_signatur");
}

export async function loadPreparedContractForFinalReview(club: ClubContext, session: BrowserSession, id: number, userId: string, documentType: PreparedContractDraft["document_type"]) {
  const item = await getCommunicationContractDraft(club, session, id);
  if (!item) throw new Error("Der Vertragsvorgang zur letzten Prüfung wurde nicht gefunden.");
  return toResumeContext(item, documentType, userId, "letzte_pruefung");
}

export async function savePreparedContractIfChanged(club: ClubContext, session: BrowserSession, userId: string, context: PreparedContractResumeContext, nextDraft: PreparedContractDraft) {
  if (nextDraft.document_type !== context.dokumenttyp || nextDraft.memberId !== context.mitglied_id || (nextDraft.document_type === "pachtvertrag" && nextDraft.draft.parcelId !== context.parzelle_id)) throw new Error("Der Vertragsentwurf passt nicht mehr zum vorbereiteten Vorgang.");
  if (preparedContractDraftsEqual(context.entwurf_daten, nextDraft)) return context;
  const updated = await updatePreparedContractDraft(club, session, context.id, context.revision, userId, nextDraft);
  return toResumeContext({ ...updated, entwurf_daten: nextDraft }, context.dokumenttyp, userId, "vorbereitet");
}
export async function parkPreparedContract(club: ClubContext, session: BrowserSession, userId: string, draft: PreparedContractDraft) {
  const isLease = draft.document_type === "pachtvertrag";
  const existing = await findActivePreparedContract(club, session, draft.document_type, draft.memberId, isLease ? draft.draft.parcelId : null);
  if (existing) {
    if (existing.status !== "vorbereitet") throw new Error("Für dieses Mitglied besteht bereits ein aktiver Vertragsvorgang. Bitte setzen Sie ihn in Kommunikation fort.");
    const context = toResumeContext(existing, draft.document_type, userId, "vorbereitet");
    return savePreparedContractIfChanged(club, session, userId, context, draft);
  }
  const rows = await createPreparedContract(club, session, { vorgangstyp: "vertrag_signatur", dokumenttyp: draft.document_type, status: "vorbereitet", mitglied_id: draft.memberId, parzelle_id: isLease ? draft.draft.parcelId : null, entwurf_daten: draft, zugewiesen_an: userId });
  return rows[0];
}

export async function startPreparedContractSigning(club: ClubContext, session: BrowserSession, context: PreparedContractResumeContext, userId: string) {
  await startPreparedContractSignature(club, session, context.id, context.revision, userId);
  const current = await getCommunicationContractDraft(club, session, context.id);
  if (!current) throw new Error("Der gestartete Vertragsvorgang wurde nicht gefunden.");
  return toResumeContext(current, context.dokumenttyp, userId, "zur_signatur");
}

export async function markPreparedContractReadyForReview(club: ClubContext, session: BrowserSession, context: PreparedContractResumeContext, userId: string, finalDocumentId: number) {
  if (context.status !== "zur_signatur" || !Number.isInteger(finalDocumentId) || finalDocumentId <= 0) throw new Error("Das finale Dokument kann nicht zur Prüfung vorgemerkt werden.");
  const updated = await markPreparedContractFinalDocument(club, session, context.id, context.revision, userId, finalDocumentId);
  return toResumeContext(updated, context.dokumenttyp, userId, "letzte_pruefung");
}
export async function recoverPreparedContractFinalDocument(club: ClubContext, session: BrowserSession, context: PreparedContractResumeContext, userId: string) { if (context.status !== "zur_signatur") return null; const document = await findFinalDocumentByProvenance(club, session, context.id, context.revision); if (!document) return null; if (document.source_kommunikationsvorgang_id !== context.id || document.source_revision !== context.revision) throw new Error("Das gefundene finale Dokument passt nicht zum Kommunikationsvorgang."); return markPreparedContractReadyForReview(club, session, context, userId, document.id); }

export async function completePreparedContractFinalReview(club: ClubContext, session: BrowserSession, context: PreparedContractResumeContext, userId: string) {
  if (context.status !== "letzte_pruefung" || context.final_dokument_id === null) throw new Error("Der Vorgang ist nicht zur letzten Prüfung bereit.");
  if (context.dokumenttyp === "pachtvertrag") return completePreparedLeaseContract(club, session, context.id, context.revision);
  return completePreparedContract(club, session, context.id, context.revision, userId, context.final_dokument_id);
}

export async function openPreparedContractFinalDocument(club: ClubContext, session: BrowserSession, context: PreparedContractResumeContext) {
  if (context.status !== "letzte_pruefung" || context.final_dokument_id === null) throw new Error("Für den Vorgang liegt kein finales Dokument vor.");
  const document = await getActiveDocumentById(club, session, context.final_dokument_id);
  if (!document) throw new Error("Das finale Dokument wurde nicht gefunden.");
  return resolveDocumentOpenUrl(club, session, document);
}
