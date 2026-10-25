import type { ClubContext } from "../../models/auth/club";
import { type BrowserSession } from "../../lib/supabase-auth";
import { type MaintenanceContract, type MaintenanceContractDraft } from "../../models/maintenance/maintenance-contract";
import { maintenanceWorkHoursEffect } from "../../models/maintenance/maintenance-contract";
import { type MaintenanceContractOverview } from "../../models/maintenance/maintenance-contract-overview";
import { type MaintenanceAssignment } from "../../models/maintenance/maintenance-assignment";
import { createMaintenanceContract, getMaintenanceContract, listMaintenanceContracts, type MaintenanceContractRow, type MaintenanceContractWriteRow, updateMaintenanceContract } from "../../repositories/maintenance/maintenance-contract-repository";
import { listMaintenanceAssignmentsForContract } from "../../repositories/maintenance/maintenance-assignment-repository";

function fromRow(row: MaintenanceContractRow): MaintenanceContract {
  return { id: row.id, title: row.titel, description: row.beschreibung, area: row.bereich, maxActiveAssignments: row.max_aktive_zuordnungen, exemptsFromDutyHours: row.befreit_von_pflichtstunden, workHoursCredit: Number(row.arbeitsstunden_gutschrift), active: row.aktiv, note: row.bemerkung };
}

export async function loadMaintenanceContracts(club: ClubContext, session: BrowserSession): Promise<MaintenanceContract[]> {
  return (await listMaintenanceContracts(club, session)).map(fromRow);
}

export function buildMaintenanceContractOverview(contracts: MaintenanceContract[], assignments: MaintenanceAssignment[], referenceDate: string): MaintenanceContractOverview[] {
  return contracts.map((contract) => {
    const occupied = assignments.filter((assignment) => assignment.maintenanceContractId === contract.id && assignment.validFrom <= referenceDate && (!assignment.validUntil || assignment.validUntil >= referenceDate)).length;
    return { ...contract, occupied, available: Math.max(0, contract.maxActiveAssignments - occupied), workHoursEffect: maintenanceWorkHoursEffect(contract) };
  });
}

export async function loadMaintenanceContract(club: ClubContext, session: BrowserSession, id: number): Promise<MaintenanceContract | null> {
  const row = await getMaintenanceContract(club, session, id);
  return row ? fromRow(row) : null;
}

export function normalizeAndValidateMaintenanceContract(draft: MaintenanceContractDraft): { payload?: MaintenanceContractWriteRow; error?: string } {
  const title = draft.title.trim();
  const credit = Number(draft.workHoursCredit);
  if (!title) return { error: "Ein Titel ist erforderlich." };
  if (!Number.isInteger(draft.maxActiveAssignments) || draft.maxActiveAssignments < 1) return { error: "Das Kontingent muss mindestens 1 sein." };
  if (!Number.isFinite(credit) || credit < 0) return { error: "Die Arbeitsstunden-Gutschrift darf nicht negativ sein." };
  if (draft.exemptsFromDutyHours && credit > 0) return { error: "Vollbefreiung und eine positive Arbeitsstunden-Gutschrift können nicht gleichzeitig gesetzt werden." };
  return { payload: { titel: title, beschreibung: draft.description?.trim() || null, bereich: draft.area?.trim() || null, max_aktive_zuordnungen: draft.maxActiveAssignments, befreit_von_pflichtstunden: draft.exemptsFromDutyHours, arbeitsstunden_gutschrift: credit, aktiv: draft.active, bemerkung: draft.note?.trim() || null, is_demo: false } };
}

export async function saveMaintenanceContract(club: ClubContext, session: BrowserSession, id: number | null, draft: MaintenanceContractDraft, fundamentalChange = false): Promise<MaintenanceContract> {
  const result = normalizeAndValidateMaintenanceContract(draft);
  if (!result.payload) throw new Error(result.error);
  if (id !== null) {
    const today = new Date();
    const localDate = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;
    const activeAssignments = await listMaintenanceAssignmentsForContract(club, session, id);
    const occupiedToday = activeAssignments.filter((assignment) => assignment.gueltig_ab <= localDate && (!assignment.gueltig_bis || assignment.gueltig_bis >= localDate)).length;
    if (result.payload.max_aktive_zuordnungen < occupiedToday) throw new Error("Das Kontingent kann nicht unter die aktuell belegten Plätze reduziert werden.");
  }
  // A material amendment must never rewrite the meaning of historical assignments.
  // It intentionally creates a new definition; the former definition remains intact.
  const rows = id === null || fundamentalChange ? await createMaintenanceContract(club, session, result.payload) : await updateMaintenanceContract(club, session, id, result.payload);
  const row = rows[0];
  if (!row) throw new Error("Wartungsvertrag konnte nicht gespeichert werden.");
  return fromRow(row);
}
