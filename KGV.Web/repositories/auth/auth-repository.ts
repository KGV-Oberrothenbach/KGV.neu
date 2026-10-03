import type { ClubContext } from "../../models/auth/club";

type SignInResponse = {
  access_token?: string;
  refresh_token?: string;
  expires_in?: number;
  user?: { id?: string; email?: string };
  error_description?: string;
  msg?: string;
};

export async function signInWithPassword(club: ClubContext, email: string, password: string): Promise<SignInResponse> {
  const url = club.supabaseUrl.trim().replace(/\/$/, "");
  const publishableKey = club.supabasePublishableKey.trim();
  if (!url || !publishableKey) throw new Error("Die Browser-App ist noch nicht mit Supabase konfiguriert.");

  const response = await fetch(`${url}/auth/v1/token?grant_type=password`, {
    method: "POST",
    headers: { apikey: publishableKey, "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  const result = await response.json() as SignInResponse;
  if (!response.ok || !result.access_token || !result.refresh_token || !result.user?.id) {
    throw new Error(result.error_description ?? result.msg ?? "Anmeldung nicht möglich.");
  }
  return result;
}
