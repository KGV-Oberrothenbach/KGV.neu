import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { invokeEdgeFunction } from "../../lib/supabase/edge-client";

export type LeaseContractCompletionResult = { success: true; pachtverhaeltnis_id?: number; pachtvertragsfassung_id?: number; bereits_abgeschlossen: boolean };

export async function completePreparedLeaseContract(club: ClubContext, session: BrowserSession, kommunikationsvorgangId: number, expectedRevision: number): Promise<LeaseContractCompletionResult> {
  const body = await invokeEdgeFunction<{ success?: boolean; message?: string; bereits_abgeschlossen?: boolean; pachtverhaeltnis_id?: number; pachtvertragsfassung_id?: number }>({ club, session }, "kgv-complete-prepared-lease-contract", { kommunikationsvorgang_id: kommunikationsvorgangId, expected_revision: expectedRevision });
  if (!body.success) throw new Error(body.message ?? "Der Pachtvertragsabschluss konnte nicht bestätigt werden.");
  return { success: true, pachtverhaeltnis_id: body.pachtverhaeltnis_id, pachtvertragsfassung_id: body.pachtvertragsfassung_id, bereits_abgeschlossen: Boolean(body.bereits_abgeschlossen) };
}
