import type { LeaseContractDraft } from "../../services/contracts/lease-service";
import type { LegalRepresentativeDraft } from "../../services/contracts/legal-representative-service";
export const preparedContractSchemaVersion = "kgv-prepared-contract/v1" as const;
export type PreparedMembershipApplicationDraft = { schema_version: typeof preparedContractSchemaVersion; document_type: "mitgliedsantrag"; memberId: number; startDate: string; memberFee: number; representative: LegalRepresentativeDraft | null; includeSecondaryMember: boolean };
export type PreparedLeaseContractDraft = { schema_version: typeof preparedContractSchemaVersion; document_type: "pachtvertrag"; memberId: number; draft: LeaseContractDraft };
export type PreparedContractDraft = PreparedMembershipApplicationDraft | PreparedLeaseContractDraft;
export function parsePreparedContractDraft(value: unknown): PreparedContractDraft {
  if (!value || typeof value !== "object") throw new Error("Der gespeicherte Vertragsentwurf ist ungültig.");
  const draft = value as Record<string, unknown>;
  if (containsSignatureData(draft)) throw new Error("Der gespeicherte Vertragsentwurf darf keine Signaturdaten enthalten.");
  if (draft.schema_version !== preparedContractSchemaVersion || (draft.document_type !== "mitgliedsantrag" && draft.document_type !== "pachtvertrag")) throw new Error("Die Version des gespeicherten Vertragsentwurfs wird nicht unterstützt.");
  if (typeof draft.memberId !== "number") throw new Error("Der gespeicherte Vertragsentwurf enthält kein Mitglied.");
  if (draft.document_type === "mitgliedsantrag") { if (typeof draft.startDate !== "string" || typeof draft.memberFee !== "number" || typeof draft.includeSecondaryMember !== "boolean") throw new Error("Der gespeicherte Mitgliedsantrag ist unvollständig."); return draft as unknown as PreparedMembershipApplicationDraft; }
  const lease = draft.draft as Record<string, unknown> | undefined;
  const requiredLeaseFields: Array<[string, string]> = [["startDate", "string"], ["parcelId", "number"], ["hasPreviousContract", "boolean"], ["includeSecondaryMember", "boolean"], ["handoverDate", "string"], ["fixedUntil", "string"], ["gardenRulesVersion", "string"], ["gardenRulesHandedOverAt", "string"], ["additionalAgreements", "string"], ["attachment1", "boolean"], ["attachment2", "boolean"], ["attachment3", "boolean"], ["attachment4", "boolean"], ["clubSignatory", "string"]];
  if (!lease || lease.previousContractDate !== null && typeof lease.previousContractDate !== "string" || requiredLeaseFields.some(([key, type]) => typeof lease[key] !== type)) throw new Error("Der gespeicherte Pachtvertrag ist unvollständig.");
  return draft as unknown as PreparedLeaseContractDraft;
}

function containsSignatureData(value: Record<string, unknown>): boolean {
  return Object.entries(value).some(([key, item]) => key.toLocaleLowerCase().includes("signature") || (item && typeof item === "object" && !Array.isArray(item) && containsSignatureData(item as Record<string, unknown>)));
}
