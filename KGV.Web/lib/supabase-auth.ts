import type { ClubContext } from "../models/auth/club";
import { isSupabaseClubConfigured, resolveSupabaseConfig } from "./supabase/client-context";

export type BrowserSession = {
  accessToken: string;
  refreshToken: string;
  expiresAt: number;
  user: { id: string; email?: string };
  isDemoAccount?: boolean;
};

export type AppUserContext = {
  userId: string;
  role: "admin" | "vorstand" | "user";
  mitgliedId: number | null;
  permissionGrants: number;
  permissionRevocations: number;
  isDemoAccount: boolean;
};

export type WorkspaceContext = {
  saisonId: number | null;
  saisonJahr: number | null;
  mitgliedId: number | null;
  parzelleId: number | null;
};

export const browserSessionStorageKey = "kgv.browser.session.v1";
const clubStorageKey = "kgv.browser.club.v1";
const workspaceStoragePrefix = "kgv.browser.workspace.v1";

function workspaceStorageKey() {
  return `${workspaceStoragePrefix}.${loadClub()?.vereinId ?? "unknown"}`;
}

export function isConfigured() {
  return isSupabaseClubConfigured(loadClub());
}

export function loadClub(): ClubContext | null {
  if (typeof window === "undefined") return null;
  try {
    const saved = window.localStorage.getItem(clubStorageKey);
    if (!saved) return null;
    const club = JSON.parse(saved) as ClubContext;
    return club.vereinsCode && club.vereinsname && club.supabaseUrl && club.supabasePublishableKey ? club : null;
  } catch { return null; }
}

export function clearClub() {
  if (typeof window !== "undefined") {
    const key = workspaceStorageKey();
    window.localStorage.removeItem(clubStorageKey);
    window.localStorage.removeItem(key);
    clearSession();
  }
}

export function loadWorkspaceContext(): WorkspaceContext {
  if (typeof window === "undefined") return { saisonId: null, saisonJahr: null, mitgliedId: null, parzelleId: null };
  try {
    const saved = window.localStorage.getItem(workspaceStorageKey());
    if (!saved) return { saisonId: null, saisonJahr: null, mitgliedId: null, parzelleId: null };
    const value = JSON.parse(saved) as Partial<WorkspaceContext>;
    return {
      saisonId: typeof value.saisonId === "number" ? value.saisonId : null,
      saisonJahr: typeof value.saisonJahr === "number" ? value.saisonJahr : null,
      mitgliedId: null,
      parzelleId: null,
    };
  } catch { return { saisonId: null, saisonJahr: null, mitgliedId: null, parzelleId: null }; }
}

export function saveWorkspaceContext(context: WorkspaceContext) {
  if (typeof window !== "undefined") window.localStorage.setItem(workspaceStorageKey(), JSON.stringify({ ...context, mitgliedId: null, parzelleId: null }));
}

export function loadSession(): BrowserSession | null {
  const session = loadStoredSession();
  return session && session.expiresAt > Date.now() ? session : null;
}

export function loadStoredSession(): BrowserSession | null {
  if (typeof window === "undefined") return null;
  try {
    const saved = window.localStorage.getItem(browserSessionStorageKey);
    if (!saved) return null;
    const session = JSON.parse(saved) as BrowserSession;
    return typeof session.accessToken === "string" && typeof session.refreshToken === "string" && typeof session.expiresAt === "number" && typeof session.user?.id === "string" ? session : null;
  } catch {
    return null;
  }
}

export function saveSession(session: BrowserSession) {
  if (typeof window !== "undefined") window.localStorage.setItem(browserSessionStorageKey, JSON.stringify(session));
}

export function clearSession() {
  if (typeof window !== "undefined") window.localStorage.removeItem(browserSessionStorageKey);
}

export async function signOut(club: ClubContext, session: BrowserSession) {
  const { url, publishableKey } = resolveSupabaseConfig(club);
  await fetch(`${url}/auth/v1/logout`, {
    method: "POST",
    headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}` },
  }).catch(() => undefined);
}
