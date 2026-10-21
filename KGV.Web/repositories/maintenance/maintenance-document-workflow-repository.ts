import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";

export type MaintenanceDocumentWorkflowRow = {
  id: number;
  wartungsvertrag_zuordnung_id: number;
  kommunikationsvorgang_id: number | null;
  dokument_id: number | null;
  status: "dokument_ausstehend" | "erzeugungsfehler" | "signatur_vorgang" | "abgeschlossen";
  fehlermeldung: string | null;
  versuche: number;
};

export async function ensureMaintenanceDocumentWorkflow(club: ClubContext, session: BrowserSession, assignmentId: number): Promise<MaintenanceDocumentWorkflowRow> {
  if (!Number.isSafeInteger(assignmentId) || assignmentId <= 0) throw new Error("Die Wartungsvertragszuordnung ist ungültig.");
  const rows = await callSupabaseRpc<MaintenanceDocumentWorkflowRow[]>({ club, session }, "ensure_wartungsvertrag_dokument_workflow", { p_zuordnung_id: assignmentId });
  const workflow = rows[0];
  if (!workflow || workflow.wartungsvertrag_zuordnung_id !== assignmentId) throw new Error("Der Wartungsvertrags-Dokumentvorgang wurde nicht bestätigt.");
  return workflow;
}
