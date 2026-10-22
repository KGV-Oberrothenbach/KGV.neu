from io import BytesIO
from pathlib import Path
from pypdf import PdfReader, PdfWriter
from reportlab.pdfgen.canvas import Canvas

template = Path("KGV.Core/Templates/pachtvertrag/v4/template.pdf")
output = Path("tmp/pdfs/pachtvertrag-v4-four-signatures-preview.pdf")
output.parent.mkdir(parents=True, exist_ok=True)
reader = PdfReader(str(template)); writer = PdfWriter()
for page in reader.pages: writer.add_page(page)
overlay = BytesIO(); canvas = Canvas(overlay, pagesize=(595.276, 841.89))
canvas.setFont("Helvetica", 8)
def text(x, top, value): canvas.drawString(x, 841.89 - top - 8, value)
def signature(x, top, label):
    canvas.setFont("Helvetica-Oblique", 16); canvas.drawString(x + 12, 841.89 - top - 22, label)
    canvas.setLineWidth(.7); canvas.line(x, 841.89 - top - 28, x + 110, 841.89 - top - 28)
text(150, 227, "Erika Beispiel"); text(315, 342, "Max Nebenbeispiel")
text(110, 613, "Zwickau, 22.10.2026")
signature(120, 650, "E. Beispiel"); signature(350, 650, "M. Nebenbeispiel")
text(155, 738, "Anna Vorstand\n1. Vorsitzende")
text(155, 783, "Bernd Vertreter\nBeisitzer\nin Vertretung für Kassenwart")
signature(355, 738, "A. Vorstand"); signature(355, 783, "B. Vertreter")
canvas.save(); overlay.seek(0)
writer.pages[2].merge_page(PdfReader(overlay).pages[0])
with output.open("wb") as stream: writer.write(stream)
