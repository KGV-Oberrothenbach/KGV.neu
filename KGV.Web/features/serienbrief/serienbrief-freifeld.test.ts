import assert from "node:assert/strict";
import test from "node:test";
import { serienbriefFreifeldFehler, serienbriefKeyVorschlag, serienbriefPlaceholderErsetzen, serienbriefPlaceholderVerwendet, type SerienbriefFreifeld } from "./serienbrief-freifeld.ts";

const existing:SerienbriefFreifeld[]=[{schluessel:"frist",anzeigename:"Frist",typ:"date",pflicht:true,sortierung:1}];
const valid={anzeigename:"Hinweis",schluessel:"hinweis",typ:"text" as const,pflicht:false};

test("supports text, date and number free fields with normalized editable keys",()=>{
  assert.equal(serienbriefKeyVorschlag("Letzter Termin"),"letzter_termin");
  assert.equal(serienbriefKeyVorschlag("Betrag €"),"betrag");
  for(const typ of ["text","date","number"] as const)assert.equal(serienbriefFreifeldFehler({...valid,typ},["vorname"],existing),null);
});

test("rejects malformed, duplicate, export and reserved free field keys",()=>{
  assert.match(serienbriefFreifeldFehler({...valid,schluessel:"Frist"},[],existing)??"",/Kleinbuchstaben/);
  assert.match(serienbriefFreifeldFehler({...valid,schluessel:"frist"},[],existing)??"",/bereits als freies Feld/);
  assert.match(serienbriefFreifeldFehler({...valid,schluessel:"vorname"},["vorname"],existing)??"",/Exportfeld/);
  assert.match(serienbriefFreifeldFehler({...valid,schluessel:"briefanrede"},[],existing)??"",/reserviert/);
});

test("renames used placeholders safely and detects deletion conflicts",()=>{
  const subject="Bitte {{frist}} beachten";
  const html="<p>{{frist}}</p>";
  assert.equal(serienbriefPlaceholderErsetzen(subject,"frist","letzter_termin"),"Bitte {{letzter_termin}} beachten");
  assert.equal(serienbriefPlaceholderErsetzen(html,"frist","letzter_termin"),"<p>{{letzter_termin}}</p>");
  assert.equal(serienbriefPlaceholderVerwendet(subject,"frist"),true);
  assert.equal(serienbriefPlaceholderVerwendet(html,"hinweis"),false);
});
