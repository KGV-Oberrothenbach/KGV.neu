"use client";

import { useState } from "react";
import { NfcScanner } from "../../components/scanner/NfcScanner";
import type { BrowserSession } from "../../lib/supabase-auth";
import { resolveRfidScanContext, type RfidScanResolution } from "../../services/rfid/rfid-service";

export function RfidBrowserScanner({ session, onResolved }: { session: BrowserSession; onResolved: (resolution: RfidScanResolution) => void }) {
  const [message, setMessage] = useState("");

  async function resolve(uid: string) {
    const resolution = await resolveRfidScanContext(session, uid);
    onResolved(resolution);
    setMessage(`RFID ${resolution.normalizedUid} wurde gelesen.`);
  }

  return <><NfcScanner onScan={resolve} onError={setMessage} />{message && <p className="notice" role="status">{message}</p>}</>;
}
