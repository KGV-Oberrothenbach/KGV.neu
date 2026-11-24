import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { join, resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';

const root = resolve(dirname(fileURLToPath(import.meta.url)),'..','..');
const src = (p) => readFileSync(join(root,p),'utf8');
const migration = readdirSync(join(root,'supabase','migrations'))
  .filter((f)=>/^\d{14}_fr2_07_work_hours_assignment_rights_cutover\.sql$/.test(f));
assert.equal(migration.length,1,'FR2-07-Migration muss einmal vorhanden sein');
const sql=src(`supabase/migrations/${migration[0]}`);
const core=src('KGV.Core/Security/PermissionChecks.cs');
const web=src('KGV.Web/app/page.tsx');
const work=src('KGV.Web/services/work-hours/work-hours-service.ts');

const codeMethod=(name)=>{
  const index=core.indexOf(`bool ${name}(`);
  assert.ok(index>=0,`${name} fehlt`);
  return core.slice(index,core.indexOf(';',index)+1);
};

test('Vier effektive Fachrechte statt Legacybits',()=>{
  for(const [method,code] of [
    ['CanReadWorkHours','FachrechteV2.ArbeitsstundenLesen'],
    ['CanEditWorkHours','FachrechteV2.ArbeitsstundenBearbeiten'],
    ['CanApproveWorkHours','FachrechteV2.ArbeitsstundenFreigeben'],
    ['CanManageWorkAssignments','FachrechteV2.ArbeitseinsatzVerwalten'],
  ]){
    assert.match(codeMethod(method),new RegExp(code.replaceAll('.','\\.')));
    assert.doesNotMatch(codeMethod(method),/HasPermission\(context,\s*PermissionFlags/);
  }
});

test('Reines Lesen erteilt keine Bearbeitung oder Freigabe',()=>{
  assert.doesNotMatch(codeMethod('CanReadWorkHours'),/CanEditWorkHours|CanApproveWorkHours|CanManageWorkHours/);
  assert.match(codeMethod('CanManageWorkHours'),/CanEditWorkHours\(context\) && CanApproveWorkHours\(context\)/);
});

test('Web-Gates sind namentlich und separat',()=>{
  assert.match(web,/canApproveWorkHours=\{hasRight\("arbeitsstunden\.freigeben"\)\}/);
  assert.match(web,/canEditWorkHours=\{hasRight\("arbeitsstunden\.bearbeiten"\)\}/);
  assert.match(web,/canEdit=\{hasRight\("arbeitseinsatz\.verwalten"\)\}/);
  assert.doesNotMatch(web,/has\(Permission\.manageWorkAssignments\)/);
});

test('Administrative Erfassung bleibt offen ohne Freigabe',()=>{
  assert.match(work,/status:\s*canApproveWorkHours\s*\?\s*"genehmigt"\s*:\s*"offen"/);
  assert.match(work,/freigegeben:\s*canApproveWorkHours/);
  assert.match(work,/if \(!canEditWorkHours\)/);
  assert.match(work,/if \(\(input\.action === "korrigieren" \|\| input\.action === "loeschen"\) && !canEditWorkHours\)/);
});

test('RLS trennt Lesen, offene Bearbeitung, Freigabe und Verwaltung',()=>{
  for(const right of ['arbeitsstunden.lesen','arbeitsstunden.bearbeiten','arbeitsstunden.freigeben','arbeitseinsatz.verwalten'])
    assert.ok(sql.includes(right),`${right} fehlt`);
  assert.match(sql,/CREATE POLICY fr207_open_work_hours_admin_insert/);
  assert.match(sql,/CREATE POLICY fr207_open_work_hours_admin_update/);
  assert.match(sql,/CREATE POLICY fr207_open_work_hours_edit_read/);
  assert.doesNotMatch(sql,/CREATE POLICY[^;]*FOR ALL\b/i);
});

test('Review-RPC: Korrektur braucht beide Rechte und finaler Zustand ist nicht retrybar',()=>{
  const r=sql.slice(sql.indexOf('CREATE OR REPLACE FUNCTION public.review_arbeitsstunde('),
    sql.indexOf('REVOKE EXECUTE ON FUNCTION public.review_arbeitsstunde'));
  assert.match(r,/v_action IN \('korrigieren','loeschen'\)/);
  assert.match(r,/arbeitsstunden\.bearbeiten/);
  assert.match(r,/FOR UPDATE/);
  assert.match(r,/ERRCODE='23514'/);
  assert.doesNotMatch(r,/ERRCODE\s*=\s*'40001'/);
});

test('Einsatzstunden-RPCs sind eigenstaendig autorisiert, no duplicates',()=>{
  assert.match(sql,/CREATE OR REPLACE FUNCTION public.confirm_arbeitseinsatz_arbeitsstunde/);
  assert.match(sql,/app\.fr207_assignment_confirm/);
  assert.match(sql,/CREATE OR REPLACE FUNCTION public.submit_arbeitseinsatz_arbeitsstunde/);
  assert.match(sql,/RETURN v_row;/);
  assert.match(sql,/Einsatzstunden bereits abweichend erfasst/);
});

test('Audit und Historie sind nicht fuer Clients manipulierbar',()=>{
  assert.match(sql,/CREATE TABLE IF NOT EXISTS public\.fr207_arbeitsaudit/);
  assert.match(sql,/REVOKE ALL ON public\.fr207_arbeitsaudit FROM PUBLIC,anon,authenticated/);
  assert.match(sql,/trg_fr207_audit_arbeitsstunde/);
  assert.match(sql,/trg_fr207_audit_arbeitseinsatz/);
  assert.match(sql,/trg_fr207_einsatz_delete_guard/);
  assert.match(sql,/REVOKE TRUNCATE,TRIGGER,REFERENCES/);
});

test('Exporte sind doppelt berechtigt und fuer anon gesperrt',()=>{
  assert.match(sql,/rpc_export_arbeitsstunden_uebersicht/);
  assert.match(sql,/arbeitsstunden\.lesen/);
  assert.match(sql,/export\.ausfuehren/);
  assert.match(sql,/REVOKE EXECUTE ON FUNCTION public\.rpc_export_arbeitsstunden_uebersicht/);
});

test('FR2-07: Mitgliedsstatus wird RLS-unabhaengig geprueft', () => {
  const policies = sql.slice(
    sql.indexOf('DROP POLICY IF EXISTS fr207_open_work_hours_edit_read'),
    sql.indexOf('-- Gesperrte/abgelehnte/genehmigte Eintraege')
  );
  assert.ok(policies.length > 100);
  assert.doesNotMatch(policies, /EXISTS\s*\(SELECT 1 FROM public\.mitglied m/);

  const updateTrigger = sql.slice(
    sql.indexOf('CREATE OR REPLACE FUNCTION public.enforce_own_open_arbeitsstunde_update'),
    sql.indexOf('-- Administrative Sofortgenehmigung')
  );
  const insertTrigger = sql.slice(
    sql.indexOf('CREATE OR REPLACE FUNCTION public.enforce_administrative_arbeitsstunde_insert'),
    sql.indexOf('-- Berechtigter Teilnehmer')
  );

  assert.match(updateTrigger, /WHERE m\.id=OLD\.mitglied_id AND m\.aktiv IS TRUE/);
  assert.match(insertTrigger, /WHERE m\.id=NEW\.mitglied_id AND m\.aktiv IS TRUE/);
  assert.match(updateTrigger, /SECURITY DEFINER/);
  assert.match(insertTrigger, /SECURITY DEFINER/);
});