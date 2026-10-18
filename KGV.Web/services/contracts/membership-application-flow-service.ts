export type MembershipApplicationSignatureKey = "applicationMember" | "applicationSecondParty" | "board" | "privacyMember" | "privacySecondParty";

export type MembershipApplicationSignatureStep = {
  key: MembershipApplicationSignatureKey;
  order: number;
  personType: "mitglied" | "nebenmitglied" | "gesetzlicher_vertreter" | "vorstand";
  personId?: number;
  purpose: "mitgliedsantrag" | "datenschutz" | "freigabe";
  label: string;
};

export type MembershipApplicationFlowStage = "edit" | "preview" | "signatures";

export function buildMembershipApplicationSignatureSteps({ isMinor, includeSecondaryMember, memberId, representativeMemberId }: { isMinor: boolean; includeSecondaryMember: boolean; memberId?: number; representativeMemberId?: number }): MembershipApplicationSignatureStep[] {
  if (isMinor && includeSecondaryMember) throw new Error("Ein minderjähriges Mitglied kann kein Nebenmitglied im Mitgliedsantrag haben.");
  const secondParty = isMinor ? { personType: "gesetzlicher_vertreter" as const, label: "Gesetzlicher Vertreter" } : { personType: "nebenmitglied" as const, label: "Nebenmitglied" };
  const steps: MembershipApplicationSignatureStep[] = [
    { key: "applicationMember", order: 1, personType: "mitglied", personId: memberId, purpose: "mitgliedsantrag", label: "Mitglied – Mitgliedsantrag" },
    { key: "privacyMember", order: 2, personType: "mitglied", personId: memberId, purpose: "datenschutz", label: "Mitglied – Datenschutz" },
  ];
  if (isMinor || includeSecondaryMember) {
    steps.push(
      { key: "applicationSecondParty", order: steps.length + 1, personType: secondParty.personType, personId: isMinor ? representativeMemberId : undefined, purpose: "mitgliedsantrag", label: `${secondParty.label} – Mitgliedsantrag` },
      { key: "privacySecondParty", order: steps.length + 2, personType: secondParty.personType, personId: isMinor ? representativeMemberId : undefined, purpose: "datenschutz", label: `${secondParty.label} – Datenschutz` },
    );
  }
  steps.push({ key: "board", order: steps.length + 1, personType: "vorstand", purpose: "freigabe", label: "Vorstand – Freigabe" });
  return steps;
}

export function hasCompletedMembershipApplicationSignatures(steps: MembershipApplicationSignatureStep[], signatures: Partial<Record<MembershipApplicationSignatureKey, string>>) {
  return steps.every((step) => Boolean(signatures[step.key]));
}

export const canStartMembershipApplicationSignatures = (stage: MembershipApplicationFlowStage) => stage === "preview";

export const canFinalizeMembershipApplication = (stage: MembershipApplicationFlowStage, steps: MembershipApplicationSignatureStep[], signatures: Partial<Record<MembershipApplicationSignatureKey, string>>) =>
  stage === "signatures" && hasCompletedMembershipApplicationSignatures(steps, signatures);
