"use client";

import { type FormEvent, useState } from "react";
import { QrScanner } from "../../components/scanner/QrScanner";

export function ClubSelection({ onSelect }: { onSelect: (code: string) => Promise<void> }) {
  const [code, setCode] = useState("");
  const [message, setMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [scannerOpen, setScannerOpen] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSubmitting(true); setMessage("");
    try { await onSelect(code); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Die Vereins-ID konnte nicht geprüft werden."); } finally { setSubmitting(false); }
  }
  async function scanned(value: string) { setScannerOpen(false); setCode(value); setSubmitting(true); setMessage(""); try { await onSelect(value); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Die Vereins-ID konnte nicht geprüft werden."); } finally { setSubmitting(false); } }
  return <main className="login-page"><section className="login-card" aria-labelledby="club-title"><div className="brand-mark" aria-hidden="true">K</div><p className="eyebrow">Kleingartenverein</p><h1 id="club-title">Verein auswählen</h1><p className="intro">Gib zuerst die Vereins-ID ein. Erst danach ist eine Anmeldung möglich.</p><form onSubmit={submit} className="login-form"><label htmlFor="club-code">Vereins-ID</label><input id="club-code" autoComplete="off" value={code} onChange={(event) => setCode(event.target.value)} placeholder="z. B. KGV-DEMO" required />{message && <p className="notice" role="alert">{message}</p>}<button disabled={submitting}>{submitting ? "Vereins-ID wird geprüft …" : "Verein bestätigen"}</button><button type="button" className="secondary-action" disabled={submitting} onClick={() => setScannerOpen(true)}>Vereins-QR-Code scannen</button></form><p className="fine-print">Die Vereins-ID bestimmt ausschließlich die Datenbank und Rechte deines Vereins.</p></section>{scannerOpen && <QrScanner title="Vereins-QR-Code scannen" hint="QR-Code des Vereins in den Rahmen halten." onScan={scanned} onClose={() => setScannerOpen(false)} />}</main>;
}
