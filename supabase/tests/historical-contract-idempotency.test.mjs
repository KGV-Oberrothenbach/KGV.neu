import assert from "node:assert/strict";
import test from "node:test";
import {setup,begin,finish,register,snapshot,operation,other,upload} from "./historical-idempotency-fixture.mjs";
await test("G11b3 real SQL: operation upload, registration and replay",async t=>{
 const db=await setup();try{
  await t.test("upload retries and candidate-ID races converge on one document",async()=>{
   const a=await begin(db);const b=await begin(db);assert.equal(a.drive_file_id,b.drive_file_id);
   const [x,y]=await Promise.all([finish(db),finish(db)]);assert.equal(x.id,y.id);assert.equal((await snapshot(db)).documents,1);
  });
  const doc=(await finish(db)).id;
  await t.test("first registration followed by unknown-result replay returns identical identifiers",async()=>{
   const first=await register(db,doc);const before=await snapshot(db);assert.equal(before.audits,1);assert.equal(before.numbers,1);
   assert.deepEqual(await register(db,doc),first);assert.deepEqual(await snapshot(db),before);
  });
  await t.test("simultaneously submitted database calls return the same persisted result",async()=>{
   const before=await snapshot(db);const [a,b]=await Promise.all([register(db,doc),register(db,doc)]);assert.deepEqual(a,b);assert.deepEqual(await snapshot(db),before);
  });
  await t.test("same key with different registration data is rejected before side effects",async()=>{
   const before=await snapshot(db);await assert.rejects(register(db,doc,{signed:false}),e=>e.code==="22023");await assert.rejects(register(db,doc,{parties:[1,2]}),e=>e.code==="22023");assert.deepEqual(await snapshot(db),before);
  });
  await t.test("same upload key with altered bytes is rejected",async()=>{
   await assert.rejects(begin(db,operation,{...upload,sha256:"different"}),e=>e.code==="22023");assert.equal((await snapshot(db)).documents,1);
  });
  await t.test("database constraint protects operation UUID",async()=>{
   await assert.rejects(db.exec("insert into public.historical_contract_operation select * from public.historical_contract_operation"),e=>e.code==="23505");
   assert.equal((await db.query("select count(*)::int n from public.historical_contract_operation")).rows[0].n,1);
  });
  await t.test("permission is rechecked even on successful replay",async()=>{
   await db.exec("select set_config('test.historical_permission','false',false)");await assert.rejects(register(db,doc),e=>e.code==="42501");await db.exec("select set_config('test.historical_permission','true',false)");
  });
  await t.test("untrusted users cannot read the operation table or forge upload/stamp completion",async()=>{
   await db.exec("set role authenticated");try{
    await assert.rejects(db.query("select * from public.historical_contract_operation"),e=>e.code==="42501");
    await assert.rejects(db.query("select public.finish_historical_contract_upload($1,'x')",[operation]),e=>e.code==="42501");
    await assert.rejects(db.query("select public.claim_historical_contract_stamp($1,$2)",[doc,operation]),e=>e.code==="42501");
   }finally{await db.exec("reset role");}
  });
  await t.test("an explicitly new operation with similar business values remains new",async()=>{
   await begin(db,other);const otherDoc=(await finish(db,other)).id;const rows=await register(db,otherDoc,{id:other,date:"2011-01-01"});assert.notEqual(otherDoc,doc);assert.notEqual(rows[0].historische_vertragsnummer,(await register(db,doc))[0].historische_vertragsnummer);assert.equal((await snapshot(db)).documents,2);
  });
  await t.test("unknown upload result can be resolved without a new document",async()=>{
   const state=(await db.query("select public.get_historical_contract_operation($1) state",[operation])).rows[0].state;assert.equal(state.documentId,doc);assert.equal(state.registered,true);
  });
  await t.test("stamp leases serialize writers, reject foreign release and recover after expiry",async()=>{
   await db.exec("select set_config('test.jwt_role','service_role',false)");const claim=async token=>(await db.query("select public.claim_historical_contract_stamp($1,$2) ok",[doc,token])).rows[0].ok;
   assert.equal(await claim(operation),true);const marker=(await db.query("select public.get_historical_contract_stamp_marker($1) marker",[doc])).rows[0].marker;assert.ok(marker.length>=64);assert.equal(await claim(other),false);await db.query("select public.release_historical_contract_stamp($1,$2)",[doc,other]);assert.equal(await claim(other),false);
   await db.exec("update public.historical_contract_stamp_lease set expires_at=now()-interval '1 second'");assert.equal(await claim(other),true);assert.equal((await db.query("select public.get_historical_contract_stamp_marker($1) marker",[doc])).rows[0].marker,marker);await db.query("select public.release_historical_contract_stamp($1,$2)",[doc,operation]);assert.equal(await claim(operation),false);
  });
 }finally{await db.close();}
});
await test("membership end and activity changes are not executed again on replay",async()=>{
 const db=await setup();try{
  await begin(db);const doc=(await finish(db)).id;
  await db.exec("update public.historical_contract_operation set contract_type='historischer_pv_folgevertrag';update public.dokument set historical_contract_type='historischer_pv_folgevertrag' where id>=1000");
  const query=`select * from public.register_historischen_pachtvertrag($1::uuid,$2::bigint,'historischer_pv_folgevertrag',date '2010-01-01',true,array[12]::bigint[],'{}'::jsonb,null::bigint,null::date,array[1]::bigint[],'[{"mitglied_id":2,"mitglied_ende":"2010-01-15"}]'::jsonb,'[{"mitglied_id":1,"aktiv":true}]'::jsonb)`;
  await db.exec("select set_config('test.jwt_role','authenticated',false)");const first=await db.query(query,[operation,doc]);const before=await snapshot(db);assert.ok(before.writes>=2);assert.deepEqual((await db.query(query,[operation,doc])).rows,first.rows);assert.deepEqual(await snapshot(db),before);
 }finally{await db.close();}
});
