import { createClient } from 'npm:@supabase/supabase-js@2';
import { performAccountDeletion } from './delete-account.mjs';

const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
  'Access-Control-Allow-Methods': 'POST, OPTIONS',
};

type RequestBody = { mitgliedId?: unknown; dryRun?: unknown };

function send(status: number, body: unknown) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...corsHeaders, 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store' },
  });
}

Deno.serve(async (request: Request) => {
  if (request.method === 'OPTIONS') return new Response('ok', { headers: corsHeaders });
  if (request.method !== 'POST') return send(405, { outcome: 'bad_request', message: 'Only POST is allowed.' });

  const url = Deno.env.get('SUPABASE_URL');
  const userKey = Deno.env.get('SUPABASE_PUBLISHABLE_KEY') ?? Deno.env.get('SUPABASE_ANON_KEY');
  const serviceKey = Deno.env.get('SUPABASE_SECRET_KEY') ?? Deno.env.get('SUPABASE_SERVICE_ROLE_KEY');
  if (!url || !userKey || !serviceKey) return send(503, { outcome: 'error', message: 'Serverkonfiguration fehlt.' });

  const authorization = request.headers.get('Authorization') ?? '';
  if (!/^Bearer\s+\S+$/i.test(authorization)) return send(401, { outcome: 'forbidden', message: 'Bearer-Token fehlt.' });

  let body: RequestBody;
  try { body = await request.json() as RequestBody; }
  catch (_error) { return send(400, { outcome: 'bad_request', message: 'Ungültiges JSON.' }); }
  if (body?.dryRun !== undefined && typeof body.dryRun !== 'boolean') {
    return send(400, { outcome: 'bad_request', message: 'dryRun muss ein boolescher Wert sein.' });
  }
  const mitgliedId = body?.mitgliedId;
  if (typeof mitgliedId !== 'number' || !Number.isSafeInteger(mitgliedId) || mitgliedId <= 0) {
    return send(400, { outcome: 'bad_request', message: 'mitgliedId muss eine positive ganze Zahl sein.' });
  }

  const callerClient = createClient(url, userKey, {
    global: { headers: { Authorization: authorization } },
    auth: { persistSession: false, autoRefreshToken: false },
  });
  // The service client and its key never leave the server. Caller identity
  // is independently checked against the Auth server, never taken from JSON.
  const adminClient = createClient(url, serviceKey, {
    auth: { persistSession: false, autoRefreshToken: false },
  });

  let caller;
  try { caller = await callerClient.auth.getUser(); }
  catch (_error) { return send(401, { outcome: 'forbidden', message: 'Anmeldung konnte nicht verifiziert werden.' }); }
  if (caller.error || !caller.data?.user?.id) return send(401, { outcome: 'forbidden', message: 'Anmeldung ungültig.' });

  try {
    const outcome = await performAccountDeletion({
      callerClient, adminClient, callerId: caller.data.user.id,
      memberId: mitgliedId, dryRun: body.dryRun === true,
    });
    const { httpStatus, ...payload } = outcome;
    return send(httpStatus, { ...payload, mitgliedId });
  } catch (_error) {
    // Do not return raw database errors or user/profile details to API callers.
    return send(503, { outcome: 'error', message: 'Benutzerkontovorgang konnte nicht sicher abgeschlossen werden.' });
  }
});
