import assert from 'node:assert/strict';
import test from 'node:test';
import {readFileSync} from 'node:fs';
import {buildLeaseV10Fields} from '../../supabase/functions/kgv-generate-contract/pachtvertrag-v10-fields.mjs';
const read = p => readFileSync(new URL('../../'+p,import.meta.url),'utf8');
const spec = (key,ver) => JSON.parse(read(`KGV.Core/Templates/${key}/${ver}/field-spec.json`));
const edge = read('supabase/functions/kgv-generate-contract/index.ts');
const renderer = read('supabase/functions/kgv-generate-contract/pdf-rendering-core.mjs');
const base={area:123.45,rate:.25,annual:30.86,primaryAddress:'Musterweg 1, 08000 Musterstadt',secondaryAddress:'Musterweg 2, 08000 Musterstadt',hasSecondParty:false,isRepresentative:false};
test('v10 monetary fields and both area fields are numeric, with no duplicated printed units',()=>{
 const v=buildLeaseV10Fields(base);assert.equal(v.flaeche_qm,'123,45');assert.equal(v.flaeche_qm_wiederholung,'123,45');assert.equal(v.pacht_pro_qm_zahl,'0,25');assert.equal(v.pachtzins_jahr_zahl,'30,86');
 for(const k of ['flaeche_qm','flaeche_qm_wiederholung','pacht_pro_qm_zahl','pachtzins_jahr_zahl'])assert.doesNotMatch(v[k],/EUR|€|m²/);
});
test('guardian, second tenant and sole tenant are distinct',()=>{
 const a=buildLeaseV10Fields(base);const b=buildLeaseV10Fields({...base,hasSecondParty:true});const c=buildLeaseV10Fields({...base,hasSecondParty:true,isRepresentative:true});
 assert.equal(a.zweite_partei_rolle,'Keine zweite Partei');assert.equal(a.zweite_partei_signatur_rolle,'');assert.equal(b.zweite_partei_rolle,'Mitpächter/in 2');assert.equal(c.zweite_partei_rolle,'Gesetzl. Vertretung');assert.equal(c.zweite_partei_anschrift,'Musterweg 2\n08000 Musterstadt');assert.throws(()=>buildLeaseV10Fields({...base,isRepresentative:true}));
});
test('all new lease fields are required by the Edge descriptor, v10 uses persistent signatures',()=>{
 const line=edge.match(/const leaseRequiredFieldsV10 = (\[.*\]);/)[1];assert.deepEqual(JSON.parse(line),spec('pachtvertrag','v10').fields.map(f=>f.name));assert.match(edge,/leaseFieldSpecV9, leaseFieldSpecV10\]\.includes\(template\.fieldSpecVersion\)/);assert.match(edge,/loadPersistentLeaseV4Signatures\(auth, sourceKommunikationsvorgangId, memberId, parcelId\)/);
});
test('new template versions do not receive legacy geometry overlays',()=>{
 assert.match(renderer,/field\.name === "arbeitsstundenwirkung" && field\.page === 1 && field\.rect\.y === 615/);assert.match(renderer,/if \(!specs\.some\(\(field\) => field\.name === "zweite_partei_rolle"\)\) drawSecondPrivatePartyLabels/);
 assert.equal(spec('wartungsvertrag','v3').fields.find(f=>f.name==='arbeitsstundenwirkung').rect.y,688);
});
test('new maintenance descriptors require every field, including canonical contract number',()=>{
 for(const key of ['wartungsvertrag','wartungsvertrag-verlaengerung']){const match=edge.match(new RegExp(`"${key}-field-spec/v3": (\\[.*?\\]) }`));assert.ok(match);assert.deepEqual(JSON.parse(match[1]),spec(key,'v3').fields.map(f=>f.name));assert.ok(JSON.parse(match[1]).includes('vertragsnummer'));}
});
test('old templates remain supported and unintegrated membership v6 cannot accidentally become active',()=>{
 assert.match(edge,/const membershipApplicationTemplate:.*membershipApplicationFieldSpecV5/);assert.doesNotMatch(edge,/mitgliedsantrag-field-spec\/v6/);
 const core=read('KGV.Core/Utilities/PachtvertragDokumentFactory.cs');assert.match(core,/"pachtvertrag-field-spec\/v9" => FieldSpecs/);assert.match(core,/"pachtvertrag-field-spec\/v10" => FieldSpecsV10/);
 assert.match(read('KGV.Infrastructure/Services/SupabasePdfTemplateProvider.cs'),/request\.SupportsFieldSpecVersion\(record\.FieldSpecVersion\)/);
});
