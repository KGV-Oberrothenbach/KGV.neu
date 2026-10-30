import assert from "node:assert/strict";
import {readFile} from "node:fs/promises";
import {resolve} from "node:path";
import test from "node:test";
import {PGlite} from "@electric-sql/pglite";
const root=resolve(import.meta.dirname,"../..");const read=p=>readFile(resolve(root,p),"utf8");
async function actualFunction(file,name){const source=await read("supabase/migrations/"+file);const start=source.indexOf("create or replace function public."+name+"(");assert.ok(start>=0,name);return source.slice(start,source.indexOf("$$;",start)+3);}
async function asUser(db,sql,args=[]){await db.exec("savepoint user_call;set local role authenticated;");try{const result=await db.query(sql,args);await db.exec("reset role;release savepoint user_call;");return result.rows;}catch(e){await db.exec("rollback to savepoint user_call;release savepoint user_call;");throw e;}}
async function denied(db,sql,args=[]){await assert.rejects(asUser(db,sql,args),error=>error.code==="42501");}
const access=async(db,id,registered=false)=>(await asUser(db,"select public.can_access_historical_contract_document($1,$2) as allowed",[id,registered]))[0].allowed;
const seed=`insert into public.mitglied(id,mitglied_seit,aktiv,is_demo,auth_user_id,mitgliedsnummer,vorname,name) values
(1,'1990-01-01',true,false,'11111111-1111-1111-1111-111111111111',1041,'Actor','A'),(2,'1990-01-01',true,false,null,1042,'Party','B'),(3,'1990-01-01',true,true,null,900003,'Demo','C');
insert into public.app_user values('11111111-1111-1111-1111-111111111111',1,'user',16777216,0,false,false);
insert into public.parzelle(id,garten_nr,"Anlage",is_demo) values(12,'12','Anlage',false),(14,'14','Demo',true);
insert into public.pachtverhaeltnis(id,parzelle_id,urspruenglicher_pachtbeginn) values(12,12,'1998-01-01'),(14,14,'1998-01-01');
insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab) values(12,2,'hauptpaechter','1998-01-01'),(14,3,'hauptpaechter','1998-01-01');
insert into public.dokument(id,mitglied_id,titel,drive_file_id,mime_type,source_kommunikationsvorgang_id) values(10,2,'Ordinary','drive10','application/pdf',null),(20,2,'Prepared','drive20','application/pdf',44);
select set_config('test.jwt_role','service_role',true);
insert into public.dokument(id,mitglied_id,titel,drive_file_id,mime_type,historical_contract_type,created_by) values
(30,2,'Draft','drive30','application/pdf','historischer_pv_folgevertrag','11111111-1111-1111-1111-111111111111'),
(31,2,'Other draft','drive31','application/pdf','historischer_pv_folgevertrag','22222222-2222-2222-2222-222222222222'),
(40,2,'Registered','drive40','application/pdf','historischer_pv_folgevertrag','22222222-2222-2222-2222-222222222222');
insert into public.dokument(id,mitglied_id,titel,drive_file_id,mime_type) values(41,2,'Legacy historical','drive41','application/pdf'),(50,3,'Demo historical','drive50','application/pdf');
update public.dokument set dokumenttyp_id=(select dt.id from public.dokumenttyp dt where dt.code='historischer_pv_folgevertrag'),dokumentdatum='2010-01-01' where id in(40,41,50);
insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp,wirksam_ab) values(12,40,'import','2010-01-01'),(12,41,'import','2010-01-01'),(14,50,'import','2010-01-01');
select set_config('test.jwt_role','authenticated',true);`;
await test("Bit 24 historical document scope executes with the real central permission evaluator",async t=>{
 const db=new PGlite();try{
  await db.exec(await read("supabase/tests/fixture.sql"));
  await db.exec(`drop function public.current_app_user_context();
   alter table public.mitglied add auth_user_id uuid,add mitgliedsnummer bigint,add vorname text,add name text;
   alter table public.parzelle add garten_nr text,add "Anlage" text,add is_demo boolean default false;
   alter table public.dokument add created_by uuid,add drive_file_id text;
   create table public.app_user(user_id uuid primary key,mitglied_id bigint,role text,permission_grants bigint,permission_revocations bigint,is_protected boolean,is_demo_account boolean);
   create or replace function auth.role() returns text language sql stable as $$ select coalesce(nullif(current_setting('test.jwt_role',true),''),'authenticated') $$;`);
  for(const name of ["permission_role_base","known_permission_mask"])await db.exec(await actualFunction("20261011130000_g13_permission_contract.sql",name));
  for(const name of ["current_app_user_context","current_effective_permissions"])await db.exec(await actualFunction("20261011160000_g13_valid_app_user_access.sql",name));
  await db.exec(await actualFunction("20261011130000_g13_permission_contract.sql","has_effective_permission"));
  for(const name of ["20261019125000_pachtverhaeltnis_contract_versions.sql","20261030110000_g6a_historical_contract_foundation.sql","20261030120000_g6b2_historical_contract_state_transitions.sql","20261030130000_g7_historical_contract_preview.sql","20261030140000_g11b1_historical_contract_validation.sql","20261030150000_g11b2_historical_document_access.sql","20261030160000_g11b3_historical_contract_idempotency.sql"])await db.exec(await read("supabase/migrations/"+name));
  await db.exec(`alter table public.dokument enable row level security;grant select on public.dokument to authenticated;
   create policy ordinary_owner_read on public.dokument for select to authenticated using(mitglied_id=(select c.mitglied_id from public.current_app_user_context() c));`);
  const scenario=async(name,fn)=>t.test(name,async()=>{await db.exec("begin;");try{await db.exec(seed);await fn(db);}finally{await db.exec("rollback;");}});
  await scenario("user + explicit grant can read only their pending historical source and registered historical documents",async db=>{
   assert.equal(await access(db,30),true);assert.equal(await access(db,30,true),false);assert.equal(await access(db,31),false);
   assert.equal(await access(db,40,true),true);assert.equal(await access(db,41,true),true);
   assert.deepEqual((await asUser(db,"select id::int from public.dokument order by id")).map(r=>r.id),[30,40,41]);
  });
  await scenario("Bit 24 alone supplies minimal member/lease lookups and in-scope preview",async db=>{
   const members=await asUser(db,"select * from public.get_historical_contract_members()");assert.equal(members.length,2);
   const documents=await asUser(db,"select * from public.get_historical_contract_documents(2)");assert.equal(documents.length,2);
   const leases=await asUser(db,"select * from public.get_historical_contract_lease_relationships(2)");assert.equal(leases.length,1);
   assert.equal((await asUser(db,"select * from public.get_historische_pachtvertragsvorschau(array[12]::bigint[],date '2015-01-01')")).length,2);
   await denied(db,"select * from public.get_historical_contract_lease_relationships(3)");
   await denied(db,"select * from public.get_historical_contract_documents(3)");
   await denied(db,"select * from public.get_historische_pachtvertragsvorschau(array[14]::bigint[],date '2015-01-01')");
   await denied(db,"select * from public.mitglied");
  });
  for(const role of ["user","admin","vorstand"])await scenario(role+" without grant cannot use historical operations",async db=>{
   await db.query("update public.app_user set role=$1,permission_grants=0",[role]);assert.equal(await access(db,40,true),false);
   await denied(db,"select * from public.get_historical_contract_documents(2)");
   await denied(db,"select * from public.get_historical_contract_members()");await denied(db,"select * from public.reserve_historische_vertragsnummer(40)");
  });
  await scenario("explicit revocation wins even over an explicit grant",async db=>{
   await db.exec("update public.app_user set permission_revocations=16777216");assert.equal(await access(db,30),false);assert.equal(await access(db,40,true),false);
   await denied(db,"select * from public.get_historical_contract_members()");await denied(db,"select * from public.reserve_historische_vertragsnummer(40)");
  });
  await scenario("ordinary, prepared, cross-demo, archived and unknown documents remain inaccessible",async db=>{
   await denied(db,"select public.reserve_fachnummer('historischer_kombivertrag','historisches_dokument',40,true)");
   for(const id of [10,20,50,999]){assert.equal(await access(db,id),false);await denied(db,"select * from public.reserve_historische_vertragsnummer($1)",[id]);}
   await db.exec("update public.dokument set archiviert_at=now() where id=40");assert.equal(await access(db,40),false);
  });
  await scenario("registered contracts with inconsistent parcel/demo scope are not exposed",async db=>{
   await db.exec("update public.parzelle set is_demo=true where id=12");assert.equal(await access(db,40,true),false);
   await denied(db,"select * from public.get_historische_pachtvertragsvorschau(array[12]::bigint[],date '2015-01-01')");
  });
  await scenario("clients cannot forge or retarget historical upload provenance",async db=>{
   for(const command of ["update public.dokument set historical_contract_type='historischer_pv_folgevertrag',created_by=auth.uid() where id=10","update public.dokument set created_by=auth.uid() where id=31","update public.dokument set mitglied_id=1 where id=30"]){
    await db.exec("savepoint provenance");await assert.rejects(db.exec(command),e=>e.code==="42501");await db.exec("rollback to savepoint provenance;release savepoint provenance");
   }
   assert.equal(await access(db,10),false);assert.equal(await access(db,31),false);
  });
  await scenario("a pending upload cannot reserve or confirm a stamp before registration",async db=>{
   await denied(db,"select * from public.reserve_historische_vertragsnummer(30)");
   assert.equal((await db.query("select has_function_privilege('authenticated','public.mark_historische_vertragsnummer_pdf_gestempelt(bigint,integer)','execute') as allowed")).rows[0].allowed,false);
  });
  await scenario("registering an arbitrary ordinary document cannot launder foreign-document access",async db=>{
   await denied(db,"select * from public.register_historischen_pachtvertrag(10,'historischer_pv_folgevertrag',date '2015-01-01',true,array[12]::bigint[],'{}',null,null,array[2]::bigint[],'[]','[]')");
   assert.equal(await access(db,10),false);
   assert.equal((await db.query("select count(*)::int as n from public.pachtvertragsfassung where dokument_id=10")).rows[0].n,0);
  });
  await scenario("normal user completes real registration and number reservation without manager role",async db=>{
   const result=await asUser(db,"select * from public.register_historischen_pachtvertrag(30,'historischer_pv_folgevertrag',date '2015-01-01',true,array[12]::bigint[],'{}',null,null,array[2]::bigint[],'[]','[]')");assert.equal(result.length,1);assert.equal(await access(db,30,true),true);
   assert.equal((await asUser(db,"select * from public.reserve_historische_vertragsnummer(30)")).length,1);
  });
  await scenario("operation replay uses the real Bit 24 evaluator and respects later revocation",async db=>{
   const id="aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
   await db.exec("select set_config('test.jwt_role','service_role',true)");
   await db.query("select public.begin_historical_contract_upload($1,'11111111-1111-1111-1111-111111111111',2,'historischer_pv_folgevertrag','{}','drive-op','op.pdf')",[id]);
   await db.query("update public.historical_contract_operation set document_id=30 where operation_id=$1",[id]);
   await db.exec("select set_config('test.jwt_role','authenticated',true)");
   const sql="select * from public.register_historischen_pachtvertrag($1::uuid,30::bigint,'historischer_pv_folgevertrag',date '2015-01-01',true,array[12]::bigint[],'{}',null,null,array[2]::bigint[],'[]','[]')";
   const first=await asUser(db,sql,[id]);assert.equal(first.length,1);assert.deepEqual(await asUser(db,sql,[id]),first);
   await db.exec("update public.app_user set permission_revocations=16777216");await denied(db,sql,[id]);
  });
 }finally{await db.close();}
});
