import type { ClubContext } from "../../models/auth/club";

type SignInResponse = {
  access_token?: string;
  refresh_token?: string;
  expires_in?: number;
  user?: { id?: string; email?: string };
  error_description?: string;
  msg?: string;
};

type OtpRequestResponse = {
  success?: boolean;
  message?: string;
  diagnosticCode?: string;
};

type OtpVerificationResponse = {
  access_token?: string;
  user?: { id?: string };
  error_description?: string;
  msg?: string;
};

function clubConfig(club: ClubContext) {
  const url = club.supabaseUrl.trim().replace(/\/$/, "");
  const publishableKey = club.supabasePublishableKey.trim();
  if (!url || !publishableKey) throw new Error("Die Browser-App ist noch nicht mit Supabase konfiguriert.");
  return { url, publishableKey };
}

export async function signInWithPassword(club: ClubContext, email: string, password: string): Promise<SignInResponse> {
  const { url, publishableKey } = clubConfig(club);

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

export async function requestFirstLoginOtp(club: ClubContext, email: string): Promise<OtpRequestResponse> {
  const { url, publishableKey } = clubConfig(club);
  const response = await fetch(`${url}/functions/v1/kgv-request-first-login-otp`, {
    method: "POST",
    headers: { apikey: publishableKey, "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  });
  const result = await response.json() as OtpRequestResponse;
  if (!response.ok || !result.success) {
    throw new Error(result.message ?? "OTP-Anforderung fehlgeschlagen. Bitte prüfe die E-Mail-Adresse oder kontaktiere den Vorstand.");
  }
  return result;
}

export async function verifyRecoveryOtp(club: ClubContext, email: string, code: string): Promise<OtpVerificationResponse> {
  const { url, publishableKey } = clubConfig(club);
  const response = await fetch(`${url}/auth/v1/verify`, {
    method: "POST",
    headers: { apikey: publishableKey, "Content-Type": "application/json" },
    body: JSON.stringify({ email, token: code, type: "recovery" }),
  });
  const result = await response.json() as OtpVerificationResponse;
  if (!response.ok || !result.access_token || !result.user?.id) {
    throw new Error(result.error_description ?? result.msg ?? "Code ungültig.");
  }
  return result;
}
