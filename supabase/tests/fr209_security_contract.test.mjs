import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
const read=(p)=>readFileSync(new URL('../../'+p, import.meta.url),'utf8');
const matching=readdirSync(new URL('../migrations/', import.meta.url)).filter(name => /^[0-9]{14}_fr209_vorgaenge_kommunikation\.sql$/.test(name));
assert.equal(matching.length,1,'exactly one FR2-09 migration');
const sql=read('supabase/migrations/'+matching[0]);

test('FR2-09: separate read/write/claim rights and no table DELETE/TRUNCATE',()=>{
  for(const term of ['vorgang.lesen','vorgang.bearbeiten','vereinsdaten.alle_lesen','fr209_kommunikation_read','fr209_kommunikation_update','fr209_claim_kommunikationsvorgang','fr209_release_kommunikationsvorgang','REVOKE DELETE, TRUNCATE'])
    assert.ok(sql.includes(term),term);
  assert.ok(sql.includes('RETURN QUERY UPDATE public.kommunikationsvorgang'));
  assert.ok(sql.includes('RETURN;'));
});
test('FR2-09: snapshot RLS has per-object guard',()=>{
  for(const t of ['versandinstanz_dokument_snapshot','versandinstanz_empfaenger_snapshot','vertragsversand_anlage_snapshot','vertragsversand_vertragspartei_snapshot'])
    assert.ok(sql.includes(t),t);
  assert.ok(sql.includes('fr209_can_read_dispatch_instance'));
  assert.ok(sql.includes('fr209_can_read_dispatch_communication'));
});
test('FR2-09: source moves off legacy communication bits',()=>{
  const core=read('KGV.Core/Security/PermissionChecks.cs');
  assert.match(core,/CanManageCommunicationWorkflows\(UserContext\? context\)[\s\S]{0,120}FachrechteV2\.VorgangBearbeiten/);
  const web=read('KGV.Web/security/serienbrief-access.ts');
  assert.ok(web.includes('Permission.CanCreateSerialLetters'));
  assert.ok(!web.includes('CanManageCommunicationWorkflows'));
  const bridge=read('KGV.Web/security/fachrechte-v2-compatibility.ts');
  assert.ok(bridge.includes('Permission.createSerialLetters, ["serienbrief.erstellen"]'));
});
test('FR2-09: Web and MAUI use atomic claim/release RPC',()=>{
  const web=read('KGV.Web/repositories/communication/communication-repository.ts');
  const maui=read('KGV.Infrastructure/Services/PreparedContractRepository.cs');
  for(const code of ['fr209_claim_kommunikationsvorgang','fr209_release_kommunikationsvorgang']){
    assert.ok(web.includes(code));assert.ok(maui.includes(code));
  }
});
