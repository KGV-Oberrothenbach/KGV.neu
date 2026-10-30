import assert from "node:assert/strict";
import test from "node:test";
import { insertSerienbriefPlaceholder, normalizeSerienbriefPlaceholderHtml, serienbriefPlaceholder } from "./serienbrief-placeholder.ts";

test("creates stable placeholders and inserts them at the selected subject range",()=>{
  assert.equal(serienbriefPlaceholder("zaehlernummer"),"{{zaehlernummer}}");
  assert.equal(insertSerienbriefPlaceholder("Austausch [alt]",10,15,"zaehlerart"),"Austausch {{zaehlerart}}");
  assert.equal(insertSerienbriefPlaceholder("",0,0,"vorname"),"{{vorname}}");
});

test("keeps consecutive placeholders whole when rich text formatting split their HTML",()=>{
  assert.equal(normalizeSerienbriefPlaceholderHtml("<p>{{zae<strong>hler</strong>nummer}}{{vorname}}</p>"),"<p>{{zaehlernummer}}{{vorname}}</p>");
  assert.equal(normalizeSerienbriefPlaceholderHtml("<p>Ihr {{zae<em>hler</em>nummer}}</p>"),"<p>Ihr {{zaehlernummer}}</p>");
});
