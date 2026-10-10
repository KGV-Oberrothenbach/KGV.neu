import { type MaintenanceContract, type MaintenanceWorkHoursEffect } from "./maintenance-contract";

export type MaintenanceContractOverview = MaintenanceContract & { occupied: number; available: number; workHoursEffect: MaintenanceWorkHoursEffect };
