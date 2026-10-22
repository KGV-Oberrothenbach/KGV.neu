"""Local visual QA preview for the MA-v4 top-left field specification."""
from __future__ import annotations

import json
from io import BytesIO
from pathlib import Path

from pypdf import PdfReader, PdfWriter
from PIL import Image, ImageDraw
from reportlab.lib.colors import HexColor
from reportlab.lib.utils import ImageReader
from reportlab.pdfgen.canvas import Canvas

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE = ROOT / "KGV.Core/Templates/mitgliedsantrag/v4/template.pdf"
SPEC = ROOT / "KGV.Core/Templates/mitgliedsantrag/v4/field-spec.json"
OUTPUT = ROOT / "tmp/pdfs/mitgliedsantrag-v4-six-signatures-preview.pdf"


def signature(label: str, color: str) -> BytesIO:
    image = Image.new("RGBA", (480, 80), (255, 255, 255, 0)); draw = ImageDraw.Draw(image)
    draw.line([(24, 56), (150, 18), (250, 60), (440, 26)], fill=color, width=4)
    draw.text((28, 60), label, fill=color)
    output = BytesIO(); image.save(output, format="PNG"); output.seek(0); return output


def main() -> None:
    fields = {item["name"]: item for item in json.loads(SPEC.read_text(encoding="utf-8"))["fields"]}
    source = PdfReader(str(TEMPLATE)); writer = PdfWriter()
    signatures = {
        "unterschrift_antragsteller_antrag": signature("Antragsteller – Antrag", "#0b5cab"),
        "unterschrift_antragsteller_datenschutz": signature("Antragsteller – Datenschutz", "#0b5cab"),
        "unterschrift_zweite_partei_antrag": signature("Vertreter – Antrag", "#9a3412"),
        "unterschrift_zweite_partei_datenschutz": signature("Vertreter – Datenschutz", "#9a3412"),
        "unterschrift_vorstand_1": signature("Max Mustermann", "#166534"),
        "unterschrift_vorstand_2": signature("Erika Beispiel", "#7e22ce"),
    }
    texts = {
        "vereinsunterzeichner_1": ["Max Mustermann", "1. Vorsitzender"],
        "vereinsunterzeichner_2": ["Erika Beispiel", "Beisitzer", "in Vertretung für 1. Vorsitzender"],
    }
    for page_number, source_page in enumerate(source.pages, 1):
        overlay_bytes = BytesIO(); canvas = Canvas(overlay_bytes, pagesize=(float(source_page.mediabox.width), float(source_page.mediabox.height)))
        for name, stream in signatures.items():
            field = fields[name]
            if field["page"] != page_number: continue
            rect = field["rect"]; canvas.drawImage(ImageReader(stream), rect["x"], float(source_page.mediabox.height) - rect["y"] - rect["h"], width=rect["w"], height=rect["h"], preserveAspectRatio=True, anchor="c", mask="auto")
        for name, lines in texts.items():
            field = fields[name]
            if field["page"] != page_number: continue
            rect = field["rect"]; canvas.setFillColor(HexColor("#172233")); canvas.setFont("Helvetica", 7)
            for index, line in enumerate(lines): canvas.drawString(rect["x"] + 2, float(source_page.mediabox.height) - rect["y"] - 9 - index * 8, line)
        canvas.save(); overlay_bytes.seek(0); source_page.merge_page(PdfReader(overlay_bytes).pages[0]); writer.add_page(source_page)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    with OUTPUT.open("wb") as file: writer.write(file)


if __name__ == "__main__": main()
