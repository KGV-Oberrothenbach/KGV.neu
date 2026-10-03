import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { signInWithPassword } from "../../repositories/auth/auth-repository";

const storageKey = "kgv.browser.session.v1";

export async function signIn(club: ClubContext, email: string, password: string): Promise<BrowserSession> {
  const result = await signInWithPassword(club, email, password);
  const session: BrowserSession = {
    accessToken: result.access_token!,
    refreshToken: result.refresh_token!,
    expiresAt: Date.now() + (result.expires_in ?? 3600) * 1000,
    user: { id: result.user!.id!, email: result.user!.email },
  };
  window.localStorage.setItem(storageKey, JSON.stringify(session));
  return session;
}
