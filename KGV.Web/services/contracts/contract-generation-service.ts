import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { invokeEdgeFunctionRaw } from "../../lib/supabase/edge-client";

type ContractGenerationOptions = {
  parcel_id?: number;
  member_fee?: number;
  admission_fee?: number;
  representative?: { mode: "existing" | "manual"; member_id?: number; vorname?: string; nachname?: string; adresse_abweichend?: boolean; adresse?: string; plz?: string; ort?: string };
  signature_application_member?: string;
  signature_privacy_member?: string;
  signature_application_representative?: string;
  signature_privacy_representative?: string;
  signature_member?: string;
  has_previous_contract?: boolean;
  previous_contract_date?: string;
  include_secondary_member?: boolean;
  signature_secondary?: string;
  signature_board?: string;
  signature_board2?: string;
  protocol_id?: number;
  protocol_type?: string;
  protocol_reason?: string;
  protocol_condition?: string;
  protocol_agreement?: string;
  board2_id?: number;
  companion_name?: string;
  photos?: string[];
  source_document_id?: number;
};

export type ContractGenerationRequest =
  | (ContractGenerationOptions & { action: "preview" | "finalize"; type: "mitgliedsantrag" | "mitgliedsvertrag" | "pachtvertrag" | "parzellenprotokoll"; member_id: number; start_date: string })
  | (ContractGenerationOptions & { action: "sign-existing"; source_document_id: number; signature_member: string });

export async function generateContract(club: ClubContext, session: BrowserSession, request: ContractGenerationRequest): Promise<{ previewUrl?: string; documentId?: number; message?: string }> {
  const response = await invokeEdgeFunctionRaw({ club, session }, "kgv-generate-contract", { method: "POST", contentType: "application/json", body: JSON.stringify(request) });
  if (!response.ok) {
    const detail = await response.json().catch(() => null) as { message?: string } | null;
    throw new Error(detail?.message ?? "Das Vertragsdokument konnte nicht erzeugt werden.");
  }
  if (request.action === "preview") return { previewUrl: URL.createObjectURL(await response.blob()) };
  const payload = await response.json() as { document_id?: number; message?: string };
  return { documentId: payload.document_id, message: payload.message };
}
