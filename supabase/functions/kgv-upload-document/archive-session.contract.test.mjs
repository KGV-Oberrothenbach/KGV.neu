import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
const source = readFileSync(new URL('./index.ts', import.meta.url), 'utf8');
const archive = source.slice(source.indexOf('if (action === "archive")'),source.indexOf('if (action === "reserve_historical_contract_number")'));
const start = archive.indexOf('let sessionId: string;');
const end = archive.indexOf('const { data: archivedId, error: archiveError }');
assert.ok(start >= 0 && end > start);
const fragment = archive.slice(start,end).replace('let sessionId: string;', 'let sessionId;');
const extract = new Function('authHeader','auth','errorResponse','requestId',fragment+'\nreturn sessionId;');
const user='11111111-1111-4111-8111-111111111111';
const session='22222222-2222-4222-8222-222222222222';
const jwt = claims => 'Bearer x.'+Buffer.from(JSON.stringify(claims)).toString('base64url')+'.x';
const run = token => extract(token,{userId:user},status=>status,'test');
test('session claim is extracted only after authenticated rights and password checks',()=>{
  assert.ok(archive.indexOf('await requireAdmin(authHeader)') < start);
  assert.ok(archive.indexOf('verifyArchivePassword(password)') < start);
  assert.match(source,/supabaseAdmin\.auth\.getUser\(jwt\)/);
  assert.match(archive,/p_session_id: sessionId/);
  assert.doesNotMatch(archive,/payload\.session_id/);
});
test('verified actor and valid UUID session accepted',()=>{
  assert.equal(run(jwt({sub:user,session_id:session})),session);
});
test('missing or malformed or mismatched claims fail closed',()=>{
  for(const token of ['','Bearer invalid','Bearer x.!.x',jwt({sub:user}),jwt({sub:user,session_id:123}),jwt({sub:user,session_id:'not-uuid'}),jwt({sub:session,session_id:session}),jwt(null)]) assert.equal(run(token),403);
});
test('SQL service binds live session and revokes old access',()=>{
  const sql=readFileSync(new URL('../../migrations/20261127224942_ap07_archive_verified_session.sql',import.meta.url),'utf8');
  assert.match(sql,/fr212_actor_session_valid\(\)/);
  assert.match(sql,/p_session_id IS NULL/);
  assert.match(sql,/REVOKE ALL ON FUNCTION public\.fr208_archive_dokument_service\(bigint,uuid,text,uuid\) FROM PUBLIC,anon,authenticated/);
  assert.match(sql,/REVOKE EXECUTE ON FUNCTION public\.fr208_archive_dokument_service\(bigint,uuid,text\) FROM service_role/);
});
