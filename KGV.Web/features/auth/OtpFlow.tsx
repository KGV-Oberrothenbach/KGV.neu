"use client";

import { useState } from "react";
import type { ClubContext } from "../../models/auth/club";
import { requestFirstLoginOtp, verifyFirstLoginOtp } from "../../services/auth/auth-service";
import { OtpRequestForm } from "./OtpRequestForm";
import { OtpVerifyForm } from "./OtpVerifyForm";

type OtpMode = "login" | "request" | "verify" | "verified";

export function OtpFlow({ club, disabled }: { club: ClubContext; disabled: boolean }) {
  const [mode, setMode] = useState<OtpMode>("login");
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState("");

  async function request(emailAddress: string) {
    const result = await requestFirstLoginOtp(club, emailAddress);
    setEmail(emailAddress);
    setMessage(result.message);
    setMode("verify");
  }

  async function verify(code: string) {
    await verifyFirstLoginOtp(club, email, code);
    setMessage("Code bestätigt. Neues Passwort setzen.");
    setMode("verified");
  }

  function returnToLogin() {
    setEmail("");
    setMessage("");
    setMode("login");
  }

  if (mode === "login") return <button className="secondary-action" type="button" disabled={disabled} onClick={() => setMode("request")}>Einladung / Erstlogin-Code anfordern</button>;
  if (mode === "request") return <OtpRequestForm onRequest={request} onCancel={returnToLogin} disabled={disabled} />;
  if (mode === "verify") return <><p className="notice" role="status">{message}</p><OtpVerifyForm email={email} onVerify={verify} onCancel={returnToLogin} disabled={disabled} /></>;
  return <><p className="notice" role="status">{message}</p><button type="button" className="secondary-action" onClick={returnToLogin}>Zurück zum Login</button></>;
}
