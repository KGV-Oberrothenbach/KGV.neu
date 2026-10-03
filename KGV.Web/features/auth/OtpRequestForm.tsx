"use client";

import { type FormEvent, useState } from "react";

export function OtpRequestForm({ onRequest, onCancel, disabled }: { onRequest: (email: string) => Promise<void>; onCancel: () => void; disabled: boolean }) {
  const [email, setEmail] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true);
    setErrorMessage("");
    try {
      await onRequest(email.trim());
    } catch (cause) {
      setErrorMessage(cause instanceof Error ? cause.message : "OTP-Anforderung fehlgeschlagen. Bitte prüfe die E-Mail-Adresse oder kontaktiere den Vorstand.");
    } finally {
      setSubmitting(false);
    }
  }

  return <form onSubmit={submit} className="login-form"><label htmlFor="otp-request-email">E-Mail-Adresse</label><input id="otp-request-email" type="email" autoComplete="email" value={email} onChange={(event) => setEmail(event.target.value)} required />{errorMessage && <p className="notice" role="alert">{errorMessage}</p>}<button disabled={disabled || submitting}>{submitting ? "Code wird angefordert …" : "Einladung / Erstlogin-Code anfordern"}</button><button type="button" className="secondary-action" disabled={submitting} onClick={onCancel}>Zurück zum Login</button></form>;
}
