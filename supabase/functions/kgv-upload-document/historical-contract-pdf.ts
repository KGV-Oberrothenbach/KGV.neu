import { PDFDocument, StandardFonts, rgb, degrees, type PDFFont } from "npm:pdf-lib@1.17.1";

export function historicalContractStampLayout(width: number, height: number, font: PDFFont, nummer: number, memberNumbers: readonly number[]) {
  if (!Number.isInteger(nummer) || nummer < 80000 || nummer > 89999) throw new Error("Ungültige historische Vertragsnummer.");
  if (!memberNumbers.length || memberNumbers.some(n => !Number.isSafeInteger(n) || n <= 0)) throw new Error("Die wirksamen Vertragsparteien besitzen keine gültigen Mitgliedsnummern.");
  const lines = [
    `Mgl.-Nr. ${[...new Set(memberNumbers)].sort((a,b) => a-b).join(" / ")}`,
    `Vertrags-Nr. ${nummer}`,
  ];
  // pdf-lib's WinAnsi font encoder cannot measure a newline. Every API call
  // receives one line, with explicit baselines and a shared right edge.
  const unitWidth = Math.max(...lines.map(line => font.widthOfTextAtSize(line, 1)));
  const fontSize = Math.min(11, (width - 70) / unitWidth);
  if (fontSize < 6 || height < 80) throw new Error("Die PDF-Seite ist für den Vertragsstempel zu klein.");
  const textWidth = unitWidth * fontSize;
  return { lines, fontSize, x: width - textWidth - 35, firstY: height - 35, lineHeight: 13,
    rectangle: { x: width - textWidth - 42, y: height - 58, width: textWidth + 14, height: 36 } };
}

export async function stampHistoricalContractPdf(bytes: ArrayBuffer | Uint8Array, nummer: number, memberNumbers: readonly number[]): Promise<Uint8Array> {
  const pdf = await PDFDocument.load(bytes);
  const page = pdf.getPages()[0];
  if (!page) throw new Error("Das PDF enthält keine Seite.");
  const font = await pdf.embedFont(StandardFonts.HelveticaBold);
  const box = page.getCropBox();
  const angle = ((page.getRotation().angle % 360) + 360) % 360;
  const width = angle % 180 === 0 ? box.width : box.height;
  const height = angle % 180 === 0 ? box.height : box.width;
  const position = (x: number, y: number) => {
    switch (angle) {
      case 90: return { x: box.x + box.width - y, y: box.y + x };
      case 180: return { x: box.x + box.width - x, y: box.y + box.height - y };
      case 270: return { x: box.x + y, y: box.y + box.height - x };
      default: return { x: box.x + x, y: box.y + y };
    }
  };
  const layout = historicalContractStampLayout(width, height, font, nummer, memberNumbers);
  const rectangle = layout.rectangle;
  page.drawRectangle({ ...rectangle, ...position(rectangle.x, rectangle.y), rotate: degrees(angle), color: rgb(1,1,1), opacity: 0.92 });
  layout.lines.forEach((line,index) => page.drawText(line, {
    ...position(width - font.widthOfTextAtSize(line, layout.fontSize) - 35, layout.firstY - index * layout.lineHeight),
    rotate: degrees(angle), size: layout.fontSize, font, color: rgb(0.05,0.05,0.05),
  }));
  return await pdf.save();
}
