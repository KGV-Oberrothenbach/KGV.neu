import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const root = new URL("../../", import.meta.url);

test("passkey uses the selected club's Supabase project and normal session validation", async () => {
  const [service, provider] = await Promise.all([
    readFile(new URL("services/auth/passkey-service.ts", root), "utf8"),
    readFile(new URL("features/auth/AuthProvider.tsx", root), "utf8"),
  ]);
  assert.match(service, /createClient\(url, publishableKey/);
  assert.match(service, /resolveSupabaseConfig\(club\)/);
  assert.match(service, /signInWithPasskey\(club: ClubContext\)/);
  assert.match(service, /registerPasskey\(club: ClubContext, currentSession/);
  assert.match(provider, /establishSession\(await signInWithSupabasePasskey\(club\), club\)/);
  assert.match(provider, /loadAppUserContext\(selectedClub, candidate\)/);
});

test("passkey registration requires an existing session and cancellation leaves login available", async () => {
  const [provider, login] = await Promise.all([
    readFile(new URL("features/auth/AuthProvider.tsx", root), "utf8"),
    readFile(new URL("features/auth/LoginForm.tsx", root), "utf8"),
  ]);
  assert.match(provider, /!club \|\| !sessionRef\.current/);
  assert.match(login, /Mit Passkey anmelden/);
  assert.match(login, /isPasskeyCancellation\(error\)/);
  assert.match(login, /Mit Passkey anmelden/);
  const source = await readFile(new URL("services/auth/passkey-service.ts", root), "utf8");
  assert.match(source, /ERROR_CEREMONY_ABORTED/);
  assert.match(login, /onSignIn/);
});
