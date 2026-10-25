import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { generateContract } from "../contracts/contract-generation-service";
import * as repository from "../../repositories/maintenance/maintenance-continuation-repository";

export const loadExpiringMaintenanceAssignments = repository.listExpiringMaintenanceAssignments;
export const loadMaintenanceContinuationDetail = repository.getMaintenanceContinuationDetail;
export const extendMaintenanceAssignment = repository.extendMaintenanceAssignment;
export const decideMaintenanceNonExtension = repository.decideMaintenanceNonExtension;
export const withdrawMaintenanceNonExtension = repository.withdrawMaintenanceNonExtension;

export function displayContinuationStart(validUntil: string) {
  const date = new Date(`${validUntil}T00:00:00`); date.setDate(date.getDate() + 1);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
}
export async function retryMaintenanceExtensionDocument(club: ClubContext, session: BrowserSession, detail: { hauptmitglied_id: number; kommunikationsvorgang_id: number | null }) {
  if (!detail.kommunikationsvorgang_id) throw new Error("Für diese Verlängerung wurde kein Dokumentvorgang bestätigt.");
  return generateContract(club, session, { action: "finalize", type: "wartungsvertrag_verlaengerung", member_id: detail.hauptmitglied_id, start_date: "", kommunikationsvorgang_id: detail.kommunikationsvorgang_id });
}

export function maintenanceContinuationError(cause: unknown) {
  const message = cause instanceof Error ? cause.message : "Die Wartungsvertragsentscheidung konnte nicht gespeichert werden.";
  if (/Berechtigung|42501/i.test(message)) return "Sie besitzen keine fachliche Berechtigung für diese Wartungsvertragsentscheidung.";
  if (/bereits eine wirksame Folgeentscheidung/i.test(message)) return "Für diese Laufzeit liegt bereits eine wirksame Entscheidung vor.";
  if (/Kapazit|Überschneid|overlap/i.test(message)) return "Die Verlängerung ist wegen Kapazität oder einer Überschneidung nicht zulässig.";
  return message;
}
