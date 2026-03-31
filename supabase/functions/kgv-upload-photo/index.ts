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
};

function errorResponse(status: number, error_code: ApiErrorCode, message: string) {
  const body: ApiErrorBody = {
    success: false,
    error_code,
    message,
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

async function requireAdminOrVorstand(authHeader: string) {
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
    .select("role")
    .eq("user_id", userData.user.id)
    .maybeSingle();

  if (appUserError) {
    return { ok: false as const, status: 500, message: "Benutzerrolle konnte nicht geladen werden." };
  }

  const role = (appUser?.role ?? "").trim().toLowerCase();
  if (role !== "admin" && role !== "vorstand") {
    return { ok: false as const, status: 403, message: "Nur Admin oder Vorstand dürfen Fotos hochladen." };
  }

  logStep("auth passed", { role });
  return { ok: true as const, userId: userData.user.id, role };
}

Deno.serve(async (req) => {
  logStep("function start", { method: req.method });

  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  if (req.method !== "POST") {
    logStep("return error", { step: "method validate", status: 405 });
    return json(405, { error: "Method not allowed" });
  }

  try {
    const authHeader = req.headers.get("Authorization") ?? "";
    logStep("auth header received", { hasAuthorizationHeader: !authHeader ? false : true });
    const auth = await requireAdminOrVorstand(authHeader);
    if (!auth.ok) {
      logStep("return error", { step: "auth failed", status: auth.status });
      return json(auth.status, { error: auth.message });
    }

    const rootFolderId = readGoogleDriveRootFolderId();

    const contentType = req.headers.get("content-type") ?? "";
    if (!contentType.toLowerCase().includes("multipart/form-data")) {
      logStep("return error", { step: "content-type validate", status: 400, contentType });
      return json(400, { error: "Erwartet multipart/form-data mit Datei und Metadaten." });
    }

    logStep("content-type validated", { contentType });

    logStep("formData start");

    const form = await req.formData();

    logStep("formData parsed", {
      hasFile: form.get("file") instanceof File,
      hasKind: form.has("kind"),
      hasMedium: form.has("medium"),
      hasDatum: form.has("datum"),
      hasAnlage: form.has("anlage"),
      hasGarten: form.has("garten"),
    });

    const file = form.get("file");
    if (!(file instanceof File)) {
      logStep("return error", { step: "file validate", status: 400 });
      return json(400, { error: "Datei-Feld 'file' fehlt." });
    }

    const rawDate = String(form.get("datum") ?? "").trim();
    const kind = normalizeKind(String(form.get("kind") ?? ""));
    const medium = normalizeMedium(String(form.get("medium") ?? ""));
    const date = normalizeDateOnly(rawDate);
    const anlageRaw = String(form.get("anlage") ?? "").trim();
    const gardenRaw = String(form.get("garten") ?? "").trim();
    const meterNumberRaw = String(form.get("zaehlernummer") ?? "").trim();

    if (!kind) {
      logStep("return error", { step: "kind validate", status: 400 });
      return json(400, { error: "Ungültiges Feld 'kind'. Erlaubt: ablesung, ausbau, einbau." });
    }

    if (!medium) {
      logStep("return error", { step: "medium validate", status: 400 });
      return json(400, { error: "Ungültiges Feld 'medium'. Erlaubt: strom, wasser." });
    }

    if (!date) {
      logStep("return error", { step: "datum validate", status: 400, rawDate });
      return json(400, { error: "Ungültiges Feld 'datum'. Erwartet YYYY-MM-DD oder dd.MM.yyyy." });
    }

    if (!anlageRaw) {
      logStep("return error", { step: "anlage validate", status: 400 });
      return json(400, { error: "Feld 'anlage' fehlt." });
    }

    if (!gardenRaw) {
      logStep("return error", { step: "garten validate", status: 400 });
      return json(400, { error: "Feld 'garten' fehlt." });
    }

    logStep("input normalized / validated", {
      kind,
      medium,
      rawDate,
      normalizedDate: date,
      hasMeterNumber: !meterNumberRaw ? false : true,
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

    logStep("return success", { relativePath });
    return json(200, {
      success: true,
      file_id: upload.id,
      file_name: upload.name,
      relative_path: relativePath,
      web_view_link: upload.webViewLink ?? null,
      uploaded_by_user_id: auth.userId,
      role: auth.role,
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    logError("return error", error);

    if (message.includes("Google-Drive-Secrets fehlen")) {
      return errorResponse(
        503,
        "CONFIG_MISSING",
        "Cloud-Upload ist nicht konfiguriert. Bitte Serverkonfiguration prüfen.",
      );
    }

    if (message.toLowerCase().includes("google token refresh") && isGoogleAuthErrorMessage(message)) {
      return errorResponse(
        503,
        "GOOGLE_AUTH_ERROR",
        "Cloud-Upload aktuell nicht verfügbar. Bitte Google-Drive-Autorisierung/Refresh-Token prüfen.",
      );
    }

    if (message.toLowerCase().includes("google token refresh")) {
      return errorResponse(
        503,
        "GOOGLE_AUTH_ERROR",
        "Cloud-Upload aktuell nicht verfügbar. Bitte Google-Drive-Autorisierung/Serverkonfiguration prüfen.",
      );
    }

    if (message.toLowerCase().includes("drive")) {
      return errorResponse(
        502,
        "GOOGLE_DRIVE_ERROR",
        "Cloud-Upload ist aktuell nicht erreichbar. Bitte später erneut versuchen.",
      );
    }

    return errorResponse(500, "INTERNAL_ERROR", "Unerwarteter Serverfehler beim Cloud-Upload.");
  }
});