import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import ts from "typescript";
import * as pdfLib from "pdf-lib";
const root = resolve(import.meta.dirname,"../..");
function compile(path, require, extras = {}) {
  const source = readFileSync(resolve(root,path),"utf8");
  const output = ts.transpileModule(source,{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS}}).outputText;
  const module = {exports:{}};
  new Function("require","module","exports",...Object.keys(extras),output)(require,module,module.exports,...Object.values(extras));
  return module.exports;
}
export function loadStamp() {
  return compile("supabase/functions/kgv-upload-document/historical-contract-pdf.ts", name => {
    if (name === "npm:pdf-lib@1.17.1") return pdfLib;
    throw new Error("Unexpected stamp import: "+name);
  });
}
export function edgeHarness(options = {}) {
  const userId = "11111111-1111-1111-1111-111111111111";
  const granted = options.grants ?? 16777216, revoked = options.revocations ?? 0;
  const permission = Boolean((granted & ~revoked) & 16777216);
  const calls = {rpc:[],writes:[],uploads:[],marks:0,clients:[]};
  const document = {id:77,mitglied_id:2,parzelle_id:null,drive_file_id:"drive-77",dateiname:"vertrag.pdf",mime_type:"application/pdf",archiviert_at:null,...options.document};
  const tables = {
    app_user:[{user_id:userId,role:options.role??"user",mitglied_id:1,is_demo_account:false,permission_grants:granted,permission_revocations:revoked}],
    dokument:[document],mitglied:[{id:2,is_demo:false}],
    pachtvertragsfassung:[{dokument_id:77,pachtverhaeltnis_id:12,wirksam_ab:"2010-01-01"}],
    pachtverhaeltnis_beteiligung:(options.numbers??[1042,1048]).map(n=>({pachtverhaeltnis_id:12,gueltig_ab:"1998-01-01",gueltig_bis:null,mitglied:{mitgliedsnummer:n}})),
    parzellen_belegung:[],
  };
  class Query {
    constructor(table){this.table=table;this.filters=[];}
    select(){return this;}
    eq(key,value){this.filters.push(row=>row[key]===value);return this;}
    is(key,value){return this.eq(key,value);}
    in(key,values){this.filters.push(row=>values.includes(row[key]));return this;}
    lte(){return this;} or(){return this;} limit(){return this;}
    insert(value){this.inserted=value;return this;}
    result(single=false){
      if(this.inserted){Object.assign(document,this.inserted);calls.uploads.push({...this.inserted});return {data:document,error:null};}
      const items=(tables[this.table]??[]).filter(row=>this.filters.every(fn=>fn(row)));
      return {data:single?(items[0]??null):items,error:null};
    }
    single(){return Promise.resolve(this.result(true));} maybeSingle(){return this.single();}
    then(resolve,reject){return Promise.resolve(this.result()).then(resolve,reject);}
  }
  const userRpc = async (name,args) => {
    calls.rpc.push({name,args});
    if(name==="can_register_historical_contracts")return {data:permission,error:null};
    if(name==="can_access_historical_contract_member")return {data:permission&&args.p_mitglied_id===2,error:null};
    if(name==="can_access_historical_contract_document")return {data:permission&&args.p_dokument_id===77&&options.context!==false&&document.archiviert_at===null&&(!args.p_require_registered||options.registered!==false),error:null};
    throw new Error("Unexpected user RPC: "+name);
  };
  const admin = {
    auth:{getUser:async token=>options.invalidToken?{data:{user:null},error:{message:"bad token"}}:{data:{user:{id:userId}},error:null}},
    from:table=>new Query(table),
    rpc:async name=>{
      if(name==="can_access_mitgliedsaufnahme_vorstand_document")return {data:false,error:null};
      if(name==="reserve_historische_vertragsnummer")return {data:[{nummer:80037,titel:"Vertrag",mime_type:document.mime_type,pdf_bereits_gestempelt:false}],error:null};
      if(name==="mark_historische_vertragsnummer_pdf_gestempelt"){calls.marks++;return {data:null,error:options.markError?{message:"test status error"}:null};}
      throw new Error("Unexpected admin RPC: "+name);
    },
  };
  let handler;
  const Deno={env:{get:key=>({SUPABASE_URL:"https://synthetic.supabase.invalid",SUPABASE_SERVICE_ROLE_KEY:"synthetic-service",SUPABASE_ANON_KEY:"synthetic-anon"})[key]},serve:fn=>{handler=fn;}};
  const google={
    token:async()=>"synthetic-google",root:()=>"root",verify:async()=>{},folder:async()=>"folder",
    upload:async args=>({id:"drive-new",name:args.fileName}),
    download:async()=>new Response(options.source??new Uint8Array()),
    replace:async(_token,_id,bytes)=>{calls.writes.push(bytes);if(options.writeError)throw new Error("Drive replacement failed");},
    remove:async()=>{},
  };
  const source=readFileSync(resolve(root,"supabase/functions/kgv-upload-document/index.ts"),"utf8");
  const overrides=`
getGoogleAccessToken=google.token;readGoogleDriveRootFolderId=google.root;verifyDriveRootFolder=google.verify;ensureFolder=google.folder;uploadFileToDrive=google.upload;downloadDriveDocument=google.download;replaceDriveFileContent=google.replace;deleteDriveFile=google.remove;`;
  const code=ts.transpileModule(source,{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS}}).outputText;
  const require=name=>{
    if(name==="npm:@supabase/supabase-js@2")return {createClient:(_url,key,config)=>{calls.clients.push({key,config});return config?.global?.headers?.Authorization?{rpc:userRpc}:admin;}};
    if(name==="./historical-contract-pdf.ts")return loadStamp();
    throw new Error("Unexpected edge import: "+name);
  };
  new Function("require","exports","Deno","google","console","fetch",code+overrides)(require,{},Deno,google,{log(){},error(){}},()=>{throw new Error("Real network disabled in Edge tests");});
  return {calls,document,request:handler};
}
