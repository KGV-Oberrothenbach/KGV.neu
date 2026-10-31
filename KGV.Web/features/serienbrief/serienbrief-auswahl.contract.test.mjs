import assert from "node:assert/strict";import { readFileSync } from "node:fs";import test from "node:test";
const start=readFileSync(new URL("./SerienbriefStart.tsx",import.meta.url),"utf8");
test("G7 keeps selection local to the full serial-letter context",()=>{assert.match(start,/initialeSerienbriefAuswahl\(context\.datensaetze\)/);assert.match(start,/context\.datensaetze\.map/);assert.match(start,/disabled=\{!ok\}/);assert.match(start,/Alle auswählen/);assert.match(start,/Auswahl aufheben/);});
