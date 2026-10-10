import type { ClubContext } from "../models/auth/club";

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

export type BrowserEditLockResult = {
  acquired: boolean;
  lockedByUserId: string | null;
  lockedByDisplayName: string;
  expiresAt: string | null;
};

const protectedEditTables: Record<string, string> = {
  mitglied: "id",
  parzelle: "id",
  parzellen_belegung: "id",
  zaehler_ablesung: "id",
  arbeitsstunde: "id",
  arbeitseinsatz: "id",
  arbeitseinsatz_anmeldung: "id",
  termin: "id",
  bekanntmachung: "id",
  wartungsvertraege: "id",
  wartungsvertrag_zuordnungen: "id",
  app_user: "user_id",
  saison: "id",
  vereinskonfiguration: "id",
};

// Diese Tabellen enthalten bewusst parallel gehaltene Demo- und Echtdaten.
// Die MAUI-App blendet die jeweils fremde Datenwelt ebenfalls aus.
const demoScopedTables = new Set([
  "app_user",
  "arbeitseinsatz",
  "arbeitsstunde",
  "bekanntmachung",
  "dokument",
  "impressum_funktion_slot",
  "mitglied",
  "mitglied_gesetzlicher_vertreter",
  "mitglied_saison",
  "parzelle",
  "saison",
  "termin",
  "vereinskonfiguration",
  "wartungsvertraege",
  "wartungsvertrag_zuordnungen",
  "zaehler",
  "zaehler_ablesung",
]);

function workspaceStorageKey() {
  return `${workspaceStoragePrefix}.${loadClub()?.vereinId ?? "unknown"}`;
}

function config() {
  const club = loadClub();
  const url = club?.supabaseUrl?.trim();
  const publishableKey = club?.supabasePublishableKey?.trim();
  if (!url || !publishableKey) {
    throw new Error("Die Browser-App ist noch nicht mit Supabase konfiguriert.");
  }
  return { url: url.replace(/\/$/, ""), publishableKey };
}

export function isConfigured() {
  return Boolean(loadClub()?.supabaseUrl?.trim() && loadClub()?.supabasePublishableKey?.trim());
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

export async function signOut(session: BrowserSession) {
  const { url, publishableKey } = config();
  await fetch(`${url}/auth/v1/logout`, {
    method: "POST",
    headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}` },
  }).catch(() => undefined);
}

export async function loadAppUserContext(session: BrowserSession): Promise<AppUserContext> {
  const { url, publishableKey } = config();
  const parameters = new URLSearchParams({
    select: "user_id,role,mitglied_id,permission_grants,permission_revocations,is_demo,is_demo_account",
    user_id: `eq.${session.user.id}`,
  });
  // Die fachlich führende Rollen-/Rechte-Tabelle heißt in der bestehenden
  // Supabase-Struktur `app_user` (Singular).
  const response = await fetch(`${url}/rest/v1/app_user?${parameters}`, {
    headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}` },
  });
  if (!response.ok) throw new Error("Die Benutzerberechtigung konnte nicht geprüft werden.");
  const rows = (await response.json()) as Array<{
    user_id?: string;
    role?: string;
    mitglied_id?: number | null;
    permission_grants?: number | null;
    permission_revocations?: number | null;
    is_demo?: boolean | null;
    is_demo_account?: boolean | null;
  }>;
  const row = rows[0];
  if (!row?.user_id) throw new Error("Für dieses Konto ist keine KGV-Berechtigung hinterlegt.");
  const role = row.role?.toLowerCase();
  const isDemoAccount = Boolean(row.is_demo_account || row.is_demo);
  session.isDemoAccount = isDemoAccount;
  saveSession(session);
  return {
    userId: row.user_id,
    role: role === "admin" || role === "vorstand" ? role : "user",
    mitgliedId: row.mitglied_id ?? null,
    permissionGrants: row.permission_grants ?? 0,
    permissionRevocations: row.permission_revocations ?? 0,
    isDemoAccount,
  };
}

export async function readSupabase<T>(session: BrowserSession, table: string, query: Record<string, string>): Promise<T[]> {
  const { url, publishableKey } = config();
  const scopedQuery = { ...query };
  if (demoScopedTables.has(table) && scopedQuery.is_demo === undefined) {
    scopedQuery.is_demo = session.isDemoAccount ? "eq.true" : "eq.false";
  }
  const parameters = new URLSearchParams(scopedQuery);
  const response = await fetch(`${url}/rest/v1/${table}?${parameters}`, {
    headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}` },
  });
  if (!response.ok) throw new Error("Die Vereinsdaten konnten nicht geladen werden.");
  return (await response.json()) as T[];
}

export async function writeSupabase<T>(session: BrowserSession, table: string, method: "POST" | "PATCH", payload: unknown, query: Record<string, string> = {}): Promise<T[]> {
  if (method === "PATCH") await acquireLockForMutation(session, table, query);
  const { url, publishableKey } = config();
  const parameters = new URLSearchParams(query);
  const response = await fetch(`${url}/rest/v1/${table}?${parameters}`, {
    method,
    headers: {
      apikey: publishableKey,
      Authorization: `Bearer ${session.accessToken}`,
      "Content-Type": "application/json",
      Prefer: "return=representation",
    },
    body: JSON.stringify(payload),
  });
  if (!response.ok) {
    const detail = await response.json().catch(() => null) as { message?: string } | null;
    throw new Error(detail?.message ?? "Die Änderung konnte nicht gespeichert werden.");
  }
  return (await response.json()) as T[];
}

export async function deleteSupabase(session: BrowserSession, table: string, query: Record<string, string>) {
  await acquireLockForMutation(session, table, query);
  const { url, publishableKey } = config();
  const response = await fetch(`${url}/rest/v1/${table}?${new URLSearchParams(query)}`, { method: "DELETE", headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}` } });
  if (!response.ok) throw new Error("Der Eintrag konnte nicht gelöscht werden.");
}

export async function callSupabaseRpc<T>(session: BrowserSession, name: string, payload: Record<string, unknown>): Promise<T> {
  const { url, publishableKey } = config();
  const response = await fetch(`${url}/rest/v1/rpc/${name}`, { method: "POST", headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}`, "Content-Type": "application/json" }, body: JSON.stringify(payload) });
  if (!response.ok) { const detail = await response.json().catch(() => null) as { message?: string } | null; throw new Error(detail?.message ?? "Der Vorgang konnte nicht gespeichert werden."); }
  return await response.json() as T;
}

function mutationEntityId(table: string, query: Record<string, string>) {
  const idColumn = protectedEditTables[table];
  if (!idColumn) return null;
  const filter = query[idColumn];
  return filter?.startsWith("eq.") ? filter.slice(3) : null;
}

async function acquireLockForMutation(session: BrowserSession, table: string, query: Record<string, string>) {
  const entityId = mutationEntityId(table, query);
  if (!entityId) return;
  const result = await acquireBrowserEditLock(session, table, entityId);
  if (!result.acquired) {
    throw new Error(`Dieser Datensatz wird gerade von ${result.lockedByDisplayName} bearbeitet.`);
  }
}

export async function acquireBrowserEditLock(session: BrowserSession, entityType: string, entityId: string | number, timeoutSeconds = 600): Promise<BrowserEditLockResult> {
  const rows = await callSupabaseRpc<Array<{ acquired?: boolean; locked_by_user_id?: string | null; locked_by_display_name?: string | null; expires_at?: string | null }>>(
    session,
    "acquire_browser_edit_lock",
    { p_entity_type: entityType, p_entity_id: String(entityId), p_timeout_seconds: timeoutSeconds },
  );
  const row = rows[0];
  return {
    acquired: row?.acquired === true,
    lockedByUserId: row?.locked_by_user_id ?? null,
    lockedByDisplayName: row?.locked_by_display_name || "einem anderen Benutzer",
    expiresAt: row?.expires_at ?? null,
  };
}

export async function releaseBrowserEditLock(session: BrowserSession, entityType: string, entityId: string | number) {
  await callSupabaseRpc<boolean>(session, "release_browser_edit_lock", { p_entity_type: entityType, p_entity_id: String(entityId) });
}

export async function releaseAllBrowserEditLocks(session: BrowserSession) {
  await callSupabaseRpc<boolean>(session, "release_all_browser_edit_locks", {});
}

async function callSupabaseFunction<T>(session: BrowserSession, name: string, payload: Record<string, unknown>): Promise<T> {
  const { url, publishableKey } = config();
  const response = await fetch(`${url}/functions/v1/${name}`, { method: "POST", headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}`, "Content-Type": "application/json" }, body: JSON.stringify(payload) });
  const result = await response.json().catch(() => null) as (T & { message?: string }) | null;
  if (!response.ok || !result) throw new Error(result?.message ?? "Der Servervorgang konnte nicht ausgeführt werden.");
  return result;
}

export async function inviteAppUser(session: BrowserSession, memberId: number, role: "admin" | "vorstand" | "user") {
  return await callSupabaseFunction<{ success?: boolean; linkPrepared?: boolean; mailSent?: boolean; message?: string }>(session, "kgv-invite-user", { mitgliedId: memberId, role, inviteMethod: "otp" });
}

export async function sendPasswordReset(session: BrowserSession, email: string) {
  return await callSupabaseFunction<{ success?: boolean; message?: string }>(session, "kgv-request-first-login-otp", { email: email.trim() });
}

export async function createStorageSignedUrl(session: BrowserSession, bucket: string, storagePath: string): Promise<string> {
  const { url, publishableKey } = config();
  const response = await fetch(`${url}/storage/v1/object/sign/${encodeURIComponent(bucket)}/${storagePath.split("/").map(encodeURIComponent).join("/")}`, {
    method: "POST",
    headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}`, "Content-Type": "application/json" },
    body: JSON.stringify({ expiresIn: 600 }),
  });
  if (!response.ok) throw new Error("Das Dokument kann derzeit nicht geöffnet werden.");
  const payload = (await response.json()) as { signedURL?: string; signedUrl?: string };
  const signedPath = payload.signedURL ?? payload.signedUrl;
  if (!signedPath) throw new Error("Für das Dokument konnte kein Zugriffslink erstellt werden.");
  return signedPath.startsWith("http") ? signedPath : `${url}/storage/v1${signedPath}`;
}

export async function callSupabaseFunctionRaw(session: BrowserSession, name: string, options: { method: "POST" | "PUT" | "PATCH" | "DELETE"; body?: BodyInit | null; contentType?: string }): Promise<Response> {
  const { url, publishableKey } = config();
  return fetch(`${url}/functions/v1/${name}`, {
    method: options.method,
    headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}`, ...(options.contentType ? { "Content-Type": options.contentType } : {}) },
    body: options.body,
  });
}

export type ContractGenerationRequest = {
  action: "preview" | "finalize" | "sign-existing";
  type: "mitgliedsantrag" | "mitgliedsvertrag" | "pachtvertrag" | "parzellenprotokoll";
  member_id: number;
  parcel_id?: number;
  start_date: string;
  member_fee?: number;
  admission_fee?: number;
  representative?: { mode: "existing" | "manual"; member_id?: number; vorname?: string; nachname?: string; adresse_abweichend?: boolean; adresse?: string; plz?: string; ort?: string };
  signature_application_member?: string;
  signature_privacy_member?: string;
  signature_application_representative?: string;
  signature_privacy_representative?: string;
  signature_member?: string;
  has_previous_contract?: boolean;
  previous_contract_date?: string;
  include_secondary_member?: boolean;
  signature_secondary?: string;
  signature_board?: string;
  signature_board2?: string;
  protocol_id?: number;
  protocol_type?: string;
  protocol_reason?: string;
  protocol_condition?: string;
  protocol_agreement?: string;
  board2_id?: number;
  companion_name?: string;
  photos?: string[];
  source_document_id?: number;
};

export async function generateContract(session: BrowserSession, request: ContractGenerationRequest): Promise<{ previewUrl?: string; documentId?: number; message?: string }> {
  const { url, publishableKey } = config();
  const response = await fetch(`${url}/functions/v1/kgv-generate-contract`, { method: "POST", headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}`, "Content-Type": "application/json" }, body: JSON.stringify(request) });
  if (!response.ok) { const detail = await response.json().catch(() => null) as { message?: string } | null; throw new Error(detail?.message ?? "Das Vertragsdokument konnte nicht erzeugt werden."); }
  if (request.action === "preview") return { previewUrl: URL.createObjectURL(await response.blob()) };
  const payload = await response.json() as { document_id?: number; message?: string };
  return { documentId: payload.document_id, message: payload.message };
}

export type MeterPhotoKind = "ablesung" | "einbau" | "ausbau";

export async function uploadMeterPhoto(session: BrowserSession, file: File, details: { datum: string; medium: string; anlage: string; garten: string; zaehlernummer: string; kind?: MeterPhotoKind }) {
  const { url, publishableKey } = config();
  const form = new FormData(); form.set("file", file); form.set("kind", details.kind ?? "ablesung"); form.set("datum", details.datum); form.set("medium", details.medium); form.set("anlage", details.anlage); form.set("garten", details.garten); form.set("zaehlernummer", details.zaehlernummer);
  const response = await fetch(`${url}/functions/v1/kgv-upload-photo`, { method: "POST", headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}` }, body: form });
  const payload = await response.json().catch(() => null) as { file_id?: string; file_name?: string; message?: string } | null;
  if (!response.ok || !payload?.file_id) throw new Error(payload?.message ?? "Das Ablesefoto konnte nicht hochgeladen werden.");
  return { fileId: payload.file_id, fileName: payload.file_name ?? file.name };
}

export async function openMeterPhoto(session: BrowserSession, readingId: number) {
  const { url, publishableKey } = config();
  const response = await fetch(`${url}/functions/v1/kgv-upload-photo`, { method: "POST", headers: { apikey: publishableKey, Authorization: `Bearer ${session.accessToken}`, "Content-Type": "application/json" }, body: JSON.stringify({ action: "download", ablesung_id: readingId }) });
  if (!response.ok) { const detail = await response.json().catch(() => null) as { message?: string } | null; throw new Error(detail?.message ?? "Das Ablesungsfoto konnte nicht geladen werden."); }
  return URL.createObjectURL(await response.blob());
}
