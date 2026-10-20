import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { resolveSupabaseConfig } from "../../lib/supabase/client-context";

export type LeaseContractCompletionResult = { success: true; pachtverhaeltnis_id?: number; pachtvertragsfassung_id?: number; bereits_abgeschlossen: boolean };

export async function completePreparedLeaseContract(club: ClubContext, session: BrowserSession, kommunikationsvorgangId: number, expectedRevision: number): Promise<LeaseContractCompletionResult> {
  const config = resolveSupabaseConfig(club);
  const response = await fetch(`${config.url.replace(/\/$/, "")}/functions/v1/kgv-complete-prepared-lease-contract`, { method: "POST", headers: { apikey: config.publishableKey, Authorization: `Bearer ${session.accessToken}`, "Content-Type": "application/json" }, body: JSON.stringify({ kommunikationsvorgang_id: kommunikationsvorgangId, expected_revision: expectedRevision }) });
  const body = await response.json().catch(() => null) as { success?: boolean; message?: string; bereits_abgeschlossen?: boolean; pachtverhaeltnis_id?: number; pachtvertragsfassung_id?: number } | null;
  if (!response.ok || !body?.success) throw new Error(body?.message ?? (response.status === 401 ? "Die Anmeldung ist abgelaufen." : response.status === 403 ? "Keine Berechtigung für den Pachtvertragsabschluss." : response.status === 409 ? "Der Pachtvertragsabschluss steht in Konflikt zum aktuellen Stand." : "Der Pachtvertragsabschluss konnte nicht bestätigt werden."));
  return { success: true, pachtverhaeltnis_id: body.pachtverhaeltnis_id, pachtvertragsfassung_id: body.pachtvertragsfassung_id, bereits_abgeschlossen: Boolean(body.bereits_abgeschlossen) };
}
