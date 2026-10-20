import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { resolveSupabaseConfig } from "../../lib/supabase/client-context";

export async function completePreparedMembershipApplication(club: ClubContext, session: BrowserSession, id: number, revision: number) {
  const config = resolveSupabaseConfig(club);
  const response = await fetch(`${config.url.replace(/\/$/, "")}/functions/v1/kgv-complete-prepared-membership-application`, { method: "POST", headers: { apikey: config.publishableKey, Authorization: `Bearer ${session.accessToken}`, "Content-Type": "application/json" }, body: JSON.stringify({ kommunikationsvorgang_id: id, expected_revision: revision }) });
  const body = await response.json().catch(() => null) as { success?: boolean; message?: string } | null;
  if (!response.ok || !body?.success) throw new Error(body?.message ?? "Der Mitgliedsantrag konnte nicht abgeschlossen werden.");
}
