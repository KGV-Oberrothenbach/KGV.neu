import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
const read=p=>readFileSync(p,'utf8');
const sql=read('supabase/migrations/20261124192619_fr209_vorgaenge_kommunikation.sql');
const contract=read('supabase/functions/kgv-send-contract-documents/index.ts');
const club=read('supabase/functions/kgv-send-club-documents/index.ts');
const web=read('KGV.Web/repositories/communication/communication-repository.ts');
test('Direct assignment privilege is removed, claim and release check current rights',()=>{
 assert.match(sql,/REVOKE UPDATE ON public\.kommunikationsvorgang FROM PUBLIC,anon,authenticated/);
 assert.match(sql,/GRANT UPDATE \(entwurf_daten,revision,status,final_dokument_id,gedruckt_am\)/);
 assert.match(sql,/v\.zugewiesen_an IS NULL AND v\.status NOT IN/);
 assert.match(sql,/v\.zugewiesen_an=auth\.uid\(\) AND v\.status NOT IN/);
});
test('Reservation before SMTP, uncertain result never automatically reclaimed',()=>{
 assert.match(sql,/reservation_key text PRIMARY KEY/);
 assert.match(sql,/f\.state<>'confirmed'/);
 assert.match(sql,/REVOKE ALL ON public\.fr209_mail_reservation FROM PUBLIC,anon,authenticated/);
 for(const [source,name] of [[contract,'fr209_reserve_contract_mail'],[club,'fr209_reserve_club_mail']]){
  assert.ok(source.indexOf(name)>0 && source.indexOf(name)<source.indexOf('transport.sendMail'));
  assert.match(source,/state: "unknown"/);
  assert.match(source,/SMTP_TEST_RECIPIENT/);
 }
 assert.match(contract,/fr209_claim_contract_dispatch/);
 assert.match(club,/caller\.from\("mitglied"\)/);
 assert.match(club,/caller\.from\("vereinsdokument_fassung"\)/);
});
test('Response identity, sensitive document rights and immutable final binding',()=>{
 assert.equal((web.match(/rows\[0\]\.id !== id/g)||[]).length,2);
 assert.match(sql,/NOT p_sensitive OR public\.ap02_has_effective_right\('dokument\.lesen'\)/);
 assert.match(sql,/d\.source_kommunikationsvorgang_id=OLD\.id AND d\.source_revision=OLD\.signatur_revision/);
});
