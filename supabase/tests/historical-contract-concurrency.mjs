// Always creates and destroys its own loopback-only PostgreSQL cluster. No URL
// or credentials for an existing database are accepted.
import assert from "node:assert/strict";
import test from "node:test";
import {mkdtemp,rm} from "node:fs/promises";
import {tmpdir} from "node:os";
import {resolve,join} from "node:path";
import {pathToFileURL} from "node:url";
import {execFile} from "node:child_process";
import {promisify} from "node:util";
import {createServer} from "node:net";
import {setTimeout as delay} from "node:timers/promises";
import {setup,begin,finish,register,snapshot,operation,other} from "./historical-idempotency-fixture.mjs";
const tools=process.env.KGV_TEST_PG_TOOLS;
if(!tools)throw new Error("Set KGV_TEST_PG_TOOLS to a scratch npm install containing @embedded-postgres/windows-x64 and pg.");
const {pg_ctl,initdb}=await import(pathToFileURL(resolve(tools,"node_modules/@embedded-postgres/windows-x64/dist/index.js")));
const {default:pg}=await import(pathToFileURL(resolve(tools,"node_modules/pg/lib/index.js")));
const exec=promisify(execFile);
const cluster=await mkdtemp(join(tmpdir(),"kgv-g11b3-pg-"));
const data=join(cluster,"data");
const server=createServer();await new Promise(resolve=>server.listen(0,"127.0.0.1",resolve));const port=server.address().port;await new Promise(resolve=>server.close(resolve));
const clients=[];let started=false;
const command=(file,args)=>exec(file,args,{windowsHide:true,timeout:60000,maxBuffer:1024*1024});
async function connect(){const client=new pg.Client({host:"127.0.0.1",port,user:"postgres",database:"postgres"});await client.connect();clients.push(client);return {query:(sql,args)=>client.query(sql,args),exec:sql=>client.query(sql),client};}
async function waitForLock(observer,pid){
 for(let i=0;i<100;i++){const row=(await observer.query("select wait_event_type from pg_stat_activity where pid=$1",[pid])).rows[0];if(row?.wait_event_type==="Lock")return;await delay(20);}
 assert.fail("Second PostgreSQL connection did not wait on the operation row lock");
}
try{
 await command(initdb,["-D",data,"-U","postgres","-A","trust","-E","UTF8","--no-locale"]);
 await command(pg_ctl,["-D",data,"-l",join(cluster,"postgres.log"),"-o",`-h 127.0.0.1 -p ${port} -F`,"-w","start"]);started=true;
 const a=await connect(),b=await connect();await setup(a);
 const pid=(await b.query("select pg_backend_pid() pid")).rows[0].pid;
 await test("native PostgreSQL: real parallel connections converge across upload and registration",async t=>{
  await t.test("parallel begin uploads wait on the unique operation row and reuse its Drive ID",async()=>{
   await a.exec("begin");const first=await begin(a);
   const pending=begin(b);await waitForLock(a,pid);await a.exec("commit");const second=await pending;
   assert.equal(first.drive_file_id,second.drive_file_id);assert.equal(first.file_name,second.file_name);
  });
  let doc;
  await t.test("parallel metadata completion inserts exactly one document",async()=>{
   await a.exec("select set_config('test.jwt_role','service_role',false);begin");const first=await finish(a);
   await b.exec("select set_config('test.jwt_role','service_role',false)");const pending=finish(b);await waitForLock(a,pid);await a.exec("commit");const second=await pending;
   doc=first.id;assert.equal(doc,second.id);assert.equal((await snapshot(a)).documents,1);
  });
  await t.test("parallel first registrations wait and return the same number and version",async()=>{
   await a.exec("begin");const first=await register(a,doc);const pending=register(b,doc);
   await waitForLock(a,pid);await a.exec("commit");const second=await pending;
   assert.deepEqual(first,second);const counts=await snapshot(a);assert.equal(counts.documents,1);assert.equal(counts.versions,1);assert.equal(counts.numbers,1);assert.equal(counts.audits,1);
   const repeat=await Promise.all([register(a,doc),register(b,doc)]);assert.deepEqual(repeat[0],first);assert.deepEqual(repeat[1],first);assert.deepEqual(await snapshot(a),counts);
  });
  await t.test("parallel conflicting request is rejected without mutation",async()=>{
   const before=await snapshot(a);await a.exec("begin");await register(a,doc);
   const pending=register(b,doc,{signed:false}).then(()=>null,error=>error);await waitForLock(a,pid);await a.exec("commit");assert.equal((await pending).code,"22023");assert.deepEqual(await snapshot(a),before);
  });
  await t.test("parallel stamp claims grant only one writer",async()=>{
   for(const db of [a,b])await db.exec("select set_config('test.jwt_role','service_role',false)");
   const claim=(db,token)=>db.query("select public.claim_historical_contract_stamp($1,$2) ok",[doc,token]);
   const results=await Promise.all([claim(a,operation),claim(b,other)]);assert.equal(results.filter(r=>r.rows[0].ok).length,1);
  });
 });
}finally{
 for(const client of clients)await client.end().catch(()=>{});
 if(started)await command(pg_ctl,["-D",data,"-m","immediate","-w","stop"]);
 await rm(cluster,{recursive:true,force:true});
}
