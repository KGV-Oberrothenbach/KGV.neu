"use client";

import { useEffect, useState } from "react";
import {
  acquireBrowserEditLock,
  releaseBrowserEditLock,
  type BrowserSession,
} from "./supabase-auth";

export type EditLockState = {
  checking: boolean;
  acquired: boolean;
  message: string;
};

export function useEditLock(
  session: BrowserSession,
  entityType: string,
  entityId: string | number | null | undefined,
  enabled = true,
): EditLockState {
  const [state, setState] = useState<EditLockState>({ checking: false, acquired: !enabled || entityId == null, message: "" });
  const activeLock = enabled && entityId != null;

  useEffect(() => {
    if (!activeLock || entityId == null) return;

    let active = true;
    let acquired = false;
    const acquire = async (checking: boolean) => {
      if (checking) setState({ checking: true, acquired: false, message: "Bearbeitungssperre wird geprüft …" });
      try {
        const result = await acquireBrowserEditLock(session, entityType, entityId);
        if (!active) return;
        acquired = result.acquired;
        setState(result.acquired
          ? { checking: false, acquired: true, message: "" }
          : { checking: false, acquired: false, message: `Dieser Datensatz wird gerade von ${result.lockedByDisplayName} bearbeitet.` });
      } catch (cause) {
        if (!active) return;
        acquired = false;
        setState({ checking: false, acquired: false, message: cause instanceof Error ? cause.message : "Die Bearbeitungssperre konnte nicht geprüft werden." });
      }
    };

    void acquire(true);
    const refreshTimer = window.setInterval(() => void acquire(false), 4 * 60 * 1000);
    const refreshWhenVisible = () => { if (document.visibilityState === "visible") void acquire(false); };
    document.addEventListener("visibilitychange", refreshWhenVisible);

    return () => {
      active = false;
      window.clearInterval(refreshTimer);
      document.removeEventListener("visibilitychange", refreshWhenVisible);
      if (acquired) void releaseBrowserEditLock(session, entityType, entityId).catch(() => undefined);
    };
  }, [session, entityType, entityId, activeLock]);

  return activeLock ? state : { checking: false, acquired: true, message: "" };
}
