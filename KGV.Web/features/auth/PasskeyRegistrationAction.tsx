"use client";

import { useState } from "react";
import { useAuth } from "./AuthProvider";

export function PasskeyRegistrationAction() {
  const { registerPasskey } = useAuth();
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState("");

  async function register() {
    setPending(true);
    setMessage("");
    try {
      await registerPasskey();
      setMessage("Passkey wurde für diesen Verein eingerichtet.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Passkey konnte nicht eingerichtet werden.");
    } finally {
      setPending(false);
    }
  }

  return <span className="passkey-registration">
    <button type="button" className="text-button" disabled={pending} onClick={() => void register()}>{pending ? "Passkey wird eingerichtet …" : "Passkey einrichten"}</button>
    {message && <span className="notice" role="status">{message}</span>}
  </span>;
}
