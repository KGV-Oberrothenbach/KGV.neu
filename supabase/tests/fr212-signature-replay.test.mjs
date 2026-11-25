import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import ts from 'typescript';
const source=readFileSync(new URL('../functions/upload-prepared-contract-signature/index.ts',import.meta.url),'utf8');
const compiled=ts.transpileModule(source,{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS}}).outputText;
const png=new Uint8Array([137,80,78,71,13,10,26,10,42]);
const hash=Buffer.from(await crypto.subtle.digest('SHA-256',png)).toString('hex');
const payload={kommunikationsvorgang_id:77,signatur_revision:2,signatur_schluessel:'p1',png_base64:Buffer.from(png).toString('base64')};
function harness(opts={}) {
  let handler; let uploads=0;
  const contract={id:77,status:opts.open?'zur_signatur':'letzte_pruefung',revision:2,signatur_revision:2};
  const position={id:4,status:opts.open?'offen':'unterschrieben',storage_path:'77/2/p1/synthetic.png',signatur_sha256:opts.different?'different':hash,unterzeichner_typ:opts.board?'vereinsunterzeichner':'mitglied',mitglied_id:42};
  const caller={auth:{getUser:async()=>({data:{user:{id:'synthetic'}},error:null})},rpc:async(name)=>{
    if(name==='current_app_user_context')return {data:[{mitglied_id:opts.member??12}],error:null};
    if(name==='ap02_has_effective_right')return {data:opts.admin===true,error:opts.permissionError?{message:'denied'}:null};
    throw Error('unexpected RPC '+name);
  }};
  const admin={from(table){const result=table==='kommunikationsvorgang'?{data:contract,error:null}:table==='prepared_contract_signatur'?{data:position,error:null}:table==='prepared_contract_signatur_snapshot_person'?{data:opts.snapshot?{id:5}:null,error:opts.snapshotError?{message:'query failed'}:null}:null;
    if(!result)throw Error('unexpected table '+table);
    return {select(){return this;},eq(){return this;},async maybeSingle(){return result;}};
  },storage:{from(){return {async upload(){uploads++;return {error:null};}};}}};
  const Deno={env:{get:key=>({SUPABASE_URL:'https://synthetic.invalid',SUPABASE_ANON_KEY:'synthetic-anon',SUPABASE_SERVICE_ROLE_KEY:'synthetic-service'})[key]},serve(fn){handler=fn;}};
  const require=name=>{assert.equal(name,'npm:@supabase/supabase-js@2');return {createClient:(_url,key)=>key==='synthetic-anon'?caller:admin};};
  new Function('require','exports','Deno','console',compiled)(require,{},Deno,{error(){}});
  return {run:()=>handler(new Request('https://synthetic.invalid',{method:'POST',headers:{Authorization:'Bearer synthetic-jwt','Content-Type':'application/json'},body:JSON.stringify(payload)})),uploads:()=>uploads};
}
for(const [name,opts,status,writes] of [
 ['foreign signer, identical replay',{},403,0],
 ['foreign signer, different replay',{different:true},403,0],
 ['former administrator with revoked right, identical replay',{admin:false},403,0],
 ['authorized member replay',{member:42},200,0],
 ['authorized administrative replay',{admin:true},200,0],
 ['board signer outside snapshot',{board:true},403,0],
 ['board snapshot query error',{board:true,snapshot:true,snapshotError:true},403,0],
 ['valid board signer replay',{board:true,snapshot:true},200,0],
 ['different bytes for actual signer',{member:42,different:true},409,0],
 ['new upload for actual signer',{member:42,open:true},200,1],
 ['new upload for foreign signer',{open:true},403,0],
 ['permission RPC error',{admin:true,permissionError:true},403,0],
])test(name,async()=>{const h=harness(opts);const response=await h.run();const body=await response.json();assert.equal(response.status,status);assert.equal(h.uploads(),writes);assert.equal(body.success,status===200);if(status!==200)assert.equal(body.storage_path,undefined);if(status===200&&writes===0)assert.equal(body.already_stored,true);});
