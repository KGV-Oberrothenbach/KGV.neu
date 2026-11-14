// CX-04 real local E2E: Auth/JWT, PostgREST, Edge signature upload, private
// Storage, membership v5 PDF rendering and the real board admission RPCs.
import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { deflateSync } from "node:zlib";
import * as pdfLib from "pdf-lib";
import { drawPdfTemplate } from "../functions/kgv-generate-contract/pdf-rendering-core.mjs";

const api = need("P11_LOCAL_API_URL").replace(/\/$/, "");
const anon = need("P11_LOCAL_ANON_KEY");
const service = need("P11_LOCAL_SERVICE_ROLE_KEY");
const db = need("P11_LOCAL_DB_URL");
const outputPdf = resolve(process.env.CX04_PDF_PATH ?? "p11-ap04d-diagnostics/cx04-signed-membership-application.pdf");
const outputJson = resolve(process.env.CX04_RESULT_PATH ?? "p11-ap04d-diagnostics/cx04-results.json");
const fixtureFile = fileURLToPath(new URL("./cx04-membership-application.fixture.sql", import.meta.url));
const templateFile = resolve("KGV.Core/Templates/mitgliedsantrag/v5/template.pdf");
const specFile = resolve("KGV.Core/Templates/mitgliedsantrag/v5/field-spec.json");
const password = "CX04-local-test-password-2026!";
const results = [];
const uploads = [];

function need(name) { const v = process.env[name]; if (!v) throw new Error(`${name} is required`); return v; }
async function check(name, kind, fn) {
  try { await fn(); results.push({ name, kind, status: "passed" }); console.log(`ok - ${name}`); }
  catch (e) { results.push({ name, kind, status: "failed", message: e.message }); console.error(`not ok - ${name}: ${e.message}`); throw e; }
}
async function http(path, { method="GET", token, key=anon, body, bytes, headers={} }={}) {
  const response = await fetch(`${api}${path}`, { method, headers: { apikey:key, ...(token?{Authorization:`Bearer ${token}`} : {}), ...((body!==undefined)?{"Content-Type":"application/json"}:{}), ...headers }, body: bytes ?? (body===undefined?undefined:JSON.stringify(body)) });
  const ct=response.headers.get("content-type")??""; const data=ct.includes("json")?await response.json():new Uint8Array(await response.arrayBuffer());
  return { response, data };
}
async function rpc(name, token, body) { return http(`/rest/v1/rpc/${name}`, { method:"POST", token, body }); }
function one(x) { return Array.isArray(x) ? x[0] : x; }
function psql(args, input) {
  const command=process.platform==="win32"?"docker":"psql";
  const commandArgs=process.platform==="win32"?["exec","-i","supabase_db_03_Arbeitsstand","psql","-U","postgres","-d","postgres","-X",...args]:[db,"-X",...args];
  const r=spawnSync(command,commandArgs,{encoding:"utf8",input}); if(r.status!==0) throw new Error(`psql failed:\n${r.stdout}\n${r.stderr}`); return r.stdout;
}
function sql(text, tuples=false) { return psql(["-q","-t","-A","-v","ON_ERROR_STOP=1",...(tuples?["-P","tuples_only=on"]:[])], text).trim(); }
async function signup(label) {
  const email=`cx04.${label}@example.test`; const r=await http("/auth/v1/signup",{method:"POST",body:{email,password}});
  assert.equal(r.response.status,200,`${label}: ${JSON.stringify(r.data)}`); return { id:r.data.user.id, token:r.data.access_token, email };
}
async function fixture(users) {
  const args=["-q","-t","-A","-v","ON_ERROR_STOP=1"];
  for(const [k,v] of Object.entries(users)) args.push("-v",`${k}_user_id=${v.id}`);
  const out=psql(args,await readFile(fixtureFile,"utf8")); const line=out.split(/\r?\n/).findLast(x=>x.trim().startsWith("{"));
  if(!line) throw new Error(`fixture returned no JSON: ${out}`); return JSON.parse(line);
}
function crc32(bytes){let c=0xffffffff;for(const b of bytes){c^=b;for(let i=0;i<8;i++)c=(c>>>1)^(0xedb88320&-(c&1));}return(c^0xffffffff)>>>0;}
function chunk(type,data){const t=Buffer.from(type),o=Buffer.alloc(12+data.length);o.writeUInt32BE(data.length);t.copy(o,4);data.copy(o,8);o.writeUInt32BE(crc32(Buffer.concat([t,data])),8+data.length);return o;}
function png(seed){const w=120,h=40,head=Buffer.alloc(13);head.writeUInt32BE(w);head.writeUInt32BE(h,4);head.set([8,6,0,0,0],8);const rows=[];for(let y=0;y<h;y++){const row=Buffer.alloc(1+w*4);for(let x=0;x<w;x++){const ink=Math.abs(y-(20+Math.round(6*Math.sin((x+seed)/8))))<=1;if(ink)row.set([(seed*47)%210+20,(seed*79)%190+20,(seed*31)%200+20,255],1+x*4);}rows.push(row);}const bytes=Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk("IHDR",head),chunk("IDAT",deflateSync(Buffer.concat(rows))),chunk("IEND",Buffer.alloc(0))]);return{bytes,base64:bytes.toString("base64"),sha:createHash("sha256").update(bytes).digest("hex")};}
async function edgeUpload(token,pid,key,image,revision=1){const r=await http("/functions/v1/upload-prepared-contract-signature",{method:"POST",token,body:{kommunikationsvorgang_id:pid,signatur_revision:revision,signatur_schluessel:key,png_base64:image.base64}});if(r.response.ok)uploads.push(r.data.storage_path);return r;}
async function save(manager,pid,key,memberId,image,stored,source=null,name=null){return rpc("speichere_prepared_contract_signatur",manager.token,{p_kommunikationsvorgang_id:pid,p_signatur_revision:1,p_signatur_schluessel:key,p_mitglied_id:memberId,p_unterzeichner_name_snapshot:name,p_vereinsfunktion_besetzung_id:source?.vereinsfunktion_besetzung_id??null,p_vertretung_id:source?.vertretung_id??null,p_vertretene_besetzung_id:source?.vertretene_besetzung_id??null,p_storage_path:stored.storage_path,p_signatur_sha256:image.sha,p_unterzeichnet_am:new Date().toISOString()});}

const paper=["satzung","kleingartenordnung","beitragsordnung","arbeitsordnung"].map(dokumenttyp=>({dokumenttyp,bereitstellungsart:"papier",bereitgestellt_am:"2026-11-14T10:00:00Z",bemerkung:"CX-04 lokale Papierübergabe"}));
function draft(extra={}) { return { startDate:"2026-11-14", vereinsunterlagenNachweis:paper, vereinsunterlagen_nachweis:paper, clubDocumentsSnapshot:"Satzung und Ordnungen lokal übergeben", ...extra }; }
async function createProcess(manager,memberId,extra={}) {
  const r=await http("/rest/v1/kommunikationsvorgang",{method:"POST",token:manager.token,body:{vorgangstyp:"vertrag_signatur",dokumenttyp:"mitgliedsantrag",status:"vorbereitet",mitglied_id:memberId,revision:1,zugewiesen_an:manager.id,entwurf_daten:draft(extra)},headers:{Prefer:"return=representation"}});
  assert.equal(r.response.status,201,JSON.stringify(r.data)); return one(r.data);
}
async function startAndPlan(manager,p,slots){
  const start=await rpc("start_prepared_contract_signature",manager.token,{p_kommunikationsvorgang_id:p.id,p_expected_revision:1,p_assigned_to:manager.id}); assert.equal(start.response.status,200,JSON.stringify(start.data));
  for(const [key,order,type,memberId,name] of slots){const r=await rpc("ensure_prepared_contract_signatur_position",manager.token,{p_kommunikationsvorgang_id:p.id,p_signatur_revision:1,p_signatur_schluessel:key,p_sort_order:order,p_pflicht:true,p_unterzeichner_typ:type,p_mitglied_id:memberId,p_unterzeichner_name_snapshot:name??null});assert.equal(r.response.status,200,`${key}: ${JSON.stringify(r.data)}`);}
  // The production retry contract is intentionally available only after the
  // application-owned position plan exists; before that, a retry must fail.
  const retry=await rpc("start_prepared_contract_signature",manager.token,{p_kommunikationsvorgang_id:p.id,p_expected_revision:1,p_assigned_to:manager.id}); assert.equal(retry.response.status,200,JSON.stringify(retry.data));
  return Number(sql(`select nummer from public.fachnummer_reservierung where art='mitgliedsantrag' and quelle_art='kommunikationsvorgang' and quelle_id=${p.id}`));
}
function slots(memberId,second=null){const out=[["member.application",10,"mitglied",memberId],["member.privacy",20,"mitglied",memberId]];if(second)out.push([`${second.kind}.antrag`,30,second.type,second.id,second.name],[`${second.kind}.datenschutz`,40,second.type,second.id,second.name]);out.push(["vorstand_1",90,"vereinsunterzeichner",null],["vorstand_2",100,"vereinsunterzeichner",null]);return out;}
async function boardSources(manager,pid){const r=await rpc("prepared_contract_snapshot_unterzeichner",manager.token,{p_kommunikationsvorgang_id:pid,p_signatur_revision:1});assert.equal(r.response.status,200,JSON.stringify(r.data));return r.data;}
async function signAll(ctx,p,plan,{skip=[]}={}){
  const sources=await boardSources(ctx.users.manager,p.id); const source1=sources.find(x=>Number(x.mitglied_id)===Number(ctx.ids.board1));const source2=sources.find(x=>Number(x.mitglied_id)===Number(ctx.ids.board2));assert.ok(source1&&source2);
  for(const [key,,type,memberId,name] of plan){if(skip.includes(key))continue;const image=png(p.id+key.length);let signer=ctx.users.manager;let source=null;let actual=memberId;
    if(key==="vorstand_1"){signer=ctx.users.board1;source=source1;actual=ctx.ids.board1;}else if(key==="vorstand_2"){signer=ctx.users.board2;source=source2;actual=ctx.ids.board2;}else if(memberId){const match=Object.entries(ctx.ids).find(([,id])=>Number(id)===Number(memberId));signer=ctx.users[match?.[0]]??ctx.users.manager;}
    const up=await edgeUpload(signer.token,p.id,key,image);assert.equal(up.response.status,200,`${key}: ${JSON.stringify(up.data)}`);const saved=await save(signer,p.id,key,actual,image,up.data,source,name);assert.equal(saved.response.status,200,`${key}: ${JSON.stringify(saved.data)}`);
  }
}
function finalFixture(pid,memberId,label){const out=sql(`begin; insert into public.dokument(mitglied_id,bucket,storage_path,titel,dateiname,mime_type,drive_file_id,source_kommunikationsvorgang_id,source_revision) values(${memberId},'dokumente','cx04/local-only/${label}.pdf','CX-04 lokale Finaldokument-Fixture (kein Google Drive)','${label}.pdf','application/pdf','cx04-local-only-${label}',${pid},1) returning id \\gset\nupdate public.kommunikationsvorgang set status='letzte_pruefung',final_dokument_id=:id where id=${pid}; commit; select :id;`,true);return Number(out.split(/\r?\n/).findLast(x=>/^\d+$/.test(x)));}
async function startBoard(manager,pid){const r=await http("/functions/v1/kgv-complete-prepared-membership-application",{method:"POST",token:manager.token,body:{kommunikationsvorgang_id:pid,expected_revision:1}});assert.equal(r.response.status,200,JSON.stringify(r.data));return Number(r.data.bearbeitung_id);}
async function vote(user,bid,value="ja",reason=null){return rpc("stimme_mitgliedsaufnahme_ab",user.token,{p_bearbeitung_id:bid,p_stimme:value,p_nein_begruendung:reason});}
async function decide(user,bid,result="annehmen",way="einstimmig",reason=null){return rpc("entscheide_mitgliedsaufnahme",user.token,{p_bearbeitung_id:bid,p_ergebnis:result,p_weg:way,p_sitzungsdatum:way==="vorstandssitzung"?"2026-11-14":null,p_begruendung:reason,p_aufnahmedatum:result==="annehmen"?"2026-11-15":null});}
async function decideAccepted(user,bid,result="annehmen",way="einstimmig",reason=null){const r=await decide(user,bid,result,way,reason);const expected=result==="annehmen"?"angenommen":"abgelehnt";assert.equal(sql(`select entscheidung from public.mitgliedsaufnahme_vorstandsbearbeitung where id=${bid}`),expected,JSON.stringify(r.data));return r;}

let report;
try {
  const users={};
  await check("thirteen real local Auth sessions are issued","positive",async()=>{for(const n of ["manager","board1","board2","decider","outsider","a","b","c","d","e","neg","rep","secondary"])users[n]=await signup(n);assert.equal(new Set(Object.values(users).map(x=>x.id)).size,13);});
  let ids;
  await check("synthetic members, representative, secondary member and function assignments are isolated","positive",async()=>{ids=await fixture(users);assert.notEqual(ids.board1,ids.board2);assert.notEqual(ids.rep,ids.secondary);});
  const ctx={users,ids}; const processes={}; const numbers=[];
  await check("five prepared applications are created through the Web/PostgREST write contract","positive",async()=>{
    processes.a=await createProcess(users.manager,ids.a);
    processes.b=await createProcess(users.manager,ids.b,{representative:{mode:"existing",memberId:ids.rep}});
    processes.c=await createProcess(users.manager,ids.c,{representative:{mode:"manual",vorname:"Manuela",nachname:"Muster",adresseAbweichend:false}});
    processes.d=await createProcess(users.manager,ids.d,{includeSecondaryMember:true,secondaryMemberId:ids.secondary});
    processes.e=await createProcess(users.manager,ids.e);
  });
  const plans={a:slots(ids.a),b:slots(ids.b,{kind:"gesetzliche_vertretung",type:"gesetzlicher_vertreter",id:ids.rep}),c:slots(ids.c,{kind:"gesetzliche_vertretung",type:"gesetzlicher_vertreter",id:null,name:"Manuela Muster"}),d:slots(ids.d,{kind:"nebenmitglied",type:"nebenmitglied",id:ids.secondary}),e:slots(ids.e)};
  await check("signature starts freeze each revision and reserve five unique 10xxx numbers idempotently","positive",async()=>{for(const k of Object.keys(plans))numbers.push(await startAndPlan(users.manager,processes[k],plans[k]));assert.equal(new Set(numbers).size,5);assert.ok(numbers.every(n=>n>=10000&&n<=19999));});
  await check("foreign applicant, wrong representative and invalid revision uploads are denied","negative",async()=>{
    assert.equal((await edgeUpload(users.outsider.token,processes.a.id,"member.application",png(1))).response.status,403);
    assert.equal((await edgeUpload(users.a.token,processes.b.id,"gesetzliche_vertretung.antrag",png(2))).response.status,403);
    assert.equal((await edgeUpload(users.a.token,processes.a.id,"member.application",png(3),2)).response.status,409);
  });
  await check("missing privacy signature keeps the application incomplete and blocks productive finalization","negative",async()=>{
    await signAll(ctx,processes.a,plans.a,{skip:["member.privacy","vorstand_1","vorstand_2"]});
    const complete=await rpc("prepared_contract_signaturen_vollstaendig",users.manager.token,{p_kommunikationsvorgang_id:processes.a.id,p_signatur_revision:1});assert.equal(complete.data,false);
    const generated=await http("/functions/v1/kgv-generate-contract",{method:"POST",token:users.manager.token,body:{type:"mitgliedsantrag",action:"finalize",member_id:ids.a,kommunikationsvorgang_id:processes.a.id,source_revision:1,start_date:"2026-11-14"}});assert.ok(generated.response.status>=400,JSON.stringify(generated.data));
  });
  await check("all application/privacy/representative/secondary and distinct board signatures bind through Edge and RPC","positive",async()=>{
    for(const [k,p] of Object.entries(processes))await signAll(ctx,p,plans[k]);
    for(const p of Object.values(processes)){const r=await rpc("prepared_contract_signaturen_vollstaendig",users.manager.token,{p_kommunikationsvorgang_id:p.id,p_signatur_revision:1});assert.equal(r.data,true);}
  });
  await check("same natural person cannot fill both board signature positions","negative",async()=>{
    const p=await createProcess(users.manager,ids.neg);const plan=slots(ids.neg);await startAndPlan(users.manager,p,plan);const sources=await boardSources(users.manager,p.id);const s=sources.find(x=>Number(x.mitglied_id)===Number(ids.board1));
    for(const key of ["vorstand_1","vorstand_2"]){const image=png(90+key.length),up=await edgeUpload(users.board1.token,p.id,key,image);assert.equal(up.response.status,200);const saved=await save(users.board1,p.id,key,ids.board1,image,up.data,s);if(key==="vorstand_1")assert.equal(saved.response.status,200);else assert.equal(saved.response.status,400);}
  });
  await check("membership v5 renderer produces a real signed local PDF from persisted private PNGs","positive",async()=>{
    const spec=JSON.parse(await readFile(specFile,"utf8"));const signed=JSON.parse(sql(`select json_agg(json_build_object('key',signatur_schluessel,'path',storage_path,'sha',signatur_sha256,'name',unterzeichner_name_snapshot,'function',vereinsfunktion_bezeichnung_snapshot) order by sort_order) from public.prepared_contract_signatur where kommunikationsvorgang_id=${processes.a.id} and status='unterschrieben'`,true));const signatures={};const map={"member.application":"unterschrift_antragsteller_antrag","member.privacy":"unterschrift_antragsteller_datenschutz",vorstand_1:"unterschrift_vorstand_1",vorstand_2:"unterschrift_vorstand_2"};
    const denied=await http(`/storage/v1/object/authenticated/prepared-contract-signatures/${signed[0].path}`,{key:anon});assert.ok([400,401,403].includes(denied.response.status));
    for(const row of signed){if(!map[row.key])continue;const dl=await http(`/storage/v1/object/authenticated/prepared-contract-signatures/${row.path}`,{token:service,key:service});assert.equal(dl.response.status,200);assert.equal(createHash("sha256").update(dl.data).digest("hex"),row.sha);signatures[map[row.key]]=`data:image/png;base64,${Buffer.from(dl.data).toString("base64")}`;}
    const values=Object.fromEntries(spec.fields.filter(x=>x.type!=="signature"&&x.type!=="checkbox").map(x=>[x.name,"CX04"]));values.antragsnummer=`Antrags-Nr. ${numbers[0]}`;values.mitglied_name="CX04 Antrag A";values.vereinsunterlagen_nachweis="Satzung und Ordnungen lokal übergeben";values.vereinsunterzeichner_1="Vera CX04 Vorstand Eins";values.vereinsunterzeichner_2="Viktor CX04 Vorstand Zwei";
    const bytes=await drawPdfTemplate(pdfLib,new Uint8Array(await readFile(templateFile)),spec.fields,values,signatures,"none");await mkdir(dirname(outputPdf),{recursive:true});await writeFile(outputPdf,bytes);const parsed=await pdfLib.PDFDocument.load(bytes);assert.equal(parsed.getPageCount(),Math.max(...spec.fields.map(x=>x.page)));assert.ok(bytes.length>10000);
  });
  const reviews={};
  await check("wrong final-document provenance cannot enter final review","negative",async()=>{let rejected=false;try{sql(`begin; insert into public.dokument(mitglied_id,bucket,storage_path,titel,dateiname,mime_type,drive_file_id,source_kommunikationsvorgang_id,source_revision) values(${ids.a},'dokumente','cx04/local-only/wrong.pdf','CX-04 falsche Provenance','wrong.pdf','application/pdf','cx04-local-only-wrong',${processes.a.id},2) returning id \\gset\nupdate public.kommunikationsvorgang set status='letzte_pruefung',final_dokument_id=:id where id=${processes.a.id}; commit;`);}catch(e){rejected=/Provenance|provenance|passt nicht|revision/i.test(e.message);}assert.equal(rejected,true);assert.equal(sql(`select status from public.kommunikationsvorgang where id=${processes.a.id}`),"zur_signatur");});
  await check("local-only final metadata with exact provenance starts real board reviews and freezes two voters","positive",async()=>{for(const [k,p] of Object.entries(processes)){finalFixture(p.id,ids[k],`cx04-${k}`);reviews[k]=await startBoard(users.manager,p.id);assert.equal(await startBoard(users.manager,p.id),reviews[k]);const count=Number(sql(`select count(*) from public.mitgliedsaufnahme_abstimmung_snapshot_person where vorstandsbearbeitung_id=${reviews[k]}`));assert.equal(count,2);}});
  await check("unauthorized voting/decision, no-vote unanimity and revoked decision permission are denied","negative",async()=>{
    assert.ok([400,403].includes((await vote(users.outsider,reviews.a)).response.status));assert.ok([400,403].includes((await decide(users.outsider,reviews.a)).response.status));
    await vote(users.board1,reviews.a,"ja");sql("update public.vereinsfunktion_besetzung set gueltig_bis=current_date-1 where vereinsfunktion_id=(select id from public.vereinsfunktion where schluessel='cx04_vorstand_2')");try{assert.equal(Number(sql(`select count(*) from public.mitgliedsaufnahme_abstimmung_snapshot_person where vorstandsbearbeitung_id=${reviews.a}`)),2);assert.equal((await vote(users.board2,reviews.a,"nein","Noch Klärungsbedarf")).response.status,200);}finally{sql("update public.vereinsfunktion_besetzung set gueltig_bis=null where vereinsfunktion_id=(select id from public.vereinsfunktion where schluessel='cx04_vorstand_2')");}assert.equal(sql(`select entscheidung from public.mitgliedsaufnahme_vorstandsbearbeitung where id=${reviews.a}`),"offen");assert.equal((await decide(users.decider,reviews.a)).response.status,400);
    sql("update public.vereinsfunktion_besetzung set gueltig_bis=current_date-1 where vereinsfunktion_id=(select id from public.vereinsfunktion where schluessel='cx04_entscheidung')");try{assert.ok([400,403].includes((await decide(users.decider,reviews.a,"annehmen","vorstandssitzung")).response.status));}finally{sql("update public.vereinsfunktion_besetzung set gueltig_bis=null where vereinsfunktion_id=(select id from public.vereinsfunktion where schluessel='cx04_entscheidung')");}
  });
  await check("adult, existing representative, manual representative and secondary-member cases are admitted consistently","positive",async()=>{
    assert.equal(Number(sql(`select count(*) from public.mitglied_gesetzlicher_vertreter where minderjaehriges_mitglied_id in (${ids.b},${ids.c})`)),0);
    assert.equal(Number(sql(`select count(*) from public.prepared_contract_signatur where kommunikationsvorgang_id=${processes.d.id} and unterzeichner_typ='nebenmitglied' and mitglied_id=${ids.secondary} and status='unterschrieben'`)),2);
    await vote(users.board2,reviews.a,"ja");await decideAccepted(users.decider,reviews.a);
    for(const k of ["b","c","d"]){const v1=await vote(users.board1,reviews[k],"ja"),v1Retry=await vote(users.board1,reviews[k],"ja");assert.equal(v1.data,v1Retry.data);await vote(users.board2,reviews[k],"ja");await decideAccepted(users.decider,reviews[k]);}
    assert.equal(Number(sql(`select count(*) from public.mitglied_gesetzlicher_vertreter where minderjaehriges_mitglied_id=${ids.b} and vertreter_mitglied_id=${ids.rep} and gueltig_bis is null`)),1);
    assert.equal(Number(sql(`select count(*) from public.mitglied m join public.mitglied_gesetzlicher_vertreter r on r.vertreter_mitglied_id=m.id where r.minderjaehriges_mitglied_id=${ids.c} and m.vorname='Manuela' and m.name='Muster'`)),1);
    assert.equal(Number(sql(`select count(*) from public.mitglied_gesetzlicher_vertreter where minderjaehriges_mitglied_id=${ids.d}`)),0);
    for(const k of ["a","b","c","d"])assert.equal(Number(sql(`select count(*) from public.kommunikationsvorgang where vorgangstyp='dokument_erzeugung' and dokumenttyp='aufnahmebestaetigung' and entwurf_daten->>'mitgliedsantrag_kommunikationsvorgang_id'='${processes[k].id}'`)),1);
  });
  await check("rejection preserves signed history and a later application receives a new process and 10xxx number","positive",async()=>{
    await vote(users.board1,reviews.e,"ja");await vote(users.board2,reviews.e,"nein","Vorstandssitzung erforderlich");await decideAccepted(users.decider,reviews.e,"ablehnen","vorstandssitzung","Antrag nach Beratung abgelehnt");
    assert.equal(sql(`select aufnahme_status||':'||aktiv||':'||coalesce(mitglied_seit::text,'null') from public.mitglied where id=${ids.e}`),"abgelehnt:false:null");
    const oldVotes=Number(sql(`select count(*) from public.mitgliedsaufnahme_abstimmung_stimme s join public.mitgliedsaufnahme_abstimmung_snapshot_person p on p.id=s.snapshot_person_id where p.vorstandsbearbeitung_id=${reviews.e}`));assert.ok(oldVotes>=2);
    const reapply=await createProcess(users.manager,ids.e);const newNumber=await startAndPlan(users.manager,reapply,slots(ids.e));assert.notEqual(reapply.id,processes.e.id);assert.notEqual(newNumber,numbers[4]);assert.ok(newNumber>=10000&&newNumber<=19999);
  });
  await check("decision retries do not duplicate representatives or confirmations","positive",async()=>{await decideAccepted(users.decider,reviews.c);assert.equal(Number(sql(`select count(*) from public.mitglied m join public.mitglied_gesetzlicher_vertreter r on r.vertreter_mitglied_id=m.id where r.minderjaehriges_mitglied_id=${ids.c} and m.vorname='Manuela' and m.name='Muster'`)),1);assert.equal(Number(sql(`select count(*) from public.kommunikationsvorgang where vorgangstyp='dokument_erzeugung' and dokumenttyp='aufnahmebestaetigung' and entwurf_daten->>'mitgliedsantrag_kommunikationsvorgang_id'='${processes.c.id}'`)),1);});
  await check("service-only compatibility completion replays without an ambiguous representative column","positive",async()=>{const r=await http("/rest/v1/rpc/complete_prepared_mitgliedsantrag",{method:"POST",token:service,key:service,body:{p_kommunikationsvorgang_id:processes.b.id,p_expected_revision:1,p_created_by:users.manager.id}});assert.equal(r.response.status,200,JSON.stringify(r.data));assert.equal(one(r.data).bereits_abgeschlossen,true);assert.equal(Number(one(r.data).vertreter_mitglied_id),Number(ids.rep));});
  await check("Web and MAUI invocation contracts still reference the guarded board RPCs","positive",async()=>{const web=await readFile(resolve("KGV.Web/repositories/communication/membership-board-review-repository.ts"),"utf8");const maui=await readFile(resolve("KGV.Infrastructure/Services/SupabaseService.cs"),"utf8");for(const source of [web,maui]){assert.match(source,/stimme_mitgliedsaufnahme_ab/);assert.match(source,/entscheide_mitgliedsaufnahme/);}const functions=sql("select count(*) from pg_proc where proname in ('stimme_mitgliedsaufnahme_ab','entscheide_mitgliedsaufnahme','ensure_aufnahmebestaetigung_dokumentvorgang')");assert.equal(Number(functions),3);});
  report={complete:true,positive_passed:results.filter(x=>x.kind==="positive"&&x.status==="passed").length,negative_passed:results.filter(x=>x.kind==="negative"&&x.status==="passed").length,cases:results,application_numbers:numbers,process_ids:Object.fromEntries(Object.entries(processes).map(([k,v])=>[k,v.id])),pdf_path:outputPdf,signature_uploads:uploads.length,local_only_final_metadata:true,external_dispatch:false};
} catch(e) { report={complete:false,error:e.message,cases:results}; process.exitCode=1; }
finally { await mkdir(dirname(outputJson),{recursive:true});await writeFile(outputJson,JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2)); }
