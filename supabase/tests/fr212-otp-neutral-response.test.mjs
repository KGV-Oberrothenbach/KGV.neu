import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import ts from 'typescript';

const source=readFileSync(new URL('../functions/kgv-request-first-login-otp/index.ts',import.meta.url),'utf8');
const compiled=ts.transpileModule(source,{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS}}).outputText;
const email='member@example.invalid';
const userId='11111111-1111-4111-8111-111111111111';
function setup(kind){
 let handler, sends=0;
 const member={id:1,email,auth_user_id:userId};
 const app={user_id:userId,mitglied_id:1,role:'user'};
 const error={message:'synthetic failure'};
 const admin={
  from(table){
   if(kind==='throw')throw Error('synthetic exception');
   if(table==='mitglied')return {select:()=>({ilike:async()=>kind==='member-error'?{error}: {data:kind==='absent'?[]:kind==='duplicate'?[member,member]:kind==='unlinked'?[{...member,auth_user_id:null}]:[member],error:null}})};
   if(table==='app_user')return {select:()=>({eq:async()=>kind==='app-error'?{error}:{data:kind==='app-absent'?[]:kind==='link-mismatch'?[{...app,user_id:'other'}]:[app],error:null}})};
   throw Error('Unexpected table');
  },
  auth:{admin:{getUserById:async()=>kind==='auth-error'?{data:{user:null},error}:{data:{user:{email:kind==='email-mismatch'?'other@example.invalid':email}},error:null}},resetPasswordForEmail:async(address)=>{assert.equal(address,email);sends++;return {error:kind==='send-error'?error:null};}}
 };
 const Deno={serve:fn=>{handler=fn;},env:{get:key=>key==='SUPABASE_URL'?'https://test.invalid':'synthetic-key'}};
 const require=name=>{assert.equal(name,'npm:@supabase/supabase-js@2');return {createClient:()=>admin};};
 new Function('require','exports','Deno','console','fetch',compiled)(require,{},Deno,{log(){},error(){}},()=>{throw Error('network disabled');});
 return {request:handler,sends:()=>sends};
}
const request=(body,method='POST')=>new Request('https://test.invalid',{method,...(method==='POST'?{body:JSON.stringify(body),headers:{'Content-Type':'application/json'}}:{})});
async function invoke(kind,body={email}){
 const h=setup(kind);const res=await h.request(request(body));return {status:res.status,body:await res.json(),sends:h.sends()};
}
const baseline=await invoke('valid');
for(const kind of ['valid','absent','duplicate','unlinked','member-error','app-error','app-absent','link-mismatch','auth-error','email-mismatch','send-error','throw']){
 test('OTP neutral response: '+kind,async()=>{
  const actual=await invoke(kind);
  assert.equal(actual.status,200);
  assert.deepEqual(actual.body,baseline.body);
  assert.equal(actual.body.success,true);
  assert.match(actual.body.message,/Falls ein berechtigtes Konto/);
  assert.equal(actual.sends,['valid','send-error'].includes(kind)?1:0);
 });
}
test('malformed input rejected without sending',async()=>{
 for(const value of ['',42,'a@@b','a b@c','x'.repeat(255)+'@a']){const actual=await invoke('valid',{email:value});assert.equal(actual.status,400);assert.equal(actual.sends,0);}
});
test('non POST not permitted',async()=>{
 const h=setup('valid');const res=await h.request(request({},'GET'));assert.equal(res.status,405);assert.equal(h.sends(),0);
});
