"use client";

import { type FormEvent, useState } from "react";

export function OtpVerifyForm({ email, onVerify, onCancel, disabled }: { email: string; onVerify: (code: string) => Promise<void>; onCancel: () => void; disabled: boolean }) {
  const [code, setCode] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true);
    setErrorMessage("");
    try {
      await onVerify(code.trim());
    } catch (cause) {
      setErrorMessage(cause instanceof Error ? cause.message : "Code ungültig.");
    } finally {
      setSubmitting(false);
    }
  }

  return <form onSubmit={submit} className="login-form"><p className="intro">Der Code wurde für <strong>{email}</strong> angefordert.</p><label htmlFor="otp-code">OTP-Code</label><input id="otp-code" autoComplete="one-time-code" inputMode="numeric" value={code} onChange={(event) => setCode(event.target.value)} required />{errorMessage && <p className="notice" role="alert">{errorMessage}</p>}<button disabled={disabled || submitting}>{submitting ? "Code wird geprüft …" : "Code prüfen"}</button><button type="button" className="secondary-action" disabled={submitting} onClick={onCancel}>Zurück zum Login</button></form>;
}
