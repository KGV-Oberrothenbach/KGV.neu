import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const read=(file)=>readFileSync(new URL(file,import.meta.url),"utf8");

test("groups metadata fields and keeps the active editor cursor while selecting a field",()=>{
  const catalog=read("./SerienbriefFeldkatalog.tsx");
  assert.match(catalog,/sort\(\(a,b\)=>a\.sortierung-b\.sortierung\)/);
  assert.match(catalog,/feld\.gruppe\?\.trim\(\)\|\|"Sonstiges"/);
  assert.match(catalog,/onMouseDown=\{event=>event\.preventDefault\(\)\}/);
  assert.doesNotMatch(catalog,/mitglied_id|parzelle_id|serienbrief_empfaenger_status/);
});

test("inserts stable placeholders into subject or HTML text without interpreting them",()=>{
  const editor=read("./SerienbriefEditor.tsx");
  const textEditor=read("./SerienbriefTextEditor.tsx");
  assert.match(editor,/serienbriefPlaceholder\(feld\.schluessel\)/);
  assert.match(editor,/target==="betreff"/);
  assert.match(editor,/textRef\.current\?\.insert\(placeholder\)/);
  assert.match(textEditor,/document\.execCommand\("insertText",false,placeholder\)/);
  assert.match(textEditor,/normalizeSerienbriefPlaceholderHtml/);
});
