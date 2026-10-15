"use client";

import { useEffect, useState } from "react";

const fallbackVersion = "Version unbekannt";
let appVersionPromise: Promise<string> | undefined;

function loadAppVersion(): Promise<string> {
  if (!appVersionPromise) {
    appVersionPromise = fetch("/release-info.json", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`release-info.json returned ${response.status}`);
        }

        const releaseInfo: unknown = await response.json();
        if (
          typeof releaseInfo === "object" &&
          releaseInfo !== null &&
          "displayVersion" in releaseInfo &&
          typeof releaseInfo.displayVersion === "string" &&
          releaseInfo.displayVersion.trim()
        ) {
          return releaseInfo.displayVersion;
        }

        throw new Error("release-info.json has no displayVersion");
      })
      .catch((error: unknown) => {
        console.warn("Unable to load the web release version.", error);
        return fallbackVersion;
      });
  }

  return appVersionPromise;
}

export function useAppVersion(): string {
  const [appVersion, setAppVersion] = useState(fallbackVersion);

  useEffect(() => {
    let active = true;

    void loadAppVersion().then((version) => {
      if (active) setAppVersion(version);
    });

    return () => {
      active = false;
    };
  }, []);

  return appVersion;
}
