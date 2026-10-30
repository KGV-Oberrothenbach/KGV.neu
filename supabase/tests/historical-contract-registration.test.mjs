import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { resolve } from "node:path";
import test from "node:test";
// Optional absolute module URL permits an isolated, external test-tool install.
const { PGlite } = await import(process.env.KGV_PGLITE_MODULE ?? "@electric-sql/pglite");
const root = resolve(import.meta.dirname, "../..");
const read = path => readFile(resolve(root, path), "utf8");
const migrationPath = "supabase/migrations/20261030140000_g11b1_historical_contract_validation.sql";
const seed = `insert into public.mitglied values (1,'1990-01-01',null,true,false),(2,'1990-01-01',null,true,false),(3,'1990-01-01',null,false,false),(4,'1990-01-01',null,true,true);
insert into public.parzelle values (12),(13),(14);
insert into public.pachtverhaeltnis(id,parzelle_id,urspruenglicher_pachtbeginn) values (12,12,'1998-01-01'),(13,13,'1998-01-01'),(14,14,'1998-01-01');
insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab) values (12,1,'hauptpaechter','1998-01-01'),(12,2,'mitpaechter','1998-01-01'),(14,3,'hauptpaechter','1998-01-01');
insert into public.dokument(id,mitglied_id,titel) values (10,1,'Altvertrag'),(20,1,'Nachfolger'),(100,1,'Neuer Scan');
insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp,vertragsdatum,wirksam_ab) values (12,10,'import','1998-01-01','1998-01-01');`;

async function register(db, options = {}) {
  const { kind = "historischer_pv_folgevertrag", date = "2010-01-01", relations = [12], parties = [1], ends = [], statuses = [], correction = null, correctionDate = null } = options;
  await db.exec("savepoint rpc_call; set local role authenticated;");
  try {
    const result = await db.query(`select * from public.register_historischen_pachtvertrag($1::bigint,$2::text,$3::date,$4::boolean,$5::bigint[],$6::jsonb,$7::bigint,$8::date,$9::bigint[],$10::jsonb,$11::jsonb)`,
      [100, kind, date, true, relations, "{}", correction, correctionDate, parties, JSON.stringify(ends), JSON.stringify(statuses)]);
    await db.exec("reset role; release savepoint rpc_call;");
    return result.rows;
  } catch (error) {
    await db.exec("rollback to savepoint rpc_call; release savepoint rpc_call;");
    throw error;
  }
}
const reject = (db, options, code = "23514") => assert.rejects(register(db, options), error => error.code === code);
const rows = async (db, query) => (await db.query(query)).rows;
const partyAt = async (db, date, relation = 12) => (await rows(db, `select b.mitglied_id::int as id from public.pachtverhaeltnis_beteiligung b where b.pachtverhaeltnis_id = ${relation} and b.gueltig_ab <= date '${date}' and (b.gueltig_bis is null or b.gueltig_bis >= date '${date}') order by b.mitglied_id`)).map(row => row.id);

await test("historical RPC executes on isolated PostgreSQL with synthetic prerequisites", async t => {
  const db = new PGlite();
  try {
    await db.exec(await read("supabase/tests/fixture.sql"));
    for (const name of ["20261019125000_pachtverhaeltnis_contract_versions.sql", "20261030110000_g6a_historical_contract_foundation.sql", "20261030120000_g6b2_historical_contract_state_transitions.sql", "20261030130000_g7_historical_contract_preview.sql"]) await db.exec(await read("supabase/migrations/" + name));
    await t.test("I: original G6 RPC reproduces the ambiguous output-parameter reference", async () => {
      await db.exec("begin;" + seed);
      try { await reject(db, {}, "42702"); } finally { await db.exec("rollback;"); }
    });
    const migration = await read(migrationPath);
    assert.match(migration, /#variable_conflict error/);
    assert.doesNotMatch(migration, /(?:where|and)\s+(?:dokument_id|pachtverhaeltnis_id)\s*=/i);
    assert.doesNotMatch(migration, /on conflict\s*\(mitglied_id, dokument_id, feld\)/i);
    await db.exec(migration);
    await db.exec(migration); // A repeated forward migration preserves signatures/ACLs.
    const scenario = async (name, body) => t.test(name, async () => {
      await db.exec("begin;" + seed);
      try { await body(db); } finally { await db.exec("rollback;"); }
    });
    await scenario("A/F/I: A+B to A closes B, retains membership, and runs the qualified helper", async db => {
      const result = await register(db);
      assert.equal(result.length, 1);
      assert.ok(result[0].historische_vertragsnummer >= 80000);
      assert.deepEqual(await partyAt(db, "1998-01-01"), [1, 2]);
      assert.deepEqual(await partyAt(db, "2010-01-01"), [1]);
      assert.deepEqual(await rows(db, "select gueltig_bis::text from public.pachtverhaeltnis_beteiligung where pachtverhaeltnis_id=12 and mitglied_id=2"), [{gueltig_bis:"2009-12-31"}]);
      assert.equal((await rows(db, "select mitglied_ende from public.mitglied where id=2"))[0].mitglied_ende, null);
    });
    await scenario("B: removed B may explicitly end membership on an edited date", async db => {
      await register(db, {ends:[{mitglied_id:2,mitglied_ende:"2010-01-15"}]});
      assert.equal((await rows(db,"select mitglied_ende::text from public.mitglied where id=2"))[0].mitglied_ende,"2010-01-15");
    });
    await scenario("C: an unrelated membership-end command is rejected atomically", async db => {
      await reject(db, {ends:[{mitglied_id:3,mitglied_ende:"2010-01-01"}]});
      assert.deepEqual(await partyAt(db,"2010-01-01"),[1,2]);
      assert.equal((await rows(db,"select count(*)::int as count from public.pachtvertragsfassung where dokument_id=100"))[0].count,0);
    });
    await scenario("D: unrelated or removed admission persons cannot receive membership-since corrections", async db => {
      for (const id of [3,2]) await reject(db, {kind:"historischer_ma_pv_erstvertrag",correction:id,correctionDate:"1998-01-01"});
      assert.equal((await rows(db,"select count(*)::int as count from public.mitglied_historische_korrektur"))[0].count,0);
    });
    await scenario("admission correction is audited for the actual new contract party only", async db => {
      await register(db,{kind:"historischer_ma_pv_erstvertrag",correction:1,correctionDate:"1998-01-01"});
      assert.equal((await rows(db,"select korrigierter_wert::text from public.mitglied_historische_korrektur where mitglied_id=1 and dokument_id=100"))[0].korrigierter_wert,"1998-01-01");
      await reject(db,{correction:1,correctionDate:"1998-01-01"});
    });
    await scenario("membership-end defaults are accepted, earlier membership dates and remaining parties stay protected", async db => {
      await reject(db,{ends:[{mitglied_id:2,mitglied_ende:"1989-01-01"}]});
      await register(db,{ends:[{mitglied_id:2,mitglied_ende:"2010-01-01"}]});
      assert.equal((await rows(db,"select mitglied_ende::text from public.mitglied where id=2"))[0].mitglied_ende,"2010-01-01");
    });
    await scenario("a rejected activity command rolls back the foundation, audit and party mutations", async db => {
      await reject(db,{kind:"historischer_ma_pv_erstvertrag",correction:1,correctionDate:"1998-01-01",statuses:[{mitglied_id:1,aktiv:false}]});
      assert.equal((await rows(db,"select count(*)::int as count from public.mitglied_historische_korrektur"))[0].count,0);
      assert.equal((await rows(db,"select mitglied_seit::text from public.mitglied where id=1"))[0].mitglied_seit,"1990-01-01");
      assert.equal((await rows(db,"select count(*)::int as count from public.pachtvertragsfassung where dokument_id=100"))[0].count,0);
      assert.deepEqual(await partyAt(db,"2010-01-01"),[1,2]);
    });
    await scenario("E: unrelated activity commands are rejected", async db => {
      await reject(db,{statuses:[{mitglied_id:3,aktiv:true}]});
      await reject(db,{statuses:[{mitglied_id:3,aktiv:false}]});
    });
    await scenario("a remaining party cannot be ended and null or duplicate commands cannot bypass validation", async db => {
      await reject(db,{ends:[{mitglied_id:1,mitglied_ende:"2010-01-01"}]});
      await reject(db,{ends:[{mitglied_id:2,mitglied_ende:null}]});
      await reject(db,{statuses:[{mitglied_id:1,aktiv:null}]});
      await reject(db,{ends:[{mitglied_id:2,mitglied_ende:"2010-01-01"},{mitglied_id:2,mitglied_ende:"2010-01-02"}]},"22023");
    });
    await scenario("G: selected garden 13 changes while garden 12 remains intact", async db => {
      await db.exec("insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab) values(13,1,'hauptpaechter','1998-01-01'),(13,2,'mitpaechter','1998-01-01'); insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp,wirksam_ab) values(13,10,'import','1998-01-01');");
      await register(db,{relations:[13]});
      assert.deepEqual(await partyAt(db,"2010-01-01",12),[1,2]);
      assert.deepEqual(await partyAt(db,"2010-01-01",13),[1]);
    });
    await scenario("H: late 2005 A+B preserves existing 1998 A+B / 2010 A timeline and preview", async db => {
      await db.exec("update public.pachtverhaeltnis_beteiligung set gueltig_bis='2009-12-31' where pachtverhaeltnis_id=12 and mitglied_id=2; insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp,wirksam_ab) values(12,20,'import','2010-01-01');");
      await register(db,{date:"2005-01-01",parties:[1,2]});
      for (const date of ["1998-01-01","2005-01-01"]) assert.deepEqual(await partyAt(db,date),[1,2]);
      assert.deepEqual(await partyAt(db,"2010-01-01"),[1]);
      const preview = await rows(db,"select wirksam_ab::text,vertragspartei_mitglied_ids from public.get_historische_pachtvertragsvorschau(array[12]::bigint[],date '2005-01-01')");
      assert.deepEqual(preview.map(row=>row.vertragspartei_mitglied_ids.map(Number)),[[1,2],[1,2],[1]]);
    });
    await scenario("newly added historical B ends at the existing successor even without a later B participation", async db => {
      await db.exec("delete from public.pachtverhaeltnis_beteiligung where pachtverhaeltnis_id=12 and mitglied_id=2; insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp,wirksam_ab) values(12,20,'import','2010-01-01');");
      await register(db,{date:"2005-01-01",parties:[1,2]});
      assert.deepEqual(await partyAt(db,"2005-01-01"),[1,2]);
      assert.deepEqual(await partyAt(db,"2010-01-01"),[1]);
    });
    await scenario("temporarily removing B splits the period and restores an existing successor B", async db => {
      await db.exec("insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp,wirksam_ab) values(12,20,'import','2010-01-01');");
      await register(db,{date:"2005-01-01"});
      assert.deepEqual(await partyAt(db,"1998-01-01"),[1,2]);
      assert.deepEqual(await partyAt(db,"2005-01-01"),[1]);
      assert.deepEqual(await partyAt(db,"2010-01-01"),[1,2]);
    });
    await scenario("passive is allowed after the last effective lease, active requires an effective lease", async db => {
      await register(db,{statuses:[{mitglied_id:2,aktiv:false},{mitglied_id:1,aktiv:true}]});
      assert.equal((await rows(db,"select aktiv from public.mitglied where id=2"))[0].aktiv,false);
    });
    await scenario("other effective parcels block both passive and membership end", async db => {
      await db.exec("insert into public.pachtverhaeltnis_beteiligung(pachtverhaeltnis_id,mitglied_id,rolle,gueltig_ab) values(13,2,'hauptpaechter','1998-01-01');");
      await reject(db,{statuses:[{mitglied_id:2,aktiv:false}]});
      await reject(db,{ends:[{mitglied_id:2,mitglied_ende:"2010-01-01"}]});
      assert.deepEqual(await partyAt(db,"2010-01-01"),[1,2]);
    });
    await scenario("an expired historical B cannot be made active today", async db => {
      await db.exec("update public.pachtverhaeltnis_beteiligung set gueltig_bis='2009-12-31' where pachtverhaeltnis_id=12 and mitglied_id=2; insert into public.pachtvertragsfassung(pachtverhaeltnis_id,dokument_id,fassungstyp,wirksam_ab) values(12,20,'import','2010-01-01');");
      await reject(db,{date:"2005-01-01",parties:[1,2],statuses:[{mitglied_id:2,aktiv:true}]});
    });
    await scenario("unrelated relations, missing capability and cross-demo parties are denied", async db => {
      await reject(db,{relations:[14]},"42501");
      await reject(db,{parties:[1,4]},"42501");
      await db.exec("select set_config('test.historical_permission','false',true);");
      await reject(db,{},"42501");
    });
    await scenario("demo actors cannot write productive contracts", async db => {
      await db.exec("select set_config('test.demo_actor','true',true);");
      await reject(db,{},"42501");
    });
    await scenario("internal helper remains unavailable to authenticated callers", async db => {
      assert.equal((await rows(db,"select has_function_privilege('authenticated','public.register_historischen_pachtvertrag_g6a(bigint,text,date,boolean,bigint[],jsonb,bigint,date)','execute') as allowed"))[0].allowed,false);
    });
  } finally { await db.close(); }
});
