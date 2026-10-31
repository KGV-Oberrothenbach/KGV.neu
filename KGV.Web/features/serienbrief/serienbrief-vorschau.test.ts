import assert from "node:assert/strict";
import test from "node:test";
import { briefanrede, fehlendePflichtFreifelder, formatFreifeld, resolveSerienbriefText } from "./serienbrief-vorschau.ts";
import type { SerienbriefEditorState } from "./SerienbriefEditor.tsx";

const vorlage:SerienbriefEditorState={vorlagenName:"Test",betreff:"Zähler {{zaehlernummer}} bis {{frist}}",anredeModus:"briefanrede",htmlInhalt:"<p>{{hinweis}} {{unbekannt}}</p>",freieFelder:[{schluessel:"frist",anzeigename:"Frist",typ:"date",pflicht:true,sortierung:1},{schluessel:"hinweis",anzeigename:"Hinweis",typ:"text",pflicht:false,sortierung:2},{schluessel:"betrag",anzeigename:"Betrag",typ:"number",pflicht:false,sortierung:3}]};
const row={mitgliedId:1,parzelleId:2,empfaengerStatus:"hauptpaechter" as const,werte:{zaehlernummer:"21426611",parzelle:"17"}};

test("resolves export and free-field placeholders while reporting unknown and missing values",()=>{
  assert.deepEqual(resolveSerienbriefText(vorlage.betreff,row,vorlage,{frist:"2027-03-31"}),{text:"Zähler 21426611 bis 31.03.2027",fehler:[]});
  const result=resolveSerienbriefText(vorlage.htmlInhalt,row,vorlage,{hinweis:"<b>sicher</b>"},true);
  assert.equal(result.text,"<p>&lt;b&gt;sicher&lt;/b&gt; [FEHLT: unbekannt]</p>");
  assert.match(result.fehler.join(" "),/Unbekannter Platzhalter/);
});

test("formats free values and validates required values",()=>{
  assert.equal(formatFreifeld("2027-03-31","date"),"31.03.2027");
  assert.equal(formatFreifeld("1234.5","number"),"1.234,5");
  assert.deepEqual(fehlendePflichtFreifelder(vorlage,{}),["Frist"]);
});

test("uses explicit salutation data only and otherwise falls back without name guessing",()=>{
  assert.equal(briefanrede({id:1,vorname:"Erika",name:"Beispiel",adresse:null,plz:null,ort:null,anrede:"frau"}),"Sehr geehrte Frau Beispiel,");
  assert.equal(briefanrede({id:2,vorname:"Max",name:"Mustermann",adresse:null,plz:null,ort:null,anrede:"herr"}),"Sehr geehrter Herr Mustermann,");
  assert.equal(briefanrede({id:3,vorname:"Alex",name:"Muster",adresse:null,plz:null,ort:null}),"Sehr geehrte/r Gartenfreund/in Alex Muster,");
});
