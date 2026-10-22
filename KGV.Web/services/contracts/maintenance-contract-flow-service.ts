import type { PreparedContractSignature } from "../../repositories/contracts/prepared-contract-signature-repository";

export const maintenanceContractPersistentSlots = (memberId: number) => [
  { key: "mitglied", sortOrder: 10, required: true, signerType: "mitglied", memberId },
  { key: "vorstand_1", sortOrder: 90, required: true, signerType: "vereinsunterzeichner" },
  { key: "vorstand_2", sortOrder: 100, required: true, signerType: "vereinsunterzeichner" },
];

export const maintenanceContractPositionLabel = (position: PreparedContractSignature) => position.signatur_schluessel === "mitglied" ? "Mitglied" : position.signatur_schluessel === "vorstand_1" ? "Vereinsunterzeichner 1" : position.signatur_schluessel === "vorstand_2" ? "Vereinsunterzeichner 2" : position.signatur_schluessel;
export const preparedMaintenanceSignatureProgress = (positions: PreparedContractSignature[]) => { const required = positions.filter(position => position.pflicht); const signed = required.filter(position => position.status === "unterschrieben").length; return { required: required.length, signed, complete: required.length > 0 && signed === required.length }; };
export const nextOpenMaintenanceSignature = (positions: PreparedContractSignature[]) => positions.find(position => position.status !== "unterschrieben") ?? null;
