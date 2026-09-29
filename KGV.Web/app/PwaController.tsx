"use client";

import { useEffect, useState } from "react";

type InstallPromptEvent = Event & {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
};

export function PwaController() {
  const [installPrompt, setInstallPrompt] = useState<InstallPromptEvent | null>(null);

  useEffect(() => {
    if (!("serviceWorker" in navigator)) return;
    if (process.env.NODE_ENV !== "production") {
      // Eine Entwicklungsinstanz auf localhost darf keinen alten Offline-Stand
      // festhalten. Die installierbare Produktionsversion registriert den
      // Service Worker weiterhin ausschließlich über HTTPS.
      navigator.serviceWorker.getRegistrations().then((registrations) => {
        registrations.forEach((registration) => registration.unregister());
      });
      caches.keys().then((names) => {
        names.filter((name) => name.startsWith("kgv-shell-")).forEach((name) => caches.delete(name));
      });
      return;
    }
    {
      navigator.serviceWorker.register("/service-worker.js").catch(() => {
        // Die App funktioniert weiterhin online, wenn ein Browser keinen Service Worker zulässt.
      });
    }
    const onBeforeInstall = (event: Event) => {
      event.preventDefault();
      setInstallPrompt(event as InstallPromptEvent);
    };
    window.addEventListener("beforeinstallprompt", onBeforeInstall);
    return () => window.removeEventListener("beforeinstallprompt", onBeforeInstall);
  }, []);

  if (!installPrompt) return null;
  return <button className="install-button" onClick={async () => { await installPrompt.prompt(); setInstallPrompt(null); }}>App installieren</button>;
}
