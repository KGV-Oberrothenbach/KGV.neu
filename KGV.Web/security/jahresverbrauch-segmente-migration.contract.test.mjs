import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const files = [
  new URL("../../supabase/migrations/20261011120000_rpc_jahresverbrauch_segmente.sql", import.meta.url),
  new URL("../../supabase/migrations/20261011121000_fix_jahresverbrauch_segmente_ambiguity.sql", import.meta.url),
];
for (const file of files) {
  const source = await readFile(file, "utf8");
  assert.match(source, /rpc_jahresverbrauch_segmente\(p_jahr integer\)|rpc_jahresverbrauch_segmente\(integer\)/);
  assert.match(source, /segment_end_ablesung_id/);
  assert.match(source, /segment_endstand/);
  assert.match(source, /segment_end_fehlercode/);
  assert.match(source, /segment_end_art/);
  assert.doesNotMatch(source, /lead\(bp\.ablesung_id\) over w as end_ablesung_id/);
  assert.doesNotMatch(source, /lead\(bp\.stand\) over w as endstand/);
  assert.doesNotMatch(source, /lead\(bp\.fehlercode\) over w as end_fehlercode/);
}
const base = await readFile(files[0], "utf8");
assert.match(base, /r\.segment_end_ablesung_id as end_ablesung_id/);
assert.match(base, /r\.segment_endstand as endstand/);
