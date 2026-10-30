import assert from "node:assert/strict";
import test from "node:test";
import { PDFDocument, PDFName, PDFString } from "pdf-lib";
import { edgeHarness, loadStamp } from "./edge-test-runtime.mjs";
const op="aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
function upload(bytes=[1,2,3],operation=op){
 const form=new FormData();form.set("file",new File([new Uint8Array(bytes)],"vertrag.pdf",{type:"application/pdf"}));
 for(const [k,v]of Object.entries({owner_kind:"mitglied",owner_id:"2",titel:"Historischer Vertrag",historical_contract_type:"historischer_pv_folgevertrag",operation_id:operation}))form.set(k,v);
 return new Request("https://synthetic.invalid",{method:"POST",headers:{Authorization:"Bearer user-token"},body:form});
}
function stamp(){return new Request("https://synthetic.invalid",{method:"POST",headers:{Authorization:"Bearer user-token","Content-Type":"application/json"},body:JSON.stringify({action:"reserve_historical_contract_number",document_id:77})});}
async function pdf(){const doc=await PDFDocument.create();doc.addPage().drawText("Original");return doc.save();}
test("sequential upload retries reuse one document and one physical file",async()=>{
 const h=edgeHarness();const a=await h.request(upload());const b=await h.request(upload());assert.equal(a.status,200);assert.equal(b.status,200);
 const first=await a.json(),retry=await b.json();assert.equal(first.document.id,retry.document.id);for(const field of ["drive_file_id","storage_path","dateiname","mime_type","size_bytes"])assert.ok(first[field],"MAUI upload response field: "+field);assert.equal(h.calls.uploads.length,1);assert.equal(h.calls.driveCreates.length,1);assert.equal(h.calls.removals.length,0);
});
test("parallel historical uploads converge on the persisted Drive ID and document",async()=>{
 const h=edgeHarness();const [a,b]=await Promise.all([h.request(upload()),h.request(upload())]);assert.equal(a.status,200);assert.equal(b.status,200);
 assert.equal((await a.json()).document.id,(await b.json()).document.id);assert.equal(h.calls.driveCreates.length,1);assert.equal(h.calls.uploads.length,1);assert.equal(new Set(h.calls.driveAttempts).size,1);
});
test("unknown DB result after physical upload resumes with the same ID and no compensation deletion",async()=>{
 const h=edgeHarness({finishError:true});assert.equal((await h.request(upload())).status,500);assert.equal(h.calls.driveCreates.length,1);assert.equal(h.calls.removals.length,0);
 h.options.finishError=false;assert.equal((await h.request(upload())).status,200);assert.equal(h.calls.driveCreates.length,1);assert.equal(h.calls.uploads.length,1);assert.equal(new Set(h.calls.driveAttempts).size,1);
});
test("same operation with different bytes is a controlled conflict without another file",async()=>{
 const h=edgeHarness();assert.equal((await h.request(upload())).status,200);const response=await h.request(upload([4,5,6]));assert.equal(response.status,409);assert.equal((await response.json()).error_code,"OPERATION_CONFLICT");assert.equal(h.calls.driveCreates.length,1);
});
test("physical create succeeded but its response was lost: retry still creates no second file",async()=>{
 const h=edgeHarness({uploadResponseLost:true});assert.equal((await h.request(upload())).status,502);assert.equal(h.calls.driveCreates.length,1);assert.equal(h.calls.removals.length,0);
 h.options.uploadResponseLost=false;assert.equal((await h.request(upload())).status,200);assert.equal(h.calls.driveCreates.length,1);assert.equal(h.calls.uploads.length,1);
});
test("historical upload refuses a missing operation identity",async()=>{
 const h=edgeHarness();assert.equal((await h.request(upload([1],""))).status,400);assert.equal(h.calls.driveCreates.length,0);
});
test("lost successful stamp response replays without another PDF write",async()=>{
 const h=edgeHarness({source:await pdf()});assert.equal((await h.request(stamp())).status,200);const a=h.calls.writes[0];assert.equal((await h.request(stamp())).status,200);assert.equal(h.calls.writes.length,1);assert.equal(h.calls.marks,1);await PDFDocument.load(a);
});
test("write succeeded / status failed resumes by recognizing authenticated PDF marker",async()=>{
 const h=edgeHarness({source:await pdf(),markError:true});assert.equal((await h.request(stamp())).status,502);assert.equal(h.calls.writes.length,1);
 h.options.markError=false;assert.equal((await h.request(stamp())).status,200);assert.equal(h.calls.writes.length,1);assert.equal(h.calls.marks,2);
});
test("failed PDF write resumes the missing stamp step",async()=>{
 const h=edgeHarness({source:await pdf(),writeError:true});assert.equal((await h.request(stamp())).status,502);assert.equal(h.calls.marks,0);h.options.writeError=false;
 assert.equal((await h.request(stamp())).status,200);assert.equal(h.calls.writes.length,1);assert.equal(h.calls.marks,1);
});
test("Drive replacement succeeded but its response was lost: authenticated marker prevents another write",async()=>{
 const h=edgeHarness({source:await pdf(),writeResponseLost:true});assert.equal((await h.request(stamp())).status,502);assert.equal(h.calls.marks,0);assert.equal(h.calls.writes.length,1);
 h.options.writeResponseLost=false;assert.equal((await h.request(stamp())).status,200);assert.equal(h.calls.writes.length,1);assert.equal(h.calls.marks,1);
});
test("parallel stamp calls serialize, then return a compatible completed result",async()=>{
 const h=edgeHarness({source:await pdf()});const responses=await Promise.all([h.request(stamp()),h.request(stamp())]);assert.ok(responses.some(r=>r.status===200));assert.ok(responses.every(r=>[200,409].includes(r.status)));
 assert.equal((await h.request(stamp())).status,200);assert.equal(h.calls.writes.length,1);
});
test("PDF marker replay is byte-for-byte stable; an untrusted marker cannot skip stamping",async()=>{
 const {stampHistoricalContractPdf}=loadStamp();const original=await pdf();const first=await stampHistoricalContractPdf(original,80037,[1042],"trusted-signature");assert.deepEqual(await stampHistoricalContractPdf(first,80037,[1042],"trusted-signature"),first);
 const forged=await PDFDocument.load(original);forged.catalog.set(PDFName.of("KGVHistoricalContractStamp"),PDFString.of("forged"));const forgedBytes=await forged.save();assert.notDeepEqual(await stampHistoricalContractPdf(forgedBytes,80037,[1042],"trusted-signature"),forgedBytes);
});
test("real Drive upload implementation recovers a create-409 using the preallocated ID",async()=>{
 const files=new Map();let creates=0,posts=0;
 const h=edgeHarness({fetch:async(url,options)=>{
  if(url.includes("generateIds"))return Response.json({ids:["allocated-123"]});
  if(options.method==="POST"){
   posts++;const body=new TextDecoder().decode(options.body);const metadata=JSON.parse(body.slice(body.indexOf('{'),body.indexOf('}')+1));assert.equal(metadata.id,"allocated-123");
   if(files.has(metadata.id))return Response.json({error:"conflict"},{status:409});files.set(metadata.id,metadata);creates++;return Response.json(metadata);
  }
  return Response.json(files.get("allocated-123"));
 }});
 assert.equal(await h.originalDrive.allocate("token"),"allocated-123");const args={accessToken:"token",parentId:"member-folder",fileName:"original.pdf",file:new File([new Uint8Array([1])],"original.pdf"),fileId:"allocated-123"};
 assert.deepEqual(await h.originalDrive.upload(args),await h.originalDrive.upload(args));assert.equal(posts,2);assert.equal(creates,1);
});
