import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
const src=p=>readFileSync(new URL('../../'+p,import.meta.url),'utf8');
test('OTP: all well-formed account lookup failures have the same public response',()=>{
 const code=src('supabase/functions/kgv-request-first-login-otp/index.ts');
 const failure=code.slice(code.indexOf('function buildFailureResponse('),code.indexOf('function buildSuccessResponse('));
 assert.match(failure,/return buildSuccessResponse\(\)/);
 assert.doesNotMatch(failure,/success:\s*false/);
 assert.match(code,/return buildFailureResponse\("OTP_FIRST_LOGIN_EDGE_UNEXPECTED_FAIL"\)/);
 assert.match(code,/resetPasswordForEmail\(normalizedEmail\)/);
 assert.match(code,/link_mismatch/);
 assert.match(code,/auth_email_mismatch/);
});
test('MAUI header shows session identity, not selected member',()=>{
 const code=src('KGV.Maui/SessionIdentityHeader.cs');
 assert.match(code,/GetMitgliedByAuthUserIdAsync\(id\)/);
 assert.match(code,/member\.Id != _state\.CurrentMitgliedId/);
 assert.doesNotMatch(code,/SelectedMember|AppSettings\.LastEmail|SupabasePublishableKey/);
 assert.match(code,/ymxhszxfutjnljiuciyj\.supabase\.co" => "Demo DB"/);
 assert.match(code,/itjcabiibuodkxayhvjq\.supabase\.co" => "KGV Oberrothenbach"/);
 assert.match(code,/shell\.Navigated/);
 assert.match(code,/layout\.Children\.Add\(club\);\s*layout\.Children\.Add\(user\);/);
 assert.match(code,/authId == id/);
 assert.doesNotMatch(code,/Mitglied-ID|uri\.Host\s*\n/);
 assert.match(src('KGV.Maui/App.xaml.cs'),/SessionIdentityHeader\.Attach\(shell,/);
 assert.match(src('KGV.Maui/AdminShell.cs'),/menu\.Children\.Add\(SessionIdentityHeader\.CreateFlyoutHeader\(this\)\)/);
});
test('session draft restricts session to current user and own read path',()=>{
 const sql=src('tools/FR212/FR212_SESSION_ERGAENZUNG.sql');
 assert.match(sql,/s\.user_id=auth\.uid\(\)/);
 assert.match(sql,/s\.not_after IS NULL OR s\.not_after>now\(\)/);
 assert.match(sql,/u\.banned_until IS NULL OR u\.banned_until<=now\(\)/);
 assert.match(sql,/ap02_can_read_mitglied\(bigint\)/);
 assert.match(sql,/ALTER POLICY app_user_select_by_effective_roles/);
 assert.match(sql,/REVOKE ALL ON FUNCTION public\.fr212_actor_session_valid\(\) FROM PUBLIC,anon,authenticated/);
});
test('MAUI recovery uses unified edge path and requires explicit success',()=>{
 const code=src('KGV.Infrastructure/Authentication/AuthService.cs');
 const flow=code.slice(code.indexOf('private async Task<bool> RequestRecoveryOtpAsync('),code.indexOf('private async Task<bool> RequestRecoveryOtpAsync(')+1800);
 assert.match(flow,/InvokeFirstLoginOtpFunctionAsync\(email\)/);
 assert.doesNotMatch(flow,/client\.Auth\.ResetPasswordForEmail\(email\)/);
 assert.match(code,/httpSuccess && TryGetJsonBool\(root, "success"\) == true/);
});
