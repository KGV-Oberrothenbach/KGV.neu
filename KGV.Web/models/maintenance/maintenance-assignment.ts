export type MaintenanceAssignment = {
  id: number;
  maintenanceContractId: number;
  principalMemberId: number;
  validFrom: string;
  validUntil: string | null;
  note: string | null;
  predecessorAssignmentId?: number | null;
};

export type MaintenanceAssignmentDraft = Omit<MaintenanceAssignment, "id">;
