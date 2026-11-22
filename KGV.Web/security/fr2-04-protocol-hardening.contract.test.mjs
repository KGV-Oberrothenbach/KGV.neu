import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import test from 'node:test';
const root = new URL('../../', import.meta.url);
const load = (p) => readFileSync(new URL(p, root), 'utf8');

test('FR2-04.4b: MAUI cannot finalize a protocol via direct table UPDATE', () => {
  const source = load('KGV.Infrastructure/Services/SupabaseService.cs');
  const section = source.slice(source.indexOf('public Task<bool> CompleteParzellenProtokollAsync('));
  assert.match(section.slice(0, 1000), /\.Rpc\("complete_parzellen_protokoll"/);
  assert.doesNotMatch(section.slice(0, 1000), /\.Set\(x => x\.Status, "abgeschlossen"\)/);
  assert.match(section.slice(0, 1100), /bool\.TryParse\(response\.Content/);
});

test('FR2-04.4b: preview cannot complete or upload a protocol document', () => {
  const edge = load('supabase/functions/kgv-generate-contract/index.ts');
  assert.match(edge, /type === "parzellenprotokoll" && action !== "finalize"\)/);
  assert.match(edge, /if \(protocolIdToComplete\) \{/);
});

test('FR2-04.4b: migration explicitly restricts protocol and aftercare operations', () => {
  const dir = new URL('supabase/migrations/', root);
  const matching = readdirSync(dir).filter(n => /^\d{14}_fr2_04_protocol_guards\.sql$/.test(n));
  assert.equal(matching.length, 1, 'expected exactly one new versioned protocol migration');
  const sql = readFileSync(new URL(matching[0], dir),'utf8');
  assert.match(sql,/CREATE FUNCTION public\.complete_parzellen_protokoll/);
  assert.match(sql,/CREATE TRIGGER trg_fr2_04_guard_direct_protokoll_mutation/);
  assert.match(sql,/CREATE TRIGGER trg_fr2_04_guard_protocol_reading/);
  assert.match(sql,/REVOKE DELETE, TRUNCATE, REFERENCES, TRIGGER/);
  assert.match(sql,/GRANT EXECUTE ON FUNCTION public\.complete_parzellen_protokoll\(bigint,bigint\) TO authenticated/);
  assert.match(sql,/SELECT \* INTO n FROM public\.pacht_nachsorge WHERE id=p_nachsorge_id FOR UPDATE;[\s\S]{0,100}IF NOT FOUND/);
});
