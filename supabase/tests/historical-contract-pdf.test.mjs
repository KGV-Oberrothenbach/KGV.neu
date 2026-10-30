import assert from "node:assert/strict";
import test from "node:test";
import { PDFDocument, StandardFonts, PDFArray, decodePDFRawStream, degrees } from "pdf-lib";
import { loadStamp, edgeHarness } from "./edge-test-runtime.mjs";
const {stampHistoricalContractPdf,historicalContractStampLayout}=loadStamp();
export async function originalPdf(pages=1){const pdf=await PDFDocument.create();for(let i=0;i<pages;i++)pdf.addPage([595,842]).drawText("ORIGINAL PAGE "+(i+1));return pdf.save();}
function pageStreams(pdf,index){const ref=pdf.getPages()[index].node.get(pdf.context.obj("Contents"));const contents=pdf.context.lookup(ref);const refs=contents instanceof PDFArray?contents.asArray():[ref];return refs.map(ref=>Buffer.from(decodePDFRawStream(pdf.context.lookup(ref)).decode()).toString("latin1")).join("\n");}
const text=stream=>[...stream.matchAll(/<([0-9A-F]+)> Tj/gi)].map(match=>Buffer.from(match[1],"hex").toString("latin1")).join("\n");
for(const numbers of [[1042],[1042,1048],[2147483646,2147483647]]){
  test("valid first-page stamp, no newline crash, members "+numbers.join("/"),async()=>{
    const source=await originalPdf();const copy=source.slice();const bytes=await stampHistoricalContractPdf(source,80037,numbers);assert.deepEqual(source,copy);
    const pdf=await PDFDocument.load(bytes);assert.equal(pdf.getPageCount(),1);
    const content=text(pageStreams(pdf,0));assert.match(content,/ORIGINAL PAGE 1/);assert.ok(content.includes("Mgl.-Nr. "+numbers.join(" / ")));assert.ok(content.includes("Vertrags-Nr. 80037"));
  });
}
test("only page one changes; other pages retain their original content streams",async()=>{
  const source=await originalPdf(3);const before=await PDFDocument.load(source);const after=await PDFDocument.load(await stampHistoricalContractPdf(source,80037,[1042,1048]));
  assert.equal(after.getPageCount(),3);for(const i of [1,2])assert.equal(pageStreams(after,i),pageStreams(before,i));
  assert.ok(!text(pageStreams(after,1)).includes("Vertrags-Nr."));
});
test("both lines stay within a compact top-right rectangle, including narrow pages",async()=>{
  const pdf=await PDFDocument.create();const font=await pdf.embedFont(StandardFonts.HelveticaBold);
  for(const width of [595,250]){const layout=historicalContractStampLayout(width,842,font,80037,[2147483646,2147483647]);
    assert.ok(layout.fontSize>=6&&layout.fontSize<=11);assert.ok(layout.rectangle.x>=0);assert.ok(layout.rectangle.x+layout.rectangle.width<=width);assert.equal(layout.rectangle.y+layout.rectangle.height,820);
    layout.lines.forEach((line,index)=>{assert.doesNotMatch(line,/[\r\n]/);const x=width-font.widthOfTextAtSize(line,layout.fontSize)-35;assert.ok(x>=layout.rectangle.x);assert.ok(layout.firstY-index*layout.lineHeight>=layout.rectangle.y);});
  }
});
test("invalid PDFs and missing party numbers fail before replacing the original",async()=>{
  await assert.rejects(stampHistoricalContractPdf(new Uint8Array([1,2,3]),80037,[1042]));
  await assert.rejects(stampHistoricalContractPdf(await originalPdf(),80037,[]),/Mitgliedsnummer/);
  const h=edgeHarness({source:new Uint8Array([1,2,3])});const response=await h.request(new Request("https://synthetic.invalid",{method:"POST",headers:{Authorization:"Bearer user-token","Content-Type":"application/json"},body:JSON.stringify({action:"reserve_historical_contract_number",document_id:77})}));
  assert.equal(response.status,422);assert.equal((await response.json()).error_code,"PDF_STAMP_ERROR");assert.equal(h.calls.writes.length,0);assert.equal(h.calls.marks,0);
});
test("a valid stamped PDF is saved before its status is marked; status errors never report success",async()=>{
  const source=await originalPdf();for(const markError of [false,true]){const h=edgeHarness({source,markError});const response=await h.request(new Request("https://synthetic.invalid",{method:"POST",headers:{Authorization:"Bearer user-token","Content-Type":"application/json"},body:JSON.stringify({action:"reserve_historical_contract_number",document_id:77})}));
    assert.equal(response.status,markError?502:200);assert.equal(h.calls.writes.length,1);assert.equal(h.calls.marks,1);await PDFDocument.load(h.calls.writes[0]);
  }
});

test("actual crop-box coordinates keep both rendered text baselines in the visible top-right area",async()=>{
 const pdf=await PDFDocument.create();const page=pdf.addPage([595,842]);page.setCropBox(50,80,400,500);
 const result=await PDFDocument.load(await stampHistoricalContractPdf(await pdf.save(),80037,[1042,1048]));
 const stream=pageStreams(result,0);const positions=[...stream.matchAll(/1 0 0 1 ([-0-9.]+) ([-0-9.]+) Tm/g)].map(m=>({x:Number(m[1]),y:Number(m[2])}));
 assert.equal(positions.length,2);for(const pos of positions){assert.ok(pos.x>=50&&pos.x<450);assert.ok(pos.y>500&&pos.y<580);}assert.equal(positions[0].y-positions[1].y,13);
});

test("rotated pages keep the two baselines in the visible upper-right corner",async()=>{
 for(const angle of [90,180,270]){const pdf=await PDFDocument.create();const page=pdf.addPage([595,842]);page.setRotation(degrees(angle));const result=await PDFDocument.load(await stampHistoricalContractPdf(await pdf.save(),80037,[1042,1048]));
  const stream=pageStreams(result,0);const positions=[...stream.matchAll(/([-0-9.]+) ([-0-9.]+) ([-0-9.]+) ([-0-9.]+) ([-0-9.]+) ([-0-9.]+) Tm/g)].map(m=>({x:Number(m[5]),y:Number(m[6])}));assert.equal(positions.length,2);
  const width=angle%180===0?595:842,height=angle%180===0?842:595;
  for(const pos of positions){const point=angle===90?{x:pos.y,y:595-pos.x}:angle===180?{x:595-pos.x,y:842-pos.y}:{x:842-pos.y,y:pos.x};assert.ok(point.x>width/2&&point.x<width);assert.ok(point.y>height-60&&point.y<height);}
 }
});
