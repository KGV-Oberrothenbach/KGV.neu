export type MaintenanceAssignment = {
  id: number;
  maintenanceContractId: number;
  principalMemberId: number;
  contractPartyMemberId: number | null;
  validFrom: string;
  validUntil: string | null;
  note: string | null;
  predecessorAssignmentId?: number | null;
};

export type MaintenanceAssignmentDraft = Omit<MaintenanceAssignment, "id">;
