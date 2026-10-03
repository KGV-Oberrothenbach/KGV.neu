"use client";

import { type FormEvent, useState } from "react";

export function LoginForm({ onSignIn, disabled, message }: { onSignIn: (email: string, password: string) => Promise<void>; disabled: boolean; message: string }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true);
    setErrorMessage("");
    try {
      await onSignIn(email.trim(), password);
      setPassword("");
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Anmeldung nicht möglich.");
    } finally {
      setSubmitting(false);
    }
  }

  return <form onSubmit={handleSubmit} className="login-form" autoComplete="on">
    <label htmlFor="email">E-Mail-Adresse</label>
    <input id="email" name="username" type="email" autoComplete="username" value={email} onChange={(event) => setEmail(event.target.value)} required />
    <label htmlFor="password">Passwort</label>
    <input id="password" name="password" type="password" autoComplete="current-password" value={password} onChange={(event) => setPassword(event.target.value)} required />
    {(message || errorMessage) && <p className="notice" role="alert">{errorMessage || message}</p>}
    <button type="submit" disabled={submitting || disabled}>{submitting ? "Anmeldung wird geprüft …" : "Anmelden"}</button>
  </form>;
}
