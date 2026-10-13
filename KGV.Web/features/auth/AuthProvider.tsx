"use client";

import { createContext, type ReactNode, useCallback, useContext, useEffect, useRef, useState } from "react";
import {
  clearClub,
  clearSession,
  loadClub,
  signOut,
  type AppUserContext,
  type BrowserSession,
} from "../../lib/supabase-auth";
import { isSupabaseClubConfigured } from "../../lib/supabase/client-context";
import type { ClubContext } from "../../models/auth/club";
import { releaseAllBrowserEditLocks } from "../../services/concurrency/edit-lock-service";
import { loadAppUserContext } from "../../services/users/app-user-service";
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

  const establishSession = useCallback(async (candidate: BrowserSession, selectedClub: ClubContext) => {
    const userContext = await loadAppUserContext(selectedClub, candidate);
    sessionRef.current = candidate;
    setSession(candidate);
    setAppUserContext(userContext);
    setStatus("signed-in");
  }, []);

  useEffect(() => {
    let selectedClub: ClubContext | null = null;
    void Promise.resolve().then(async () => {
      selectedClub = loadClub();
      if (!selectedClub) {
        setStatus("club-selection");
        return;
      }
      setClub(selectedClub);
      if (!isSupabaseClubConfigured(selectedClub)) {
        setStatus("configuration-error");
        return;
      }
      const existing = await restoreBrowserSession(selectedClub);
      if (!existing) { setStatus("signed-out"); return; }
      await establishSession(existing, selectedClub);
    }).catch((error: Error) => {
      const invalidSession = sessionRef.current;
      if (invalidSession && selectedClub) void signOut(selectedClub, invalidSession);
      clearSession();
      sessionRef.current = null;
      setSession(null);
      setAppUserContext(null);
      setMessage(error.message);
      setStatus("access-error");
    });
  }, [establishSession]);

  const logout = useCallback((reason = "") => {
    const currentSession = sessionRef.current;
    if (currentSession && club) void releaseAllBrowserEditLocks({ club, session: currentSession }).catch(() => undefined).finally(() => signOut(club, currentSession));
    clearSession();
    sessionRef.current = null;
    setSession(null);
    setAppUserContext(null);
    setMessage(reason);
    setStatus("signed-out");
  }, [club]);

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
      if (club) {
        try { await releaseAllBrowserEditLocks({ club, session: currentSession }); } catch { /* Der lokale Vereinswechsel darf nicht blockiert werden. */ }
        try { await signOut(club, currentSession); } catch { /* Der lokale Vereinswechsel darf nicht blockiert werden. */ }
      }
    }
    clearClub();
    sessionRef.current = null;
    setSession(null);
    setAppUserContext(null);
    setClub(null);
    setMessage("");
    setStatus("club-selection");
  }, [club]);

  const login = useCallback(async (email: string, password: string) => {
    setMessage("");
    try {
      if (!club) throw new Error("Bitte zuerst einen Verein auswählen.");
      await establishSession(await signInWithPassword(club, email, password), club);
    } catch (error) {
      const invalidSession = sessionRef.current;
      if (invalidSession && club) void signOut(club, invalidSession);
      clearSession();
      sessionRef.current = null;
      setSession(null);
      setAppUserContext(null);
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

export function useSelectedClub(): ClubContext {
  const { club } = useAuth();
  if (!club) throw new Error("Bitte zuerst einen Verein auswählen.");
  return club;
}
