import { invokeEdgeFunctionRaw } from "../../lib/supabase/edge-client";
import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import type { Document, DocumentOwner } from "../../models/documents/document";

type DocumentUploadResponse = {
  document?: Document;
  message?: string;
  error_code?: string;
  request_id?: string;
};

type HistoricalContractNumberResponse = {
  success?: boolean;
  nummer?: number;
  titel?: string;
  pdf_gestempelt?: boolean;
  message?: string;
  error_code?: string;
  request_id?: string;
};

export class DocumentUploadError extends Error {
  readonly status: number;
  readonly errorCode: string | null;
  readonly requestId: string | null;

  constructor(status: number, response: DocumentUploadResponse | null) {
    const message = response?.message?.trim() || `Dokument-Upload wurde mit HTTP ${status} abgelehnt.`;
    const requestId = response?.request_id?.trim() || null;
    super(requestId ? `${message} Support-ID: ${requestId}` : message);
    this.name = "DocumentUploadError";
    this.status = status;
    this.errorCode = response?.error_code?.trim() || null;
    this.requestId = requestId;
  }
}

export async function uploadDocumentForOwnerRequest(club: ClubContext, session: BrowserSession, owner: DocumentOwner, file: File, title: string, historicalContractType?: "historischer_ma_pv_erstvertrag" | "historischer_pv_folgevertrag", operationId?: string): Promise<Document> {
  const form = new FormData();
  form.set("file", file);
  form.set("owner_kind", owner.kind === "member" ? "mitglied" : "parzelle");
  form.set("owner_id", String(owner.id));
  form.set("titel", title);
  if (historicalContractType !== undefined) {
    if (!operationId) throw new Error("Die Vorgangs-ID des Altvertrags fehlt.");
    form.set("historical_contract_type", historicalContractType);
    form.set("operation_id", operationId);
  }

  const response = await invokeEdgeFunctionRaw({ club, session }, "kgv-upload-document", { method: "POST", body: form });
  const payload = await response.json().catch(() => null) as DocumentUploadResponse | null;
  if (!response.ok) throw new DocumentUploadError(response.status, payload);
  if (!payload?.document) throw new DocumentUploadError(response.status, { ...payload, error_code: payload?.error_code ?? "UPLOAD_RESPONSE_INVALID", message: payload?.message ?? "Der Server lieferte keinen Dokumentdatensatz." });
  return payload.document;
}

export async function reserveHistoricalContractNumberRequest(club: ClubContext, session: BrowserSession, documentId: number) {
  const response = await invokeEdgeFunctionRaw({ club, session }, "kgv-upload-document", {
    method: "POST",
    body: JSON.stringify({ action: "reserve_historical_contract_number", document_id: documentId }),
    contentType: "application/json",
  });
  const payload = await response.json().catch(() => null) as HistoricalContractNumberResponse | null;
  if (!response.ok) throw new DocumentUploadError(response.status, payload);
  const nummer = Number(payload?.nummer);
  if (!Number.isInteger(nummer) || nummer < 80000 || nummer > 89999) {
    throw new DocumentUploadError(response.status, { ...payload, error_code: "HISTORICAL_NUMBER_RESPONSE_INVALID", message: payload?.message ?? "Der Server lieferte keine gültige historische Vertragsnummer." });
  }
  return { nummer, titel: payload?.titel ?? `Vertrags-Nr. ${nummer}`, pdfGestempelt: payload?.pdf_gestempelt === true };
}

export async function downloadHistoricalContractDocumentRequest(club: ClubContext, session: BrowserSession, documentId: number) {
  const response = await invokeEdgeFunctionRaw({ club, session }, "kgv-upload-document", {
    method: "POST", body: JSON.stringify({ action: "download_historical_contract", document_id: documentId }), contentType: "application/json",
  });
  if (!response.ok) throw new DocumentUploadError(response.status, await response.json().catch(() => null));
  return response.blob();
}
