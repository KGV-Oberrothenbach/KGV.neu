// Executed from the repository root; static checks are not HTTP E2E evidence.
import {readFileSync} from 'node:fs';
import assert from 'node:assert/strict';
const read=p=>readFileSync(p,'utf8');
const src=read('KGV.Web/services/readings/reading-service.ts');
const page=read('KGV.Web/app/page.tsx');
const nav=read('KGV.Web/services/workspace/navigation-service.ts');
const sec=read('KGV.Core/Security/PermissionChecks.cs');
const sql=read('supabase/migrations/20261122190917_fr2_05_meter_reading_rfid_rights.sql');
assert.match(src,/isDirectCapture = permissions.canCaptureMeterReadings === true/);
assert.doesNotMatch(src,/isDirectCapture = permissions.canReadMeters/);
assert.match(page,/canCaptureMeterReadings=\{hasRight\('ablesung.erfassen'\)\}/);
assert.match(nav,/capabilities.canManageRfid \? \[navigationConfig.groups.meters.items.rfidSetup\]/);
assert.match(sec,/FachrechteV2.AblesungEinstellungen/);
assert.match(sec,/FachrechteV2.RfidVerwalten/);
assert.match(sql,/tr_fr205_restrict_rfid_column_change/);
assert.match(sql,/ap02_has_effective_right\('rfid.verwalten'\)/);
console.log('FR205_STATIC_CONTRACT_PASS');
