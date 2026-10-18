import { createClient, type Session } from "@supabase/supabase-js";
import type { BrowserSession } from "../../lib/supabase-auth";
import { resolveSupabaseConfig } from "../../lib/supabase/client-context";
import type { ClubContext } from "../../models/auth/club";

function createPasskeyClient(club: ClubContext) {
  const { url, publishableKey } = resolveSupabaseConfig(club);
  // A passkey request is always sent to the Supabase project of the selected club.
  // The application keeps the returned session in its existing session store.
  return createClient(url, publishableKey, {
    auth: { persistSession: false, autoRefreshToken: false },
  });
}

function browserSessionFrom(session: Session): BrowserSession {
  return {
    accessToken: session.access_token,
    refreshToken: session.refresh_token,
    expiresAt: (session.expires_at ?? Math.floor(Date.now() / 1000) + session.expires_in) * 1000,
    user: { id: session.user.id, email: session.user.email },
  };
}

function passkeyError(error: Error | { message?: string } | null) {
  return error instanceof Error ? error : new Error(error?.message ?? "Passkey-Anmeldung ist nicht möglich.");
}

export function isPasskeySupported() {
  return typeof window !== "undefined" && "PublicKeyCredential" in window;
}

export function isPasskeyCancellation(error: unknown) {
  if (error instanceof DOMException && (error.name === "AbortError" || error.name === "NotAllowedError")) return true;
  return typeof error === "object" && error !== null && "code" in error && (error as { code?: string }).code === "ERROR_CEREMONY_ABORTED";
}

export async function signInWithPasskey(club: ClubContext): Promise<BrowserSession> {
  if (!isPasskeySupported()) throw new Error("Dieses Gerät unterstützt keine Passkey-Anmeldung.");
  const { data, error } = await createPasskeyClient(club).auth.signInWithPasskey();
  if (error || !data.session || !data.user) throw passkeyError(error);
  return browserSessionFrom(data.session);
}

export async function registerPasskey(club: ClubContext, currentSession: BrowserSession) {
  if (!isPasskeySupported()) throw new Error("Dieses Gerät unterstützt keine Passkey-Anmeldung.");
  const client = createPasskeyClient(club);
  const { error: sessionError } = await client.auth.setSession({
    access_token: currentSession.accessToken,
    refresh_token: currentSession.refreshToken,
  });
  if (sessionError) throw passkeyError(sessionError);
  const { error } = await client.auth.registerPasskey();
  if (error) throw passkeyError(error);
}
