"use client";

import { useEffect, useState } from "react";

type InstallPromptEvent = Event & {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
};

export function PwaController() {
  const [installPrompt, setInstallPrompt] = useState<InstallPromptEvent | null>(null);

  useEffect(() => {
    if (process.env.NODE_ENV !== "production") {
      // Eine Entwicklungsinstanz auf localhost darf keinen alten Offline-Stand
      // festhalten. Die installierbare Produktionsversion registriert den
      // Service Worker weiterhin ausschließlich über HTTPS.
      if (sessionStorage.getItem("kgv-dev-cache-reset")) return;

      void (async () => {
        try {
          const [registrations, cacheNames] = await Promise.all([
            "serviceWorker" in navigator ? navigator.serviceWorker.getRegistrations() : Promise.resolve([]),
            "caches" in window ? caches.keys() : Promise.resolve([]),
          ]);

          await Promise.all([
            ...registrations.map((registration) => registration.unregister()),
            ...cacheNames
              .filter((name) => name.startsWith("kgv-shell-"))
              .map((name) => caches.delete(name)),
          ]);
        } catch {
          // Fehler beim Aufräumen dürfen die Entwicklungsinstanz nicht blockieren.
        } finally {
          sessionStorage.setItem("kgv-dev-cache-reset", "1");
          window.location.reload();
        }
      })();
      return;
    }

    if (!("serviceWorker" in navigator)) return;

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
