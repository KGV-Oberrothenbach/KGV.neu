import assert from "node:assert/strict";
import test from "node:test";
import { PDFDocument } from "pdf-lib";
import { edgeHarness } from "./edge-test-runtime.mjs";
const bit=16777216;
function uploadRequest(extra={}){const form=new FormData();form.set("file",new File([new Uint8Array([1,2,3])],extra.fileName??"vertrag.pdf",{type:extra.mime??"application/pdf"}));form.set("owner_kind","mitglied");form.set("owner_id","2");form.set("titel","Historischer Vertrag");form.set("historical_contract_type","historischer_pv_folgevertrag");form.set("operation_id","aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");for(const [k,v]of Object.entries(extra.fields??{}))v===null?form.delete(k):form.set(k,v);return new Request("https://synthetic.invalid",{method:"POST",headers:{Authorization:"Bearer user-token"},body:form});}
const jsonRequest=(action,id=77)=>new Request("https://synthetic.invalid",{method:"POST",headers:{Authorization:"Bearer user-token","Content-Type":"application/json"},body:JSON.stringify({action,document_id:id})});
async function pdf(){const doc=await PDFDocument.create();doc.addPage();return doc.save();}
test("A: user + grant uploads only an explicit historical context without document-admin rights",async()=>{
  const h=edgeHarness();const response=await h.request(uploadRequest({mime:"",fields:{created_by:"forged"}}));assert.equal(response.status,200);assert.equal(h.calls.uploads.length,1);
  assert.equal(h.calls.uploads[0].historical_contract_type,"historischer_pv_folgevertrag");assert.equal(h.calls.uploads[0].created_by,"11111111-1111-1111-1111-111111111111");assert.equal(h.calls.uploads[0].mime_type,"application/pdf");
  assert.ok(h.calls.rpc.some(call=>call.name==="can_register_historical_contracts"));assert.ok(h.calls.rpc.some(call=>call.name==="can_access_historical_contract_member"));
  const client=h.calls.clients.find(c=>c.config?.global?.headers?.Authorization);assert.equal(client.key,"synthetic-anon");assert.equal(client.config.global.headers.Authorization,"Bearer user-token");
});
test("B: scoped historical source and final documents are downloadable with Bit 24",async()=>{
  for(const registered of [false,true]){const h=edgeHarness({registered,source:await pdf()});const response=await h.request(jsonRequest("download_historical_contract"));assert.equal(response.status,200);assert.ok((await response.arrayBuffer()).byteLength>0);}
});
test("C: user + grant may stamp a registered contract through the shared backend",async()=>{
  const h=edgeHarness({source:await pdf()});const response=await h.request(jsonRequest("reserve_historical_contract_number"));assert.equal(response.status,200);assert.equal(h.calls.marks,1);
  assert.ok(h.calls.rpc.some(call=>call.name==="can_access_historical_contract_document"&&call.args.p_require_registered===true));
});
for(const role of ["user","admin","vorstand"]){test("D/E/F/G: "+role+" without Bit 24 cannot upload, stamp or read in the historical workflow",async()=>{
  for(const action of ["upload","reserve_historical_contract_number","download_historical_contract"]){const h=edgeHarness({role,grants:0,source:await pdf()});const response=await h.request(action==="upload"?uploadRequest():jsonRequest(action));assert.equal(response.status,403);assert.equal(h.calls.uploads.length,0);assert.equal(h.calls.writes.length,0);}
});}
test("H: Bit 24 never grants arbitrary foreign-document access or stamps an unrelated PDF",async()=>{
  for(const action of ["download","download_historical_contract","reserve_historical_contract_number"]){const h=edgeHarness({context:false,source:await pdf()});const response=await h.request(jsonRequest(action));assert.equal(response.status,403);assert.equal(h.calls.writes.length,0);}
  const h=edgeHarness();assert.equal((await h.request(jsonRequest("download_historical_contract",12345))).status,403);
});
test("I: explicit revocation overrides the historical grant for every workflow operation",async()=>{
  for(const action of ["upload","reserve_historical_contract_number","download_historical_contract"]){const h=edgeHarness({grants:bit,revocations:bit,source:await pdf()});assert.equal((await h.request(action==="upload"?uploadRequest():jsonRequest(action))).status,403);}
});
test("pending sources may be read but never stamped before relational registration",async()=>{
  const h=edgeHarness({registered:false});assert.equal((await h.request(jsonRequest("reserve_historical_contract_number"))).status,403);assert.equal(h.calls.writes.length,0);
});
test("no broadening of generic upload, deletion, parcel or prepared-contract operations",async()=>{
  for(const fields of [{historical_contract_type:null},{owner_kind:"parzelle"},{source_kommunikationsvorgang_id:"33",source_revision:"1"}]){const h=edgeHarness();assert.equal((await h.request(uploadRequest({fields}))).status,403);assert.equal(h.calls.uploads.length,0);}
  const h=edgeHarness();assert.equal((await h.request(new Request("https://synthetic.invalid",{method:"DELETE",headers:{Authorization:"Bearer user-token","Content-Type":"application/json"},body:JSON.stringify({drive_file_id:"foreign-file"})}))).status,403);
});
test("unknown historical type and non-scan formats are rejected server-side",async()=>{
  const h=edgeHarness();assert.equal((await h.request(uploadRequest({fields:{historical_contract_type:"unrelated"}}))).status,400);
  assert.equal((await h.request(uploadRequest({fileName:"document.docx",mime:"application/vnd.openxmlformats-officedocument.wordprocessingml.document"}))).status,403);assert.equal(h.calls.uploads.length,0);
});
test("a foreign-project/invalid token cannot create historical context",async()=>{
  const h=edgeHarness({invalidToken:true});assert.equal((await h.request(uploadRequest())).status,401);assert.equal(h.calls.uploads.length,0);
});
