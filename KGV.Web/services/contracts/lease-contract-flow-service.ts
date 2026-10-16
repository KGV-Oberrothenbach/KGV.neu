export type LeaseContractSignatureKey = "member" | "secondary" | "board";

export type LeaseContractSignatureStep = {
  key: LeaseContractSignatureKey;
  label: string;
};

export function buildLeaseContractSignatureSteps({ isMinor, includeSecondaryMember }: { isMinor: boolean; includeSecondaryMember: boolean }): LeaseContractSignatureStep[] {
  const needsSecondParty = isMinor || includeSecondaryMember;
  return [
    { key: "member", label: "Unterschrift Pächter/in 1" },
    ...(needsSecondParty ? [{ key: "secondary" as const, label: isMinor ? "Unterschrift gesetzliche/r Vertreter/in" : "Unterschrift Pächter/in 2" }] : []),
    { key: "board", label: "Unterschrift Vorstand / Verpächter" },
  ];
}

export function hasCompletedLeaseContractSignatures(steps: LeaseContractSignatureStep[], signatures: Partial<Record<LeaseContractSignatureKey, string>>) {
  return steps.every((step) => Boolean(signatures[step.key]));
}
