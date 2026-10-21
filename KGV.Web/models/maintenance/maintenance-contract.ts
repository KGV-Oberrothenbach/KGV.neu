export type MaintenanceWorkHoursEffect = "none" | "credit" | "exempt";

export type MaintenanceContract = {
  id: number;
  title: string;
  description: string | null;
  area: string | null;
  maxActiveAssignments: number;
  exemptsFromDutyHours: boolean;
  workHoursCredit: number;
  active: boolean;
  note: string | null;
};

export type MaintenanceContractDraft = Omit<MaintenanceContract, "id">;

export function maintenanceWorkHoursEffect(contract: Pick<MaintenanceContract, "exemptsFromDutyHours" | "workHoursCredit">): MaintenanceWorkHoursEffect {
  if (contract.exemptsFromDutyHours) return "exempt";
  return contract.workHoursCredit > 0 ? "credit" : "none";
}

export function maintenanceWorkHoursEffectLabel(contract: Pick<MaintenanceContract, "exemptsFromDutyHours" | "workHoursCredit">): string {
  const effect = maintenanceWorkHoursEffect(contract);
  if (effect === "exempt") return "Vollbefreiung von Pflichtstunden";
  if (effect === "credit") return `${contract.workHoursCredit} Stunden Gutschrift pro Saison`;
  return "Keine Anrechnung";
}
