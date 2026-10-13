import type { ClubContext } from "../../models/auth/club";

export type SupabaseTransportSession = {
  accessToken: string;
  isDemoAccount?: boolean;
};

export type SupabaseClientContext = {
  club: ClubContext;
  session: SupabaseTransportSession;
};

export type SupabaseConfig = {
  url: string;
  publishableKey: string;
};

export function resolveSupabaseConfig(club: ClubContext): SupabaseConfig {
  const url = club.supabaseUrl?.trim();
  const publishableKey = club.supabasePublishableKey?.trim();
  if (!url || !publishableKey) throw new Error("Die Browser-App ist noch nicht mit Supabase konfiguriert.");
  return { url: url.replace(/\/+$/, ""), publishableKey };
}

export function isSupabaseClubConfigured(club: ClubContext | null | undefined) {
  return Boolean(club?.supabaseUrl?.trim() && club.supabasePublishableKey?.trim());
}
