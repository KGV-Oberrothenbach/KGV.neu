// CI-only wrapper: preserve assertions and exit status; never forward raw
// HTTP responses, session tokens or subprocess error messages to Actions logs.
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { existsSync, readFileSync } from 'node:fs';
const files={T07:'p11-ap04d-signed-change.e2e.mjs',CX01:'cx01-signature-workflow.e2e.mjs',CX02:'cx02-signed-lease-pdf.e2e.mjs',CX03:'cx03-contract-change-history.e2e.mjs',CX04:'cx04-membership-application-board.e2e.mjs',CX05:'cx05-historical-contract.e2e.mjs',CX06:'cx06-maintenance-contract.e2e.mjs'};
const id=process.argv[2];
try {
  if(!Object.hasOwn(files,id)||process.argv.length!==3)throw Error('INVALID_SUITE');
  const db=new URL(process.env.P11_LOCAL_DB_URL);
  if(db.protocol!=='postgresql:'||db.hostname!=='127.0.0.1'||db.pathname!=='/postgres'||db.search||db.hash)throw Error('NONLOCAL_DB');
  if(id!=='T07'){
    const api=new URL(process.env.P11_LOCAL_API_URL);
    if(api.protocol!=='http:'||api.hostname!=='127.0.0.1'||api.username||api.password||api.pathname!=='/'||api.search||api.hash)throw Error('NONLOCAL_API');
  }
  if(process.platform==='win32'&&!/^supabase_db_kgvfr2ci[a-f0-9]{12}$/.test(process.env.FR2_LOCAL_DB_CONTAINER??''))throw Error('LOCAL_CONTAINER_REQUIRED');
  const reportPath=process.env[id+'_RESULT_PATH'];
  if(id!=='T07'&&(!reportPath||existsSync(reportPath)))throw Error('NEW_REPORT_PATH_REQUIRED');
  const result=spawnSync(process.execPath,[fileURLToPath(new URL(files[id],import.meta.url))],{encoding:'utf8',timeout:240000,maxBuffer:16*1024*1024,windowsHide:true});
  console.log(`${id}: exit=${result.status??'NO_EXIT'}`);
  if(id!=='T07'&&existsSync(reportPath)){
    const data=JSON.parse(readFileSync(reportPath,'utf8'));
    if(!Array.isArray(data.cases))throw Error('REPORT_CASES_MISSING');
    const passed=data.cases.filter(c=>c.status==='passed').length;
    const failed=data.cases.filter(c=>c.status==='failed').length;
    console.log(`${id}: passed=${passed}; failed=${failed}; complete=${data.complete===true}`);
    if(result.status!==0||data.complete!==true||passed===0||passed!==data.cases.length)throw Error('SUITE_FAILED');
  } else if(id!=='T07'||result.status!==0||!result.stdout.includes('P11-T07-P: prepared signed-change RPC path passed'))throw Error('SUITE_FAILED');
} catch(e){console.error(`${Object.hasOwn(files,id)?id:'UNKNOWN'}: ${/^[A-Z_]{3,60}$/.test(e.message)?e.message:'LOCAL_CI_FAILED'}`);process.exitCode=1;}
