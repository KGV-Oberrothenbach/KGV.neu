import type { FormDocumentStatus } from "./form-document-status";

export type MembershipAdmissionStatus = "antrag_offen" | "aufgenommen" | "abgelehnt" | undefined;

export function canStartMembershipApplication(admissionStatus: MembershipAdmissionStatus, documentStatus: FormDocumentStatus) {
  if (admissionStatus === "aufgenommen") return false;
  return admissionStatus === "abgelehnt" || documentStatus === "none";
}

export function membershipBoardDecisionPath(unanimousYes: boolean): "einstimmig" | "vorstandssitzung" {
  return unanimousYes ? "einstimmig" : "vorstandssitzung";
}
