"use client";

import { useState } from "react";
import type { ClubContext } from "../../models/auth/club";
import { requestFirstLoginOtp, requestPasswordRecoveryOtp, setPasswordAfterOtp, verifyFirstLoginOtp } from "../../services/auth/auth-service";
import { OtpRequestForm } from "./OtpRequestForm";
import { OtpVerifyForm } from "./OtpVerifyForm";
import { SetPasswordForm } from "./SetPasswordForm";

type OtpMode = "login" | "request" | "verify" | "set-password" | "completed";
type OtpPurpose = "first-login" | "password-reset";

export function OtpFlow({ club, disabled }: { club: ClubContext; disabled: boolean }) {
  const [mode, setMode] = useState<OtpMode>("login");
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState("");
  const [recoveryAccessToken, setRecoveryAccessToken] = useState("");
  const [purpose, setPurpose] = useState<OtpPurpose>("first-login");

  async function request(emailAddress: string) {
    const result = purpose === "first-login"
      ? await requestFirstLoginOtp(club, emailAddress)
      : await requestPasswordRecoveryOtp(club, emailAddress);
    setEmail(emailAddress);
    setMessage(result.message);
    setMode("verify");
  }

  async function verify(code: string) {
    const result = await verifyFirstLoginOtp(club, email, code);
    setRecoveryAccessToken(result.recoveryAccessToken);
    setMessage("Code bestätigt. Neues Passwort setzen.");
    setMode("set-password");
  }

  async function setPassword(password: string, confirmation: string) {
    await setPasswordAfterOtp(club, recoveryAccessToken, password, confirmation);
    setRecoveryAccessToken("");
    setEmail("");
    setMessage("Passwort gesetzt. Bitte normal anmelden.");
    setMode("completed");
  }

  function returnToLogin() {
    setRecoveryAccessToken("");
    setEmail("");
    setMessage("");
    setMode("login");
  }

  function startRequest(nextPurpose: OtpPurpose) {
    setPurpose(nextPurpose);
    setMode("request");
  }

  if (mode === "login") return <div className="login-form"><button className="secondary-action" type="button" disabled={disabled} onClick={() => startRequest("first-login")}>Einladung / Erstlogin-Code anfordern</button><button className="secondary-action" type="button" disabled={disabled} onClick={() => startRequest("password-reset")}>Passwort vergessen</button></div>;
  if (mode === "request") return <OtpRequestForm onRequest={request} onCancel={returnToLogin} disabled={disabled} submitLabel={purpose === "first-login" ? "Einladung / Erstlogin-Code anfordern" : "Passwort-Reset anfordern"} />;
  if (mode === "verify") return <><p className="notice" role="status">{message}</p><OtpVerifyForm email={email} onVerify={verify} onCancel={returnToLogin} disabled={disabled} /></>;
  if (mode === "set-password") return <><p className="notice" role="status">{message}</p><SetPasswordForm onSetPassword={setPassword} onCancel={returnToLogin} disabled={disabled} /></>;
  return <><p className="notice" role="status">{message}</p><button type="button" className="secondary-action" onClick={returnToLogin}>Zurück zum Login</button></>;
}
