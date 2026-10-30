import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import test from "node:test";
const root=resolve(import.meta.dirname,"../../.."); const editor=readFileSync(resolve(root,"KGV.Web/features/serienbrief/SerienbriefEditor.tsx"),"utf8"); const text=readFileSync(resolve(root,"KGV.Web/features/serienbrief/SerienbriefTextEditor.tsx"),"utf8");
test("G4 keeps the template local and constrains rich-text commands",()=>{assert.match(editor,/anredeModus:"briefanrede"/);assert.match(editor,/vorlagenName/);assert.match(editor,/betreff/);assert.match(editor,/htmlInhalt/);assert.doesNotMatch(editor,/supabase|fetch\(|writeSupabase/);for(const command of ["bold","italic","insertUnorderedList"])assert.match(text,new RegExp(command));});
