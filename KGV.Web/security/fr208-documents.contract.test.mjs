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
  assert.match(edge,/fr208_archive_dokument_service/);
  assert.match(edge,/fr208_can_write_document_owner/);
  assert.doesNotMatch(frontend,/createSignedStorageUrl/);
  assert.match(frontend,/"kgv-upload-document"/);
});

test('Document effects cannot be silently overwritten via type-only RPC',()=>{
  assert.match(sql,/v_effects_changed AND NOT public\.ap02_has_effective_right\('dokumentwirkung\.verwalten'\)/);
  assert.match(sql,/fr208_save_dokumenttyp_wirkungen/);
  assert.match(read('KGV.Web/features/documents-management/DocumentTypeManagement.tsx'),/canManageEffects/);
});

test('Archive requires password and service-role-only transactional RPC', () => {
  const edge = read('supabase/functions/kgv-upload-document/index.ts');
  const archive = edge.slice(edge.indexOf('if (action === "archive")'), edge.indexOf('if (action === "reserve_historical_contract_number")'));
  assert.ok(archive.indexOf('verifyArchivePassword(password)') >= 0);
  assert.ok(archive.indexOf('verifyArchivePassword(password)') < archive.indexOf('fr208_archive_dokument_service'));
  assert.match(archive, /supabaseAdmin[\s\S]*?\.rpc\("fr208_archive_dokument_service"/);
  assert.doesNotMatch(archive, /\.from\("dokument"\)[\s\S]*?\.update\(/);
  assert.match(sql, /REVOKE ALL ON FUNCTION public\.fr208_archive_dokument_service\(bigint,uuid,text\)[\s\S]*?FROM PUBLIC,anon,authenticated/);
  assert.match(sql, /GRANT EXECUTE ON FUNCTION public\.fr208_archive_dokument_service\(bigint,uuid,text\)[\s\S]*?TO service_role/);
  assert.match(sql, /SELECT d\.\* INTO v_d[\s\S]*?FOR UPDATE/);
  assert.match(sql, /set_config\('request\.jwt\.claim\.sub',p_actor::text,true\)/);
});

test('Maintenance RLS drops permissive policy and verifies both object scopes', () => {
  assert.match(sql, /DROP POLICY IF EXISTS dokument_wartungsvertrag_zuordnung_read_effective/);
  assert.match(sql, /CREATE POLICY dokument_wartungsvertrag_zuordnung_read_fr208[\s\S]*?fr208_can_read_maintenance_document_link/);
  assert.match(sql, /JOIN public\.wartungsvertrag_zuordnungen wz ON wz\.id=p_zuordnung_id/);
  assert.match(sql, /d\.id=p_dokument_id/);
  assert.match(sql, /ap02_can_read_mitglied\(wz\.hauptmitglied_id\)/);
});

test('Historical effect mutations use shared document-type locks and expected revisions', () => {
  assert.match(sql, /CREATE TRIGGER fr208_lock_document_type_usage[\s\S]*?BEFORE INSERT OR UPDATE OF dokumenttyp_id/);
  assert.match(sql, /FOR SHARE/);
  assert.match(sql, /FOR UPDATE/);
  assert.match(sql, /ARRAY\[OLD\.dokumenttyp_id,NEW\.dokumenttyp_id\]/);
  assert.match(sql, /fr208_set_dokumenttyp_wirkungen_checked/);
  assert.match(sql, /v_existing IS DISTINCT FROM v_expected/);
  assert.match(read('KGV.Web/repositories/documents/document-type-repository.ts'), /p_expected_wirkungen/);
});

test('Type usage is queried through a restricted boolean RPC', () => {
  const repository = read('KGV.Web/repositories/documents/document-type-repository.ts');
  const ui = read('KGV.Web/features/documents-management/DocumentTypeManagement.tsx');
  assert.match(repository, /fr208_dokumenttyp_ist_verwendet/);
  assert.doesNotMatch(repository, /"dokument", \{ select: "id"/);
  assert.match(ui, /const effectsLocked = !canManageEffects \|\| inUse !== false/);
  assert.match(ui, /saveManagedDocumentEffects\(club, session, typeId, draft\.wirkungen, originalEffects\)/);
});

test('Registered document deletion is blocked before any file deletion', () => {
  const infra = read('KGV.Infrastructure/Services/SupabaseService.cs');
  const deletion = infra.slice(infra.indexOf('public Task<DokumentDeleteResult> DeleteDokumentAsync'), infra.indexOf('public async Task<DokumentDeleteResult> ArchiveDokumentAsync'));
  assert.match(deletion, /PHYSICAL_DELETE_DISABLED/);
  assert.doesNotMatch(deletion, /DeleteDokumentFromDriveAsync|\.Delete\(/);
  assert.doesNotMatch(infra, /HttpMethod\.Delete[\s\S]{0,100}DokumentUploadFunctionName/);
  assert.match(sql, /CREATE TRIGGER fr208_block_document_delete/);
  assert.match(sql, /CREATE TRIGGER fr208_guard_pacht_attachment/);
  assert.match(sql, /CREATE TRIGGER fr208_guard_maintenance_attachment/);
});

test('Failed upload compensation verifies file is not already registered', () => {
  const edge = read('supabase/functions/kgv-upload-document/index.ts');
  const catchBlock = edge.slice(edge.lastIndexOf('if (uploadedDriveFileId)'));
  assert.ok(catchBlock.indexOf('.eq("drive_file_id", uploadedDriveFileId)') >= 0);
  assert.ok(catchBlock.indexOf('.eq("drive_file_id", uploadedDriveFileId)') < catchBlock.indexOf('deleteDriveFile(await getGoogleAccessToken()'));
  assert.match(catchBlock, /if \(registered\) \{/);
});

test('Club document storage cannot delete registered revisions via RLS-blind orphan check', () => {
  const cleanup = sql.slice(sql.indexOf('-- A permissive Storage DELETE'));
  assert.match(cleanup, /DROP POLICY IF EXISTS vereinsunterlagen_cleanup_unregistered_fr208 ON storage\.objects/);
  assert.doesNotMatch(cleanup, /CREATE POLICY vereinsunterlagen_cleanup_unregistered_fr208/);
});
