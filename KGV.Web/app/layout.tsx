import type { Metadata } from "next";
import "./globals.css";
import { PwaController } from "./PwaController";

export const metadata: Metadata = {
  title: "KGV Verwaltung",
  description: "Browser-App für die KGV-Verwaltung.",
  other: {
    "codex-preview": "development",
  },
  icons: {
    icon: "/favicon.svg",
    shortcut: "/favicon.svg",
  },
  manifest: "/manifest.webmanifest",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="de">
      <body className="antialiased">
        {process.env.NODE_ENV !== "production" && <script dangerouslySetInnerHTML={{ __html: `
          (function () {
            if (sessionStorage.getItem('kgv-dev-cache-reset') || !('serviceWorker' in navigator)) return;
            Promise.all([navigator.serviceWorker.getRegistrations(), caches.keys()]).then(function (result) {
              result[0].forEach(function (registration) { registration.unregister(); });
              return Promise.all(result[1].filter(function (name) { return name.indexOf('kgv-shell-') === 0; }).map(function (name) { return caches.delete(name); }));
            }).then(function () { sessionStorage.setItem('kgv-dev-cache-reset', '1'); location.reload(); });
          }());
        ` }} />}
        {children}<PwaController />
      </body>
    </html>
  );
}
