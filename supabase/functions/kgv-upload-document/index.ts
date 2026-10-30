import { createClient, type SupabaseClient } from "npm:@supabase/supabase-js@2";
import { stampHistoricalContractPdf } from "./historical-contract-pdf.ts";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

const GOOGLE_TOKEN_REFRESH_TIMEOUT_MS = 15_000;
const DRIVE_FILES_LIST_TIMEOUT_MS = 15_000;
const DRIVE_CREATE_FOLDER_TIMEOUT_MS = 15_000;
const DRIVE_ROOT_FOLDER_LOOKUP_TIMEOUT_MS = 15_000;
const DRIVE_UPLOAD_TIMEOUT_MS = 30_000;
const DRIVE_DELETE_TIMEOUT_MS = 30_000;

type OwnerKind = "mitglied" | "parzelle";

type DriveUploadResult = {
  id: string;
  name: string;
};

type ApiErrorCode =
  | "OPERATION_CONFLICT"
  | "OPERATION_IN_PROGRESS"
  | "PDF_STAMP_ERROR"
  | "BAD_REQUEST"
  | "UNAUTHORIZED"
  | "FORBIDDEN"
  | "CONFIG_MISSING"
  | "GOOGLE_AUTH_ERROR"
  | "GOOGLE_DRIVE_ERROR"
  | "INTERNAL_ERROR";

type DeleteDocumentRequest = {
  drive_file_id?: string | null;
  fileId?: string | null;
};

type DownloadDocumentRequest = {
  action?: string | null;
  document_id?: number | string | null;
};

type ArchiveDocumentRequest = {
  action?: string | null;
  document_id?: number | string | null;
  archive_password?: string | null;
  reason?: string | null;
};

type HistoricalContractNumberRequest = {
  action?: string | null;
  document_id?: number | string | null;
};

type AuthenticatedDocumentUser = {
  userId: string;
  role: string;
  mitgliedId: number | null;
  isDemoAccount: boolean;
  permissionGrants: number;
  permissionRevocations: number;
};

type DriveDocumentRecord = {
  id: number;
  mitglied_id: number | null;
  parzelle_id: number | null;
  drive_file_id: string | null;
  dateiname: string | null;
  mime_type: string | null;
  archiviert_at: string | null;
  historische_vertragsnummer?: number | null;
  historische_vertragsnummer_pdf_gestempelt_am?: string | null;
};

const supportedDocumentTypes: Record<string, readonly string[]> = {
  ".pdf": ["application/pdf"],
  ".jpg": ["image/jpeg"],
  ".jpeg": ["image/jpeg"],
  ".png": ["image/png"],
  ".webp": ["image/webp"],
  ".doc": ["application/msword"],
  ".docx": ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
  ".odt": ["application/vnd.oasis.opendocument.text"],
};

function logStep(step: string, details?: Record<string, unknown>) {
  if (details) {
    console.log(`[kgv-upload-document] ${step}`, details);
    return;
  }

  console.log(`[kgv-upload-document] ${step}`);
}

function logError(step: string, error: unknown) {
  const message = error instanceof Error ? error.message : String(error);
  console.error(`[kgv-upload-document] ${step}`, { message });
}

function isTimeoutError(error: unknown): boolean {
  if (error instanceof DOMException) {
    return error.name === "AbortError" || error.name === "TimeoutError";
  }

  if (error instanceof Error) {
    const message = error.message.toLowerCase();
    return message.includes("timeout") || message.includes("timed out") || message.includes("aborted");
  }

  return false;
}

function buildStepError(step: string, error: unknown): Error {
  if (isTimeoutError(error)) {
    return new Error(`${step} timeout`);
  }

  if (error instanceof Error) {
    return new Error(`${step} failed: ${error.message}`);
  }

  return new Error(`${step} failed`);
}

function json(status: number, body: unknown) {
  return new Response(JSON.stringify(body, null, 2), {
    status,
    headers: {
      ...corsHeaders,
      "Content-Type": "application/json; charset=utf-8",
    },
  });
}

function createRequestId(): string {
  try {
    return crypto.randomUUID();
  } catch {
    return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  }
}

async function deleteDriveFile(accessToken: string, driveFileId: string): Promise<void> {
  try {
    const res = await fetch(`https://www.googleapis.com/drive/v3/files/${encodeURIComponent(driveFileId)}?supportsAllDrives=true`, {
      method: "DELETE",
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
      signal: AbortSignal.timeout(DRIVE_DELETE_TIMEOUT_MS),
    });

    if (res.status === 404) {
      throw new Error("Drive file not found");
    }

    if (!res.ok) {
      const responseText = await res.text();
      throw new Error(`Drive delete failed: ${responseText}`);
    }
  } catch (error) {
    throw buildStepError("Drive delete", error);
  }
}

function errorResponse(status: number, errorCode: ApiErrorCode, message: string, requestId: string) {
  return json(status, {
    success: false,
    error_code: errorCode,
    message,
    request_id: requestId,
  });
}

function isGoogleAuthErrorMessage(message: string): boolean {
  const m = (message ?? "").toLowerCase();
  return m.includes("invalid_grant") ||
    m.includes("token has been expired") ||
    m.includes("token has been revoked") ||
    m.includes("unauthorized_client") ||
    m.includes("invalid_client") ||
    m.includes("invalid refresh token") ||
    m.includes("refresh token") && m.includes("invalid");
}

function sanitizeSegment(value: string): string {
  return value
    .trim()
    .replace(/[<>:"/\\|?*\x00-\x1F]/g, "-")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "")
    .slice(0, 120);
}

function normalizeOwnerKind(value: string | null): OwnerKind | null {
  const normalized = (value ?? "").trim().toLowerCase();
  return normalized === "mitglied" || normalized === "parzelle" ? normalized : null;
}

function normalizeOwnerId(value: string | null): number | null {
  const normalized = (value ?? "").trim();
  if (!/^\d+$/.test(normalized)) {
    return null;
  }

  const parsed = Number(normalized);
  return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
}

function readGoogleDriveRootFolderId(): string {
  logStep("root folder validate start");

  const rawValue = Deno.env.get("GOOGLE_DRIVE_ROOT_FOLDER_ID");
  const trimmedValue = rawValue?.trim() ?? "";
  const details = {
    hasValue: rawValue != null,
    rawLength: rawValue?.length ?? 0,
    trimmedLength: trimmedValue.length,
    trimmedEmpty: trimmedValue.length === 0,
  };

  if (!trimmedValue) {
    logStep("root folder validate failed", details);
    throw new Error("Google Drive root folder id missing or empty");
  }

  logStep("root folder validate result", details);
  return trimmedValue;
}

function buildTimestamp(): string {
  const now = new Date();
  const year = now.getUTCFullYear().toString().padStart(4, "0");
  const month = (now.getUTCMonth() + 1).toString().padStart(2, "0");
  const day = now.getUTCDate().toString().padStart(2, "0");
  const hour = now.getUTCHours().toString().padStart(2, "0");
  const minute = now.getUTCMinutes().toString().padStart(2, "0");
  const second = now.getUTCSeconds().toString().padStart(2, "0");
  return `${year}-${month}-${day}_${hour}-${minute}-${second}`;
}

function guessExtension(file: File): string {
  const name = file.name ?? "";
  const dotIndex = name.lastIndexOf(".");
  if (dotIndex > -1 && dotIndex < name.length - 1) {
    return name.slice(dotIndex);
  }

  const type = (file.type || "").toLowerCase();
  if (type === "image/jpeg") return ".jpg";
  if (type === "image/png") return ".png";
  if (type === "image/webp") return ".webp";
  if (type === "application/pdf") return ".pdf";
  if (type === "text/plain") return ".txt";
  if (type === "application/vnd.openxmlformats-officedocument.wordprocessingml.document") return ".docx";
  if (type === "application/msword") return ".doc";
  if (type === "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") return ".xlsx";
  if (type === "application/vnd.ms-excel") return ".xls";
  return ".bin";
}

function buildFileName(title: string, file: File): string {
  const sanitizedTitle = sanitizeSegment(title) || "Dokument";
  const timestamp = buildTimestamp();
  return `${sanitizedTitle}_${timestamp}${guessExtension(file)}`;
}

function buildStorageSegments(ownerKind: OwnerKind, ownerId: number): string[] {
  return [
    ownerKind === "mitglied" ? "KGV-APP" : "KGV-App",
    "Dokumente",
    ownerKind === "mitglied" ? "Mitglieder" : "Parzellen",
    ownerId.toString(),
  ];
}

function buildStoragePath(storageSegments: string[], fileName: string): string {
  return `${storageSegments.join("/")}/${fileName}`;
}

function isExpectedStoragePath(storagePath: string, ownerKind: OwnerKind, ownerId: number, fileName: string): boolean {
  const segments = storagePath.split("/").filter(Boolean);
  const expectedRootFolder = ownerKind === "mitglied" ? "KGV-APP" : "KGV-App";
  const expectedOwnerFolder = ownerKind === "mitglied" ? "Mitglieder" : "Parzellen";
  return segments.length === 5
    && segments[0] === expectedRootFolder
    && segments[1] === "Dokumente"
    && segments[2] === expectedOwnerFolder
    && segments[3] === ownerId.toString()
    && segments[4] === fileName;
}

function isGeneratedFileName(fileName: string): boolean {
  return /^.+_\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}\.[^./\\]+$/u.test(fileName);
}

async function getGoogleAccessToken(): Promise<string> {
  const clientId = Deno.env.get("GOOGLE_DRIVE_CLIENT_ID");
  const clientSecret = Deno.env.get("GOOGLE_DRIVE_CLIENT_SECRET");
  const refreshToken = Deno.env.get("GOOGLE_DRIVE_REFRESH_TOKEN");

  if (!clientId || !clientSecret || !refreshToken) {
    logStep("google token refresh config missing", {
      hasClientId: !!clientId,
      hasClientSecret: !!clientSecret,
      hasRefreshToken: !!refreshToken,
    });
    throw new Error("Google-Drive-Secrets fehlen");
  }

  const body = new URLSearchParams({
    client_id: clientId,
    client_secret: clientSecret,
    refresh_token: refreshToken,
    grant_type: "refresh_token",
  });

  logStep("google token refresh start");

  try {
    const tokenRes = await fetch("https://oauth2.googleapis.com/token", {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body,
      signal: AbortSignal.timeout(GOOGLE_TOKEN_REFRESH_TIMEOUT_MS),
    });

    const tokenJson = await tokenRes.json();
    if (!tokenRes.ok || !tokenJson.access_token) {
      throw new Error(`Google token refresh failed: ${JSON.stringify(tokenJson)}`);
    }

    logStep("google token refresh success");
    return tokenJson.access_token as string;
  } catch (error) {
    throw buildStepError("Google token refresh", error);
  }
}

async function verifyDriveRootFolder(accessToken: string, rootFolderId: string): Promise<void> {
  logStep("drive root folder lookup start", { rootFolderIdLength: rootFolderId.length });

  try {
    const res = await fetch(
      `https://www.googleapis.com/drive/v3/files/${encodeURIComponent(rootFolderId)}?fields=id,name,mimeType,trashed&supportsAllDrives=true`,
      {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
        signal: AbortSignal.timeout(DRIVE_ROOT_FOLDER_LOOKUP_TIMEOUT_MS),
      },
    );

    const jsonBody = await res.json();
    if (res.status === 403 || res.status === 404) {
      throw new Error("Drive root folder not accessible");
    }

    if (!res.ok || !jsonBody.id) {
      throw new Error(`Drive root folder lookup failed: ${JSON.stringify(jsonBody)}`);
    }

    if (jsonBody.trashed === true) {
      throw new Error("Drive root folder not accessible");
    }

    if (jsonBody.mimeType !== "application/vnd.google-apps.folder") {
      throw new Error("Drive root folder lookup failed: target is not a folder");
    }

    logStep("drive root folder lookup success", {
      rootFolderIdLength: rootFolderId.length,
      isFolder: true,
      trashed: false,
    });
  } catch (error) {
    throw buildStepError("Drive root folder lookup", error);
  }
}

async function driveList(accessToken: string, query: string): Promise<Array<{ id: string; name: string }>> {
  const url = new URL("https://www.googleapis.com/drive/v3/files");
  url.searchParams.set("q", query);
  url.searchParams.set("fields", "files(id,name)");
  url.searchParams.set("pageSize", "10");
  url.searchParams.set("spaces", "drive");

  try {
    const res = await fetch(url, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
      signal: AbortSignal.timeout(DRIVE_FILES_LIST_TIMEOUT_MS),
    });

    const jsonBody = await res.json();
    if (!res.ok) {
      throw new Error(`Drive files.list failed: ${JSON.stringify(jsonBody)}`);
    }

    return (jsonBody.files ?? []) as Array<{ id: string; name: string }>;
  } catch (error) {
    throw buildStepError("Drive files.list", error);
  }
}

async function createDriveFolder(accessToken: string, parentId: string, folderName: string): Promise<string> {
  try {
    const res = await fetch("https://www.googleapis.com/drive/v3/files?fields=id,name", {
      method: "POST",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        "Content-Type": "application/json; charset=utf-8",
      },
      body: JSON.stringify({
        name: folderName,
        mimeType: "application/vnd.google-apps.folder",
        parents: [parentId],
      }),
      signal: AbortSignal.timeout(DRIVE_CREATE_FOLDER_TIMEOUT_MS),
    });

    const jsonBody = await res.json();
    if (!res.ok || !jsonBody.id) {
      throw new Error(`Drive create folder failed: ${JSON.stringify(jsonBody)}`);
    }

    return jsonBody.id as string;
  } catch (error) {
    throw buildStepError("Drive create folder", error);
  }
}

async function ensureFolder(accessToken: string, parentId: string, folderName: string): Promise<string> {
  logStep("ensure folder start", { folderName });
  const safeName = folderName.replace(/'/g, "\\'");
  const q = `mimeType = 'application/vnd.google-apps.folder' and trashed = false and name = '${safeName}' and '${parentId}' in parents`;

  const existing = await driveList(accessToken, q);
  if (existing.length > 0) {
    logStep("ensure folder success", { folderName, source: "existing" });
    return existing[0].id;
  }

  const folderId = await createDriveFolder(accessToken, parentId, folderName);
  logStep("ensure folder success", { folderName, source: "created" });
  return folderId;
}

function concatUint8Arrays(parts: Uint8Array[]): Uint8Array {
  const length = parts.reduce((sum, part) => sum + part.length, 0);
  const out = new Uint8Array(length);
  let offset = 0;
  for (const part of parts) {
    out.set(part, offset);
    offset += part.length;
  }
  return out;
}

async function uploadFileToDrive(params: { accessToken: string; parentId: string; fileName: string; file: File; fileId?: string }): Promise<DriveUploadResult> {
  const boundary = `kgv-${crypto.randomUUID()}`;
  const encoder = new TextEncoder();
  const fileBytes = new Uint8Array(await params.file.arrayBuffer());
  const metadata = { name: params.fileName, parents: [params.parentId], ...(params.fileId ? { id: params.fileId } : {}) };

  const header1 = `--${boundary}\r\nContent-Type: application/json; charset=UTF-8\r\n\r\n${JSON.stringify(metadata)}\r\n`;
  const header2 = `--${boundary}\r\nContent-Type: ${params.file.type || "application/octet-stream"}\r\n\r\n`;
  const footer = `\r\n--${boundary}--`;
  const body = concatUint8Arrays([
    encoder.encode(header1),
    encoder.encode(header2),
    fileBytes,
    encoder.encode(footer),
  ]);

  try {
    const res = await fetch(
      "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart&fields=id,name",
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${params.accessToken}`,
          "Content-Type": `multipart/related; boundary=${boundary}`,
        },
        body: new Uint8Array(body),
        signal: AbortSignal.timeout(DRIVE_UPLOAD_TIMEOUT_MS),
      },
    );

    // Preallocated IDs make POST safe to repeat even after a lost response.
    if (params.fileId && res.status === 409) {
      const existing = await fetch("https://www.googleapis.com/drive/v3/files/" + encodeURIComponent(params.fileId) + "?fields=id,name,trashed", { headers: { Authorization: "Bearer " + params.accessToken }, signal: AbortSignal.timeout(DRIVE_UPLOAD_TIMEOUT_MS) });
      const data = await existing.json();
      if (!existing.ok || data.id !== params.fileId || data.name !== params.fileName || data.trashed) throw new Error("Drive idempotent upload conflict could not be verified");
      return { id: data.id, name: data.name };
    }
    const jsonBody = await res.json();
    if (!res.ok || !jsonBody.id) {
      throw new Error(`Drive upload failed: ${JSON.stringify(jsonBody)}`);
    }

    return {
      id: jsonBody.id as string,
      name: (jsonBody.name as string) ?? params.fileName,
    };
  } catch (error) {
    throw buildStepError("Drive upload", error);
  }
}


async function allocateDriveFileId(accessToken: string): Promise<string> {
 const response = await fetch("https://www.googleapis.com/drive/v3/files/generateIds?count=1&space=drive&type=files", { headers: { Authorization: "Bearer " + accessToken }, signal: AbortSignal.timeout(DRIVE_UPLOAD_TIMEOUT_MS) });
 const payload = await response.json();
 if (!response.ok || !payload.ids?.[0]) throw new Error("Drive file ID allocation failed");
 return payload.ids[0];
}
class HistoricalOperationConflict extends Error {}
async function uploadHistoricalOperation(auth: { userId: string; supabaseAdmin: SupabaseClient }, operationId: string, ownerId: number, contractType: string, title: string, mime: string, file: File) {
 const bytes = await file.arrayBuffer();
 const sha256 = Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", bytes)), b => b.toString(16).padStart(2,"0")).join("");
 const { data: existing, error: lookupError } = await auth.supabaseAdmin.from("historical_contract_operation").select("drive_file_id,file_name").eq("operation_id",operationId).maybeSingle();
 if (lookupError) throw new Error("Historischer Uploadvorgang konnte nicht geladen werden.");
 // Unused generated IDs do not create physical files. Only the winning row's ID
 // and name are used, even when two Edge instances begin concurrently.
 const candidateId = existing?.drive_file_id ?? await allocateDriveFileId(await getGoogleAccessToken());
 const { data: operation, error } = await auth.supabaseAdmin.rpc("begin_historical_contract_upload", {
  p_operation_id: operationId, p_actor: auth.userId, p_member_id: ownerId, p_contract_type: contractType,
  p_upload_request: { sha256, name: file.name, size: file.size, mime, title }, p_drive_file_id: candidateId, p_file_name: buildFileName(title,file),
 });
 if (error) { if (error.code === "22023") throw new HistoricalOperationConflict(error.message); throw new Error("Historischer Uploadvorgang konnte nicht begonnen werden: " + error.message); }
 if (!operation) throw new Error("Historischer Uploadvorgang wurde nicht bestätigt.");
 if (!operation.document_id) {
  const accessToken = await getGoogleAccessToken();
  const rootFolder = readGoogleDriveRootFolderId();
  await verifyDriveRootFolder(accessToken, rootFolder);
  const segments = buildStorageSegments("mitglied",ownerId);
  let parentId = rootFolder;
  for (const segment of segments) parentId = await ensureFolder(accessToken,parentId,segment);
  await uploadFileToDrive({ accessToken, parentId, fileName: operation.file_name, file, fileId: operation.drive_file_id });
 }
 const { data: document, error: finishError } = await auth.supabaseAdmin.rpc("finish_historical_contract_upload", {
  p_operation_id: operationId, p_storage_path: buildStoragePath(buildStorageSegments("mitglied",ownerId),operation.file_name),
 });
 // Do not compensate by deleting the shared file on an ambiguous DB/network
 // response. The persisted operation owns it and the next attempt resumes it.
 if (finishError || !document) throw new Error("Historischer Upload bleibt wiederaufnehmbar: " + (finishError?.message ?? "keine Bestätigung"));
 return document;
}
async function historicalStampMarker(auth: { supabaseAdmin: SupabaseClient }, documentId: number, nummer: number) {
 const { data: marker, error } = await auth.supabaseAdmin.rpc("get_historical_contract_stamp_marker", { p_document_id: documentId });
 if (error || typeof marker !== "string" || marker.length < 64) throw new Error("Persistierter PDF-Stempelmarker konnte nicht geladen werden.");
 return documentId + ":" + nummer + ":" + marker;
}

async function authenticateDocumentUser(authHeader: string) {
  logStep("auth start");
  const supabaseUrl = Deno.env.get("SUPABASE_URL");
  const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");

  if (!supabaseUrl || !serviceRoleKey) {
    throw new Error("SUPABASE_URL oder SUPABASE_SERVICE_ROLE_KEY fehlt.");
  }

  const supabaseAdmin = createClient(supabaseUrl, serviceRoleKey);
  const jwt = authHeader.replace(/^Bearer\s+/i, "").trim();
  if (!jwt) {
    return { ok: false as const, status: 401, message: "Kein Bearer-Token vorhanden." };
  }

  const { data: userData, error: userError } = await supabaseAdmin.auth.getUser(jwt);
  if (userError || !userData.user) {
    return { ok: false as const, status: 401, message: "Ungültiger oder abgelaufener Benutzer-Token." };
  }

  const { data: appUser, error: appUserError } = await supabaseAdmin
    .from("app_user")
    .select("role,mitglied_id,is_demo_account,permission_grants,permission_revocations")
    .eq("user_id", userData.user.id)
    .maybeSingle();

  if (appUserError) {
    return { ok: false as const, status: 500, message: "Benutzerrolle konnte nicht geladen werden." };
  }

  const role = (appUser?.role ?? "").trim().toLowerCase();
  const rawMitgliedId = appUser?.mitglied_id;
  const mitgliedId = typeof rawMitgliedId === "number" && Number.isSafeInteger(rawMitgliedId) && rawMitgliedId > 0
    ? rawMitgliedId
    : null;

  return {
    ok: true as const,
    userId: userData.user.id,
    role,
    mitgliedId,
    isDemoAccount: appUser?.is_demo_account === true,
    permissionGrants: Number(appUser?.permission_grants ?? 0),
    permissionRevocations: Number(appUser?.permission_revocations ?? 0),
    supabaseAdmin,
    // Authenticated RPCs reuse the database permission evaluator, including revocations.
    supabaseUser: createClient(supabaseUrl, Deno.env.get("SUPABASE_ANON_KEY") ?? serviceRoleKey, { global: { headers: { Authorization: authHeader } }, auth: { persistSession: false } }),
  };
}

async function requireAdminOrVorstand(authHeader: string, existingAuth?: Awaited<ReturnType<typeof authenticateDocumentUser>>) {
  const auth = existingAuth ?? await authenticateDocumentUser(authHeader);
  if (!auth.ok)
    return auth;

  const { role } = auth;
  if (role !== "admin" && role !== "vorstand") {
    return { ok: false as const, status: 403, message: "Nur Admin oder Vorstand dürfen Dokumente verwalten." };
  }

  logStep("auth passed", { role });
  return auth;
}

async function requireAdmin(authHeader: string) {
  const auth = await authenticateDocumentUser(authHeader);
  if (!auth.ok)
    return auth;

  if (auth.role !== "admin")
    return { ok: false as const, status: 403, message: "Nur Admin dürfen Dokumente archivieren." };

  return auth;
}

async function requireHistoricalContractRegistration(authHeader: string, existingAuth?: Awaited<ReturnType<typeof authenticateDocumentUser>>) {
  const auth = existingAuth ?? await authenticateDocumentUser(authHeader);
  if (!auth.ok)
    return auth;

  const { data: permitted, error } = await auth.supabaseUser.rpc("can_register_historical_contracts");
  if (error) throw new Error("Altvertragsberechtigung konnte nicht geprüft werden.");
  if (permitted !== true)
    return { ok: false as const, status: 403, message: "Für die Nacherfassung historischer Verträge fehlt die Berechtigung." };

  return auth;
}

async function sha256Hex(value: string): Promise<string> {
  const bytes = new TextEncoder().encode(value);
  const hash = await crypto.subtle.digest("SHA-256", bytes);
  return Array.from(new Uint8Array(hash)).map((value) => value.toString(16).padStart(2, "0")).join("");
}

async function verifyArchivePassword(value: string): Promise<boolean> {
  const configuredHash = (Deno.env.get("KGV_DOCUMENT_ARCHIVE_PASSWORD_SHA256") ?? "").trim().toLowerCase();
  if (!/^[a-f0-9]{64}$/.test(configuredHash))
    throw new Error("KGV_DOCUMENT_ARCHIVE_PASSWORD_SHA256 ist nicht konfiguriert.");

  return (await sha256Hex(value)).toLowerCase() === configuredHash;
}

async function isDocumentInDemoScope(auth: { supabaseAdmin: SupabaseClient }, document: Pick<DriveDocumentRecord, "mitglied_id" | "parzelle_id">): Promise<boolean> {
  const owner = document.mitglied_id
    ? { table: "mitglied", id: document.mitglied_id }
    : document.parzelle_id
      ? { table: "parzelle", id: document.parzelle_id }
      : null;
  if (!owner) return false;

  const { data, error } = await auth.supabaseAdmin
    .from(owner.table)
    .select("is_demo")
    .eq("id", owner.id)
    .maybeSingle();
  if (error) throw new Error(`Dokument-Demo-Scope konnte nicht geprüft werden: ${error.message}`);
  return data?.is_demo === true;
}

async function resolveUploadOwnerScope(auth: { supabaseAdmin: SupabaseClient }, ownerKind: OwnerKind, ownerId: number): Promise<boolean | null> {
  const table = ownerKind === "mitglied" ? "mitglied" : "parzelle";
  const { data, error } = await auth.supabaseAdmin.from(table).select("is_demo").eq("id", ownerId).maybeSingle();
  if (error) throw new Error(`Dokument-Owner konnte nicht geprüft werden: ${error.message}`);
  return data ? data.is_demo === true : null;
}

function hasAllowedDocumentType(file: File): boolean {
  const extension = /\.[^.]+$/.exec(file.name.trim().toLowerCase())?.[0] ?? "";
  const allowedMimes = supportedDocumentTypes[extension];
  if (!allowedMimes) return false;
  const mimeType = file.type.trim().toLowerCase();
  return !mimeType || mimeType === "application/octet-stream" || allowedMimes.includes(mimeType);
}

async function mayReadDocument(auth: AuthenticatedDocumentUser & { supabaseAdmin: SupabaseClient; supabaseUser: SupabaseClient }, documentId: number): Promise<DriveDocumentRecord | null> {
  const { data: document, error: documentError } = await auth.supabaseAdmin
    .from("dokument")
    .select("id,mitglied_id,parzelle_id,drive_file_id,dateiname,mime_type,archiviert_at")
    .eq("id", documentId)
    .maybeSingle();

  if (documentError) {
    throw new Error(`Dokumentrechte konnten nicht geprüft werden: ${documentError.message}`);
  }

  if (!document)
    return null;

  if (document.archiviert_at)
    return null;

  // The service-role client bypasses RLS, so scope parity must precede role checks.
  if (await isDocumentInDemoScope(auth, document as DriveDocumentRecord) !== auth.isDemoAccount)
    return null;

  const { data: historicalAccess, error: historicalError } = await auth.supabaseUser.rpc("can_access_historical_contract_document", { p_dokument_id: documentId, p_require_registered: false });
  if (historicalError) throw new Error("Historischer Dokumentkontext konnte nicht geprüft werden.");
  if (historicalAccess === true) return document as DriveDocumentRecord;

  // This RPC owns the same narrowly scoped rule as the document SELECT policy.
  // It is evaluated before the legacy role/owner checks because the service-role
  // client used here deliberately bypasses RLS.
  const { data: boardAccess, error: boardAccessError } = await auth.supabaseAdmin
    .rpc("can_access_mitgliedsaufnahme_vorstand_document", {
      p_auth_user_id: auth.userId,
      p_dokument_id: documentId,
    });
  if (boardAccessError)
    throw new Error(`Vorstands-Dokumentberechtigung konnte nicht geprüft werden: ${boardAccessError.message}`);
  if (boardAccess === true)
    return document as DriveDocumentRecord;

  const isManager = auth.role === "admin" || auth.role === "vorstand";
  if (isManager)
    return document as DriveDocumentRecord;

  if (!auth.mitgliedId)
    return null;

  if (document.mitglied_id === auth.mitgliedId)
    return document as DriveDocumentRecord;

  if (!document.parzelle_id)
    return null;

  const today = new Date().toISOString().slice(0, 10);
  const { data: activeOccupancy, error: occupancyError } = await auth.supabaseAdmin
    .from("parzellen_belegung")
    .select("id")
    .eq("parzelle_id", document.parzelle_id)
    .eq("mitglied_id", auth.mitgliedId)
    .lte("von_datum", today)
    .or(`bis_datum.is.null,bis_datum.gte.${today}`)
    .limit(1)
    .maybeSingle();

  if (occupancyError) {
    throw new Error(`Parzellenrechte konnten nicht geprüft werden: ${occupancyError.message}`);
  }

  return activeOccupancy ? document as DriveDocumentRecord : null;
}

async function downloadDriveDocument(accessToken: string, driveFileId: string): Promise<Response> {
  const url = new URL(`https://www.googleapis.com/drive/v3/files/${encodeURIComponent(driveFileId)}`);
  url.searchParams.set("alt", "media");
  url.searchParams.set("supportsAllDrives", "true");
  const response = await fetch(url, {
    headers: { Authorization: `Bearer ${accessToken}` },
    signal: AbortSignal.timeout(DRIVE_DELETE_TIMEOUT_MS),
  });

  if (!response.ok) {
    const responseText = await response.text();
    throw new Error(`Drive download failed: ${responseText}`);
  }

  return response;
}

async function replaceDriveFileContent(accessToken: string, driveFileId: string, content: Uint8Array): Promise<void> {
  const url = new URL(`https://www.googleapis.com/upload/drive/v3/files/${encodeURIComponent(driveFileId)}`);
  url.searchParams.set("uploadType", "media");
  url.searchParams.set("supportsAllDrives", "true");
  const response = await fetch(url, {
    method: "PATCH",
    headers: { Authorization: `Bearer ${accessToken}`, "Content-Type": "application/pdf" },
    body: new Uint8Array(content),
    signal: AbortSignal.timeout(DRIVE_UPLOAD_TIMEOUT_MS),
  });
  if (!response.ok) throw new Error(`Drive PDF update failed: ${await response.text()}`);
}

class HistoricalPdfStampError extends Error {}

async function mayUseHistoricalContractDocument(auth: { supabaseAdmin: SupabaseClient; supabaseUser: SupabaseClient }, documentId: number, registered: boolean): Promise<DriveDocumentRecord | null> {
  const { data: allowed, error } = await auth.supabaseUser.rpc("can_access_historical_contract_document", { p_dokument_id: documentId, p_require_registered: registered });
  if (error) throw new Error("Historischer Dokumentkontext konnte nicht geprüft werden.");
  if (allowed !== true) return null;
  const { data, error: documentError } = await auth.supabaseAdmin.from("dokument")
    .select("id,mitglied_id,parzelle_id,drive_file_id,dateiname,mime_type,archiviert_at").eq("id", documentId).is("archiviert_at", null).maybeSingle();
  if (documentError) throw new Error("Historisches Dokument konnte nicht geladen werden.");
  return data as DriveDocumentRecord | null;
}

async function reserveHistoricalContractNumber(auth: { supabaseAdmin: SupabaseClient; role: string }, document: DriveDocumentRecord, documentId: number) {
  const { data, error } = await auth.supabaseAdmin.rpc("reserve_historische_vertragsnummer", { p_dokument_id: documentId });
  if (error || !Array.isArray(data) || data.length !== 1) throw new Error(`Historische Vertragsnummer konnte nicht reserviert werden: ${error?.message ?? "ungültige Antwort"}`);
  const reservation = data[0] as { nummer: number; titel: string; mime_type: string | null; pdf_bereits_gestempelt: boolean };
  const nummer = Number(reservation.nummer);
  if (!Number.isInteger(nummer) || nummer < 80000 || nummer > 89999) throw new Error("Die reservierte historische Vertragsnummer ist ungültig.");

  const isPdf = (reservation.mime_type ?? document.mime_type ?? "").toLowerCase() === "application/pdf";
  if (isPdf && !reservation.pdf_bereits_gestempelt) {
    const token = crypto.randomUUID();
    const { data: claimed, error: claimError } = await auth.supabaseAdmin.rpc("claim_historical_contract_stamp", { p_document_id: documentId, p_token: token });
    if (claimError) throw new Error("Stempelsperre konnte nicht geprüft werden.");
    if (claimed !== true) throw new Error("Historischer PDF-Stempel wird bereits verarbeitet.");
    try {
      const { data: current, error: currentError } = await auth.supabaseAdmin.rpc("reserve_historische_vertragsnummer", { p_dokument_id: documentId });
      if (currentError || !current?.[0]) throw new Error("Stempelstatus konnte nicht geprüft werden.");
      if (current[0].pdf_bereits_gestempelt) return { nummer, titel: reservation.titel, pdf_gestempelt: true };
      const driveFileId = (document.drive_file_id ?? "").trim();
      if (!driveFileId) throw new Error("Das PDF besitzt keine Google-Drive-Dateireferenz.");
      const accessToken = await getGoogleAccessToken();
      const source = await downloadDriveDocument(accessToken, driveFileId);
      const { data: versions, error: versionError } = await auth.supabaseAdmin.from("pachtvertragsfassung").select("pachtverhaeltnis_id,wirksam_ab").eq("dokument_id", documentId);
      if (versionError) throw new Error(`Vertragsfassungen für PDF-Stempel konnten nicht geladen werden: ${versionError.message}`);
      const starts = new Map((versions ?? []).map((row: { pachtverhaeltnis_id: number; wirksam_ab: string }) => [row.pachtverhaeltnis_id, row.wirksam_ab]));
      const { data: parties, error: partyError } = await auth.supabaseAdmin.from("pachtverhaeltnis_beteiligung").select("pachtverhaeltnis_id,gueltig_ab,gueltig_bis,mitglied:mitglied_id(mitgliedsnummer)").in("pachtverhaeltnis_id", [...starts.keys()]);
      if (partyError) throw new Error(`Vertragsparteien für PDF-Stempel konnten nicht geladen werden: ${partyError.message}`);
      const stampParties = (parties ?? []) as unknown as { pachtverhaeltnis_id: number; gueltig_ab: string; gueltig_bis: string | null; mitglied: { mitgliedsnummer?: number | null } | { mitgliedsnummer?: number | null }[] | null }[];
      const memberNumbers = stampParties.filter(row => { const start = starts.get(row.pachtverhaeltnis_id); return !!start && row.gueltig_ab <= start && (!row.gueltig_bis || row.gueltig_bis >= start); }).map(row => {
        const member = Array.isArray(row.mitglied) ? row.mitglied[0] : row.mitglied;
        return Number(member?.mitgliedsnummer);
      }).filter(Number.isInteger);
      const sourceBytes = new Uint8Array(await source.arrayBuffer());
      let stampedBytes: Uint8Array;
      try { stampedBytes = await stampHistoricalContractPdf(sourceBytes, nummer, memberNumbers, await historicalStampMarker(auth,documentId,nummer)); }
      catch (error) { throw new HistoricalPdfStampError(error instanceof Error ? error.message : String(error)); }
      // Only complete, valid pdf-lib output reaches the existing Drive replacement.
      if (stampedBytes !== sourceBytes) await replaceDriveFileContent(accessToken, driveFileId, stampedBytes);
      const { error: markError } = await auth.supabaseAdmin.rpc("mark_historische_vertragsnummer_pdf_gestempelt", { p_dokument_id: documentId, p_nummer: nummer });
      if (markError) throw new Error(`PDF-Stempel konnte nicht bestätigt werden: ${markError.message}`);
      } finally {
      const { error: releaseError } = await auth.supabaseAdmin.rpc("release_historical_contract_stamp", { p_document_id: documentId, p_token: token });
      if (releaseError) logError("historical stamp lease release failed",releaseError);
    }
  }

  return { nummer, titel: reservation.titel, pdf_gestempelt: isPdf };
}

Deno.serve(async (req) => {
  const requestId = createRequestId();
  let uploadedDriveFileId: string | null = null;
  logStep("function start", { method: req.method });

  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  if (req.method !== "POST" && req.method !== "DELETE") {
    return errorResponse(405, "BAD_REQUEST", "Method not allowed", requestId);
  }

  try {
    const authHeader = req.headers.get("Authorization") ?? "";

    if (req.method === "POST" && (req.headers.get("content-type") ?? "").toLowerCase().includes("application/json")) {
      const payload = await req.json() as DownloadDocumentRequest & ArchiveDocumentRequest & HistoricalContractNumberRequest;
      const action = (payload.action ?? "").trim().toLowerCase();
      if (action === "archive") {
        const documentId = Number(payload.document_id);
        const password = (payload.archive_password ?? "").trim();
        const reason = (payload.reason ?? "").trim();
        if (!Number.isSafeInteger(documentId) || documentId <= 0 || password.length === 0 || reason.length < 3)
          return errorResponse(400, "BAD_REQUEST", "Dokument, Archivpasswort und Begründung sind erforderlich.", requestId);

        const auth = await requireAdmin(authHeader);
        if (!auth.ok)
          return errorResponse(auth.status, "FORBIDDEN", auth.message, requestId);

        const document = await mayReadDocument(auth, documentId);
        if (!document)
          return errorResponse(403, "FORBIDDEN", "Dieses Dokument ist für den aktuellen Benutzer nicht freigegeben.", requestId);

        if (!await verifyArchivePassword(password))
          return errorResponse(403, "FORBIDDEN", "Das Archivpasswort ist nicht korrekt.", requestId);

        const { data: archived, error: archiveError } = await auth.supabaseAdmin
          .from("dokument")
          .update({ archiviert_at: new Date().toISOString(), archiviert_by: auth.userId, archiviert_begruendung: reason })
          .eq("id", documentId)
          .is("archiviert_at", null)
          .select("id")
          .maybeSingle();

        if (archiveError)
          throw new Error(`Dokumentarchivierung fehlgeschlagen: ${archiveError.message}`);
        if (!archived)
          return errorResponse(409, "BAD_REQUEST", "Dokument wurde nicht gefunden oder ist bereits archiviert.", requestId);

        logStep("document archived", { documentId, userId: auth.userId });
        return json(200, { success: true, message: "Dokument wurde archiviert.", request_id: requestId });
      }

      if (action === "reserve_historical_contract_number") {
        const documentId = Number(payload.document_id);
        if (!Number.isSafeInteger(documentId) || documentId <= 0)
          return errorResponse(400, "BAD_REQUEST", "document_id fehlt oder ist ungültig.", requestId);
        const auth = await requireHistoricalContractRegistration(authHeader);
        if (!auth.ok) return errorResponse(auth.status, "FORBIDDEN", auth.message, requestId);
        const document = await mayUseHistoricalContractDocument(auth, documentId, true);
        if (!document) return errorResponse(403, "FORBIDDEN", "Dieses Dokument ist für den aktuellen Benutzer nicht freigegeben.", requestId);
        const result = await reserveHistoricalContractNumber(auth, document, documentId);
        return json(200, { success: true, ...result, request_id: requestId });
      }

      if (action !== "download" && action !== "download_historical_contract") {
        return errorResponse(400, "BAD_REQUEST", "Unbekannte Dokumentaktion.", requestId);
      }

      const documentId = Number(payload.document_id);
      if (!Number.isSafeInteger(documentId) || documentId <= 0) {
        return errorResponse(400, "BAD_REQUEST", "document_id fehlt oder ist ungültig.", requestId);
      }

      const auth = action === "download_historical_contract"
        ? await requireHistoricalContractRegistration(authHeader) : await authenticateDocumentUser(authHeader);
      if (!auth.ok) {
        return errorResponse(auth.status, "UNAUTHORIZED", auth.message, requestId);
      }

      const document = action === "download_historical_contract"
        ? await mayUseHistoricalContractDocument(auth, documentId, false) : await mayReadDocument(auth, documentId);
      if (!document) {
        return errorResponse(403, "FORBIDDEN", "Dieses Dokument ist für den aktuellen Benutzer nicht freigegeben.", requestId);
      }

      const driveFileId = (document.drive_file_id ?? "").trim();
      if (!driveFileId) {
        return errorResponse(409, "GOOGLE_DRIVE_ERROR", "Das Dokument besitzt keine Google-Drive-Dateireferenz.", requestId);
      }

      const accessToken = await getGoogleAccessToken();
      const driveResponse = await downloadDriveDocument(accessToken, driveFileId);
      const contentType = driveResponse.headers.get("content-type") || document.mime_type || "application/octet-stream";
      const safeFileName = (document.dateiname ?? "dokument.pdf").replace(/[\r\n"]/g, "_");
      logStep("drive download success", { documentId, role: auth.role });

      return new Response(driveResponse.body, {
        status: 200,
        headers: {
          ...corsHeaders,
          "Content-Type": contentType,
          "Content-Disposition": `inline; filename="${safeFileName}"`,
          "Cache-Control": "private, no-store",
        },
      });
    }

    if (req.method === "DELETE") {
      const auth = await requireAdminOrVorstand(authHeader);
      if (!auth.ok) return errorResponse(auth.status, "UNAUTHORIZED", auth.message, requestId);
      const payload = await req.json() as DeleteDocumentRequest;
      const driveFileId = (payload.drive_file_id ?? payload.fileId ?? "").trim();
      if (!driveFileId) {
        return errorResponse(400, "BAD_REQUEST", "drive_file_id fehlt für das Löschen.", requestId);
      }

      const accessToken = await getGoogleAccessToken();
      await deleteDriveFile(accessToken, driveFileId);

      return json(200, {
        success: true,
        message: "Datei erfolgreich gelöscht.",
        drive_file_id: driveFileId,
        request_id: requestId,
      });
    }

    const uploadIdentity = await authenticateDocumentUser(authHeader);
    if (!uploadIdentity.ok) return errorResponse(uploadIdentity.status, "UNAUTHORIZED", uploadIdentity.message, requestId);
    const contentType = req.headers.get("content-type") ?? "";
    if (!contentType.toLowerCase().includes("multipart/form-data")) {
      return errorResponse(400, "BAD_REQUEST", "Erwartet multipart/form-data mit Datei und Metadaten.", requestId);
    }

    logStep("formData start");
    const form = await req.formData();
    const historicalUpload = form.has("historical_contract_type");
    const historicalType = String(form.get("historical_contract_type") ?? "");
    const auth = historicalUpload ? await requireHistoricalContractRegistration(authHeader, uploadIdentity) : await requireAdminOrVorstand(authHeader, uploadIdentity);
    if (!auth.ok) return errorResponse(auth.status, "FORBIDDEN", auth.message, requestId);
    if (historicalUpload && !["historischer_ma_pv_erstvertrag", "historischer_pv_folgevertrag"].includes(historicalType))
      return errorResponse(400, "BAD_REQUEST", "Unzulässige historische Vertragsart.", requestId);
    logStep("formData parsed", {
      hasFile: form.get("file") instanceof File,
      hasOwnerKind: form.has("owner_kind"),
      hasOwnerId: form.has("owner_id"),
      hasTitel: form.has("titel"),
    });

    const file = form.get("file");
    if (!(file instanceof File)) {
      return errorResponse(400, "BAD_REQUEST", "Datei-Feld 'file' fehlt.", requestId);
    }

    const ownerKind = normalizeOwnerKind(String(form.get("owner_kind") ?? ""));
    const ownerId = normalizeOwnerId(String(form.get("owner_id") ?? ""));
    const titel = String(form.get("titel") ?? "").trim();
    const sourceProcessRaw = String(form.get("source_kommunikationsvorgang_id") ?? "").trim();
    const sourceRevisionRaw = String(form.get("source_revision") ?? "").trim();
    const sourceAssignmentRaw = String(form.get("source_wartungsvertrag_zuordnung_id") ?? "").trim();
    const sourceProcessId = sourceProcessRaw ? normalizeOwnerId(sourceProcessRaw) : null;
    const sourceRevision = sourceRevisionRaw ? Number(sourceRevisionRaw) : null;
    const sourceAssignmentId = sourceAssignmentRaw ? normalizeOwnerId(sourceAssignmentRaw) : null;
    const hasProvenance = sourceProcessRaw !== "" || sourceRevisionRaw !== "" || sourceAssignmentRaw !== "";
    if (!ownerKind || !ownerId) {
      return errorResponse(400, "BAD_REQUEST", "Owner muss als Mitglied oder Parzelle mit gültiger ID angegeben werden.", requestId);
    }

    if (!titel) {
      return errorResponse(400, "BAD_REQUEST", "Feld 'titel' fehlt.", requestId);
    }
    if (hasProvenance && (!sourceProcessId || !Number.isSafeInteger(sourceRevision) || sourceRevision! < 1)) {
      return errorResponse(400, "BAD_REQUEST", "Dokument-Provenienz ist unvollständig.", requestId);
    }
    if (hasProvenance && !["admin", "vorstand"].includes(auth.role)) {
      return errorResponse(403, "FORBIDDEN", "Dokument-Provenienz darf nur durch die Vertragsbearbeitung gesetzt werden.", requestId);
    }

    if (historicalUpload) {
      if (ownerKind !== "mitglied" || hasProvenance || !/\.(pdf|jpe?g|png|webp)$/i.test(file.name))
        return errorResponse(403, "FORBIDDEN", "Altvertragsupload benötigt ein Mitglied und einen eigenständigen Vertragsnachweis als PDF oder Bildscan.", requestId);
      const { data: memberAccess, error: memberError } = await auth.supabaseUser.rpc("can_access_historical_contract_member", { p_mitglied_id: ownerId });
      if (memberError) throw new Error("Historischer Mitgliedskontext konnte nicht geprüft werden.");
      if (memberAccess !== true) return errorResponse(403, "FORBIDDEN", "Mitglied liegt außerhalb des historischen Vertragsbereichs.", requestId);
    }

    const ownerIsDemo = await resolveUploadOwnerScope(auth, ownerKind, ownerId);
    if (ownerIsDemo === null || ownerIsDemo !== auth.isDemoAccount) {
      return errorResponse(403, "FORBIDDEN", "Dieses Dokument ist für den aktuellen Benutzer nicht freigegeben.", requestId);
    }
    if (hasProvenance) {
      const { data: source, error: sourceError } = await auth.supabaseAdmin
        .from("kommunikationsvorgang")
        .select("id,revision,signatur_revision,mitglied_id,parzelle_id")
        .eq("id", sourceProcessId!)
        .maybeSingle();
      const effectiveRevision = Number(source?.signatur_revision ?? source?.revision);
      const ownerMatches = ownerKind === "mitglied"
        ? Number(source?.mitglied_id) === ownerId
        : Number(source?.parzelle_id) === ownerId;
      if (sourceError || !source || effectiveRevision !== sourceRevision || !ownerMatches) {
        return errorResponse(400, "BAD_REQUEST", "Dokument-Provenienz passt nicht zum Owner oder Vertragsstand.", requestId);
      }
    }

    if (!hasAllowedDocumentType(file)) {
      return errorResponse(400, "BAD_REQUEST", "Der Dateityp wird nicht unterstützt.", requestId);
    }

    if (historicalUpload) {
      const operationId = String(form.get("operation_id") ?? "");
      if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(operationId))
        return errorResponse(400,"BAD_REQUEST","Eine stabile operation_id ist für den Altvertrag erforderlich.",requestId);
      const mimes: Record<string,string> = { pdf:"application/pdf",jpg:"image/jpeg",jpeg:"image/jpeg",png:"image/png",webp:"image/webp" };
      const document = await uploadHistoricalOperation(auth,operationId,ownerId,historicalType,titel,mimes[file.name.split(".").pop()!.toLowerCase()],file);
      return json(200,{success:true,document_id:document.id,document,drive_file_id:document.drive_file_id,storage_path:document.storage_path,dateiname:document.dateiname,mime_type:document.mime_type,size_bytes:document.size_bytes,request_id:requestId});
    }

    const rootFolderId = readGoogleDriveRootFolderId();
    const historicalMimes: Record<string,string> = { pdf: "application/pdf", jpg: "image/jpeg", jpeg: "image/jpeg", png: "image/png", webp: "image/webp" };
    const uploadMime = historicalUpload ? historicalMimes[file.name.split(".").pop()!.toLowerCase()] : (file.type || "application/octet-stream");
    const fileName = buildFileName(titel, file);
    const storageSegments = buildStorageSegments(ownerKind, ownerId);

    const accessToken = await getGoogleAccessToken();
    await verifyDriveRootFolder(accessToken, rootFolderId);

    let parentId = rootFolderId;
    for (const segment of storageSegments) {
      parentId = await ensureFolder(accessToken, parentId, segment);
    }

    logStep("drive upload start", { fileName, ownerKind, ownerId });
    const upload = await uploadFileToDrive({
      accessToken,
      parentId,
      fileName,
      file,
    });
    uploadedDriveFileId = upload.id;

    const storagePath = buildStoragePath(storageSegments, upload.name);
    if (!isGeneratedFileName(upload.name) || !isExpectedStoragePath(storagePath, ownerKind, ownerId, upload.name)) {
      throw new Error("Drive upload returned unexpected document path contract");
    }

    const { data: document, error: documentError } = await auth.supabaseAdmin
      .from("dokument")
      .insert({
        mitglied_id: ownerKind === "mitglied" ? ownerId : null,
        parzelle_id: ownerKind === "parzelle" ? ownerId : null,
        bucket: "dokumente",
        storage_path: storagePath,
        drive_file_id: upload.id,
        titel,
        dateiname: upload.name,
        mime_type: uploadMime,
        size_bytes: file.size,
        created_by: auth.userId,
        ...(historicalUpload ? { historical_contract_type: historicalType } : {}),
        source_kommunikationsvorgang_id: sourceProcessId,
        source_revision: sourceRevision,
        source_wartungsvertrag_zuordnung_id: sourceAssignmentId,
      })
      .select("id,mitglied_id,parzelle_id,bucket,storage_path,drive_file_id,titel,dateiname,mime_type,size_bytes,created_at,updated_at,created_by,archiviert_at,archiviert_by,archiviert_begruendung")
      .single();
    if (documentError || !document) throw new Error(`Dokument-Metadaten konnten nicht gespeichert werden: ${documentError?.message ?? "kein Datensatz"}`);
    uploadedDriveFileId = null;

    logStep("drive upload success", { driveFileId: upload.id, storagePath });

    return json(200, {
      success: true,
      document_id: document.id,
      document,
      drive_file_id: upload.id,
      fileId: upload.id,
      storage_path: storagePath,
      relativePath: storagePath,
      dateiname: upload.name,
      fileName: upload.name,
      mime_type: uploadMime,
      mimeType: uploadMime,
      size_bytes: file.size,
      sizeBytes: file.size,
      uploaded_by_user_id: auth.userId,
      role: auth.role,
      request_id: requestId,
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    if (uploadedDriveFileId) {
      try {
        await deleteDriveFile(await getGoogleAccessToken(), uploadedDriveFileId);
        logStep("drive upload compensation success");
      } catch (rollbackError) {
        logError("drive upload compensation failed", rollbackError);
      }
    }
    logError("return error", error);

    if (error instanceof HistoricalOperationConflict) return errorResponse(409,"OPERATION_CONFLICT",message,requestId);
    if (message === "Historischer PDF-Stempel wird bereits verarbeitet.") return errorResponse(409,"OPERATION_IN_PROGRESS",message + " Bitte denselben Vorgang erneut versuchen.",requestId);
    if (error instanceof HistoricalPdfStampError)
      return errorResponse(422, "PDF_STAMP_ERROR", "PDF-Stempel konnte nicht erstellt werden. Die Originaldatei bleibt unverändert. " + message, requestId);
    if (message.startsWith("PDF-Stempel konnte nicht bestätigt werden"))
      return errorResponse(502, "PDF_STAMP_ERROR", "Das PDF wurde gespeichert, sein Stempelstatus konnte jedoch nicht bestätigt werden.", requestId);

    if (message.includes("Google-Drive-Secrets fehlen")) {
      return errorResponse(503, "CONFIG_MISSING", "Cloud-Dokumentdienst ist nicht konfiguriert. Bitte Serverkonfiguration prüfen.", requestId);
    }

    if (message.toLowerCase().includes("google token refresh") && isGoogleAuthErrorMessage(message)) {
      return errorResponse(503, "GOOGLE_AUTH_ERROR", "Cloud-Dokumentdienst aktuell nicht verfügbar. Bitte Google-Drive-Autorisierung/Refresh-Token prüfen.", requestId);
    }

    if (message.toLowerCase().includes("google token refresh")) {
      return errorResponse(503, "GOOGLE_AUTH_ERROR", "Cloud-Dokumentdienst aktuell nicht verfügbar. Bitte Google-Drive-Autorisierung/Serverkonfiguration prüfen.", requestId);
    }

    if (message.toLowerCase().includes("drive file not found")) {
      return errorResponse(409, "GOOGLE_DRIVE_ERROR", "Die Dokumentdatei wurde in Google Drive nicht gefunden. Der Datenbankeintrag bleibt daher unverändert.", requestId);
    }

    if (message.toLowerCase().includes("drive")) {
      return errorResponse(502, "GOOGLE_DRIVE_ERROR", "Cloud-Dokumentdienst ist aktuell nicht erreichbar. Bitte später erneut versuchen.", requestId);
    }

    return errorResponse(500, "INTERNAL_ERROR", "Unerwarteter Serverfehler beim Cloud-Upload.", requestId);
  }
});
