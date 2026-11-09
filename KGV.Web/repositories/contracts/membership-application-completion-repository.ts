import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { invokeEdgeFunction } from "../../lib/supabase/edge-client";

export async function completePreparedMembershipApplication(club: ClubContext, session: BrowserSession, id: number, revision: number) {
  const body = await invokeEdgeFunction<{ success?: boolean; message?: string }>({ club, session }, "kgv-complete-prepared-membership-application", { kommunikationsvorgang_id: id, expected_revision: revision });
  if (!body.success) throw new Error(body.message ?? "Der Mitgliedsantrag konnte nicht abgeschlossen werden.");
}
