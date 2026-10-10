import { type BrowserSession } from "../../lib/supabase-auth";
import { type MaintenanceContract, type MaintenanceContractDraft } from "../../models/maintenance/maintenance-contract";
import { createMaintenanceContract, getMaintenanceContract, listMaintenanceContracts, type MaintenanceContractRow, type MaintenanceContractWriteRow, updateMaintenanceContract } from "../../repositories/maintenance/maintenance-contract-repository";

function fromRow(row: MaintenanceContractRow): MaintenanceContract {
  return { id: row.id, title: row.titel, description: row.beschreibung, area: row.bereich, maxActiveAssignments: row.max_aktive_zuordnungen, exemptsFromDutyHours: row.befreit_von_pflichtstunden, workHoursCredit: Number(row.arbeitsstunden_gutschrift), active: row.aktiv, note: row.bemerkung };
}

export async function loadMaintenanceContracts(session: BrowserSession): Promise<MaintenanceContract[]> {
  return (await listMaintenanceContracts(session)).map(fromRow);
}

export async function loadMaintenanceContract(session: BrowserSession, id: number): Promise<MaintenanceContract | null> {
  const row = await getMaintenanceContract(session, id);
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

export async function saveMaintenanceContract(session: BrowserSession, id: number | null, draft: MaintenanceContractDraft): Promise<MaintenanceContract> {
  const result = normalizeAndValidateMaintenanceContract(draft);
  if (!result.payload) throw new Error(result.error);
  const rows = id === null ? await createMaintenanceContract(session, result.payload) : await updateMaintenanceContract(session, id, result.payload);
  const row = rows[0];
  if (!row) throw new Error("Wartungsvertrag konnte nicht gespeichert werden.");
  return fromRow(row);
}
