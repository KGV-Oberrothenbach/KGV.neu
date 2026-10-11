// @ts-nocheck
import { createClient } from "npm:@supabase/supabase-js@2";
import nodemailer from "npm:nodemailer@6.9.10";

type InviteRequest = {
  mitgliedId: number;
  role?: string;
  inviteMethod?: string;
};

type JsonResponse = {
  success: boolean;
  outcome?: string;
  errorCode?: string;
  message: string;
  mitgliedId?: number;
  email?: string;
  userId?: string;
  authUserId?: string;
  mailSent?: boolean;
};

const allowedRoles = new Set(["admin", "vorstand", "user"]);

function newRequestId() {
  try {
    return crypto.randomUUID();
  } catch {
    return String(Date.now());
  }
}

function maskEmail(email?: string) {
  const e = String(email ?? "").trim();
  const at = e.indexOf("@");
  if (at <= 1) return e ? "***" : "";
  const user = e.slice(0, at);
  const domain = e.slice(at + 1);
  const prefix = user.slice(0, 2);
  return `${prefix}***@${domain}`;
}

function getHostAndProjectRef(rawUrl?: string) {
  const value = String(rawUrl ?? "").trim();
  if (!value) {
    return { host: "", projectRef: "" };
  }

  try {
    const url = new URL(value);
    const host = url.host;
    const hostSegments = host.split(".");
    return {
      host,
      projectRef: hostSegments.length >= 1 ? hostSegments[0] : "",
    };
  } catch {
    return { host: value, projectRef: "" };
  }
}

function safeErrorInfo(err: any) {
  if (!err) return null;
  return {
    name: err?.name,
    status: err?.status,
    code: err?.code,
    message: err?.message,
  };
}

function classifyGetUserError(message?: string) {
  const m = String(message ?? "").toLowerCase();
  if (!m) return "Unknown";
  if (m.includes("invalid jwt") || (m.includes("jwt") && m.includes("invalid"))) return "InvalidJwt";
  if (m.includes("expired") && m.includes("jwt")) return "ExpiredJwt";
  if (m.includes("missing") && m.includes("authorization")) return "MissingAuthorization";
  if (m.includes("bearer")) return "BearerFormat";
  if (m.includes("signature")) return "BadSignature";
  return "Other";
}

function classifyProvisioningError(message?: string) {
  const m = String(message ?? "").toLowerCase();
  if (!m) return "Unknown";
  if (m.includes("already") && m.includes("registered")) return "AlreadyRegistered";
  if (m.includes("already") && m.includes("exists")) return "AlreadyExists";
  if (m.includes("validation")) return "Validation";
  if (m.includes("config") || m.includes("missing")) return "ConfigError";
  if (m.includes("rate") && m.includes("limit")) return "RateLimited";
  return "Other";
}

function maskSubject(subject?: string) {
  const s = String(subject ?? "").trim();
  if (!s) return undefined;
  if (s.length <= 8) return "***";
  return `${s.slice(0, 4)}***${s.slice(-4)}`;
}

function parseJwtMetaFromAuthorizationHeader(authHeader?: string) {
  try {
    const normalized = String(authHeader ?? "").trim();
    if (!normalized.toLowerCase().startsWith("bearer ")) {
      return null;
    }

    const token = normalized.slice(7);
    const parts = token.split(".");
    if (parts.length < 2) {
      return null;
    }

    const payloadPart = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = payloadPart + "=".repeat((4 - (payloadPart.length % 4)) % 4);
    const payloadJson = atob(padded);
    const payload = JSON.parse(payloadJson);

    return {
      iss: payload?.iss,
      aud: payload?.aud,
      subMasked: maskSubject(payload?.sub),
      role: payload?.role,
      exp: payload?.exp,
      email: maskEmail(payload?.email),
    };
  } catch {
    return null;
  }
}

function requestMeta(req: Request) {
  return {
    contentType: req.headers.get("content-type") ?? undefined,
    contentLength: req.headers.get("content-length") ?? undefined,
    accept: req.headers.get("accept") ?? undefined,
    userAgent: req.headers.get("user-agent") ?? undefined,
    origin: req.headers.get("origin") ?? undefined,
    referer: req.headers.get("referer") ?? undefined,
    xClientInfo: req.headers.get("x-client-info") ?? undefined,
    forwardedHost: req.headers.get("x-forwarded-host") ?? undefined,
    forwardedProto: req.headers.get("x-forwarded-proto") ?? undefined,
    hasApiKey: !!req.headers.get("apikey"),
    apiKeyLength: (req.headers.get("apikey") ?? "").length,
  };
}

function json(body: JsonResponse, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      "Content-Type": "application/json",
    },
  });
}

function escapeHtml(value?: string) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function normalizeId(value: any): string {
  return String(value ?? "").trim();
}

function normalizeRole(value: any): string {
  return String(value ?? "").trim().toLowerCase();
}

function isMissingAuthUserError(err: any) {
  const message = String(err?.message ?? "").toLowerCase();
  return err?.status === 404 ||
    (message.includes("user") && message.includes("not found")) ||
    (message.includes("auth") && message.includes("not found"));
}

async function tryFindAuthUserIdByEmail(
  supabaseAdmin: any,
  email: string,
  requestId?: string,
): Promise<string | null> {
  const normalized = String(email ?? "").trim().toLowerCase();
  if (!normalized) return null;

  const perPage = 200;
  for (let page = 1; page <= 10; page++) {
    const { data, error } = await supabaseAdmin.auth.admin.listUsers({ page, perPage });
    if (error) {
      console.log("[kgv-invite-user] listUsers failed", {
        requestId,
        page,
        error: safeErrorInfo(error),
      });
      return null;
    }

    const users = data?.users ?? [];
    const hit = users.find((u: any) =>
      String(u?.email ?? "").trim().toLowerCase() === normalized
    );
    if (hit?.id) return hit.id;

    if (users.length < perPage) break;
  }

  return null;
}

async function syncAuthUserMetadata(
  supabaseAdmin: any,
  userId: string,
  email: string,
  mitglied: any,
  callerUserId: string,
  requestId?: string,
): Promise<string> {
  const normalizedUserId = normalizeId(userId);
  if (!normalizedUserId) {
    throw new Error("Missing auth user id");
  }

  const { error } = await supabaseAdmin.auth.admin.updateUserById(normalizedUserId, {
    email,
    email_confirm: true,
    user_metadata: {
      vorname: mitglied.vorname,
      name: mitglied.name,
      mitglied_id: mitglied.id,
      invite_source: "admin_otp_bootstrap",
      invited_by: callerUserId,
    },
  });

  if (error) {
    console.log("[kgv-invite-user] sync auth user metadata failed", {
      requestId,
      email: maskEmail(email),
      userId: normalizedUserId,
      classification: classifyProvisioningError(error?.message),
      error: safeErrorInfo(error),
    });
    throw error;
  }

  return normalizedUserId;
}

async function createAuthUserForInvite(
  supabaseAdmin: any,
  email: string,
  mitglied: any,
  callerUserId: string,
  requestId?: string,
): Promise<string> {
  const { data, error } = await supabaseAdmin.auth.admin.createUser({
    email,
    email_confirm: true,
    user_metadata: {
      vorname: mitglied.vorname,
      name: mitglied.name,
      mitglied_id: mitglied.id,
      invite_source: "admin_otp_bootstrap",
      invited_by: callerUserId,
    },
  });

  if (error || !data?.user?.id) {
    console.log("[kgv-invite-user] create auth user failed", {
      requestId,
      email: maskEmail(email),
      classification: classifyProvisioningError(error?.message),
      error: safeErrorInfo(error),
    });
    throw error ?? new Error("createUser returned no user id");
  }

  return data.user.id;
}

async function ensureAuthUserForInvite(
  supabaseAdmin: any,
  email: string,
  mitglied: any,
  callerUserId: string,
  requestId?: string,
  candidateUserIds: string[] = [],
): Promise<string> {
  const tried = new Set<string>();

  for (const rawCandidate of candidateUserIds) {
    const candidate = normalizeId(rawCandidate);
    if (!candidate || tried.has(candidate)) continue;
    tried.add(candidate);

    try {
      return await syncAuthUserMetadata(
        supabaseAdmin,
        candidate,
        email,
        mitglied,
        callerUserId,
        requestId,
      );
    } catch (err) {
      if (isMissingAuthUserError(err)) {
        console.log("[kgv-invite-user] candidate auth user missing, continue", {
          requestId,
          candidateUserId: candidate,
          error: safeErrorInfo(err),
        });
        continue;
      }
      throw err;
    }
  }

  const existingUserId = await tryFindAuthUserIdByEmail(supabaseAdmin, email, requestId);
  const normalizedExistingUserId = normalizeId(existingUserId);

  if (normalizedExistingUserId && !tried.has(normalizedExistingUserId)) {
    return await syncAuthUserMetadata(
      supabaseAdmin,
      normalizedExistingUserId,
      email,
      mitglied,
      callerUserId,
      requestId,
    );
  }

  return await createAuthUserForInvite(
    supabaseAdmin,
    email,
    mitglied,
    callerUserId,
    requestId,
  );
}

async function readMitgliedForVerification(
  supabaseAdmin: any,
  mitgliedId: number,
  requestId?: string,
) {
  const { data, error } = await supabaseAdmin
    .from("mitglied")
    .select("id, email, auth_user_id")
    .eq("id", mitgliedId)
    .maybeSingle();

  if (error) {
    console.log("[kgv-invite-user] mitglied readback failed", {
      requestId,
      mitgliedId,
      error: safeErrorInfo(error),
    });
    throw error;
  }

  return data;
}

async function verifyMitgliedLinkPersisted(
  supabaseAdmin: any,
  mitgliedId: number,
  expectedUserId: string,
  requestId?: string,
) {
  const verifiedMitglied = await readMitgliedForVerification(supabaseAdmin, mitgliedId, requestId);
  const actualUserId = normalizeId(verifiedMitglied?.auth_user_id);

  if (!verifiedMitglied || actualUserId !== normalizeId(expectedUserId)) {
    console.log("[kgv-invite-user] mitglied readback mismatch", {
      requestId,
      mitgliedId,
      expectedUserId,
      actualUserId,
    });

    return {
      ok: false,
      verifiedMitglied,
    };
  }

  return {
    ok: true,
    verifiedMitglied,
  };
}

async function readAppUserForVerification(
  supabaseAdmin: any,
  mitgliedId: number,
  requestId?: string,
) {
  const { data, error } = await supabaseAdmin
    .from("app_user")
    .select("user_id, mitglied_id, role")
    .eq("mitglied_id", mitgliedId)
    .maybeSingle();

  if (error) {
    console.log("[kgv-invite-user] app_user readback failed", {
      requestId,
      mitgliedId,
      error: safeErrorInfo(error),
    });
    throw error;
  }

  return data;
}

async function verifyAppUserPersisted(
  supabaseAdmin: any,
  mitgliedId: number,
  expectedUserId: string,
  expectedRole: string,
  requestId?: string,
) {
  const verifiedAppUser = await readAppUserForVerification(supabaseAdmin, mitgliedId, requestId);

  const actualUserId = normalizeId(verifiedAppUser?.user_id);
  const actualMitgliedId = Number(verifiedAppUser?.mitglied_id ?? 0);
  const actualRole = normalizeRole(verifiedAppUser?.role);

  if (
    !verifiedAppUser ||
    actualUserId !== normalizeId(expectedUserId) ||
    actualMitgliedId !== Number(mitgliedId) ||
    actualRole !== normalizeRole(expectedRole)
  ) {
    console.log("[kgv-invite-user] app_user readback mismatch", {
      requestId,
      mitgliedId,
      expectedUserId,
      actualUserId,
      expectedMitgliedId: mitgliedId,
      actualMitgliedId,
      expectedRole: normalizeRole(expectedRole),
      actualRole,
    });

    return {
      ok: false,
      verifiedAppUser,
    };
  }

  return {
    ok: true,
    verifiedAppUser,
  };
}

function buildWelcomeMailHtml(vorname: string, playstoreUrl: string) {
  const safeVorname = escapeHtml(vorname || "Hallo");
  const safePlaystoreUrl = escapeHtml(playstoreUrl);

  return `
    <div style="font-family: Arial, Helvetica, sans-serif; color: #222; line-height: 1.5;">
      <p>Hallo ${safeVorname},</p>

      <p>für dich wurde ein Zugang zur KGV-App vorbereitet.</p>

      <p>
        Du kannst die App hier herunterladen:<br>
        <a href="${safePlaystoreUrl}">${safePlaystoreUrl}</a>
      </p>

      <p><strong>So funktioniert dein Erstlogin:</strong></p>
      <ol>
        <li>Öffne die App.</li>
        <li>Wähle <strong>Erstlogin</strong>.</li>
        <li>Gib deine E-Mail-Adresse ein.</li>
        <li>Fordere deinen Anmeldecode an.</li>
        <li>Du erhältst anschließend per E-Mail einen Einmalcode.</li>
        <li>Gib diesen Code in der App ein.</li>
        <li>Lege danach direkt dein persönliches Passwort fest.</li>
      </ol>

      <p><strong>Wichtig:</strong></p>
      <ul>
        <li>Der Code wird erst dann versendet, wenn du ihn in der App anforderst.</li>
        <li>Diese erste E-Mail enthält bewusst noch keinen Login-Link.</li>
        <li>Bitte verwende für den Erstlogin dieselbe E-Mail-Adresse, an die diese Nachricht gesendet wurde.</li>
      </ul>

      <p>Viele Grüße<br>KGV Oberrothenbach</p>
    </div>
  `.trim();
}

function buildWelcomeMailText(vorname: string, playstoreUrl: string) {
  const name = String(vorname ?? "").trim() || "Hallo";
  return [
    `Hallo ${name},`,
    "",
    "für dich wurde ein Zugang zur KGV-App vorbereitet.",
    "",
    "Du kannst die App hier herunterladen:",
    playstoreUrl,
    "",
    "So funktioniert dein Erstlogin:",
    "1. Öffne die App.",
    "2. Wähle 'Erstlogin'.",
    "3. Gib deine E-Mail-Adresse ein.",
    "4. Fordere deinen Anmeldecode an.",
    "5. Du erhältst anschließend per E-Mail einen Einmalcode.",
    "6. Gib diesen Code in der App ein.",
    "7. Lege danach direkt dein persönliches Passwort fest.",
    "",
    "Wichtig:",
    "- Der Code wird erst dann versendet, wenn du ihn in der App anforderst.",
    "- Diese erste E-Mail enthält bewusst noch keinen Login-Link.",
    "- Bitte verwende für den Erstlogin dieselbe E-Mail-Adresse, an die diese Nachricht gesendet wurde.",
    "",
    "Viele Grüße",
    "KGV Oberrothenbach",
  ].join("\n");
}

async function sendWelcomeMail(
  email: string,
  vorname: string,
  requestId?: string,
) {
  const smtpHost = Deno.env.get("SMTP_HOST") ?? "";
  const smtpPort = Number(Deno.env.get("SMTP_PORT") ?? "465");
  const smtpUser = Deno.env.get("SMTP_USER") ?? "";
  const smtpPassword =
    Deno.env.get("SMTP_PASSWORD") ??
    Deno.env.get("smtpPassword") ??
    "";
  const smtpFrom = Deno.env.get("SMTP_FROM") ?? smtpUser;
  const playstoreUrl = Deno.env.get("PLAYSTORE_URL") ?? "https://play.google.com/store";

  if (!smtpHost || !smtpPort || !smtpUser || !smtpPassword || !smtpFrom) {
    console.log("[kgv-invite-user] welcome mail config missing", {
      requestId,
      hasSmtpHost: !!smtpHost,
      hasSmtpPort: !!smtpPort,
      hasSmtpUser: !!smtpUser,
      hasSmtpPassword: !!smtpPassword,
      hasSmtpFrom: !!smtpFrom,
    });
    throw new Error("Missing SMTP configuration");
  }

  const transport = nodemailer.createTransport({
    host: smtpHost,
    port: smtpPort,
    secure: smtpPort === 465,
    auth: {
      user: smtpUser,
      pass: smtpPassword,
    },
  });

  const subject = "Willkommen beim KGV-App-Zugang";
  const html = buildWelcomeMailHtml(vorname, playstoreUrl);
  const text = buildWelcomeMailText(vorname, playstoreUrl);

  await transport.sendMail({
    from: smtpFrom,
    to: email,
    subject,
    text,
    html,
  });
}

Deno.serve(async (req: Request) => {
  const requestId = newRequestId();
  const requestStartedAt = Date.now();

  try {
    const clientRequestId = req.headers.get("x-kgv-client-request-id") ?? undefined;

    console.log("[kgv-invite-user] request start", {
      requestId,
      clientRequestId,
      method: req.method,
      url: req.url,
      meta: requestMeta(req),
    });

    if (req.method !== "POST") {
      console.log("[kgv-invite-user] reject: method", {
        requestId,
        method: req.method,
      });

      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "MethodNotAllowed",
          message: "Nur POST ist erlaubt.",
        },
        405,
      );
    }

    const authHeader = req.headers.get("Authorization");
    const normalizedAuthHeader = (authHeader ?? "").trim();
    const hasAuthorizationHeader = normalizedAuthHeader.length > 0;
    const isBearer = hasAuthorizationHeader && normalizedAuthHeader.toLowerCase().startsWith("bearer ");
    const bearerTokenLength = isBearer ? Math.max(0, normalizedAuthHeader.length - "Bearer ".length) : 0;
    const looksLikeJwt = isBearer && bearerTokenLength > 0;

    let tokenDotCount = 0;
    if (isBearer) {
      try {
        const token = normalizedAuthHeader.slice(7);
        tokenDotCount = (token.match(/\./g) || []).length;
      } catch {
        tokenDotCount = 0;
      }
    }

    console.log("[kgv-invite-user] auth header", {
      requestId,
      clientRequestId,
      hasAuthorizationHeader,
      isBearer,
      bearerTokenLength,
      looksLikeJwt: looksLikeJwt && tokenDotCount === 2,
      tokenDotCount,
      authorizationHeaderLength: normalizedAuthHeader.length,
      jwtMeta: parseJwtMetaFromAuthorizationHeader(normalizedAuthHeader),
    });

    if (!hasAuthorizationHeader) {
      console.log("[kgv-invite-user] reject401: missing auth header", {
        requestId,
        clientRequestId,
      });

      return json(
        {
          success: false,
          outcome: "unauthorized",
          errorCode: "Unauthorized",
          message: "Keine Berechtigung.",
        },
        401,
      );
    }

    if (!isBearer) {
      console.log("[kgv-invite-user] reject401: invalid bearer format", {
        requestId,
        clientRequestId,
        hasAuthorizationHeader,
        authorizationHeaderLength: normalizedAuthHeader.length,
      });

      return json(
        {
          success: false,
          outcome: "unauthorized",
          errorCode: "Unauthorized",
          message: "Keine Berechtigung.",
        },
        401,
      );
    }

    const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? "";
    const publishableKey = Deno.env.get("SUPABASE_PUBLISHABLE_KEY") ?? Deno.env.get("SUPABASE_ANON_KEY") ?? "";
    const publishableKeySource = Deno.env.get("SUPABASE_PUBLISHABLE_KEY")
      ? "SUPABASE_PUBLISHABLE_KEY"
      : (Deno.env.get("SUPABASE_ANON_KEY") ? "SUPABASE_ANON_KEY (legacy fallback)" : "");

    const serviceRoleKey =
      Deno.env.get("KGV_SERVICE_ROLE_KEY") ??
      Deno.env.get("SERVICE_ROLE_KEY") ??
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ??
      "";

    const serviceRoleKeySource = Deno.env.get("KGV_SERVICE_ROLE_KEY")
      ? "KGV_SERVICE_ROLE_KEY"
      : (Deno.env.get("SERVICE_ROLE_KEY")
        ? "SERVICE_ROLE_KEY (legacy fallback)"
        : (Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ? "SUPABASE_SERVICE_ROLE_KEY (legacy fallback)" : ""));

    const requestHostInfo = getHostAndProjectRef(req.url);
    const supabaseHostInfo = getHostAndProjectRef(supabaseUrl);

    console.log("[kgv-invite-user] env status", {
      requestId,
      hasUrl: !!supabaseUrl,
      hasPublishable: !!publishableKey,
      publishableKeySource,
      hasServiceRoleKey: !!serviceRoleKey,
      serviceRoleKeySource,
      requestHost: requestHostInfo.host,
      requestProjectRef: requestHostInfo.projectRef,
      supabaseHost: supabaseHostInfo.host,
      supabaseProjectRef: supabaseHostInfo.projectRef,
      projectRefMatch: !!requestHostInfo.projectRef && !!supabaseHostInfo.projectRef
        ? requestHostInfo.projectRef === supabaseHostInfo.projectRef
        : undefined,
    });

    if (!supabaseUrl || !publishableKey || !serviceRoleKey) {
      console.log("[kgv-invite-user] reject: server config missing", {
        requestId,
        hasUrl: !!supabaseUrl,
        hasPublishable: !!publishableKey,
        publishableKeySource,
        hasServiceRoleKey: !!serviceRoleKey,
        serviceRoleKeySource,
      });

      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "ServerConfig",
          message: "Server-Konfiguration unvollständig.",
        },
        500,
      );
    }

    const supabaseUser = createClient(supabaseUrl, publishableKey, {
      global: {
        headers: {
          Authorization: normalizedAuthHeader,
        },
      },
    });

    const supabaseAdmin = createClient(supabaseUrl, serviceRoleKey);

    const getUserStartedAt = Date.now();
    const { data: userData, error: userError } = await supabaseUser.auth.getUser();

    if (userError || !userData.user) {
      console.log("[kgv-invite-user] reject401: getUser failed", {
        requestId,
        clientRequestId,
        classification: classifyGetUserError(userError?.message),
        error: safeErrorInfo(userError),
        elapsedMs: Date.now() - getUserStartedAt,
      });

      return json(
        {
          success: false,
          outcome: "unauthorized",
          errorCode: "Unauthorized",
          message: "Keine gültige Session.",
        },
        401,
      );
    }

    console.log("[kgv-invite-user] getUser ok", {
      requestId,
      clientRequestId,
      callerUserId: userData.user.id,
      callerEmail: maskEmail(userData.user.email),
      elapsedMs: Date.now() - getUserStartedAt,
    });

    const callerUserId = userData.user.id;

    const callerLookupStartedAt = Date.now();
    const { data: callerAppUser, error: callerAppUserError } = await supabaseAdmin
      .from("app_user")
      .select("role, mitglied_id")
      .eq("user_id", callerUserId)
      .maybeSingle();

    if (callerAppUserError) {
      console.log("[kgv-invite-user] caller app_user lookup failed", {
        requestId,
        error: safeErrorInfo(callerAppUserError),
        elapsedMs: Date.now() - callerLookupStartedAt,
      });

      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "ServerError",
          message: "Serverfehler.",
        },
        500,
      );
    }

    console.log("[kgv-invite-user] caller app_user lookup ok", {
      requestId,
      clientRequestId,
      callerRole: callerAppUser?.role ?? null,
      callerMitgliedId: callerAppUser?.mitglied_id ?? null,
      elapsedMs: Date.now() - callerLookupStartedAt,
    });

    const callerRole = normalizeRole(callerAppUser?.role);
    if (callerRole !== "admin" && callerRole !== "vorstand") {
      console.log("[kgv-invite-user] reject403: insufficient role", {
        requestId,
        clientRequestId,
        callerRole,
      });

      return json(
        {
          success: false,
          outcome: "unauthorized",
          errorCode: "Unauthorized",
          message: "Keine Berechtigung.",
        },
        403,
      );
    }

    let body: InviteRequest;
    try {
      body = (await req.json()) as InviteRequest;
    } catch (parseError) {
      console.log("[kgv-invite-user] reject: invalid json", {
        requestId,
        clientRequestId,
        error: safeErrorInfo(parseError),
      });

      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "InvalidJson",
          message: "Ungültiger Request-Body.",
        },
        400,
      );
    }

    const mitgliedId = Number(body?.mitgliedId);
    const role = normalizeRole(body?.role || "user");
    const inviteMethod = String(body?.inviteMethod ?? "otp").trim().toLowerCase();

    console.log("[kgv-invite-user] payload", {
      requestId,
      clientRequestId,
      mitgliedId,
      role,
      inviteMethod,
    });

    if (!Number.isInteger(mitgliedId) || mitgliedId <= 0) {
      console.log("[kgv-invite-user] reject: invalid mitgliedId", {
        requestId,
        mitgliedId,
      });

      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "InvalidMitgliedId",
          message: "Ungültige Mitglied-ID.",
        },
        400,
      );
    }

    if (!allowedRoles.has(role)) {
      console.log("[kgv-invite-user] reject: invalid role", {
        requestId,
        role,
      });

      return json(
        {
          success: false,
          outcome: "invalid_role",
          errorCode: "InvalidRole",
          message: "Ungültige Rolle.",
        },
        400,
      );
    }

    if (inviteMethod && inviteMethod !== "otp") {
      console.log("[kgv-invite-user] non-otp inviteMethod requested; prepare-flow remains otp-bootstrap only", {
        requestId,
        inviteMethod,
        mitgliedId,
      });
    }

    const mitgliedLookupStartedAt = Date.now();
    const { data: mitglied, error: mitgliedError } = await supabaseAdmin
      .from("mitglied")
      .select("id, vorname, name, email, auth_user_id")
      .eq("id", mitgliedId)
      .maybeSingle();

    if (mitgliedError) {
      console.log("[kgv-invite-user] mitglied lookup failed", {
        requestId,
        mitgliedId,
        error: safeErrorInfo(mitgliedError),
        elapsedMs: Date.now() - mitgliedLookupStartedAt,
      });

      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "ServerError",
          message: "Serverfehler.",
        },
        500,
      );
    }

    console.log("[kgv-invite-user] mitglied lookup ok", {
      requestId,
      clientRequestId,
      mitgliedId,
      email: maskEmail(mitglied?.email),
      hasAuthUserId: !!mitglied?.auth_user_id,
      elapsedMs: Date.now() - mitgliedLookupStartedAt,
    });

    if (!mitglied) {
      return json(
        {
          success: false,
          outcome: "not_found",
          errorCode: "NotFound",
          message: "Mitglied nicht gefunden.",
        },
        404,
      );
    }

    const email = String(mitglied.email ?? "").trim();
    if (!email) {
      return json(
        {
          success: false,
          outcome: "missing_email",
          errorCode: "MissingEmail",
          message: "Keine E-Mail-Adresse vorhanden.",
        },
        400,
      );
    }

    const existingAppUserLookupStartedAt = Date.now();
    const { data: existingAppUser, error: existingAppUserError } = await supabaseAdmin
      .from("app_user")
      .select("user_id, mitglied_id, role")
      .eq("mitglied_id", mitgliedId)
      .maybeSingle();

    if (existingAppUserError) {
      console.log("[kgv-invite-user] existing app_user lookup failed", {
        requestId,
        mitgliedId,
        error: safeErrorInfo(existingAppUserError),
        elapsedMs: Date.now() - existingAppUserLookupStartedAt,
      });

      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "ServerError",
          message: "Serverfehler.",
        },
        500,
      );
    }

    console.log("[kgv-invite-user] existing app_user lookup ok", {
      requestId,
      clientRequestId,
      mitgliedId,
      exists: !!existingAppUser,
      existingUserId: existingAppUser?.user_id ?? null,
      elapsedMs: Date.now() - existingAppUserLookupStartedAt,
    });

    const memberAuthUserId = normalizeId(mitglied.auth_user_id);
    const existingAppUserId = normalizeId(existingAppUser?.user_id);

    if (memberAuthUserId && existingAppUserId && memberAuthUserId === existingAppUserId) {
      console.log("[kgv-invite-user] reject: already linked and consistent", {
        requestId,
        mitgliedId,
        userId: memberAuthUserId,
      });

      return json(
        {
          success: false,
          outcome: "already_linked",
          errorCode: "AlreadyLinked",
          message: "Für dieses Mitglied existiert bereits ein verknüpfter Account.",
          mitgliedId,
          email,
          userId: memberAuthUserId,
          authUserId: memberAuthUserId,
          mailSent: false,
        },
        409,
      );
    }

    if (memberAuthUserId && existingAppUserId && memberAuthUserId !== existingAppUserId) {
      console.log("[kgv-invite-user] reject: inconsistent member/app_user linkage", {
        requestId,
        mitgliedId,
        memberAuthUserId,
        existingAppUserId,
      });

      return json(
        {
          success: false,
          outcome: "conflict",
          errorCode: "LinkConflict",
          message: "Für dieses Mitglied liegt ein widersprüchlicher Nutzerzustand vor.",
          mitgliedId,
          email,
          userId: memberAuthUserId,
          authUserId: memberAuthUserId,
        },
        409,
      );
    }

    console.log("[kgv-invite-user] auth bootstrap start", {
      requestId,
      clientRequestId,
      mitgliedId,
      email: maskEmail(email),
      candidateUserIds: [memberAuthUserId || null, existingAppUserId || null],
    });

    const bootstrapStartedAt = Date.now();
    let userId: string;

    try {
      userId = await ensureAuthUserForInvite(
        supabaseAdmin,
        email,
        mitglied,
        callerUserId,
        requestId,
        [memberAuthUserId, existingAppUserId],
      );
    } catch (bootstrapError) {
      console.log("[kgv-invite-user] auth bootstrap failed", {
        requestId,
        mitgliedId,
        email: maskEmail(email),
        classification: classifyProvisioningError(bootstrapError?.message),
        error: safeErrorInfo(bootstrapError),
        elapsedMs: Date.now() - bootstrapStartedAt,
      });

      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "InvitePreparationFailed",
          message: "Das Nutzerkonto konnte nicht vorbereitet werden.",
          mitgliedId,
          email,
        },
        400,
      );
    }

    console.log("[kgv-invite-user] auth bootstrap ok", {
      requestId,
      clientRequestId,
      mitgliedId,
      userId,
      elapsedMs: Date.now() - bootstrapStartedAt,
    });

    const conflictingMitgliedLookupStartedAt = Date.now();
    const { data: conflictingMitglied, error: conflictingMitgliedError } = await supabaseAdmin
      .from("mitglied")
      .select("id, auth_user_id")
      .eq("auth_user_id", userId)
      .neq("id", mitgliedId)
      .maybeSingle();

    if (conflictingMitgliedError) {
      console.log("[kgv-invite-user] conflicting mitglied lookup failed", {
        requestId,
        mitgliedId,
        userId,
        error: safeErrorInfo(conflictingMitgliedError),
        elapsedMs: Date.now() - conflictingMitgliedLookupStartedAt,
      });

      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "ServerError",
          message: "Serverfehler.",
        },
        500,
      );
    }

    if (conflictingMitglied) {
      console.log("[kgv-invite-user] reject: auth user already linked to other mitglied", {
        requestId,
        mitgliedId,
        userId,
        conflictingMitgliedId: conflictingMitglied.id,
      });

      return json(
        {
          success: false,
          outcome: "conflict",
          errorCode: "AuthUserAlreadyLinked",
          message: "Diese E-Mail ist bereits einem anderen Mitglied zugeordnet.",
          mitgliedId,
          email,
          userId,
          authUserId: userId,
        },
        409,
      );
    }

    const conflictingAppUserLookupStartedAt = Date.now();
    const { data: conflictingAppUser, error: conflictingAppUserError } = await supabaseAdmin
      .from("app_user")
      .select("user_id, mitglied_id, role")
      .eq("user_id", userId)
      .maybeSingle();

    if (conflictingAppUserError) {
      console.log("[kgv-invite-user] conflicting app_user lookup failed", {
        requestId,
        mitgliedId,
        userId,
        error: safeErrorInfo(conflictingAppUserError),
        elapsedMs: Date.now() - conflictingAppUserLookupStartedAt,
      });

      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "ServerError",
          message: "Serverfehler.",
        },
        500,
      );
    }

    if (conflictingAppUser && Number(conflictingAppUser.mitglied_id) !== mitgliedId) {
      console.log("[kgv-invite-user] reject: app_user already linked to other mitglied", {
        requestId,
        mitgliedId,
        userId,
        conflictingMitgliedId: conflictingAppUser.mitglied_id,
      });

      return json(
        {
          success: false,
          outcome: "conflict",
          errorCode: "AppUserAlreadyLinked",
          message: "Für diese E-Mail existiert bereits ein Nutzerkonto zu einem anderen Mitglied.",
          mitgliedId,
          email,
          userId,
          authUserId: userId,
        },
        409,
      );
    }

    const linkMitgliedStartedAt = Date.now();
    const { error: linkMitgliedError } = await supabaseAdmin
      .from("mitglied")
      .update({
        auth_user_id: userId,
      })
      .eq("id", mitgliedId);

    if (linkMitgliedError) {
      console.log("[kgv-invite-user] mitglied link failed", {
        requestId,
        mitgliedId,
        userId,
        error: safeErrorInfo(linkMitgliedError),
        elapsedMs: Date.now() - linkMitgliedStartedAt,
      });

      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "LinkFailed",
          message: "Verknüpfung mit Mitglied fehlgeschlagen.",
          mitgliedId,
          email,
          userId,
        },
        500,
      );
    }

    const memberReadback = await verifyMitgliedLinkPersisted(
      supabaseAdmin,
      mitgliedId,
      userId,
      requestId,
    );

    if (!memberReadback.ok) {
      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "LinkNotPersisted",
          message: "Die Verknüpfung zum Mitglied wurde nicht dauerhaft gespeichert.",
          mitgliedId,
          email,
          userId,
          authUserId: userId,
        },
        500,
      );
    }

    console.log("[kgv-invite-user] mitglied link verified", {
      requestId,
      clientRequestId,
      mitgliedId,
      userId,
      role,
      elapsedMs: Date.now() - linkMitgliedStartedAt,
    });

    const writeAppUserStartedAt = Date.now();

    const { error: upsertAppUserError } = await supabaseAdmin
      .from("app_user")
      .upsert(
        {
          user_id: userId,
          mitglied_id: mitgliedId,
          role,
          updated_at: new Date().toISOString(),
        },
        { onConflict: "user_id" },
      );

    if (upsertAppUserError) {
      console.log("[kgv-invite-user] app_user upsert failed", {
        requestId,
        mitgliedId,
        userId,
        error: safeErrorInfo(upsertAppUserError),
        elapsedMs: Date.now() - writeAppUserStartedAt,
      });

      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "AppUserUpsertFailed",
          message: "Nutzerkonto konnte nicht gespeichert werden.",
          mitgliedId,
          email,
          userId,
          authUserId: userId,
        },
        500,
      );
    }

    const appUserReadback = await verifyAppUserPersisted(
      supabaseAdmin,
      mitgliedId,
      userId,
      role,
      requestId,
    );

    if (!appUserReadback.ok) {
      return json(
        {
          success: false,
          outcome: "error",
          errorCode: "AppUserNotPersisted",
          message: "Das Nutzerkonto wurde nicht korrekt gespeichert.",
          mitgliedId,
          email,
          userId,
          authUserId: userId,
        },
        500,
      );
    }

    console.log("[kgv-invite-user] app_user verified", {
      requestId,
      clientRequestId,
      mitgliedId,
      userId,
      elapsedMs: Date.now() - writeAppUserStartedAt,
    });

    let mailSent = false;
    const sendMailStartedAt = Date.now();

    try {
      await sendWelcomeMail(email, mitglied.vorname, requestId);
      mailSent = true;

      console.log("[kgv-invite-user] welcome mail sent", {
        requestId,
        mitgliedId,
        email: maskEmail(email),
        elapsedMs: Date.now() - sendMailStartedAt,
      });
    } catch (mailError) {
      console.log("[kgv-invite-user] welcome mail failed", {
        requestId,
        mitgliedId,
        email: maskEmail(email),
        error: safeErrorInfo(mailError),
        elapsedMs: Date.now() - sendMailStartedAt,
      });

      return json(
        {
          success: true,
          outcome: "prepared_mail_failed",
          errorCode: "WelcomeMailFailed",
          message: "Nutzerkonto wurde vorbereitet, aber die Willkommensmail konnte nicht versendet werden.",
          mitgliedId,
          email,
          userId,
          authUserId: userId,
          mailSent: false,
        },
        200,
      );
    }

    console.log("[kgv-invite-user] completed", {
      requestId,
      clientRequestId,
      mitgliedId,
      userId,
      mailSent,
      totalElapsedMs: Date.now() - requestStartedAt,
    });

    return json({
      success: true,
      outcome: "prepared",
      message: "Nutzerkonto vorbereitet und Willkommensmail versendet.",
      mitgliedId,
      email,
      userId,
      authUserId: userId,
      mailSent: true,
    });
  } catch (err) {
    const message = err instanceof Error ? err.message : "Unknown error";

    console.log("[kgv-invite-user] UnhandledError", {
      requestId,
      message,
      totalElapsedMs: Date.now() - requestStartedAt,
    });

    return json(
      {
        success: false,
        outcome: "error",
        errorCode: "UnhandledError",
        message: "Serverfehler.",
      },
      500,
    );
  }
});