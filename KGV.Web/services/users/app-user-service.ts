import type { ClubContext } from "../../models/auth/club";
import { saveSession, type AppUserContext, type BrowserSession } from "../../lib/supabase-auth";
import { invokeEdgeFunction } from "../../lib/supabase/edge-client";
import { callSupabaseRpc } from "../../lib/supabase/rpc-client";

export async function loadAppUserContext(club: ClubContext, session: BrowserSession): Promise<AppUserContext> {
  const rows = await callSupabaseRpc<Array<{
    user_id?: string;
    role?: string;
    mitglied_id?: number | null;
    permission_grants?: number | null;
    permission_revocations?: number | null;
    is_demo_account?: boolean | null;
  }>>({ club, session }, "current_app_user_context", {});
  const row = rows[0];
  if (!row?.user_id) throw new Error("Für dieses Konto ist kein gültiger KGV-Zugang hinterlegt. Bitte wende dich an den Vorstand.");
  const role = row.role?.toLowerCase();
  if (role !== "admin" && role !== "vorstand" && role !== "user") throw new Error("Für dieses Konto ist kein gültiger KGV-Zugang hinterlegt. Bitte wende dich an den Vorstand.");
  const isDemoAccount = Boolean(row.is_demo_account);
  session.isDemoAccount = isDemoAccount;
  saveSession(session);
  return {
    userId: row.user_id,
    role,
    mitgliedId: row.mitglied_id ?? null,
    permissionGrants: row.permission_grants ?? 0,
    permissionRevocations: row.permission_revocations ?? 0,
    isDemoAccount,
  };
}

export async function inviteAppUser(club: ClubContext, session: BrowserSession, memberId: number, role: "admin" | "vorstand" | "user") {
  return invokeEdgeFunction<{ success?: boolean; outcome?: string; errorCode?: string; mailSent?: boolean; message?: string; authUserId?: string }>(
    { club, session },
    "kgv-invite-user",
    { mitgliedId: memberId, role, inviteMethod: "otp" },
  );
}

export async function sendPasswordReset(club: ClubContext, session: BrowserSession, email: string) {
  return invokeEdgeFunction<{ success?: boolean; message?: string }>(
    { club, session },
    "kgv-request-first-login-otp",
    { email: email.trim() },
  );
}
