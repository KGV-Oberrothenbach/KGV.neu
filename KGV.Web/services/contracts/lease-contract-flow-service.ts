import type { PreparedContractSignature } from "../../repositories/contracts/prepared-contract-signature-repository";

export type LeasePersistentSlot = { key: "lease.member" | "lease.second_party" | "vorstand_1" | "vorstand_2"; sortOrder: number; required: boolean; signerType: string; memberId?: number | null };

export function buildLeaseContractPersistentSlots({ isMinor, includeSecondaryMember, memberId, secondaryMemberId }: { isMinor: boolean; includeSecondaryMember: boolean; memberId: number; secondaryMemberId?: number | null }): LeasePersistentSlot[] {
  const slots: LeasePersistentSlot[] = [{ key: "lease.member", sortOrder: 10, required: true, signerType: "mitglied", memberId }];
  if (isMinor) slots.push({ key: "lease.second_party", sortOrder: 20, required: true, signerType: "gesetzlicher_vertreter" });
  else if (includeSecondaryMember) slots.push({ key: "lease.second_party", sortOrder: 20, required: true, signerType: "nebenmitglied", memberId: secondaryMemberId ?? null });
  slots.push({ key: "vorstand_1", sortOrder: 90, required: true, signerType: "vereinsunterzeichner" }, { key: "vorstand_2", sortOrder: 100, required: true, signerType: "vereinsunterzeichner" });
  return slots;
}

export const leaseContractPositionLabel = (position: PreparedContractSignature) => position.signatur_schluessel === "lease.member" ? "Pächter/in 1" : position.signatur_schluessel === "lease.second_party" ? "Pächter/in 2" : position.signatur_schluessel === "vorstand_1" ? "Vereinsunterzeichner 1" : position.signatur_schluessel === "vorstand_2" ? "Vereinsunterzeichner 2" : position.signatur_schluessel;
export const preparedLeaseSignatureProgress = (positions: PreparedContractSignature[]) => { const required = positions.filter(position => position.pflicht); const signed = required.filter(position => position.status === "unterschrieben").length; return { required: required.length, signed, complete: required.length > 0 && signed === required.length }; };
export const nextOpenLeaseSignature = (positions: PreparedContractSignature[]) => positions.find(position => position.status !== "unterschrieben") ?? null;
