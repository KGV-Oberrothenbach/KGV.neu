"use client";

import { useEffect, useRef, useState } from "react";
import { ndefReaderConstructor, supportsWebNfc, type NdefReaderLike } from "../../lib/browser/media-capabilities";

export function NfcScanner({ onScan, onError }: { onScan: (uid: string) => void | Promise<void>; onError?: (message: string) => void }) {
  const mounted = useRef(true);
  const reader = useRef<NdefReaderLike | null>(null);
  const abortController = useRef<AbortController | null>(null);
  const [scanning, setScanning] = useState(false);
  const [message, setMessage] = useState(() => supportsWebNfc()
    ? "Web-NFC ist auf diesem Gerät verfügbar."
    : "Dieser Browser bietet kein nutzbares Web-NFC. Die manuelle Auswahl bleibt möglich.");
  const supported = supportsWebNfc();

  function updateMessage(next: string) {
    if (mounted.current) setMessage(next);
  }

  function stopScan() {
    if (reader.current) {
      reader.current.onreading = null;
      reader.current.onreadingerror = null;
      reader.current = null;
    }
    abortController.current?.abort();
    abortController.current = null;
    if (mounted.current) setScanning(false);
  }

  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
      stopScan();
    };
  }, []);

  async function start() {
    if (!supported || reader.current) return;
    const Reader = ndefReaderConstructor();
    if (!Reader) return;

    const nextReader = new Reader();
    const nextAbortController = new AbortController();
    reader.current = nextReader;
    abortController.current = nextAbortController;
    setScanning(true);
    updateMessage("NFC-Scan aktiv. Tag an das Gerät halten.");

    nextReader.onreadingerror = () => {
      if (reader.current !== nextReader) return;
      updateMessage("Der NFC-Tag konnte nicht gelesen werden.");
      stopScan();
    };
    nextReader.onreading = async (event) => {
      if (reader.current !== nextReader) return;
      const uid = (event.serialNumber ?? "").trim();
      if (!uid) {
        updateMessage("Der Browser hat keine NFC-UID geliefert.");
        stopScan();
        return;
      }
      stopScan();
      try {
        await onScan(uid);
        updateMessage("NFC-UID wurde gelesen.");
      } catch (cause) {
        const detail = cause instanceof Error ? cause.message : "Die NFC-UID konnte nicht verarbeitet werden.";
        updateMessage(detail);
        if (mounted.current) onError?.(detail);
      }
    };

    try {
      await nextReader.scan({ signal: nextAbortController.signal });
    } catch (cause) {
      if (nextAbortController.signal.aborted || reader.current !== nextReader) return;
      const detail = cause instanceof Error ? `Web-NFC konnte nicht gestartet werden: ${cause.message}` : "Web-NFC konnte nicht gestartet werden.";
      updateMessage(detail);
      if (mounted.current) onError?.(detail);
      stopScan();
    }
  }

  return <div className={supported ? "rfid-browser supported" : "rfid-browser"}><div><strong>{supported ? "NFC im Browser" : "NFC nicht verfügbar"}</strong><p>{message}</p></div>{supported && <button type="button" className="secondary-action" disabled={scanning} onClick={() => void start()}>{scanning ? "Scan läuft …" : "NFC scannen"}</button>}</div>;
}
