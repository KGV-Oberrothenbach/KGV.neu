import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const root = new URL('../../', import.meta.url);
const load = (relative) => readFileSync(new URL(relative, root), 'utf8');

test('FR2-04.5 historical contract authorization fails closed without JWT role', () => {
  const dir = fileURLToPath(new URL('../../supabase/migrations/', import.meta.url));
  const names = readdirSync(dir).filter(name => /^\d{14}_fr2_04_historical_rights_fail_closed\.sql$/.test(name));
  assert.equal(names.length, 1, 'exactly one versioned migration');
  const sql = readFileSync(new URL('../../supabase/migrations/' + names[0], import.meta.url), 'utf8');
  assert.match(sql, /CREATE OR REPLACE FUNCTION public\.can_register_historical_contracts\(\)/);
  assert.match(sql, /COALESCE\([\s\S]*auth\.role\(\) = 'service_role'/);
  assert.match(sql, /auth\.role\(\) = 'authenticated'[\s\S]*ap02_has_effective_right\('historische_vertraege\.erfassen'\)/);
  assert.match(sql, /ap02_has_effective_right\('historische_vertraege\.erfassen'\)/);
  assert.match(sql, /REVOKE EXECUTE ON FUNCTION public\.can_register_historical_contracts\(\) FROM PUBLIC, anon/);
  assert.match(sql, /REVOKE EXECUTE ON FUNCTION public\.erstelle_pacht_nachsorge\(bigint,date\) FROM PUBLIC, anon/);
});

test('FR2-04.5 Web/MAUI keep mapped historical Fachrecht', () => {
  const web = load('KGV.Web/security/fachrechte-v2-compatibility.ts');
  const maui = load('KGV.Core/Security/FachrechteV2EffectiveMask.cs');
  assert.match(web, /Permission\.registerHistoricalContracts,\s*\["historische_vertraege\.erfassen"\]/);
  assert.match(maui, /PermissionFlags\.CanRegisterHistoricalContracts,\s*"historische_vertraege\.erfassen"/);
});
