export type MembershipApplicationSignatureKey = "applicationMember" | "applicationSecondParty" | "board" | "privacyMember" | "privacySecondParty";

export type MembershipApplicationSignatureStep = {
  key: MembershipApplicationSignatureKey;
  label: string;
};

export type MembershipApplicationFlowStage = "edit" | "preview" | "signatures";

export function buildMembershipApplicationSignatureSteps({ isMinor, includeSecondaryMember }: { isMinor: boolean; includeSecondaryMember: boolean }): MembershipApplicationSignatureStep[] {
  const secondParty = isMinor ? "gesetzliche/r Vertreter/in" : "Nebenmitglied";
  const needsSecondParty = isMinor || includeSecondaryMember;
  return [
    { key: "applicationMember", label: "Unterschrift Antragsteller/in – Mitgliedsantrag" },
    ...(needsSecondParty ? [{ key: "applicationSecondParty" as const, label: `Unterschrift ${secondParty} – Mitgliedsantrag` }] : []),
    { key: "board", label: "Unterschrift Für den Verein" },
    { key: "privacyMember", label: "Unterschrift Antragsteller/in – Datenschutzerklärung" },
    ...(needsSecondParty ? [{ key: "privacySecondParty" as const, label: `Unterschrift ${secondParty} – Datenschutzerklärung` }] : []),
  ];
}

export function hasCompletedMembershipApplicationSignatures(steps: MembershipApplicationSignatureStep[], signatures: Partial<Record<MembershipApplicationSignatureKey, string>>) {
  return steps.every((step) => Boolean(signatures[step.key]));
}

export const canStartMembershipApplicationSignatures = (stage: MembershipApplicationFlowStage) => stage === "preview";

export const canFinalizeMembershipApplication = (stage: MembershipApplicationFlowStage, steps: MembershipApplicationSignatureStep[], signatures: Partial<Record<MembershipApplicationSignatureKey, string>>) =>
  stage === "signatures" && hasCompletedMembershipApplicationSignatures(steps, signatures);
