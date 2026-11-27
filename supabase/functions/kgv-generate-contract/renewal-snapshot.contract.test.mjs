import { readFileSync } from 'node:fs';
import assert from 'node:assert/strict';
import test from 'node:test';

const source = readFileSync(new URL('./index.ts', import.meta.url), 'utf8');
const guard = 'if (Number(draft.mitglied_id) !== memberId || !safe(draft.template_key) || !safe(draft.template_version)) throw new Error("Der unveränderliche Verlängerungssnapshot ist unvollständig.");';
const safe = value => typeof value === 'string' ? value.trim() : '';
test('renewal uses the canonical immutable database snapshot field', () => {
  const start = source.indexOf('} else if (type === "wartungsvertrag_verlaengerung") {');
  const end = source.indexOf('} else if (type === "wartungsvertrag") {', start);
  assert.ok(start >= 0 && end > start);
  const block = source.slice(start, end);
  assert.equal(block.split(guard).length, 2);
  assert.doesNotMatch(block, /Number\(draft\.member_id\)/);
  const verify = new Function('draft', 'memberId', 'safe', `${guard}\nreturn true;`);
  const fixture = { mitglied_id: 990000420, template_key: 'wartungsvertrag-verlaengerung', template_version: 'v1' };
  assert.equal(verify(fixture, 990000420, safe), true);
  assert.equal(verify({...fixture, mitglied_id: '990000420'}, 990000420, safe), true);
  for (const draft of [{...fixture, mitglied_id: 1}, {member_id: 990000420, template_key: fixture.template_key, template_version: 'v1'}, {...fixture, template_key: ' '}, {...fixture, template_version: null}]) {
    assert.throws(() => verify(draft, 990000420, safe), /Verlängerungssnapshot/);
  }
});

test('wrong process/member scope returns 403 and invalid workflow state returns 409', () => {
  const start = source.indexOf('} else if (type === "wartungsvertrag_verlaengerung") {');
  const end = source.indexOf('} else if (type === "wartungsvertrag") {', start);
  assert.ok(start >= 0 && end > start);
  const block = source.slice(start, end);
  const scope = block.split('\n').find(line => line.includes('Number(process.mitglied_id) !== memberId'));
  const state = block.split('\n').find(line => line.includes('!["vorbereitet", "erledigt"].includes(process.status)'));
  assert.ok(scope?.includes('return errorResponse(403, "FORBIDDEN"'));
  assert.ok(state?.includes('return errorResponse(409, "CONFLICT"'));
  const check = new Function('process', 'memberId', 'errorResponse', 'requestId', `${scope}\n${state}\nreturn 200;`);
  const proc = { vorgangstyp: 'dokument_erzeugung', dokumenttyp: 'wartungsvertrag_verlaengerung', mitglied_id: 990000420, status: 'vorbereitet' };
  const respond = code => code;
  assert.equal(check(proc, 990000420, respond, 'synthetic'), 200);
  assert.equal(check({...proc, status: 'erledigt'}, 990000420, respond, 'synthetic'), 200);
  assert.equal(check(proc, 990000401, respond, 'synthetic'), 403);
  assert.equal(check(null, 990000420, respond, 'synthetic'), 403);
  assert.equal(check({...proc, dokumenttyp: 'mitgliedsantrag'}, 990000420, respond, 'synthetic'), 403);
  assert.equal(check({...proc, status: 'abgebrochen'}, 990000420, respond, 'synthetic'), 409);
});
