import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { validateNewPassword } from "../../lib/auth/password-policy";
import { requestFirstLoginOtp as requestFirstLoginOtpFromRepository, signInWithPassword, updatePasswordWithRecoveryToken, verifyRecoveryOtp } from "../../repositories/auth/auth-repository";

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

export async function requestFirstLoginOtp(club: ClubContext, email: string) {
  const emailTrim = email.trim();
  if (!emailTrim) throw new Error("Bitte E-Mail eingeben.");
  const result = await requestFirstLoginOtpFromRepository(club, emailTrim);
  return { message: result.message ?? "Einladungs-/Erstlogin-Code wurde versendet. Bitte OTP eingeben." };
}

export async function verifyFirstLoginOtp(club: ClubContext, email: string, code: string) {
  const emailTrim = email.trim();
  const codeTrim = code.trim();
  if (!emailTrim || !codeTrim) throw new Error("Code ungültig.");
  const result = await verifyRecoveryOtp(club, emailTrim, codeTrim);
  return { recoveryAccessToken: result.access_token! };
}

export async function setPasswordAfterOtp(club: ClubContext, recoveryAccessToken: string, password: string, confirmation: string) {
  const validationError = validateNewPassword(password, confirmation);
  if (validationError) throw new Error(validationError);
  if (!recoveryAccessToken) throw new Error("Der Bestätigungscode ist nicht mehr gültig. Bitte erneut anfordern.");
  await updatePasswordWithRecoveryToken(club, recoveryAccessToken, password);
}
