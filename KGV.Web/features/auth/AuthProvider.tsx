"use client";

import { createContext, type ReactNode, useCallback, useContext, useEffect, useRef, useState } from "react";
import {
  clearClub,
  clearSession,
  isConfigured,
  loadAppUserContext,
  loadClub,
  releaseAllBrowserEditLocks,
  signOut,
  type AppUserContext,
  type BrowserSession,
} from "../../lib/supabase-auth";
import type { ClubContext } from "../../models/auth/club";
import { signIn as signInWithPassword } from "../../services/auth/auth-service";
import { resolveClub } from "../../services/auth/club-service";
import { restoreBrowserSession, startInactivityMonitor, startSessionRefreshMonitor } from "../../services/auth/session-service";

export type AuthStatus = "checking" | "club-selection" | "signed-out" | "signed-in" | "configuration-error" | "access-error";

type AuthContextValue = {
  status: AuthStatus;
  session: BrowserSession | null;
  appUserContext: AppUserContext | null;
  club: ClubContext | null;
  message: string;
  selectClub: (code: string) => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  logout: (reason?: string) => void;
  changeClub: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>("checking");
  const [session, setSession] = useState<BrowserSession | null>(null);
  const [appUserContext, setAppUserContext] = useState<AppUserContext | null>(null);
  const [club, setClub] = useState<ClubContext | null>(null);
  const [message, setMessage] = useState("");
  const sessionRef = useRef<BrowserSession | null>(null);

  useEffect(() => {
    sessionRef.current = session;
  }, [session]);

  const establishSession = useCallback(async (candidate: BrowserSession) => {
    const userContext = await loadAppUserContext(candidate);
    sessionRef.current = candidate;
    setSession(candidate);
    setAppUserContext(userContext);
    setStatus("signed-in");
  }, []);

  useEffect(() => {
    void Promise.resolve().then(async () => {
      const selectedClub = loadClub();
      if (!selectedClub) {
        setStatus("club-selection");
        return;
      }
      setClub(selectedClub);
      if (!isConfigured()) {
        setStatus("configuration-error");
        return;
      }
      const existing = await restoreBrowserSession(selectedClub);
      if (!existing) { setStatus("signed-out"); return; }
      await establishSession(existing);
    }).catch((error: Error) => {
      clearSession();
      setMessage(error.message);
      setStatus("access-error");
    });
  }, [establishSession]);

  const logout = useCallback((reason = "") => {
    const currentSession = sessionRef.current;
    if (currentSession) void releaseAllBrowserEditLocks(currentSession).catch(() => undefined).finally(() => signOut(currentSession));
    clearSession();
    sessionRef.current = null;
    setSession(null);
    setAppUserContext(null);
    setMessage(reason);
    setStatus("signed-out");
  }, []);

  const userId = session?.user.id;
  useEffect(() => {
    if (status !== "signed-in" || !userId) return;
    return startInactivityMonitor({
      vereinId: club?.vereinId,
      userId,
      onTimeout: () => logout("Du wurdest nach 15 Minuten Inaktivität automatisch abgemeldet."),
    });
  }, [status, userId, club?.vereinId, logout]);

  useEffect(() => {
    if (status !== "signed-in" || !session || !club) return;
    return startSessionRefreshMonitor({
      club,
      session,
      onSessionRefreshed: (refreshedSession) => {
        sessionRef.current = refreshedSession;
        setSession(refreshedSession);
      },
      onSessionExpired: () => logout(),
    });
  }, [status, session, club, logout]);

  const selectClub = useCallback(async (code: string) => {
    const selectedClub = await resolveClub(code);
    setClub(selectedClub);
    setStatus("signed-out");
  }, []);

  const changeClub = useCallback(async () => {
    const currentSession = sessionRef.current;
    if (currentSession) {
      try { await releaseAllBrowserEditLocks(currentSession); } catch { /* Der lokale Vereinswechsel darf nicht blockiert werden. */ }
      try { await signOut(currentSession); } catch { /* Der lokale Vereinswechsel darf nicht blockiert werden. */ }
    }
    clearClub();
    sessionRef.current = null;
    setSession(null);
    setAppUserContext(null);
    setClub(null);
    setMessage("");
    setStatus("club-selection");
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    setMessage("");
    try {
      await establishSession(await signInWithPassword(club!, email, password));
    } catch (error) {
      clearSession();
      setStatus("access-error");
      throw error;
    }
  }, [club, establishSession]);

  return <AuthContext.Provider value={{ status, session, appUserContext, club, message, selectClub, login, logout, changeClub }}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error("useAuth muss innerhalb eines AuthProvider verwendet werden.");
  return value;
}
