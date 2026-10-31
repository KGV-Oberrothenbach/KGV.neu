import assert from "node:assert/strict";
import test from "node:test";
import { ausgewaehlteSerienbriefDatensaetze, initialeSerienbriefAuswahl, serienbriefAuswahlZaehler, serienbriefDatensatzSchluessel } from "./serienbrief-auswahl.ts";
import type { SerienbriefExportDatensatz } from "../../models/serienbrief/serienbrief-export-context.ts";

const rows:SerienbriefExportDatensatz[]=[
  {mitgliedId:7,parzelleId:11,empfaengerStatus:"hauptpaechter",werte:{parzelle:"12",zaehlernummer:"123"}},
  {mitgliedId:7,parzelleId:11,empfaengerStatus:"hauptpaechter",werte:{parzelle:"12",zaehlernummer:"456"}},
  {mitgliedId:null,parzelleId:22,empfaengerStatus:"mehrdeutig",werte:{parzelle:"22",zaehlernummer:"789"}}
];

test("initially selects every valid row while leaving recipient conflicts out",()=>{
  const selection=initialeSerienbriefAuswahl(rows);
  assert.deepEqual(serienbriefAuswahlZaehler(rows,selection),{gesamt:3,gueltig:2,ausgewaehlt:2,konflikte:1});
  assert.equal(selection.has(serienbriefDatensatzSchluessel(rows[2])),false);
});

test("uses stable row content identity so equal members remain independently selectable",()=>{
  const first=serienbriefDatensatzSchluessel(rows[0]),second=serienbriefDatensatzSchluessel(rows[1]);
  assert.notEqual(first,second);
  const selection=new Set([second]);
  assert.deepEqual(ausgewaehlteSerienbriefDatensaetze(rows,selection),[rows[1]]);
  assert.deepEqual(serienbriefAuswahlZaehler(rows,selection),{gesamt:3,gueltig:2,ausgewaehlt:1,konflikte:1});
});

test("select all uses the complete supplied result set and never selects conflicts",()=>{
  const selection=initialeSerienbriefAuswahl(rows);
  const none=new Set<string>();
  assert.equal(ausgewaehlteSerienbriefDatensaetze(rows,none).length,0);
  assert.equal(ausgewaehlteSerienbriefDatensaetze(rows,selection).length,2);
});
