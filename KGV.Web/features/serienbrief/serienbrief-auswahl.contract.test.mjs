import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const start=readFileSync(new URL("./SerienbriefStart.tsx",import.meta.url),"utf8");
test("G7 keeps selection local to the full serial-letter context",()=>{
  assert.match(start,/useState<Set<string>>\(\(\)=>initialeSerienbriefAuswahl\(context\.datensaetze\)\)/);
  assert.match(start,/context\.datensaetze\.map/);
  assert.match(start,/disabled=\{!gueltig\}/);
  assert.match(start,/Alle auswählen/);
  assert.match(start,/Auswahl aufheben/);
  assert.match(start,/disabled=\{selectedRows\.length===0\}/);
  assert.doesNotMatch(start,/fetch\(|supabase/);
});
