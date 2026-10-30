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
  const calls = {rpc:[],writes:[],uploads:[],marks:0,clients:[],driveCreates:[],driveAttempts:[],removals:[]};
  let operation=null, stampLease=null, sourceBytes=options.source??new Uint8Array(), stamped=false;
  const driveFiles=new Map();
  const document = {id:77,mitglied_id:2,parzelle_id:null,drive_file_id:"drive-77",dateiname:"vertrag.pdf",mime_type:"application/pdf",archiviert_at:null,...options.document};
  const tables = {
    app_user:[{user_id:userId,role:options.role??"user",mitglied_id:1,is_demo_account:false,permission_grants:granted,permission_revocations:revoked}],
    dokument:[document],mitglied:[{id:2,is_demo:false}],
    pachtvertragsfassung:[{dokument_id:77,pachtverhaeltnis_id:12,wirksam_ab:"2010-01-01"}],
    pachtverhaeltnis_beteiligung:(options.numbers??[1042,1048]).map(n=>({pachtverhaeltnis_id:12,gueltig_ab:"1998-01-01",gueltig_bis:null,mitglied:{mitgliedsnummer:n}})),
    parzellen_belegung:[], historical_contract_operation:[],
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
    rpc:async (name,args)=>{
      calls.rpc.push({name,args,admin:true});
      if(name==="begin_historical_contract_upload"){
        if(operation && (operation.operation_id!==args.p_operation_id || JSON.stringify(operation.upload_request)!==JSON.stringify(args.p_upload_request)))return {data:null,error:{code:"22023",message:"Vorgangs-ID wurde bereits mit anderen Uploaddaten verwendet."}};
        if(!operation){operation={operation_id:args.p_operation_id,drive_file_id:args.p_drive_file_id,file_name:args.p_file_name,upload_request:args.p_upload_request,created_by:args.p_actor,mitglied_id:args.p_member_id,contract_type:args.p_contract_type};tables.historical_contract_operation.push(operation);}
        return {data:{...operation},error:null};
      }
      if(name==="finish_historical_contract_upload"){
        if(options.finishError)return {data:null,error:{message:"unknown DB result"}};
        if(!operation.document_id){Object.assign(document,{historical_contract_type:operation.contract_type,created_by:operation.created_by,mime_type:operation.upload_request.mime,drive_file_id:operation.drive_file_id,storage_path:args.p_storage_path,dateiname:operation.file_name,size_bytes:operation.upload_request.size});operation.document_id=document.id;calls.uploads.push({...document});}
        return {data:document,error:null};
      }
      if(name==="get_historical_contract_stamp_marker")return {data:"persisted-backend-marker-".repeat(4),error:null};
      if(name==="claim_historical_contract_stamp"){if(stampLease)return {data:false,error:null};stampLease=args.p_token;return {data:true,error:null};}
      if(name==="release_historical_contract_stamp"){if(stampLease===args.p_token)stampLease=null;return {data:null,error:null};}
      if(name==="can_access_mitgliedsaufnahme_vorstand_document")return {data:false,error:null};
      if(name==="reserve_historische_vertragsnummer")return {data:[{nummer:80037,titel:"Vertrag",mime_type:document.mime_type,pdf_bereits_gestempelt:stamped}],error:null};
      if(name==="mark_historische_vertragsnummer_pdf_gestempelt"){calls.marks++;if(!options.markError)stamped=true;return {data:null,error:options.markError?{message:"test status error"}:null};}
      throw new Error("Unexpected admin RPC: "+name);
    },
  };
  let handler;
  const Deno={env:{get:key=>({SUPABASE_URL:"https://synthetic.supabase.invalid",SUPABASE_SERVICE_ROLE_KEY:"synthetic-service",SUPABASE_ANON_KEY:"synthetic-anon"})[key]},serve:fn=>{handler=fn;}};
  const google={
    token:async()=>"synthetic-google",root:()=>"root",verify:async()=>{},folder:async()=>"folder",
    allocate:async()=>"drive-candidate-"+crypto.randomUUID(),
    upload:async args=>{calls.driveAttempts.push(args.fileId);if(options.uploadError)throw new Error("Drive upload failed");const id=args.fileId??"drive-new";if(!driveFiles.has(id)){driveFiles.set(id,args.file);calls.driveCreates.push(id);}if(options.uploadResponseLost)throw new Error("Drive upload response lost");return {id,name:args.fileName};},
    download:async()=>new Response(sourceBytes),
    replace:async(_token,_id,bytes)=>{if(options.writeError)throw new Error("Drive replacement failed");calls.writes.push(bytes);sourceBytes=bytes;if(options.writeResponseLost)throw new Error("Drive replacement response lost");},
    remove:async(_token,id)=>{calls.removals.push(id);},
  };
  const source=readFileSync(resolve(root,"supabase/functions/kgv-upload-document/index.ts"),"utf8");
  const overrides=`
allocateDriveFileId=google.allocate;getGoogleAccessToken=google.token;readGoogleDriveRootFolderId=google.root;verifyDriveRootFolder=google.verify;ensureFolder=google.folder;uploadFileToDrive=google.upload;downloadDriveDocument=google.download;replaceDriveFileContent=google.replace;deleteDriveFile=google.remove;`;
  const code=ts.transpileModule(source,{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS}}).outputText;
  const require=name=>{
    if(name==="npm:@supabase/supabase-js@2")return {createClient:(_url,key,config)=>{calls.clients.push({key,config});return config?.global?.headers?.Authorization?{rpc:userRpc}:admin;}};
    if(name==="./historical-contract-pdf.ts")return loadStamp();
    throw new Error("Unexpected edge import: "+name);
  };
  const originalDrive = new Function("require","exports","Deno","google","console","fetch",code+";const originalDriveUpload=uploadFileToDrive;const originalAllocate=allocateDriveFileId;"+overrides+";return {upload:originalDriveUpload,allocate:originalAllocate};")(require,{},Deno,google,{log(){},error(){}},options.fetch??(()=>{throw new Error("Real network disabled in Edge tests");}));
  return {calls,document,options,request:handler,originalDrive};
}
