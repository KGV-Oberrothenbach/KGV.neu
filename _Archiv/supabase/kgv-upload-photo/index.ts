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

function json(status: number, body: unknown) {
  return new Response(JSON.stringify(body, null, 2), {
    status,
    headers: {
      ...corsHeaders,
      "Content-Type": "application/json; charset=utf-8",
    },
  });
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
  if (!/^\d{4}-\d{2}-\d{2}$/.test(v)) return null;
  return v;
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
    throw new Error("Google-Drive-Secrets fehlen: GOOGLE_DRIVE_CLIENT_ID / SECRET / REFRESH_TOKEN.");
  }

  const body = new URLSearchParams({
    client_id: clientId,
    client_secret: clientSecret,
    refresh_token: refreshToken,
    grant_type: "refresh_token",
  });

  const tokenRes = await fetch("https://oauth2.googleapis.com/token", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body,
  });

  const tokenJson = await tokenRes.json();
  if (!tokenRes.ok || !tokenJson.access_token) {
    throw new Error(`Google-Token konnte nicht erneuert werden: ${JSON.stringify(tokenJson)}`);
  }

  return tokenJson.access_token as string;
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

  const res = await fetch(url, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  });

  const jsonBody = await res.json();
  if (!res.ok) {
    throw new Error(`Drive files.list fehlgeschlagen: ${JSON.stringify(jsonBody)}`);
  }

  return (jsonBody.files ?? []) as Array<{ id: string; name: string }>;
}

async function createDriveFolder(
  accessToken: string,
  parentId: string,
  folderName: string,
): Promise<string> {
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
  });

  const jsonBody = await res.json();
  if (!res.ok || !jsonBody.id) {
    throw new Error(`Drive-Ordner konnte nicht angelegt werden: ${JSON.stringify(jsonBody)}`);
  }

  return jsonBody.id as string;
}

async function ensureFolder(
  accessToken: string,
  parentId: string,
  folderName: string,
): Promise<string> {
  const safeName = folderName.replace(/'/g, "\\'");
  const q =
    `mimeType = 'application/vnd.google-apps.folder' and trashed = false and name = '${safeName}' and '${parentId}' in parents`;

  const existing = await driveList(accessToken, q);
  if (existing.length > 0) {
    return existing[0].id;
  }

  return await createDriveFolder(accessToken, parentId, folderName);
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

  const res = await fetch(
    "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart&fields=id,name,webViewLink",
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${params.accessToken}`,
        "Content-Type": `multipart/related; boundary=${boundary}`,
      },
      body,
    },
  );

  const jsonBody = await res.json();
  if (!res.ok || !jsonBody.id) {
    throw new Error(`Drive-Upload fehlgeschlagen: ${JSON.stringify(jsonBody)}`);
  }

  return {
    id: jsonBody.id as string,
    name: (jsonBody.name as string) ?? params.fileName,
    webViewLink: (jsonBody.webViewLink as string | null) ?? null,
  };
}

async function requireAdminOrVorstand(authHeader: string) {
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

  return { ok: true as const, userId: userData.user.id, role };
}

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  if (req.method !== "POST") {
    return json(405, { error: "Method not allowed" });
  }

  try {
    const authHeader = req.headers.get("Authorization") ?? "";
    const auth = await requireAdminOrVorstand(authHeader);
    if (!auth.ok) {
      return json(auth.status, { error: auth.message });
    }

    const rootFolderId = Deno.env.get("GOOGLE_DRIVE_ROOT_FOLDER_ID");
    if (!rootFolderId) {
      return json(500, { error: "GOOGLE_DRIVE_ROOT_FOLDER_ID fehlt." });
    }

    const contentType = req.headers.get("content-type") ?? "";
    if (!contentType.toLowerCase().includes("multipart/form-data")) {
      return json(400, { error: "Erwartet multipart/form-data mit Datei und Metadaten." });
    }

    const form = await req.formData();

    const file = form.get("file");
    if (!(file instanceof File)) {
      return json(400, { error: "Datei-Feld 'file' fehlt." });
    }

    const kind = normalizeKind(String(form.get("kind") ?? ""));
    const medium = normalizeMedium(String(form.get("medium") ?? ""));
    const date = normalizeDateOnly(String(form.get("datum") ?? ""));
    const anlageRaw = String(form.get("anlage") ?? "").trim();
    const gardenRaw = String(form.get("garten") ?? "").trim();
    const meterNumberRaw = String(form.get("zaehlernummer") ?? "").trim();

    if (!kind) return json(400, { error: "Ungültiges Feld 'kind'. Erlaubt: ablesung, ausbau, einbau." });
    if (!medium) return json(400, { error: "Ungültiges Feld 'medium'. Erlaubt: strom, wasser." });
    if (!date) return json(400, { error: "Ungültiges Feld 'datum'. Erwartet YYYY-MM-DD." });
    if (!anlageRaw) return json(400, { error: "Feld 'anlage' fehlt." });
    if (!gardenRaw) return json(400, { error: "Feld 'garten' fehlt." });

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

    let parentId = rootFolderId;
    const folderSegments = ["Ablesungen", year, anlage, garden, medium];
    for (const segment of folderSegments) {
      parentId = await ensureFolder(accessToken, parentId, segment);
    }

    const upload = await uploadFileToDrive({
      accessToken,
      parentId,
      fileName,
      file,
    });

    const relativePath = `${folderSegments.join("/")}/${upload.name}`;

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
    return json(500, {
      success: false,
      error: message,
    });
  }
});