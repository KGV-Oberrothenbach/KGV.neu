"use client";

import { type FormEvent, useState } from "react";

export function SetPasswordForm({ onSetPassword, onCancel, disabled }: { onSetPassword: (password: string, confirmation: string) => Promise<void>; onCancel: () => void; disabled: boolean }) {
  const [password, setPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true);
    setErrorMessage("");
    try {
      await onSetPassword(password, confirmation);
      setPassword("");
      setConfirmation("");
    } catch (cause) {
      setErrorMessage(cause instanceof Error ? cause.message : "Neues Passwort konnte nicht gesetzt werden.");
    } finally {
      setSubmitting(false);
    }
  }

  return <form onSubmit={submit} className="login-form"><label htmlFor="new-password">Neues Passwort</label><input id="new-password" name="new-password" type="password" autoComplete="new-password" value={password} onChange={(event) => setPassword(event.target.value)} required /><label htmlFor="new-password-confirmation">Passwort wiederholen</label><input id="new-password-confirmation" name="new-password-confirmation" type="password" autoComplete="new-password" value={confirmation} onChange={(event) => setConfirmation(event.target.value)} required /><p className="fine-print">Mindestens 8 Zeichen, Groß- und Kleinbuchstaben, eine Zahl und ein Sonderzeichen.</p>{errorMessage && <p className="notice" role="alert">{errorMessage}</p>}<button disabled={disabled || submitting}>{submitting ? "Passwort wird gesetzt …" : "Neues Passwort setzen"}</button><button type="button" className="secondary-action" disabled={submitting} onClick={onCancel}>Zurück zum Login</button></form>;
}
