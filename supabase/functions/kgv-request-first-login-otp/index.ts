import { createClient } from "npm:@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

const GENERIC_FAILURE_MESSAGE = "OTP-Anforderung fehlgeschlagen. Bitte prüfe die E-Mail-Adresse oder kontaktiere den Vorstand.";
const GENERIC_SUCCESS_MESSAGE = "Einladungs-/Erstlogin-Code wurde versendet. Bitte OTP eingeben.";

type MemberRow = {
  id: number;
  email: string | null;
  vorname: string | null;
  name: string | null;
  auth_user_id: string | null;
};

type AppUserRow = {
  user_id: string;
  mitglied_id: number | null;
  role: string | null;
};

function logStep(step: string, details?: Record<string, unknown>) {
  if (details) {
    console.log(`[kgv-request-first-login-otp] ${step}`, details);
    return;
  }

  console.log(`[kgv-request-first-login-otp] ${step}`);
}

function logError(step: string, error: unknown, details?: Record<string, unknown>) {
  const message = error instanceof Error ? error.message : String(error);
  console.error(`[kgv-request-first-login-otp] ${step}`, {
    ...(details ?? {}),
    message,
  });
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

function maskEmail(email: string): string {
  const trimmed = email.trim();
  const parts = trimmed.split("@");
  if (parts.length !== 2) {
    return "***";
  }

  const [localPart, domain] = parts;
  const maskedLocalPart = localPart.length <= 2
    ? "**"
    : `${localPart[0]}***${localPart[localPart.length - 1]}`;

  return `${maskedLocalPart}@${domain}`;
}

function normalizeEmail(value: unknown): string {
  return String(value ?? "").trim().toLowerCase();
}

function containsOperationalMarker(value: string | null | undefined): boolean {
  if (!value) {
    return false;
  }

  const normalized = value.trim().toLowerCase();
  return normalized.includes("demo")
    || normalized.includes("test")
    || normalized.includes("play store")
    || normalized.includes("playstore")
    || normalized.includes("example.com")
    || normalized.includes("example.org")
    || normalized.includes("example.net");
}

function isOperationalMember(member: MemberRow | null | undefined): member is MemberRow {
  if (!member) {
    return false;
  }

  return !containsOperationalMarker(member.vorname)
    && !containsOperationalMarker(member.name)
    && !containsOperationalMarker(member.email);
}

function buildFailureResponse(diagnosticCode: string) {
  return json(200, {
    success: false,
    diagnosticCode,
    message: GENERIC_FAILURE_MESSAGE,
  });
}

function buildSuccessResponse() {
  return json(200, {
    success: true,
    diagnosticCode: "OTP_FIRST_LOGIN_EDGE_ACCEPTED",
    message: GENERIC_SUCCESS_MESSAGE,
  });
}

function createServiceClient() {
  const supabaseUrl = Deno.env.get("SUPABASE_URL");
  const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");

  if (!supabaseUrl || !serviceRoleKey) {
    throw new Error("SUPABASE_URL oder SUPABASE_SERVICE_ROLE_KEY fehlt.");
  }

  return createClient(supabaseUrl, serviceRoleKey, {
    auth: {
      autoRefreshToken: false,
      persistSession: false,
    },
  });
}

Deno.serve(async (req) => {
  logStep("function start", { method: req.method });

  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  if (req.method !== "POST") {
    return json(405, {
      success: false,
      diagnosticCode: "OTP_FIRST_LOGIN_EDGE_METHOD_NOT_ALLOWED",
      message: "Method not allowed",
    });
  }

  try {
    const body = await req.json().catch(() => null);
    const normalizedEmail = normalizeEmail(body?.email);
    const maskedEmail = maskEmail(normalizedEmail || "<leer>");

    if (!normalizedEmail || !normalizedEmail.includes("@")) {
      logStep("request validate failed", { reason: "email_invalid", maskedEmail });
      return json(400, {
        success: false,
        diagnosticCode: "OTP_FIRST_LOGIN_EDGE_BAD_REQUEST",
        message: GENERIC_FAILURE_MESSAGE,
      });
    }

    logStep("request normalized", { maskedEmail });
    const supabaseAdmin = createServiceClient();

    const { data: memberRows, error: memberError } = await supabaseAdmin
      .from("mitglied")
      .select("id,email,vorname,name,auth_user_id")
      .ilike("email", normalizedEmail);

    if (memberError) {
      logError("member lookup failed", memberError, { maskedEmail });
      return buildFailureResponse("OTP_FIRST_LOGIN_EDGE_MEMBER_LOOKUP_FAIL");
    }

    const matchingMembers = (memberRows ?? [])
      .filter(isOperationalMember)
      .filter((member) => normalizeEmail(member.email) === normalizedEmail);

    if (matchingMembers.length !== 1) {
      logStep("precheck rejected", {
        maskedEmail,
        reason: "member_match_count",
        matchCount: matchingMembers.length,
      });
      return buildFailureResponse("OTP_FIRST_LOGIN_EDGE_REJECTED");
    }

    const member = matchingMembers[0];
    if (!member.auth_user_id) {
      logStep("precheck rejected", {
        maskedEmail,
        reason: "member_auth_user_missing",
        mitgliedId: member.id,
      });
      return buildFailureResponse("OTP_FIRST_LOGIN_EDGE_REJECTED");
    }

    const { data: appUserRows, error: appUserError } = await supabaseAdmin
      .from("app_user")
      .select("user_id,mitglied_id,role")
      .eq("mitglied_id", member.id);

    if (appUserError) {
      logError("app_user lookup failed", appUserError, {
        maskedEmail,
        mitgliedId: member.id,
      });
      return buildFailureResponse("OTP_FIRST_LOGIN_EDGE_APPUSER_LOOKUP_FAIL");
    }

    if ((appUserRows ?? []).length !== 1) {
      logStep("precheck rejected", {
        maskedEmail,
        reason: "app_user_match_count",
        mitgliedId: member.id,
        matchCount: appUserRows?.length ?? 0,
      });
      return buildFailureResponse("OTP_FIRST_LOGIN_EDGE_REJECTED");
    }

    const appUser = appUserRows![0] as AppUserRow;
    if ((appUser.user_id ?? "").trim().toLowerCase() !== member.auth_user_id.trim().toLowerCase()) {
      logStep("precheck rejected", {
        maskedEmail,
        reason: "link_mismatch",
        mitgliedId: member.id,
      });
      return buildFailureResponse("OTP_FIRST_LOGIN_EDGE_REJECTED");
    }

    const { data: authUserData, error: authUserError } = await supabaseAdmin.auth.admin.getUserById(member.auth_user_id);
    if (authUserError || !authUserData.user) {
      logError("auth user lookup failed", authUserError ?? "auth user missing", {
        maskedEmail,
        mitgliedId: member.id,
      });
      return buildFailureResponse("OTP_FIRST_LOGIN_EDGE_AUTH_LOOKUP_FAIL");
    }

    if (normalizeEmail(authUserData.user.email) !== normalizedEmail) {
      logStep("precheck rejected", {
        maskedEmail,
        reason: "auth_email_mismatch",
        mitgliedId: member.id,
      });
      return buildFailureResponse("OTP_FIRST_LOGIN_EDGE_REJECTED");
    }

    logStep("precheck accepted", {
      maskedEmail,
      mitgliedId: member.id,
      role: appUser.role ?? "<leer>",
    });

    const { error: otpError } = await supabaseAdmin.auth.resetPasswordForEmail(normalizedEmail);
    if (otpError) {
      logError("otp send failed", otpError, {
        maskedEmail,
        mitgliedId: member.id,
      });
      return buildFailureResponse("OTP_FIRST_LOGIN_EDGE_SEND_FAIL");
    }

    logStep("otp send accepted", { maskedEmail, mitgliedId: member.id });
    return buildSuccessResponse();
  } catch (error) {
    logError("unexpected exception", error);
    return json(500, {
      success: false,
      diagnosticCode: "OTP_FIRST_LOGIN_EDGE_UNEXPECTED_FAIL",
      message: GENERIC_FAILURE_MESSAGE,
    });
  }
});
