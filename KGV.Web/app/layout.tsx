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
        {children}<PwaController />
      </body>
    </html>
  );
}
