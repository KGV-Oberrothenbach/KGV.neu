import { createClient } from "npm:@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

type UploadKind = "ablesung" | "ausbau" | "einbau";
type Medium = "strom" | "wasser";

type DriveUploadResult = {
  id: string;
  name: string;
  webViewLink?: string | null;
};

type AuthenticatedPhotoUser = {
  userId: string;
  supabaseUser: ReturnType<typeof createClient>;
};

type MeterReadingPhotoRecord = {
  id: number;
  ablesedatum: string;
  art: UploadKind;
  foto_drive_file_id: string | null;
  foto_dateiname: string | null;
  zaehler: { parzelle_id: number | null; medium: Medium; parzelle: { garten_nr: string | null; Anlage: string | null } | null } | null;
};

const GOOGLE_TOKEN_REFRESH_TIMEOUT_MS = 15_000;
const DRIVE_FILES_LIST_TIMEOUT_MS = 15_000;
const DRIVE_CREATE_FOLDER_TIMEOUT_MS = 15_000;
const DRIVE_ROOT_FOLDER_LOOKUP_TIMEOUT_MS = 15_000;
const DRIVE_UPLOAD_TIMEOUT_MS = 30_000;

function logStep(step: string, details?: Record<string, unknown>) {
  if (details) {
    console.log(`[kgv-upload-photo] ${step}`, details);
    return;
  }

  console.log(`[kgv-upload-photo] ${step}`);
}

function logError(step: string, error: unknown) {
  const message = error instanceof Error ? error.message : String(error);
  console.error(`[kgv-upload-photo] ${step}`, { message });
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

type ApiErrorCode =
  | "BAD_REQUEST"
  | "UNAUTHORIZED"
  | "CONFIG_MISSING"
  | "GOOGLE_AUTH_ERROR"
  | "GOOGLE_DRIVE_ERROR"
  | "INTERNAL_ERROR";

type ApiErrorBody = {
  success: false;
  error_code: ApiErrorCode;
  message: string;
  request_id: string;
};

function errorResponse(status: number, error_code: ApiErrorCode, message: string, requestId: string) {
  const body: ApiErrorBody = {
    success: false,
    error_code,
    message,
    request_id: requestId,
  };

  return json(status, body);
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
    .slice(0, 80);
}

function normalizeMedium(value: string | null): Medium | null {
  const v = (value ?? "").trim().toLowerCase();
  return v === "strom" || v === "wasser" ? v : null;
}

function normalizeKind(value: string | null): UploadKind | null {
  const v = (value ?? "").trim().toLowerCase();
  return v === "ablesung" || v === "ausbau" || v === "einbau" ? v : null;
}

function normalizeDateOnly(value: string | null): string | null {
  const v = (value ?? "").trim();

  const isoMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(v);
  if (isoMatch) {
    const [, year, month, day] = isoMatch;
    return normalizeDateParts(Number(year), Number(month), Number(day));
  }

  const germanMatch = /^(\d{2})\.(\d{2})\.(\d{4})$/.exec(v);
  if (germanMatch) {
    const [, day, month, year] = germanMatch;
    return normalizeDateParts(Number(year), Number(month), Number(day));
  }

  return null;
}

function normalizeDateParts(year: number, month: number, day: number): string | null {
  if (!Number.isInteger(year) || !Number.isInteger(month) || !Number.isInteger(day)) {
    return null;
  }

  const candidate = new Date(Date.UTC(year, month - 1, day));
  if (
    candidate.getUTCFullYear() !== year ||
    candidate.getUTCMonth() !== month - 1 ||
    candidate.getUTCDate() !== day
  ) {
    return null;
  }

  return `${year.toString().padStart(4, "0")}-${month.toString().padStart(2, "0")}-${day.toString().padStart(2, "0")}`;
}

function guessExtension(file: File): string {
  const name = file.name ?? "";
  const dotIndex = name.lastIndexOf(".");
  if (dotIndex > -1 && dotIndex < name.length - 1) {
    return name.slice(dotIndex).toLowerCase();
  }

  const type = (file.type || "").toLowerCase();
  if (type === "image/jpeg") return ".jpg";
  if (type === "image/png") return ".png";
  if (type === "image/webp") return ".webp";
  if (type === "application/pdf") return ".pdf";
  return ".bin";
}

function buildFileName(input: {
  date: string;
  kind: UploadKind;
  medium: Medium;
  garden: string;
  meterNumber?: string | null;
  extension: string;
}) {
  const garden = sanitizeSegment(input.garden);
  const meter = sanitizeSegment(input.meterNumber ?? "");
  const meterPart = meter ? `_zaehler-${meter}` : "";
  return `${input.date}_${input.kind}_${input.medium}_garten-${garden}${meterPart}${input.extension}`;
}

async function getGoogleAccessToken(): Promise<string> {
  const clientId = Deno.env.get("GOOGLE_DRIVE_CLIENT_ID");
  const clientSecret = Deno.env.get("GOOGLE_DRIVE_CLIENT_SECRET");
  const refreshToken = Deno.env.get("GOOGLE_DRIVE_REFRESH_TOKEN");

  if (!clientId || !clientSecret || !refreshToken) {
    const missing = {
      hasClientId: !!clientId,
      hasClientSecret: !!clientSecret,
      hasRefreshToken: !!refreshToken,
    };
    logStep("google token refresh config missing", missing);
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
      const safeDetails = {
        status: tokenRes.status,
        error: typeof tokenJson?.error === "string" ? tokenJson.error : undefined,
        error_description: typeof tokenJson?.error_description === "string" ? tokenJson.error_description : undefined,
      };
      logStep("google token refresh failed", safeDetails);
      throw new Error(`Google token refresh failed: ${safeDetails.error ?? "unknown"}`);
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

async function driveList(
  accessToken: string,
  query: string,
): Promise<Array<{ id: string; name: string }>> {
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

async function createDriveFolder(
  accessToken: string,
  parentId: string,
  folderName: string,
): Promise<string> {
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

async function ensureFolder(
  accessToken: string,
  parentId: string,
  folderName: string,
): Promise<string> {
  logStep("ensure folder start", { folderName });
  const safeName = folderName.replace(/'/g, "\\'");
  const q =
    `mimeType = 'application/vnd.google-apps.folder' and trashed = false and name = '${safeName}' and '${parentId}' in parents`;

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
  const length = parts.reduce((sum, p) => sum + p.length, 0);
  const out = new Uint8Array(length);
  let offset = 0;
  for (const part of parts) {
    out.set(part, offset);
    offset += part.length;
  }
  return out;
}

async function uploadFileToDrive(params: {
  accessToken: string;
  parentId: string;
  fileName: string;
  file: File;
}): Promise<DriveUploadResult> {
  const boundary = `kgv-${crypto.randomUUID()}`;
  const encoder = new TextEncoder();
  const fileBytes = new Uint8Array(await params.file.arrayBuffer());

  const metadata = {
    name: params.fileName,
    parents: [params.parentId],
  };

  const header1 =
    `--${boundary}\r\nContent-Type: application/json; charset=UTF-8\r\n\r\n${JSON.stringify(metadata)}\r\n`;
  const header2 =
    `--${boundary}\r\nContent-Type: ${params.file.type || "application/octet-stream"}\r\n\r\n`;
  const footer = `\r\n--${boundary}--`;

  const body = concatUint8Arrays([
    encoder.encode(header1),
    encoder.encode(header2),
    fileBytes,
    encoder.encode(footer),
  ]);

  try {
    const res = await fetch(
      "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart&fields=id,name,webViewLink",
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${params.accessToken}`,
          "Content-Type": `multipart/related; boundary=${boundary}`,
        },
        body,
        signal: AbortSignal.timeout(DRIVE_UPLOAD_TIMEOUT_MS),
      },
    );

    const jsonBody = await res.json();
    if (!res.ok || !jsonBody.id) {
      throw new Error(`Drive upload failed: ${JSON.stringify(jsonBody)}`);
    }

    return {
      id: jsonBody.id as string,
      name: (jsonBody.name as string) ?? params.fileName,
      webViewLink: (jsonBody.webViewLink as string | null) ?? null,
    };
  } catch (error) {
    throw buildStepError("Drive upload", error);
  }
}

async function downloadDriveFile(accessToken: string, driveFileId: string): Promise<Response> {
  const url = new URL(`https://www.googleapis.com/drive/v3/files/${encodeURIComponent(driveFileId)}`);
  url.searchParams.set("alt", "media");
  url.searchParams.set("supportsAllDrives", "true");
  const response = await fetch(url, {
    headers: { Authorization: `Bearer ${accessToken}` },
    signal: AbortSignal.timeout(DRIVE_UPLOAD_TIMEOUT_MS),
  });

  if (!response.ok) {
    throw new Error(`Drive photo download failed: ${await response.text()}`);
  }

  return response;
}

async function authenticatePhotoUser(authHeader: string) {
  const supabaseUrl = Deno.env.get("SUPABASE_URL");
  const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
  const anonKey = Deno.env.get("SUPABASE_ANON_KEY");
  if (!supabaseUrl || !serviceRoleKey || !anonKey) {
    throw new Error("Supabase-Serverkonfiguration fehlt.");
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

  return {
    ok: true as const,
    userId: userData.user.id,
    supabaseAdmin,
    // All caller-dependent decisions run through PostgREST with the validated
    // user JWT.  The service client is used only after authorization to access
    // the private Drive object or write its trusted provenance.
    supabaseUser: createClient(supabaseUrl, anonKey, { global: { headers: { Authorization: `Bearer ${jwt}` } } }),
  };
}

async function mayReadMeterReadingPhoto(auth: AuthenticatedPhotoUser & { supabaseAdmin: ReturnType<typeof createClient> }, ablesungId: number): Promise<MeterReadingPhotoRecord | null> {
  const { data: reading, error: readingError } = await auth.supabaseAdmin
    .from("zaehler_ablesung")
    .select("id,ablesedatum,art,foto_drive_file_id,foto_dateiname,zaehler:zaehler_id(parzelle_id,medium,parzelle:parzelle_id(garten_nr,Anlage))")
    .eq("id", ablesungId)
    .maybeSingle();
  if (readingError) {
    throw new Error(`Ablesungsfoto-Rechte konnten nicht geprüft werden: ${readingError.message}`);
  }
  if (!reading) return null;

  const [{ data: canRead, error: permissionError }, { data: ownsHistory, error: ownError }] = await Promise.all([
    auth.supabaseUser.rpc("has_effective_permission", { p_permission: 32 }),
    auth.supabaseUser.rpc("can_access_own_meter_reading", { p_ablesung_id: ablesungId }),
  ]);
  if (permissionError || ownError) throw new Error("Ablesungsfoto-Rechte konnten nicht geprüft werden.");
  return canRead === true || ownsHistory === true ? reading as MeterReadingPhotoRecord : null;
}

Deno.serve(async (req) => {
  const requestId = createRequestId();
  logStep("function start", { method: req.method });

  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  if (req.method !== "POST") {
    logStep("return error", { step: "method validate", status: 405, requestId });
    return errorResponse(405, "BAD_REQUEST", "Method not allowed", requestId);
  }

  try {
    const authHeader = req.headers.get("Authorization") ?? "";
    logStep("auth header received", { requestId, hasAuthorizationHeader: !authHeader ? false : true });

    if ((req.headers.get("content-type") ?? "").toLowerCase().includes("application/json")) {
      const payload = await req.json() as { action?: string; ablesung_id?: number | string };
      if ((payload.action ?? "").trim().toLowerCase() !== "download") {
        return errorResponse(400, "BAD_REQUEST", "Unbekannte Fotoaktion.", requestId);
      }
      const ablesungId = Number(payload.ablesung_id);
      if (!Number.isSafeInteger(ablesungId) || ablesungId <= 0) {
        return errorResponse(400, "BAD_REQUEST", "ablesung_id fehlt oder ist ungültig.", requestId);
      }

      const auth = await authenticatePhotoUser(authHeader);
      if (!auth.ok) return errorResponse(auth.status, "UNAUTHORIZED", auth.message, requestId);
      const reading = await mayReadMeterReadingPhoto(auth, ablesungId);
      if (!reading) return errorResponse(403, "UNAUTHORIZED", "Dieses Ablesungsfoto ist nicht freigegeben.", requestId);
      const driveFileId = (reading.foto_drive_file_id ?? "").trim();
      if (!driveFileId) return errorResponse(409, "GOOGLE_DRIVE_ERROR", "Das Ablesungsfoto besitzt keine Google-Drive-Dateireferenz.", requestId);

      const driveResponse = await downloadDriveFile(await getGoogleAccessToken(), driveFileId);
      const contentType = driveResponse.headers.get("content-type") || "image/jpeg";
      const safeFileName = (reading.foto_dateiname ?? "ablesungsfoto.jpg").replace(/[\r\n"]/g, "_");
      logStep("drive photo download success", { requestId, ablesungId, role: auth.role });
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

    const auth = await authenticatePhotoUser(authHeader);
    if (!auth.ok) return errorResponse(auth.status, "UNAUTHORIZED", auth.message, requestId);

    const rootFolderId = readGoogleDriveRootFolderId();

    const contentType = req.headers.get("content-type") ?? "";
    if (!contentType.toLowerCase().includes("multipart/form-data")) {
      logStep("return error", { step: "content-type validate", status: 400, contentType, requestId });
      return errorResponse(400, "BAD_REQUEST", "Erwartet multipart/form-data mit Datei und Metadaten.", requestId);
    }

    logStep("content-type validated", { contentType });

    logStep("formData start");

    const form = await req.formData();

    logStep("formData parsed", {
      hasFile: form.get("file") instanceof File,
      hasReadingId: form.has("ablesung_id"),
    });

    const file = form.get("file");
    if (!(file instanceof File)) {
      logStep("return error", { step: "file validate", status: 400, requestId });
      return errorResponse(400, "BAD_REQUEST", "Datei-Feld 'file' fehlt.", requestId);
    }

    const ablesungId = Number(form.get("ablesung_id"));
    if (!Number.isSafeInteger(ablesungId) || ablesungId <= 0) return errorResponse(400, "BAD_REQUEST", "ablesung_id fehlt oder ist ungültig.", requestId);
    const reading = await mayReadMeterReadingPhoto(auth, ablesungId);
    if (!reading || !reading.zaehler || reading.foto_drive_file_id) return errorResponse(403, "UNAUTHORIZED", "Dieses Ablesungsfoto darf nicht gespeichert werden.", requestId);
    const kind = normalizeKind(reading.art);
    const medium = normalizeMedium(reading.zaehler.medium);
    const date = normalizeDateOnly(reading.ablesedatum);
    const anlageRaw = reading.zaehler.parzelle?.Anlage ?? "Hauptzaehler";
    const gardenRaw = reading.zaehler.parzelle?.garten_nr ?? "Hauptzaehler";
    if (!kind || !medium || !date) return errorResponse(409, "BAD_REQUEST", "Die Ablesung besitzt keine gültige Foto-Provenance.", requestId);
    const meterNumberRaw = "";

    logStep("input normalized / validated", {
      kind,
      medium,
      ablesungId,
    });

    const anlage = sanitizeSegment(anlageRaw);
    const garden = sanitizeSegment(gardenRaw);
    const year = date.slice(0, 4);
    const extension = guessExtension(file);
    const fileName = buildFileName({
      date,
      kind,
      medium,
      garden,
      meterNumber: meterNumberRaw,
      extension,
    });

    const accessToken = await getGoogleAccessToken();
    await verifyDriveRootFolder(accessToken, rootFolderId);

    let parentId = rootFolderId;
    const folderSegments = ["Ablesungen", year, anlage, garden, medium];
    for (const segment of folderSegments) {
      logStep("ensure folder segment", { segment });
      parentId = await ensureFolder(accessToken, parentId, segment);
    }

    logStep("drive upload start", { fileName, folderDepth: folderSegments.length });
    const upload = await uploadFileToDrive({
      accessToken,
      parentId,
      fileName,
      file,
    });

    logStep("drive upload success", { fileId: upload.id, fileName: upload.name });

    const relativePath = `${folderSegments.join("/")}/${upload.name}`;
    const { data: linked, error: linkError } = await auth.supabaseAdmin
      .from("zaehler_ablesung")
      .update({ foto_pfad: relativePath, foto_drive_file_id: upload.id, foto_dateiname: upload.name })
      .eq("id", ablesungId)
      .is("foto_drive_file_id", null)
      .select("id")
      .maybeSingle();
    if (linkError || !linked) throw new Error("Ablesungsfoto konnte nicht unveränderlich verknüpft werden.");

    logStep("return success", { requestId, relativePath });
    return json(200, {
      success: true,
      message: "Ablesungsfoto wurde geschützt gespeichert und verknüpft.",
      request_id: requestId,
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    logError("return error", error);
    logStep("return error details", { requestId, message });

    if (message.includes("Google-Drive-Secrets fehlen")) {
      return errorResponse(
        503,
        "CONFIG_MISSING",
        "Cloud-Upload ist nicht konfiguriert. Bitte Serverkonfiguration prüfen.",
        requestId,
      );
    }

    if (message.toLowerCase().includes("google token refresh") && isGoogleAuthErrorMessage(message)) {
      return errorResponse(
        503,
        "GOOGLE_AUTH_ERROR",
        "Cloud-Upload aktuell nicht verfügbar. Bitte Google-Drive-Autorisierung/Refresh-Token prüfen.",
        requestId,
      );
    }

    if (message.toLowerCase().includes("google token refresh")) {
      return errorResponse(
        503,
        "GOOGLE_AUTH_ERROR",
        "Cloud-Upload aktuell nicht verfügbar. Bitte Google-Drive-Autorisierung/Serverkonfiguration prüfen.",
        requestId,
      );
    }

    if (message.toLowerCase().includes("drive")) {
      return errorResponse(
        502,
        "GOOGLE_DRIVE_ERROR",
        "Cloud-Upload ist aktuell nicht erreichbar. Bitte später erneut versuchen.",
        requestId,
      );
    }

    return errorResponse(500, "INTERNAL_ERROR", "Unerwarteter Serverfehler beim Cloud-Upload.", requestId);
  }
});
