import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const read=(p)=>readFileSync(path.join(root,p),'utf8');
const migrationFiles=readdirSync(path.join(root,'supabase/migrations')).filter(n=>/_fr2_08_document_security_cutover\.sql$/.test(n));
assert.equal(migrationFiles.length,1,'FR2-08 migration must exist once');
const sql=read('supabase/migrations/'+migrationFiles[0]);

test('Core authority is exclusive to effective Fachrechte',()=>{
  const s=read('KGV.Core/Security/PermissionChecks.cs');
  for(const right of ['DokumentLesen','DokumentVerwalten','DokumentArchivieren','DokumenttypVerwalten','DokumentwirkungVerwalten','VorlageVerwalten'])
    assert.match(s,new RegExp('FachrechteV2\\.'+right));
  assert.doesNotMatch(s,/CanReadDocuments\(UserContext\? context\)[\s\S]{0,110}CanManageDocuments\(context\)/);
});

test('RLS separates SELECT from INSERT/UPDATE and preserves narrow historical reads',()=>{
  const readPolicy=sql.match(/CREATE POLICY dokument_select_effective[\s\S]*?\);/)[0];
  assert.match(readPolicy,/dokument\.lesen/);
  assert.doesNotMatch(readPolicy,/dokument\.verwalten/);
  for(const fn of ['can_access_own_member_history_document','can_access_own_historical_lease_document']) assert.ok(sql.includes(fn));
  assert.match(sql,/fr208_can_archive_dokument/);
  assert.match(sql,/REVOKE INSERT, UPDATE, DELETE ON TABLE public\.dokument/);
});

test('Private storage does not use legacy admin/board policy predicates',()=>{
  assert.match(sql,/DROP POLICY IF EXISTS pdf_templates_read_admin/);
  assert.match(sql,/DROP POLICY IF EXISTS vereinsunterlagen_read_admin_vorstand/);
  assert.match(sql,/vorlage\.verwalten/);
  assert.match(sql,/vereinsunterlagen\.verwalten/);
  assert.doesNotMatch(sql,/CREATE POLICY[^;]*current_app_user_context\(\)/);
});

test('Document downloads use caller JWT + row security and not browser signed URLs',()=>{
  const edge=read('supabase/functions/kgv-upload-document/index.ts');
  const frontend=read('KGV.Web/repositories/documents/document-file-repository.ts');
  assert.match(edge,/async function mayReadDocument[\s\S]*?auth\.supabaseUser\.from\("dokument"\)/);
  assert.match(edge,/fr208_can_archive_dokument/);
  assert.match(edge,/fr208_can_write_document_owner/);
  assert.doesNotMatch(frontend,/createSignedStorageUrl/);
  assert.match(frontend,/"kgv-upload-document"/);
});

test('Document effects cannot be silently overwritten via type-only RPC',()=>{
  assert.match(sql,/v_effects_changed AND NOT public\.ap02_has_effective_right\('dokumentwirkung\.verwalten'\)/);
  assert.match(sql,/fr208_save_dokumenttyp_wirkungen/);
  assert.match(read('KGV.Web/features/documents-management/DocumentTypeManagement.tsx'),/canManageEffects/);
});
