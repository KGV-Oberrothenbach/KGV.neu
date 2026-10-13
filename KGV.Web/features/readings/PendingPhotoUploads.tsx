"use client";
import { useSelectedClub } from "../auth/AuthProvider";

import { useCallback, useEffect, useRef, useState } from "react";
import type { BrowserSession } from "../../lib/supabase-auth";
import { subscribePendingMediaChanges } from "../../services/offline/pending-media-store";
import { countLegacyPendingReadingPhotos, deleteLegacyPendingReadingPhotos, deletePendingReadingPhoto, listPendingReadingPhotos, retryAllPendingReadingPhotos, retryPendingReadingPhoto, type PendingMeterPhoto } from "../../services/readings/reading-photo-service";
import { getWifiOnlyPreference } from "../../lib/browser/storage";
import { subscribeConnectivityChanges } from "../../lib/browser/connectivity";

export function PendingPhotoUploads({ session, clubId }: { session: BrowserSession; clubId: string }) {
  const club = useSelectedClub();
  const mounted = useRef(true);
  const [items, setItems] = useState<PendingMeterPhoto[]>([]);
  const [legacyCount, setLegacyCount] = useState(0);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [wifiOnly] = useState(getWifiOnlyPreference);

  const load = useCallback(async () => {
    try {
      const [nextItems, nextLegacyCount] = await Promise.all([listPendingReadingPhotos(session, clubId), countLegacyPendingReadingPhotos(clubId)]);
      if (!mounted.current) return;
      setItems(nextItems);
      setLegacyCount(nextLegacyCount);
    } catch (cause) {
      if (mounted.current) setMessage(cause instanceof Error ? cause.message : "Lokale Foto-Warteschlange konnte nicht geladen werden.");
    }
  }, [session, clubId]);

  const retryAll = useCallback(async () => {
    if (busy) return;
    setBusy(true);
    try {
      const result = await retryAllPendingReadingPhotos(club, session, clubId, wifiOnly);
      await load();
      if (mounted.current) setMessage(result.uploaded || result.failed ? `${result.uploaded} Foto(s) hochgeladen, ${result.failed} fehlgeschlagen.` : result.queued ? "Das aktuelle Netz erlaubt keinen Upload. Fotos bleiben lokal." : "Keine offenen Foto-Uploads vorhanden.");
    } finally {
      if (mounted.current) setBusy(false);
    }
  }, [busy, session, clubId, wifiOnly, load, club]);

  const retryOne = useCallback(async (item: PendingMeterPhoto) => {
    if (busy) return;
    setBusy(true);
    try {
      const result = await retryPendingReadingPhoto(club, session, clubId, wifiOnly, item);
      await load();
      if (mounted.current) setMessage(result.message);
    } finally {
      if (mounted.current) setBusy(false);
    }
  }, [busy, session, clubId, wifiOnly, load, club]);

  const remove = useCallback(async (item: PendingMeterPhoto) => {
    const warning = item.uploadedFileId
      ? "Die Datei wurde möglicherweise bereits hochgeladen, ist aber noch nicht mit der Ablesung verknüpft. Durch Löschen wird die automatische Verknüpfung aufgegeben."
      : "Dieses lokal gespeicherte Foto endgültig aus der Warteschlange löschen? Ein späterer automatischer Upload ist danach nicht mehr möglich.";
    if (!window.confirm(warning)) return;
    setBusy(true);
    try {
      const result = await deletePendingReadingPhoto(session, clubId, item.id);
      await load();
      if (mounted.current) setMessage(result.message);
    } finally {
      if (mounted.current) setBusy(false);
    }
  }, [session, clubId, load]);

  const removeLegacy = useCallback(async () => {
    if (!window.confirm("Ältere, keinem Benutzer zuordenbare Fotoeinträge endgültig löschen? Sie können danach nicht wiederhergestellt oder automatisch hochgeladen werden.")) return;
    setBusy(true);
    try {
      const removed = await deleteLegacyPendingReadingPhotos(clubId);
      await load();
      if (mounted.current) setMessage(`${removed} Alt-Daten wurden endgültig gelöscht.`);
    } finally {
      if (mounted.current) setBusy(false);
    }
  }, [clubId, load]);

  useEffect(() => {
    mounted.current = true;
    void Promise.resolve().then(load);
    const refresh = () => { void load(); };
    const unsubscribeConnectivity = subscribeConnectivityChanges(refresh);
    const unsubscribeBroadcast = subscribePendingMediaChanges(refresh);
    return () => { mounted.current = false; unsubscribeConnectivity(); unsubscribeBroadcast(); };
  }, [load]);

  return <section className="data-workspace pending-photos"><div className="data-toolbar"><span>{items.length ? `${items.length} offene Foto-Uploads` : "Keine offenen Foto-Uploads"}</span><button disabled={!items.length || busy} onClick={() => void retryAll()}>{busy ? "Uploads laufen …" : "Alle erneut versuchen"}</button></div><p className="context-note">Fotos bleiben bis zum erfolgreichen Upload ausschließlich in diesem Browser sowie deinem Vereins- und Benutzerkontext gespeichert.</p>{message && <p className="notice" role="status">{message}</p>}{legacyCount > 0 && <section className="parcel-section"><p>Es befinden sich {legacyCount} ältere, keinem Benutzer eindeutig zuordenbare Fotoeinträge in diesem Browser.</p><button className="secondary-action" disabled={busy} onClick={() => void removeLegacy()}>Alt-Daten endgültig löschen</button></section>}<div className="pending-photo-list">{items.map((item) => <article key={item.id}><LocalPhotoThumbnail item={item} /><div><strong>{item.fileName}</strong><span>Garten {item.details.garten} · {item.details.medium} · Ablesung #{item.readingId}</span><span>{new Intl.DateTimeFormat("de-DE", { dateStyle: "short", timeStyle: "short" }).format(new Date(item.createdAt))} · Versuch {item.attemptCount}</span><small>{item.status === "failed" ? `Fehlgeschlagen: ${item.lastError ?? "erneuter Versuch möglich"}` : item.status === "uploading" ? "Wird hochgeladen …" : "Lokal gespeichert, Upload ausstehend"}</small></div><div><button className="secondary-action" onClick={() => preview(item)}>Anzeigen</button><button disabled={busy} onClick={() => void retryOne(item)}>Erneut versuchen</button><button className="reject-action" disabled={busy} onClick={() => void remove(item)}>Löschen</button></div></article>)}{items.length === 0 && <p className="empty-state">Es sind keine eigenen lokalen Ablesefotos vorgemerkt.</p>}</div></section>;
}

function preview(item: PendingMeterPhoto) {
  const url = URL.createObjectURL(item.content);
  window.open(url, "_blank", "noopener,noreferrer");
  window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
}

function LocalPhotoThumbnail({ item }: { item: PendingMeterPhoto }) {
  const [url, setUrl] = useState("");
  useEffect(() => {
    const next = URL.createObjectURL(item.content);
    let active = true;
    queueMicrotask(() => { if (active) setUrl(next); });
    return () => { active = false; URL.revokeObjectURL(next); };
  }, [item.id, item.content]);
  return <div className="pending-photo-thumb">{url && <>{/* Blob-Vorschau bleibt lokal und ist nicht über Next Image optimierbar. */}<img src={url} alt="Lokale Vorschau des Ablesefotos" /></>}</div>;
}
