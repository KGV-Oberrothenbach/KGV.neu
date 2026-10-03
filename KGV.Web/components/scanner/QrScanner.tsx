"use client";

import { useEffect, useRef, useState } from "react";
import { barcodeDetectorConstructor } from "../../lib/browser-media";

export function QrScanner({ title, hint, onScan, onClose }: { title: string; hint: string; onScan: (value: string) => void; onClose: () => void }) {
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const scanCallback = useRef(onScan);
  const detectorAvailable = Boolean(barcodeDetectorConstructor());
  const [message, setMessage] = useState(() => detectorAvailable
    ? "Kamera wird vorbereitet …"
    : "Dieser Browser unterstützt das direkte QR-Scannen nicht. Bitte die Vereins-ID von Hand eingeben oder Chrome/Edge auf einem unterstützten Gerät verwenden.");
  useEffect(() => { scanCallback.current = onScan; }, [onScan]);
  useEffect(() => {
    if (!detectorAvailable) return;
    let active = true; let stream: MediaStream | null = null; let frame = 0; let detecting = false;
    const Detector = barcodeDetectorConstructor()!; const detector = new Detector({ formats: ["qr_code"] });
    navigator.mediaDevices?.getUserMedia({ video: { facingMode: { ideal: "environment" } }, audio: false }).then((nextStream) => {
      if (!active || !videoRef.current) { nextStream.getTracks().forEach((track) => track.stop()); return; }
      stream = nextStream; videoRef.current.srcObject = stream; void videoRef.current.play(); setMessage(hint);
      const scan = async () => { if (!active) return; const video = videoRef.current; if (video && video.readyState >= 2 && !detecting) { detecting = true; try { const result = await detector.detect(video); const value = result[0]?.rawValue?.trim(); if (value) { scanCallback.current(value); return; } } catch { setMessage("Der QR-Code konnte noch nicht gelesen werden."); } finally { detecting = false; } } frame = requestAnimationFrame(scan); };
      frame = requestAnimationFrame(scan);
    }).catch(() => setMessage("Die Kamera konnte nicht geöffnet werden. Bitte Kameraberechtigung prüfen oder die Vereins-ID von Hand eingeben."));
    return () => { active = false; cancelAnimationFrame(frame); stream?.getTracks().forEach((track) => track.stop()); };
  }, [detectorAvailable, hint]);
  return <section className="scanner-overlay" role="dialog" aria-modal="true" aria-labelledby="scanner-title"><div className="scanner-card"><div className="detail-title"><div><h2 id="scanner-title">{title}</h2><p>{message}</p></div><button className="secondary-action" onClick={onClose}>Schließen</button></div>{detectorAvailable && <video ref={videoRef} muted playsInline aria-label="Kamerabild für QR-Code" />}<div className="scanner-frame" aria-hidden="true" /></div></section>;
}
