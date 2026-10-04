import { browserSessionStorageKey, clearSession, loadStoredSession, saveSession, type BrowserSession } from "../../lib/supabase-auth";
import type { ClubContext } from "../../models/auth/club";
import { RefreshSessionRejectedError, refreshBrowserSession } from "../../repositories/auth/auth-repository";

const inactivityMs = 15 * 60 * 1000;
const refreshSafetyWindowMs = 60 * 1000;
const refreshRetryDelayMs = 15 * 1000;

function latestSessionFor(current: BrowserSession) {
  const stored = loadStoredSession();
  return stored?.user.id === current.user.id ? stored : current;
}

async function refreshSession(club: ClubContext, current: BrowserSession): Promise<BrowserSession> {
  const source = latestSessionFor(current);
  if (source.expiresAt > Date.now() + refreshSafetyWindowMs) return source;

  try {
    const refreshed = await refreshBrowserSession(club, source.refreshToken);
    const session: BrowserSession = {
      accessToken: refreshed.access_token!,
      refreshToken: refreshed.refresh_token!,
      expiresAt: Date.now() + refreshed.expires_in! * 1000,
      user: { id: refreshed.user!.id!, email: refreshed.user!.email ?? source.user.email },
      isDemoAccount: source.isDemoAccount,
    };
    saveSession(session);
    return session;
  } catch (error) {
    const newer = latestSessionFor(source);
    if (error instanceof RefreshSessionRejectedError && newer.refreshToken !== source.refreshToken) return newer;
    throw error;
  }
}

export async function restoreBrowserSession(club: ClubContext): Promise<BrowserSession | null> {
  const storedSession = loadStoredSession();
  if (!storedSession) return null;
  if (storedSession.expiresAt > Date.now() + refreshSafetyWindowMs) return storedSession;

  try {
    return await refreshSession(club, storedSession);
  } catch (error) {
    if (error instanceof RefreshSessionRejectedError) clearSession();
    return null;
  }
}

export function startSessionRefreshMonitor({ club, session, onSessionRefreshed, onSessionExpired }: {
  club: ClubContext;
  session: BrowserSession;
  onSessionRefreshed: (session: BrowserSession) => void;
  onSessionExpired: () => void;
}) {
  let timer = 0;
  let retried = false;
  let stopped = false;
  let current = session;

  const schedule = (next: BrowserSession, delay?: number) => {
    current = next;
    window.clearTimeout(timer);
    const untilRefresh = Math.max(0, next.expiresAt - Date.now() - refreshSafetyWindowMs);
    timer = window.setTimeout(() => { void refresh(); }, delay ?? untilRefresh);
  };
  const expire = () => {
    window.clearTimeout(timer);
    if (!stopped) onSessionExpired();
  };
  const refresh = async () => {
    try {
      const refreshed = await refreshSession(club, current);
      if (stopped) return;
      retried = false;
      onSessionRefreshed(refreshed);
      schedule(refreshed);
    } catch (error) {
      if (stopped) return;
      if (error instanceof RefreshSessionRejectedError) {
        clearSession();
        expire();
        return;
      }
      const remaining = current.expiresAt - Date.now();
      if (!retried && remaining > refreshRetryDelayMs) {
        retried = true;
        schedule(current, refreshRetryDelayMs);
        return;
      }
      if (remaining > 0) {
        window.clearTimeout(timer);
        timer = window.setTimeout(expire, remaining);
        return;
      }
      expire();
    }
  };
  const synchronize = (event: StorageEvent) => {
    if (event.key !== browserSessionStorageKey) return;
    const stored = loadStoredSession();
    if (!stored || stored.user.id !== current.user.id) {
      expire();
      return;
    }
    if (stored.accessToken !== current.accessToken || stored.refreshToken !== current.refreshToken || stored.expiresAt !== current.expiresAt) {
      retried = false;
      onSessionRefreshed(stored);
      schedule(stored);
    }
  };

  window.addEventListener("storage", synchronize);
  schedule(session);
  return () => {
    stopped = true;
    window.clearTimeout(timer);
    window.removeEventListener("storage", synchronize);
  };
}

export function startInactivityMonitor({ vereinId, userId, onTimeout }: { vereinId?: string; userId: string; onTimeout: () => void }) {
  const activityKey = `kgv.browser.lastActivity.v1.${vereinId ?? "unknown"}.${userId}`;
  let lastActivity = Date.now();
  let lastStored = 0;
  let timer = 0;

  const schedule = () => {
    window.clearTimeout(timer);
    const remaining = Math.max(0, inactivityMs - (Date.now() - lastActivity));
    timer = window.setTimeout(onTimeout, remaining);
  };
  const activity = () => {
    const now = Date.now();
    lastActivity = now;
    if (now - lastStored >= 1000) {
      lastStored = now;
      window.localStorage.setItem(activityKey, String(now));
    }
    schedule();
  };
  const sharedActivity = (event: StorageEvent) => {
    if (event.key !== activityKey || !event.newValue) return;
    const value = Number(event.newValue);
    if (Number.isFinite(value) && value > lastActivity) { lastActivity = value; schedule(); }
  };
  const events: Array<keyof WindowEventMap> = ["pointerdown", "pointermove", "keydown", "touchstart", "scroll"];
  events.forEach((name) => window.addEventListener(name, activity, { passive: true }));
  window.addEventListener("storage", sharedActivity);
  activity();
  return () => {
    window.clearTimeout(timer);
    events.forEach((name) => window.removeEventListener(name, activity));
    window.removeEventListener("storage", sharedActivity);
  };
}
